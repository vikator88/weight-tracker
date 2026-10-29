# Design

## Context

See proposal.md for motivation and specs/user-administration/spec.md for the behaviour. Relevant current state:

- `GetAllWorkoutsUseCase` already lists by role: it receives the caller's `Id` and `Role` and switches on the role,
  throwing `UnauthorizedOperationException` for any role it does not serve.
- `IUserRepository` has `GetById`, `GetByEmail`, `ExistsByEmail`, `IsTrainerOrAdmin` and `Save`; no list method.
- `UsersController` inherits `AuthorizedController`; `POST /users` already uses `[RequireRole(Role.ADMIN)]`.
- `ExceptionMapper` maps custom exceptions to `400`, `401`, `403`, `404` and `409`. Unmapped exceptions, including
  the framework `NotImplementedException`, become `500`.
- `UserProfileResponse(Id, Email, Name, Surname, Role)` has a `From(User)` factory.

## Goals / Non-Goals

**Goals:**
- One use case, one repository read and one action, built on the existing role-scoped list pattern.
- A trainer branch that is explicit and distinguishable from a server fault.

**Non-Goals:**
- Any trainer–athlete relationship, even a provisional one.
- Pagination, filtering or search.

## Decisions

**Layer mapping**
- Domain: no change.
- Application: `GetAllUsersUseCase` in `UseCases/Users` with `Execute(Id callerId, Role callerRole, CancellationToken)`
  returning `IEnumerable<User>`. New `TrainerAthletesNotImplementedException` in `Application/Exceptions`.
  `IUserRepository.GetAll(CancellationToken)`.
- Infrastructure: `UserRepository.GetAll` reads all user rows ordered by `Email`, maps each through the existing user
  mapper, no tracking.
- Api: `UsersController.GetAll` (`[HttpGet]`, `[RequireRole(Role.ADMIN, Role.TRAINER)]`) returning
  `Ok(users.Select(UserProfileResponse.From).ToList())`. `ExceptionMapper` maps the new exception to `501`.
  Registration in `Program.cs`.

**Role switch in the use case, as `GetAllWorkoutsUseCase` does.** `ADMIN` -> `GetAll`; `TRAINER` -> throw
`TrainerAthletesNotImplementedException`; any other role -> `UnauthorizedOperationException`. The `[RequireRole]`
attribute already stops `USER` with `403` before the use case runs; the use case check keeps the rule out of the
HTTP layer alone. The trainer branch throws before any repository call. `callerId` is unused today and kept so the
trainer branch can scope by it later without changing the signature.
Alternative: `[RequireRole(Role.ADMIN)]` only, giving trainers `403`. Rejected: the request asks for trainers to be
a declared, not-yet-implemented case, which a `403` would hide.

**`501` via a dedicated application exception, not `NotImplementedException`.** The framework exception falls into
`ExceptionMapper`'s `500` branch and is logged as an unexpected fault. A named exception mapped explicitly to `501`
tells clients the route exists and the case is deliberately pending, and satisfies the rule that every custom
exception has an explicit mapping. The name is specific to this gap so it is removed with it, rather than a generic
"feature not implemented" type that would invite reuse.

**Order by email in the repository.** Email is unique, so the order is total and deterministic for tests and
clients. Ordering in SQL avoids loading then sorting in memory.

**Reuse `UserProfileResponse`.** It already has exactly the fields the spec allows. A list-specific DTO would
duplicate it with no difference.

## Risks / Trade-offs

- [The list is unbounded] -> Accepted while the user base is small; pagination is a separate change.
- [`501` is unusual for clients] -> Documented in the OpenAPI responses; replaced when the trainer relationship lands.
- [`callerId` is unused] -> Kept deliberately for the trainer branch; documented in the use case.

## Migration Plan

None. No schema change. Deploy and roll back as a normal API release.
