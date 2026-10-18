# Tasks

## 1. Application: authenticated caller lookup

- [x] 1.1 Add `AuthenticatedUserNotFoundException : ApplicationLayerException` in `WeightTracker.Application/Exceptions` with a generic message that does not reveal the user was removed; verify `dotnet build WeightTracker.slnx` passes
- [x] 1.2 Add the `AuthenticatedUser` sealed record (`Id`, `Email`, `Name`, `Surname`, `Role`, no password hash) in `WeightTracker.Application/UseCases/Auth`, documented like `AuthResult`; verify the build passes
- [x] 1.3 Add `GetAuthenticatedUserUseCase` in `WeightTracker.Application/UseCases/Auth`: `Execute(Id userId, CancellationToken)` loads through `IUserRepository.GetById`, throws `AuthenticatedUserNotFoundException` when missing, and returns `AuthenticatedUser`; verify the build passes
- [x] 1.4 Add `test/WeightTracker.Unit/Application/Auth/GetAuthenticatedUserUseCaseTests.cs` mirroring `LoginUseCaseTests` (mocked `IUserRepository`, labelled Arrange/Act/Assert, one Act): returns the mapped user when it exists, throws `AuthenticatedUserNotFoundException` when it does not; verify `dotnet test test/WeightTracker.Unit` passes
- [x] 1.5 Review the use case, record and exception against `LoginUseCase`, `AuthResult` and an existing Application exception for naming, folder and doc-comment style

## 2. Api: base controller and error mapping

- [x] 2.1 Map `AuthenticatedUserNotFoundException` to `401` in `ExceptionMapper`, in the same arm as `InvalidCredentialsException`, and add the case to `test/WeightTracker.Integration/Common/ExceptionMapperTests.cs` following its existing cases; verify that test passes
- [x] 2.2 Register `GetAuthenticatedUserUseCase` in `Program.cs` next to the other Auth use cases; verify the build passes
- [x] 2.3 Add abstract `AuthorizedController : ControllerBase, IAsyncActionFilter` at `WeightTracker.Api/Endpoints/AuthorizedController.cs`, `[Authorize]` at class level, with the protected constructor, `protected ExceptionMapper ExceptionMapper`, `protected AuthenticatedUser? CurrentUser`, and `OnActionExecutionAsync` as described in design.md (skip on `IAllowAnonymous`, `401` on unusable `sub`, mapped exception on lookup failure); verify the build passes with warnings as errors

## 3. Integration test support

- [x] 3.1 Add `RemoveUser(string email)` to `test/WeightTracker.Integration/Bootstrap/ApiFactory.cs` next to `ResetDatabase`: deletes the workouts where the user is owner or trainer, then the user; copy the change byte-for-byte to `.stpr/template/test/WeightTracker.Integration/Bootstrap/ApiFactory.cs`; verify the build passes and the parity check from `AGENTS.md` reports no drift for `ApiFactory.cs`

## 4. Workouts controller inherits the base

- [x] 4.1 Make `WorkoutsController` inherit `AuthorizedController`: take `GetAuthenticatedUserUseCase` and pass it with `ExceptionMapper` to `base(...)`, drop its `_exceptionMapper` field and its class-level `[Authorize]`, delete `TryReadCaller` and the per-action `Unauthorized()` checks, and pass `CurrentUser!.Id` and `CurrentUser!.Role` to the use cases; verify the existing Workouts integration tests (`WorkoutScopingTests`, `WorkoutCreateEndpointTests`, `SuperSeriesCreateEndpointTests`, `SuperSeriesBreakEndpointTests`, `WorkoutResponseShapeTests`) still pass unchanged
- [x] 4.2 Add the removed-user case to each Workouts route test class, following each class's existing `401` cases: `GET /workouts` (`WorkoutScopingTests`), `POST /workouts` (`WorkoutCreateEndpointTests`), `POST /workouts/{workoutId}/superseries` (`SuperSeriesCreateEndpointTests`), `DELETE /workouts/{workoutId}/superseries/{superSeriesId}` (`SuperSeriesBreakEndpointTests`). Each logs a seeded `USER` in through `AuthenticatedClientFor`, calls `Factory.RemoveUser`, calls the route, and asserts `401` and that nothing was created or changed; verify these tests pass
- [x] 4.3 Review `WorkoutsController` against the mapped `WorkoutsController` reference row and `AuthorizedController`: every catch delegates to `ExceptionMapper.Map(exception)`, no claim parsing is left, `[ProducesResponseType]` attributes are unchanged

