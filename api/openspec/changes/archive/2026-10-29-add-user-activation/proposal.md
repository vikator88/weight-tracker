# Proposal

## Why

Administrators can create accounts but cannot switch one off. When someone leaves or must be suspended, the account
keeps working: it can log in, renew sessions and receive workouts. Admins need a reversible deactivation that keeps
the data. The front also needs to tell a deactivated user why login fails, so that error must be distinguishable
from wrong credentials.

Source brief: `.stpr/plan/openspec/proposal-change-add-user-activation.md`.

## What Changes

- `User` gains `IsActive` and a nullable `DeactivatedAtUtc`. Every existing and new user starts active.
- `PUT /users/{id}/deactivate` (admin only) deactivates an active user and records when. Deactivating an already
  deactivated user is a `409`. An admin cannot deactivate themselves (`403`).
- `PUT /users/{id}/activate` (admin only) reactivates a deactivated user and clears the date. Activating an
  already active user is a `409`.
- Both return `200 OK` with the same user item that `GET /users` lists.
- `POST /auth/login` rejects a deactivated user with valid credentials with `403 Forbidden` and
  `code: "USER_DEACTIVATED"` in the problem body. Unknown email and wrong password keep their `401`.
- `POST /auth/refresh` rejects a valid refresh token of a deactivated user with the same `403` and code.
- Creating a workout, or reassigning one, with a deactivated owner or trainer is rejected with `400`. Workouts
  already assigned stay untouched.
- **BREAKING (contract)**: `GET /users` items gain `isActive` and `deactivatedAtUtc`. Additive for JSON clients,
  but the requirement "exactly these fields" changes.

## Capabilities

### New Capabilities

None.

### Modified Capabilities

- `user-administration`: adds activation and deactivation by admins, the effect of deactivation on login and
  refresh, and the two new fields in listed users.
- `workout-creation`: the owner and the trainer of a new workout must be active.
- `workout-details`: a newly assigned owner or trainer must be active when an admin reassigns a workout.

## Canonical Template Mapping

STPR requires each capability to name its reference. All references follow `.stpr/template/README.md`.

- Activation state and actions on `User` -> `src/WeightTracker.Domain/Workouts/Workout.cs` (status transitions) ->
  nearest aggregate action that guards its own transition and throws a domain exception
- `ActivateUserUseCase` / `DeactivateUserUseCase` -> `src/WeightTracker.Application/UseCases/Workouts/ChangeWorkoutStatusUseCase.cs`
  -> load by id, `404` if missing, call the action, save, commit
- Endpoints -> `.stpr/template/src/WeightTracker.Api/Endpoints/Workouts/WorkoutsController.cs` and the existing
  `POST /users` -> `AuthorizedController`, `[RequireRole(Role.ADMIN)]`, catches through `ExceptionMapper.Map`
- Login and refresh rule -> `src/WeightTracker.Application/UseCases/Auth/LoginUseCase.cs` and
  `RefreshTokenUseCase.cs` -> extended in place
- Assignment rule -> `src/WeightTracker.Application/Services/WorkoutAssignmentValidator.cs` -> already shared by
  create and update workout
- Persistence -> `src/WeightTracker.Migrations/Migrations/202610182247_AddStatusToWorkouts.cs` -> nearest
  "add columns to an existing table" migration

`User` is a mutable aggregate, not a catalog. No conflict with the template or invariants was found.

## Business Identifiers And Mutability

- `User.Id`: technical identifier from `Id.New()`, immutable. Not affected.
- `User.Email`: unique business identifier, immutable. Not affected.

## Impact

- Domain: `User` (state, `Activate`, `Deactivate`, `Rehydrate`), two new domain exceptions.
- Application: two new use cases, `LoginUseCase`, `RefreshTokenUseCase`, `WorkoutAssignmentValidator`, a new
  `UserDeactivatedException`, possibly a new `IUserRepository` lookup used by the trainer check.
- API: `UsersController` (two routes), a list item DTO replacing `UserProfileResponse` in `GET /users`,
  `ExceptionMapper` (new mappings and a `code` extension), `AuthController` response metadata, `Program.cs`.
- Infra: `UserEntity`, EF configuration, `UserMapper`, new FluentMigrator migration on `Users`.
- OpenAPI document changes; the front refreshes its snapshot and handles `USER_DEACTIVATED` in its own plan.
- Out of scope: revoking live access tokens (they expire in 15 minutes), deleting users, role changes, a
  deactivation reason, filtering `GET /users` by state, and any front change.
