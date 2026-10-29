## Change

Let admins activate and deactivate users (`PUT /users/{id}/activate`, `PUT /users/{id}/deactivate`), block login
for deactivated users, and keep them out of new workout assignments

## Objective

Admins can create users but cannot switch an account off. When someone leaves or must be suspended, the only
option today is to leave the account fully working. Admins need a reversible deactivation that stops the user from
logging in and from being given new workouts, without deleting their data. The front must also be able to tell a
deactivated user why login fails, so login needs an error the front can tell apart from wrong credentials.

## Capabilities

- `User` gains `IsActive` and a nullable `DeactivatedAtUtc`.
- `PUT /users/{id}/deactivate` (admin only): deactivates an active user and records when.
- `PUT /users/{id}/activate` (admin only): reactivates a deactivated user and clears `DeactivatedAtUtc`.
- `POST /auth/login` rejects a deactivated user with a dedicated, machine-readable error code.
- Creating or reassigning a workout rejects a deactivated user as owner or trainer.
- `GET /users` items include `isActive` and `deactivatedAtUtc`.

## Module Classification

- Users (domain) -> mutable aggregate -> `src/WeightTracker.Domain/Users/User.cs` (new state and two actions)
- Users (application) -> mutable aggregate use cases (write) -> `src/WeightTracker.Application/UseCases/Users`
- Auth (application) -> auth/authorization support -> `src/WeightTracker.Application/UseCases/Auth/LoginUseCase.cs`
- Workouts (application) -> mutable aggregate, assignment rule only -> `src/WeightTracker.Application/Services/WorkoutAssignmentValidator.cs`
- Users (API) -> REST controller with protected routes -> `src/WeightTracker.Api/Endpoints/Users/UsersController.cs`
- Users (infra) -> existing repository and EF configuration, new columns -> `src/WeightTracker.Infra/Repositories/UserRepository.cs`
- Migrations -> new FluentMigrator migration on `users` -> `src/WeightTracker.Migrations/Migrations`

`User` is a mutable aggregate, not a catalog. Activation state changes through business actions on the aggregate.

## Canonical Template Mapping

- `User.Activate` / `User.Deactivate` -> `src/WeightTracker.Domain/Workouts/Workout.cs` (status transitions) ->
  nearest aggregate action that guards its own state transition and throws a domain exception on an invalid one
- `ActivateUserUseCase` / `DeactivateUserUseCase` -> `src/WeightTracker.Application/UseCases/Workouts/ChangeWorkoutStatusUseCase.cs`
  -> load aggregate by id, `404` when missing, call the action, save, commit
- Controller actions -> `.stpr/template/src/WeightTracker.Api/Endpoints/Workouts/WorkoutsController.cs` and the
  existing `POST /users` -> protected `AuthorizedController` action, `[RequireRole(Role.ADMIN)]`, every catch through
  `ExceptionMapper.Map`
- Login rule -> `src/WeightTracker.Application/UseCases/Auth/LoginUseCase.cs` -> extend in place
- Assignment rule -> `src/WeightTracker.Application/Services/WorkoutAssignmentValidator.cs` -> already shared by
  create and update workout, so the rule lands once
- Persistence -> `src/WeightTracker.Migrations/Migrations/202610182247_AddStatusToWorkouts.cs` -> nearest "add
  columns to an existing table" migration with a reversible `Down()`
- Tests -> `test/WeightTracker.Unit/Domain/Workouts` (status transitions), `test/WeightTracker.Unit/Application/Workouts/ChangeWorkoutStatusUseCaseTests.cs`,
  `test/WeightTracker.Integration/Users/CreateUserEndpointTests.cs` and the existing login and workout endpoint tests

## State And Mutability

- `users.is_active` (boolean, not null, default `true`) and `users.deactivated_at_utc` (timestamptz, nullable) are
  added. Existing rows and the seeded admin become active with no deactivation date.
- `Deactivate` sets `IsActive = false` and `DeactivatedAtUtc = now`. `Activate` sets `IsActive = true` and
  `DeactivatedAtUtc = null`. The invariant `IsActive == (DeactivatedAtUtc is null)` always holds.
- Workouts already assigned to a user who is later deactivated stay as they are.

## Business Rules

- A user who is already deactivated cannot be deactivated again (domain exception, `409`).
- A user who is already active cannot be activated (domain exception, `409`).
- A new user (`User.Create`, signup, `POST /users`) starts active.
- A deactivated user cannot log in. `LoginUseCase` checks activation **after** the password matches, so the
  deactivated error is only revealed to someone holding valid credentials; unknown email and wrong password keep
  answering the indistinguishable `InvalidCredentialsException`.
- The deactivated login error answers `403` with a stable `code` (proposed `USER_DEACTIVATED`) in the
  `ProblemDetails` body, distinct from the `401` of bad credentials.
- A deactivated user cannot be assigned a workout: creating a workout for them or reassigning one to them fails,
  whether they would be the owner or the trainer. The check is gated by the aggregate (for example
  `User.CanBeAssignedWorkouts()` or `User.EnsureActive()`) and enforced in `WorkoutAssignmentValidator`.