## 5. Exercises controller inherits the base

- [x] 5.1 Make `ExercisesController` inherit `AuthorizedController` the same way (constructor passes the use case and `ExceptionMapper` to `base(...)`, `_exceptionMapper` field removed), keeping `[AllowAnonymous]` on `GetAll` and `GetById` and the admin restriction on the writes (now `[RequireRole(Role.ADMIN)]`, see 8.1); verify the existing Exercises integration tests (`ExerciseReadEndpointsTests`, `ExerciseCreateEndpointTests`, `ExerciseUpdateEndpointTests`, `ExerciseDeleteEndpointTests`) still pass unchanged, including anonymous reads
- [x] 5.2 Add the removed-admin case to `ExerciseCreateEndpointTests`, `ExerciseUpdateEndpointTests` and `ExerciseDeleteEndpointTests`, following their existing `401` cases: log the seeded admin in, call `Factory.RemoveUser`, call the route, assert `401` and that no exercise was created, changed or deleted; verify these tests pass
- [x] 5.3 Review `ExercisesController` against `WorkoutsController` and `AuthorizedController`, and confirm `AuthController` still derives from `ControllerBase` and its `AuthEndpointsTests` pass unchanged

## 6. Template and invariants

- [x] 6.1 Update `.stpr/template/README.md`: add an `AuthorizedController` row to the Canonical Items Classification and Reference Origin (skeleton-backed, `src/WeightTracker.Api/Endpoints/AuthorizedController.cs`), change the `WorkoutsController` description from "claims to use case" to "inherits `AuthorizedController`, `CurrentUser` to use case", and add a quick-selection rule that controllers with protected routes inherit `AuthorizedController`; verify by reading the updated rows
- [x] 6.2 Add to `STPR_INVARIANTS.md` §4 API Layer: controllers with protected routes inherit `AuthorizedController` and read the caller from `CurrentUser`, never from claims; verify the rule reads consistently with the existing API Layer bullets

## 7. Full verification

- [x] 7.1 Run `dotnet build WeightTracker.slnx` and `dotnet test WeightTracker.slnx` with Docker running; verify both pass with no skipped integration tests
- [x] 7.2 Run the template parity check from `AGENTS.md`; verify it prints no `DRIFT` lines
- [x] 7.3 Walk the `AGENTS.md` quality checklist for this change (use cases registered, catches delegate to `ExceptionMapper`, new exception mapped, per-route `401` matrix intact, Arrange/Act/Assert labels) and record any item that does not apply

## 8. 401 before 403 on role-gated routes

- [x] 8.1 Add `RequireRoleAttribute` in `WeightTracker.Api/Endpoints`, have `AuthorizedController` check it against `CurrentUser.Role` after the lookup (`403` when not listed), and replace `[Authorize(Roles = ADMIN)]` with `[RequireRole(Role.ADMIN)]` on the Exercises write actions; verify the existing `403` tests for `USER` and `TRAINER` on the three write routes still pass
- [x] 8.2 Add to `ExerciseCreateEndpointTests`, `ExerciseUpdateEndpointTests` and `ExerciseDeleteEndpointTests` a case where a removed `TRAINER` gets `401` and not `403`; verify these tests pass
- [x] 8.3 Add the `RequireRole` rule to `STPR_INVARIANTS.md` §4 API Layer; verify it reads consistently with the `AuthorizedController` bullet
