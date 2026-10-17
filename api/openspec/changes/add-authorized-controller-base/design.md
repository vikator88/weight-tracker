# Design

## Context

See proposal.md for motivation. Current state that shapes the approach:

- `WorkoutsController` is `[Authorize]` at class level and calls a private `TryReadCaller` at the top of every action. It reads `sub` as an `Id` and `ClaimTypes.Role` as a `Role`, and returns `Unauthorized()` if either is unusable.
- `ExercisesController` has no class-level `[Authorize]`. Its reads are `[AllowAnonymous]` and its writes are `[Authorize(Roles = ADMIN)]`, enforced by the authorization middleware before any caller lookup. It never reads the caller.
- `IUserRepository.GetById(Id, CancellationToken)` already exists. `UserNotFoundException` exists but maps to `404`.
- `ExceptionMapper` is injected into every controller and is the only place that picks status codes.
- In ASP.NET Core, authorization filters run before action filters. So `[Authorize]` rejects missing and invalid tokens before any caller lookup runs. `[Authorize(Roles = ...)]` is enforced in the same early step, which would answer `403` for a removed user, so it cannot be used for role-gated routes of an `AuthorizedController`.

## Goals / Non-Goals

**Goals:**
- One place in the API that turns a validated token into a known, existing caller.
- Controllers stay free of repositories and claim parsing.

**Non-Goals:**
- Changing which roles may call which route.
- Changing any use case signature or any existing status code.

## Decisions

### Layer mapping

| Layer | Change |
| --- | --- |
| Api | New `Endpoints/AuthorizedController.cs`. `WorkoutsController` and `ExercisesController` inherit it. `ExceptionMapper` gains one `401` mapping. `Program.cs` registers the new use case. |
| Application | New `UseCases/Auth/GetAuthenticatedUserUseCase.cs`, `UseCases/Auth/AuthenticatedUser.cs`, `Exceptions/AuthenticatedUserNotFoundException.cs`. |
| Domain | None. |
| Infrastructure | None. The existing `UserRepository.GetById` is reused. |

Cross-layer dependencies: Api → Application (`GetAuthenticatedUserUseCase`, `AuthenticatedUser`, the new exception). Application → its own `IUserRepository` port. No new dependency points inward-to-outward.

### Base controller runs as an action filter

`AuthorizedController : ControllerBase, IAsyncActionFilter` is abstract and `[Authorize]` at class level. MVC runs controllers that implement `IAsyncActionFilter` as an action filter, so `OnActionExecutionAsync` runs after authorization, before model binding and the action body. The method is marked `[NonAction]`, because otherwise MVC discovers it as a route.

In `OnActionExecutionAsync`:
1. If the endpoint has `IAllowAnonymous` metadata, call `next()` and leave `CurrentUser` `null`.
2. Read `sub` with `Id.TryFrom`. If it is unusable, set `context.Result = Unauthorized()` and return. This keeps today's behavior for a token without a usable subject.
3. Call `GetAuthenticatedUserUseCase.Execute(callerId, HttpContext.RequestAborted)`. On success set `CurrentUser` and call `next()`.
4. Catch any exception and set `context.Result = ExceptionMapper.Map(exception)`. The missing-user case becomes `401` through the mapping. Anything else becomes `500`, as in actions.

Alternatives considered:
- *A global `IAsyncAuthorizationFilter` or middleware.* Rejected: it hides the dependency from the controller, needs service location to reach the use case, and the source plan asks for inheritance.
- *A `ResolveCaller()` helper called at the top of each action.* Rejected: it keeps the per-action boilerplate that this change removes, and an action can forget to call it.
- *Inheriting `Controller` to override `OnActionExecutionAsync`.* Rejected: it pulls in view support that an API does not use. Implementing `IAsyncActionFilter` on a `ControllerBase` subclass gives the same hook.

### Dependencies come through the constructor

The base constructor is `protected AuthorizedController(GetAuthenticatedUserUseCase getAuthenticatedUser, ExceptionMapper exceptionMapper)`. It stores the mapper as `protected ExceptionMapper ExceptionMapper { get; }`, so derived actions keep writing `ExceptionMapper.Map(exception)` and stop holding their own `_exceptionMapper` field. Derived constructors take both and pass them to `base(...)`. No service locator.

### `CurrentUser` is an Application record, not the aggregate

