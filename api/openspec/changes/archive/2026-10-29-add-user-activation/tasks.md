# Tasks

## 1. Domain: activation state on `User`

- [x] 1.1 Add `UserAlreadyActiveException` and `UserAlreadyDeactivatedException` in `src/WeightTracker.Domain/Exceptions`, following the existing domain exception shape; verify `dotnet build WeightTracker.slnx` succeeds
- [x] 1.2 Add `IsActive`, `DeactivatedAtUtc`, `Activate()`, `Deactivate(DateTime nowUtc)` and `CanBeAssignedWorkouts()` to `User`; `Create` starts active and `Rehydrate` takes both fields; verify the build succeeds
- [x] 1.3 Extend `test/WeightTracker.Unit/Domain/Users/UserTests.cs`: `Create` starts active; deactivate sets both fields; deactivate twice throws and keeps the date; activate an active user throws; activate after deactivate clears the date; `CanBeAssignedWorkouts` follows `IsActive`; verify the tests pass

## 2. Persistence

- [x] 2.1 Add FluentMigrator migration `AddActivationToUsers` (`IsActive` bool not null, backfilled with `true` then default dropped; `DeactivatedAtUtc` timestamptz nullable) with a `Down()` dropping both, mirroring `202610182247_AddStatusToWorkouts`; verify the migration tests in `test/WeightTracker.Integration/Migrations` pass up and down
- [x] 2.2 Map both columns in `UserEntity` and the `Users` EF configuration in `WeightTrackerDbContext`; verify the build succeeds and no EF Core migration was generated
- [x] 2.3 Round-trip both fields in `UserMapper` and extend `test/WeightTracker.Unit/Infra/Mappers/AggregateMapperTests.cs` for an active and a deactivated user; verify the tests pass

## 3. Activate and deactivate use cases

- [x] 3.1 Add `DeactivateUserUseCase` in `Application/UseCases/Users` mirroring `ChangeWorkoutStatusUseCase` (non-admin `403`, self `403` before lookup, unknown `404`, aggregate action with `IClock`, save, commit, return `User`); verify the build succeeds
- [x] 3.2 Add `ActivateUserUseCase` with the same shape (non-admin `403`, unknown `404`, aggregate action, save, commit, return `User`); verify the build succeeds
- [x] 3.3 Add `DeactivateUserUseCaseTests` and `ActivateUserUseCaseTests` in `test/WeightTracker.Unit/Application/Users` with mocked ports: happy path saves and commits; non-admin, self-deactivation, unknown user and already-in-state throw without saving; verify the tests pass
- [x] 3.4 Register both use cases in `Program.cs`; verify the API starts in the integration fixture (any integration test run passes)

## 4. API: routes, DTO and error mapping

- [x] 4.1 Add `UserSummaryResponse` with `From(User)` in `Endpoints/Users/Dtos` and switch `GET /users` to it, leaving `UserProfileResponse` for `/users/me`; verify the build succeeds
- [x] 4.2 Add `PUT /users/{id}/activate` and `PUT /users/{id}/deactivate` to `UsersController` with `[RequireRole(Role.ADMIN)]`, `200` with `UserSummaryResponse`, catches through `ExceptionMapper.Map`, and `{id:guid}` and `[ProducesResponseType]` for `200`, `401`, `403`, `404`, `409`; verify the build succeeds
- [x] 4.3 Map `UserAlreadyActiveException` and `UserAlreadyDeactivatedException` to `409` in `ExceptionMapper` and extend `test/WeightTracker.Integration/Common/ExceptionMapperTests.cs`; verify the tests pass
- [x] 4.4 Add integration tests for both routes in `test/WeightTracker.Integration/Users`, replicating `CreateUserEndpointTests.cs`: `401` no token, `401` invalid token, `403` `USER`, `403` `TRAINER`, `404` unknown id, `404` malformed id (route not matched), `409` already in state, `403` admin self-deactivation, `200` body shape and state confirmed through `GET /users`; verify the tests pass
- [x] 4.5 Update `ListUsersEndpointTests.cs` for the new item shape (exactly seven fields), all seeded users active, and a deactivated user listed with `isActive` `false`; verify the tests pass
- [x] 4.6 Update `test/WeightTracker.Integration/OpenApi/OpenApiDocumentTests.cs` so both routes are secured and declare their responses, and `GET /users` items carry the new fields; verify the tests pass
- [x] 4.7 Review the user-administration activation work against its references (`ChangeWorkoutStatusUseCase`, `WorkoutsController`, existing `POST /users`) and `.stpr/template/STPR_INVARIANTS.md`; record any deviation or confirm none