- Activation and deactivation are not profile edits; `PUT /users/me` never changes them.

## Roles And Visibility

- `PUT /users/{id}/activate` and `PUT /users/{id}/deactivate`: `ADMIN` only. `TRAINER` and `USER` get `403`;
  missing or invalid token `401`; unknown id `404`; malformed id `400`.
- The use cases also reject a non-admin caller with `UnauthorizedOperationException`, so the rule does not depend
  only on the HTTP attribute.
- `GET /users` (admin) items expose `isActive` and `deactivatedAtUtc` in addition to the current fields.

## Business Identifiers And Mutability

- `User.Id` is the technical identifier, generated by `Id.New()`, immutable. Not affected.
- `User.Email` is the unique business identifier, immutable. Not affected.

If a future change wants any listed identifier to become mutable, exact persistence semantics must be defined before coding starts.

## Out Of Scope

- Revoking access tokens already issued. They expire on their own (15 minutes).
- Deleting users, changing roles, or a deactivation reason/comment.
- Unassigning or hiding workouts of a user who is deactivated.
- Filtering `GET /users` by active state.
- Any front change. The front consumes the new code and fields in its own plan after the OpenAPI snapshot refresh.

## Notes

- Decisions to confirm at proposal time:
  - **Refresh tokens.** Without a check, a deactivated user keeps renewing sessions through `POST /auth/refresh`.
    Proposed: `RefreshTokenUseCase` rejects a deactivated user with the same `403` + `USER_DEACTIVATED`, and
    `DeactivateUserUseCase` does not need to revoke stored refresh tokens.
  - **Protected endpoints with a live access token.** Proposed: not checked per request; the token simply expires.
    Listed in Out Of Scope.
  - **Self-deactivation.** Proposed: an admin cannot deactivate themselves (`403` or `409`), to avoid locking out
    the last admin.
  - **Error code.** `USER_DEACTIVATED`, carried as a `ProblemDetails` extension member `code`. `ExceptionMapper`
    does not emit codes today; add the extension only for this exception, not a general code scheme.
  - **Assignment error status.** Reuse `InvalidWorkoutAssignmentException` (`400`) with a specific message, or add a
    dedicated exception. Proposed: reuse, as owner/trainer validity is already reported that way.
- Exceptions: domain `UserAlreadyActiveException` and `UserAlreadyDeactivatedException` -> `409`; application
  `UserDeactivatedException` -> `403` with `code`. Map all explicitly in `ExceptionMapper`.
- `User.Deactivate(DateTime nowUtc)` takes the time from `IClock` in the use case. `Rehydrate` gains `isActive`
  and `deactivatedAtUtc`; the mapper round-trips both.
- `IUserRepository.IsTrainerOrAdmin` (used by `EnsureCanTrain`) must also exclude deactivated users, or the
  validator loads the user and asks the aggregate. Prefer the aggregate.
- Response DTO: `UserProfileResponse` is shared by `GET /users/me`, `PUT /users/me` and `GET /users`. Add a
  dedicated list item DTO (for example `UserListItemResponse`) so `/me` does not change. Activate/deactivate return
  `204 No Content` unless the proposal prefers returning the list item DTO.
- Migration `AddActivationToUsers` adds both columns with `Down()` dropping them. EF configuration maps
  `is_active` and `deactivated_at_utc`.
- Register both use cases in `Program.cs`. Declare `[ProducesResponseType]` for `204`, `400`, `401`, `403`, `404`,
  `409`, and `403` on `POST /auth/login`.
- Tests to add:
  - Unit `User`: deactivate an active user sets both fields; deactivate twice throws; activate an active user
    throws; activate after deactivate clears the date; `Create` starts active.
  - Unit use cases: happy paths, `404` on unknown id, non-admin rejected without saving, already-in-state throws
    without saving.
  - Unit `LoginUseCase`: deactivated user with correct password throws `UserDeactivatedException`; deactivated user
    with wrong password still throws `InvalidCredentialsException`; no refresh token saved.
  - Unit `WorkoutAssignmentValidator` / create and update workout: deactivated owner and deactivated trainer rejected.
  - Integration `PUT /users/{id}/deactivate` and `/activate`: `401` no token, `401` invalid token, `403` trainer,
    `403` user, `404` unknown id, `400` malformed id, `409` already in state, `204` success verified through
    `GET /users`.
  - Integration `POST /auth/login`: deactivated seeded user answers `403` with `code = USER_DEACTIVATED`;
    reactivated user logs in again.
  - Integration `POST /workouts` and `PUT /workouts/{id}`: deactivated owner and deactivated trainer rejected.
  - Integration `GET /users`: items carry `isActive`/`deactivatedAtUtc` for both an active and a deactivated user.
  - `ExceptionMapper` tests for the three new exceptions, including the `code` extension.
  - OpenAPI document test for the two new routes and the new login response.
- OpenAPI snapshot changes; the front refreshes it in its own plan.
- No deviations from the mapped template references are allowed.