`protected AuthenticatedUser? CurrentUser { get; private set; }`. `AuthenticatedUser` is a `sealed record` in `UseCases/Auth`, next to `AuthResult`, with `Id`, `Email`, `Name`, `Surname` and `Role`. It never carries `PasswordHash`. It is used inside the controller only and is never serialized.

It is nullable because anonymous actions leave it unset, as the source plan asks. In protected actions the filter guarantees it is set, so actions read it with `CurrentUser!`.

`ControllerBase.User` (the `ClaimsPrincipal`) is not hidden.

### Use case and exception

`GetAuthenticatedUserUseCase.Execute(Id userId, CancellationToken)` calls `IUserRepository.GetById`. If the result is `null` it throws `AuthenticatedUserNotFoundException`. Otherwise it maps the aggregate to `AuthenticatedUser`. It is read-only and does not touch `IUnitOfWork`.

`AuthenticatedUserNotFoundException : ApplicationLayerException` has a generic message ("Authentication is required") that does not say the user was removed. `ExceptionMapper` adds it to the `401` arm next to `InvalidCredentialsException`. `UserNotFoundException` keeps its `404` meaning for users named in a request.

### Role passed to use cases

`WorkoutsController` passes `CurrentUser!.Id` and `CurrentUser!.Role`. The role comes from the stored user, not the token.

### Role requirements run after the lookup

A new `RequireRoleAttribute(params Role[] roles)` in `Endpoints/` replaces `[Authorize(Roles = ...)]` on role-gated actions. `AuthorizedController.OnActionExecutionAsync` reads it from the endpoint metadata after `CurrentUser` is set and sets `context.Result = StatusCode(403)` when the stored role is not listed. The order is therefore: no token or invalid token `401` (authorization middleware, class-level `[Authorize]`), token of a removed user `401` (lookup), role not allowed `403` (filter). The `403` has the same empty body as before.

Alternatives considered:
- *Keep `[Authorize(Roles)]`.* Rejected: the middleware runs it before the lookup, so a removed non-admin gets `403`.
- *Check user existence in `JwtBearerEvents.OnTokenValidated`.* Rejected: it moves the lookup out of the base controller the source plan asks for, and needs service location in the authentication pipeline.
- *A custom `IAuthorizationHandler` that looks the user up.* Rejected: the handler would need the use case through `HttpContext.RequestServices`, and the result would be `403`, not `401`, for a missing user.

### Test support for a removed user

Tests must authenticate through the API, and no endpoint deletes users. A test logs in through `AuthenticatedClientFor(email)` first, then removes the user. Direct `DbContext` access is allowed only in the integration bootstrap, so a `RemoveUser(string email)` method is added to `ApiFactory`, next to `ResetDatabase`. It deletes the workouts where the user is owner or trainer, then the user. Workout exercises, sets and refresh tokens follow through the existing cascades. The next test's truncate-and-reseed restores the baseline.

`ApiFactory` has a template copy, so `.stpr/template/test/WeightTracker.Integration/Bootstrap/ApiFactory.cs` gets the same change to keep parity.

### Template and invariants

- `.stpr/template/README.md`: add an `AuthorizedController` row (base controller, resolves the caller through a use case) and change the `WorkoutsController` row from "claims to use case" to "inherits `AuthorizedController`, `CurrentUser` to use case". Add a quick-selection rule: a controller with protected routes inherits `AuthorizedController`.
- `STPR_INVARIANTS.md` §4 API Layer: add "Controllers with protected routes inherit `AuthorizedController` and read the caller from `CurrentUser`. They never parse claims."

## Risks / Trade-offs

- [One extra database read per protected request] → It is a primary-key lookup. Acceptable at this scale. Caching is out of scope.
- [A new role-gated action uses `[Authorize(Roles = ...)]` out of habit and gets `403` before `401` for removed users] → The invariants require `[RequireRole]` on `AuthorizedController` actions, and the Exercises tests cover the order on each write route.
- [A derived controller forgets `[AllowAnonymous]` on a public action and now requires a token] → Exercises public reads keep their attribute, and their existing anonymous tests catch a regression.
- [Changing `ApiFactory` changes a template file] → The template copy is updated in the same task and the parity check runs before closing.

## Migration Plan

No schema or data migration. Deploys as a normal release. Rollback is a code revert. The only observable change is `401` for tokens of removed users.