## 5. Login and refresh block deactivated users

- [x] 5.1 Add `UserDeactivatedException` in `Application/Exceptions` with a public `Code = "USER_DEACTIVATED"`; map it in `ExceptionMapper` to `403` with `ProblemDetails.Extensions["code"]`, through a `Problem` overload that accepts an optional code; extend `ExceptionMapperTests` to assert status and `code`; verify the tests pass
- [x] 5.2 In `LoginUseCase`, throw `UserDeactivatedException` after the password matches and before generating tokens; extend `LoginUseCaseTests`: deactivated with correct password throws and saves no refresh token; deactivated with wrong password still throws `InvalidCredentialsException`; verify the tests pass
- [x] 5.3 In `RefreshTokenUseCase`, throw `UserDeactivatedException` after loading the user and before rotation; extend `RefreshTokenUseCaseTests`: nothing is saved and the stored token is not revoked; verify the tests pass
- [x] 5.4 Declare `403` on `POST /auth/login` and `POST /auth/refresh` with `[ProducesResponseType]` and update the OpenAPI document test; verify the tests pass
- [x] 5.5 Extend `test/WeightTracker.Integration/Auth/AuthEndpointsTests.cs` (deactivating through the API in Arrange): login `403` with `code` and no tokens; wrong password `401` without the code; refresh after deactivation `403` with `code`; login succeeds after reactivation; verify the tests pass
- [x] 5.6 Review the login and refresh changes against the existing auth use cases and invariants; confirm no deviation

## 6. Workout assignment requires active users

- [x] 6.1 Add `EnsureCanBeAssigned(User)` to `WorkoutAssignmentValidator` (asks `CanBeAssignedWorkouts()`, throws `InvalidWorkoutAssignmentException`) and rewrite `EnsureCanTrain` to load the user and ask `CanTrain()` and `CanBeAssignedWorkouts()`; update `WorkoutAssignmentValidatorTests`; verify the tests pass
- [x] 6.2 Remove `IsTrainerOrAdmin` from `IUserRepository` and `UserRepository`, with its cases in `test/WeightTracker.Integration/Repositories/UserRepositoryTests.cs`, and update the mocks in `CreateWorkoutUseCaseTests` and `UpdateWorkoutUseCaseTests` to `GetById`; verify the build and those tests pass
- [x] 6.3 Call `EnsureCanBeAssigned` for the owner in `CreateWorkoutUseCase` after the trainer role check, and for a changed owner in `UpdateWorkoutUseCase`; extend both unit test classes with deactivated owner and deactivated trainer cases, plus an unchanged deactivated owner in update; verify the tests pass
- [x] 6.4 Extend the workout creation and update integration tests in `test/WeightTracker.Integration/Workouts`: deactivated owner `400` and no workout created; deactivated trainer `400`; reactivated user can be prescribed again; admin reassigns to a deactivated owner or trainer `400` with nothing changed; renaming a workout of a deactivated owner `200`; verify the tests pass
- [x] 6.5 Review the assignment change against `WorkoutAssignmentValidator`'s existing pattern and the workout edit rule in `STPR_INVARIANTS.md`; confirm no deviation

## 7. Integration checks

- [x] 7.1 Run `dotnet build WeightTracker.slnx` with warnings as errors and `dotnet test WeightTracker.slnx` with Docker running; verify both pass
- [x] 7.2 Run the template parity check from `AGENTS.md`; verify it prints no `DRIFT` line, or update `.stpr/template` copies of changed canonical files
- [x] 7.3 Walk the `AGENTS.md` quality checklist and confirm every item applies or is satisfied

## Workflow follow-up

- Refresh the front OpenAPI snapshot and handle `USER_DEACTIVATED` in a separate front plan.
- Archive the change after review.
