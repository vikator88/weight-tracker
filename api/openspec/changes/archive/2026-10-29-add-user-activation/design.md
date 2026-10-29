# Design

## Context

See `proposal.md` for motivation and `specs/` for the required behavior. Current state that shapes the approach:

- `User` (`src/WeightTracker.Domain/Users/User.cs`) has no lifecycle state. Its only action is `UpdateProfile`, and
  `CanTrain()` already lets the aggregate answer an assignment question.
- `LoginUseCase` throws one `InvalidCredentialsException` (`401`) for a malformed email, an unknown email or a wrong
  password, so the endpoint cannot be used to enumerate accounts.
- `RefreshTokenUseCase` loads the user after validating the token and before rotating it.
- `WorkoutAssignmentValidator` is shared by `CreateWorkoutUseCase` and `UpdateWorkoutUseCase`.
  `EnsureOwnerExists` loads the owner; `EnsureCanTrain` asks the repository `IsTrainerOrAdmin`, a query that keeps
  the "can train" rule outside the aggregate.
- `ExceptionMapper` produces `ProblemDetails` with `status` and `detail` only; no error codes exist today.
- `UserProfileResponse` is shared by `GET /users/me`, `PUT /users/me` and `GET /users`.
- `Users` columns are PascalCase (`CreatedAt`, `PasswordHash`). The nearest column-adding migration is
  `202610182247_AddStatusToWorkouts`, which backfills with a default and then drops it.

## Goals / Non-Goals

**Goals:**

- Keep both activation rules (no double activation, no double deactivation) inside the aggregate.
- Make the login/refresh rejection machine-readable without introducing an error-code scheme for every exception.
- Put the "can be assigned a workout" rule on the aggregate and keep one enforcement point for create and update.

**Non-Goals:**

- Checking activation on every authenticated request. Live access tokens expire on their own (15 minutes).
- Revoking stored refresh tokens on deactivation. The refresh rejection makes that unnecessary.

## Decisions

### Layer mapping

| Layer | Change |
| --- | --- |
| Domain | `User.IsActive`, `User.DeactivatedAtUtc`, `Activate()`, `Deactivate(DateTime nowUtc)`, `CanBeAssignedWorkouts()`; `UserAlreadyActiveException`, `UserAlreadyDeactivatedException`; `Create` starts active; `Rehydrate` takes both fields |
| Application | `ActivateUserUseCase`, `DeactivateUserUseCase` in `UseCases/Users`; `UserDeactivatedException`; `LoginUseCase` and `RefreshTokenUseCase` check `IsActive`; `WorkoutAssignmentValidator` asks the aggregate |
| Api | Two `PUT` actions on `UsersController`; new `UserSummaryResponse`; `ExceptionMapper` mappings and `code` extension; response metadata on login and refresh |
| Infra | `UserEntity` and EF configuration columns; `UserMapper` round-trip; FluentMigrator migration on `Users` |

Cross-module dependency: Workouts (application) depends on the new Users aggregate method through
`WorkoutAssignmentValidator`. Auth (application) depends on `User.IsActive`. No new dependency direction is added.

### 1. Activation state on the aggregate

`IsActive` plus nullable `DeactivatedAtUtc`, always consistent (`IsActive == DeactivatedAtUtc is null`).
`Deactivate(nowUtc)` throws `UserAlreadyDeactivatedException` when inactive; `Activate()` throws
`UserAlreadyActiveException` when active. Both are domain exceptions mapped explicitly to `409`, following how
`InvalidWorkoutStatusTransitionException` guards `Workout` transitions. The time comes from `IClock` in the use
case, as other timestamps do.

*Alternative:* only `DeactivatedAtUtc`, deriving `IsActive`. Rejected because the brief asks for both and the
explicit flag reads better in queries and the API; the invariant is enforced by having only the two actions set them.

### 2. Use cases and authorization

Both use cases take `callerId`, `callerRole` and the target id, mirroring `ChangeWorkoutStatusUseCase`.
Order: non-admin -> `UnauthorizedOperationException` (`403`); for deactivate, target equal to caller ->
`UnauthorizedOperationException` (`403`) before any lookup; unknown target -> `UserNotFoundException` (`404`);
then the aggregate action (`409` on repeat), save, commit. Both return the `User`, which the controller maps to
`UserSummaryResponse` with `200 OK`. The routes also carry `[RequireRole(Role.ADMIN)]`, as `POST /users` does.

### 3. Login and refresh

`LoginUseCase` checks `IsActive` **after** the password matches and before any token is generated, throwing
`UserDeactivatedException`. Unknown email and wrong password keep the indistinguishable `401`, so the account state
is only revealed to someone who holds valid credentials.

`RefreshTokenUseCase` checks `IsActive` after loading the user and before rotation, so nothing is saved and the
presented token is not revoked. If the user is reactivated, that refresh token works again while it is still valid.

### 4. Error code

`UserDeactivatedException` maps to `403` with `ProblemDetails.Extensions["code"] = "USER_DEACTIVATED"`. The code is
a constant on the exception so the mapper and tests share it. `ExceptionMapper` gets a `Problem` overload that
accepts an optional code; every other mapping is unchanged.

*Alternative:* a dedicated status (for example `423 Locked`). Rejected: the front would still need to branch on
something, and a stable code is more explicit than an unusual status. *Alternative:* codes for every exception.
Rejected as out of scope.

### 5. Assignment rule

`User.CanBeAssignedWorkouts()` returns `IsActive`. `WorkoutAssignmentValidator` gains
`EnsureCanBeAssigned(User user)` that throws `InvalidWorkoutAssignmentException` (`400`) with an "is deactivated"
message. `EnsureCanTrain` stops using `IsTrainerOrAdmin`: it loads the user with `GetById` and checks
`CanTrain()` and `CanBeAssignedWorkouts()`, keeping both rules on the aggregate. `IsTrainerOrAdmin` is then
unused and is removed from `IUserRepository`, `UserRepository` and their tests.

Order in `CreateWorkoutUseCase` stays: role `400` checks -> owner exists (`404`) -> trainer-targets-non-`USER`
(`403`) -> owner active (`400`) -> trainer can train and is active (`400`). `UpdateWorkoutUseCase` checks only the
owner or trainer that changes, so an unchanged deactivated owner does not block an edit.

*Alternative:* keep a repository query extended with `IsActive`. Rejected because `AGENTS.md` asks aggregates to
gate domain rules.

### 6. Response DTO

New `UserSummaryResponse(Id, Email, Name, Surname, Role, IsActive, DeactivatedAtUtc)` with `From(User)`, used by
`GET /users` and both new routes. `UserProfileResponse` stays for `/users/me` so the profile contract does not change.

### 7. Persistence

Migration `AddActivationToUsers` adds `IsActive` (`bool`, not null, default `true` to backfill existing rows
including the admin inserted by `InsertAdminUser`, then the default is dropped as in `AddStatusToWorkouts`) and
`DeactivatedAtUtc` (`timestamptz`, nullable). `Down()` drops both columns. EF configuration maps both;
`UserMapper` round-trips them. No EF Core migration is generated.

## Risks / Trade-offs

- [A deactivated user keeps a live access token for up to 15 minutes] -> Accepted and listed as out of scope;
  refresh is blocked, so the session cannot be extended.
- [A deactivated `USER` with a live token can still create a workout for themselves, since self-creation does not
  go through the assignment validator] -> Accepted within the same 15-minute window.
- [`403` with `USER_DEACTIVATED` tells the caller the credentials are right] -> Only reachable with the real
  password; the brief requires the front to show this state.
- [`GET /users` item shape changes] -> Additive JSON fields; the OpenAPI snapshot changes and the front updates its
  contract in its own plan.

## Migration Plan

Deploy the migration with the API. Existing rows become active, so behavior is unchanged until an admin deactivates
someone. Rollback: `Down()` drops both columns; any deactivation is lost and every user is effectively active again.
