# Tasks: Workouts GetAll With Users And Auth

Execute in order. Each phase ends with a build; phases 2 onward also run the affected tests.
Reference: `plan.md` in this folder, `.stpr/template/STPR_INVARIANTS.md`, `.stpr/template/README.md`, `AGENTS.md`.

---

## Phase 0 — Prerequisites

1. Start Postgres: `docker compose -f ../infra/docker-compose.yml up -d`. Verify it listens on 5432.
2. Create the integration database `weight-tracker-test` on that container.
3. Confirm `dotnet --version` resolves to 10.0.x and `dotnet build WeightTracker.slnx` succeeds on the untouched skeleton.

## Phase 1 — Shared domain primitives

4. Add `src/WeightTracker.Domain/Common/Id.cs` — `sealed record Id(Guid Value)` with `Id.New()` (`Guid.CreateVersion7()`), `Id.From(Guid)` rejecting `Guid.Empty`, and `ToString()`.
5. Add `src/WeightTracker.Domain/Common/Email.cs` — `sealed record Email` with lowercase normalization, shape validation, 254-char max.
6. Add `src/WeightTracker.Domain/Exceptions/DomainException.cs` (abstract base) and rebase `ExerciseNotInWorkoutException` onto it.
7. Add `InvalidIdException` and `InvalidEmailException` under `Domain/Exceptions`.
8. Change `Entity.Id` from `Guid` to `Id`, and override `Equals`/`GetHashCode` on `(concrete type, Id)`. See plan §9 — required for dictionary correctness, not cosmetic.
9. Update `Exercise` to use `Id.New()`, and add `Exercise.Rehydrate(Id id, string name, string? videoUrl, BodyParts bodyPart)` setting `IsNew = false`.
10. Write unit tests for `Id`, `Email`, and `Entity` equality. Delete the `WeightTest.cs` placeholder.

## Phase 2 — Users and Auth domain

11. Add `src/WeightTracker.Domain/Users/Role.cs` — `USER = 1`, `TRAINER = 2`, `ADMIN = 3`.
12. Add `src/WeightTracker.Domain/Users/User.cs` as an aggregate root, mirroring `Workout`'s Create/Rehydrate/IsNew shape. `Create` accepts an already-hashed password.
13. Add `src/WeightTracker.Domain/Auth/RefreshToken.cs` as an aggregate root with `IsActive(DateTime utcNow)` and `Revoke(Id replacedByTokenId, DateTime utcNow)` that throws when already revoked.
14. Add `Domain/Exceptions/RefreshTokenAlreadyRevokedException.cs`.
15. Unit-test `User` (Create/Rehydrate, `IsNew`) and `RefreshToken` (active, expired, revoke, double-revoke).

## Phase 3 — Workout ownership

16. Add `Domain/Exceptions/TrainerCannotBeWorkoutOwnerException.cs`.
17. Change `Workout`: add immutable `UserId` and nullable `TrainerId`; `Create(Id userId, Id? trainerId, DateTime workoutDate)` rejecting `trainerId == userId`; extend `Rehydrate` with both.
18. Add public read accessors `WorkoutDate` and `Exercises` (as `IReadOnlyDictionary<Exercise, IReadOnlyList<SetPrescription>>`) — the mapper and DTOs need them.
19. Unit-test `Workout`: owner/trainer collision throws; null trainer allowed; `AddExercise` merges when given a distinct `Exercise` instance with the same `Id`; `RemoveExercise` on an absent exercise throws; `Rehydrate` restores sets.

## Phase 4 — Application contracts and use cases

20. Retype `IExerciseRepository.GetById` and `IWorkoutRepository.GetById` from `Guid` to `Id`.
21. Extend `IWorkoutRepository` with `GetAllByUserId(Id)` and `GetAllByUserOrTrainerId(Id)`.
22. Add `IUserRepository` — `GetById(Id)`, `GetByEmail(Email)`, `ExistsByEmail(Email)`, `IsTrainerOrAdmin(Id)`, `Save(User)`.
23. Add `IRefreshTokenRepository` — `GetByTokenHash(string)`, `GetById(Id)`, `Save(RefreshToken)`.
24. Add Application service contracts: `IPasswordHasher`, `ITokenService`, `IClock`.
25. Add `Application/Exceptions/ApplicationLayerException.cs` (abstract) plus `InvalidCredentialsException`, `InvalidRefreshTokenException`, `UserNotFoundException`, `UnauthorizedOperationException`.
26. Add `Application/UseCases/Workouts/GetAllWorkoutsUseCase.cs` — single `Execute(Id callerId, Role callerRole)`, role switch per plan §7.
27. Add `Application/UseCases/Auth/LoginUseCase.cs` — single `Execute(string email, string password)`; indistinguishable failure for unknown email and wrong password.
28. Add `Application/UseCases/Auth/RefreshTokenUseCase.cs` — validate, rotate, revoke, link replacement.
29. Add the `AuthResult` record (`AccessToken`, `RefreshToken`, `ExpiresIn`).
30. Unit-test all three use cases with mocked repositories, covering every branch listed in plan §12.

## Phase 5 — Migrations

31. Add the Postgres connection and migration runner wiring to `WeightTracker.Migrations`.
32. `CreateUsersTable_202610080917` — includes the unique index on `email`.
33. `CreateExercisesTable_202610080923`.
34. `CreateWorkoutsTable_202610080931` — FKs to `users` for `user_id` and nullable `trainer_id`.
35. `CreateWorkoutExercisesTable_202610080938` — cascade from `workouts`, unique `(workout_id, exercise_id)`, `position`.
36. `CreateWorkoutExerciseSetsTable_202610080944` — cascade from `workout_exercises`, `position`, `count`, `target_type`, nullable `target_value`.
37. `CreateRefreshTokensTable_202610080952` — cascade from `users`, unique `token_hash`, self-FK `replaced_by_token_id`.
38. Apply all migrations against `weight-tracker` and `weight-tracker-test` from empty, and verify each `Down()` reverses cleanly.

## Phase 6 — Infrastructure

39. Add the Infra packages: `Npgsql.EntityFrameworkCore.PostgreSQL`, `Microsoft.Extensions.Identity.Core`, `Microsoft.IdentityModel.JsonWebTokens`.
40. Add EF entities `UserEntity`, `ExerciseEntity`, `WorkoutEntity`, `WorkoutExerciseEntity`, `WorkoutExerciseSetEntity`, `RefreshTokenEntity` — FK columns only, no navigation across aggregate roots.
41. Add `WeightTrackerDbContext` with explicit snake_case `ToTable`/`HasColumnName` configuration matching the migrations exactly. Do not enable EF migrations.
42. Add `UserMapper`, `ExerciseMapper`, `RefreshTokenMapper` with `MapToEntity` / `MapToDomain`.
43. Add `WorkoutMapper` — `MapToEntity` emitting all three workout tables with `position` preserved; `MapToDomain` reading all three and calling `Workout.Rehydrate`.
44. Add repository implementations (`UserRepository`, `ExerciseRepository`, `WorkoutRepository`, `RefreshTokenRepository`) with mapping inside each repository. `WorkoutRepository` eager-loads exercises and sets for the three `GetAll*` variants.
45. Add `UnitOfWork` implementing `IUnitOfWork`.
46. Add `PasswordHasher` (ASP.NET Core `PasswordHasher<T>`), `JwtTokenService` (HS256, claims `sub`/`email`/`role`, refresh = 32 random bytes base64url stored as SHA-256), and `SystemClock`.
47. Add `Infra/Seed/DatabaseSeeder.cs` — idempotent, fixed ids and records per plan §10, hashes computed through `IPasswordHasher`.
48. Add a `WorkoutMapper` round-trip test covering ordering and all three `SetTarget` variants.

## Phase 7 — API

49. Add `Microsoft.AspNetCore.Authentication.JwtBearer` to the Api project.
50. Add the `ConnectionStrings:Default` and `Jwt` sections to `appsettings.json`; put a development-only `Jwt:SigningKey` in `appsettings.Development.json`.
51. Add `Api/Common/ExceptionMapper.cs` with the exact status mapping from plan §9, logging unmapped exceptions and returning 500.
52. Wire `Program.cs`: DbContext, repositories, use cases, services, JWT bearer authentication with fail-fast signing-key validation, `UseAuthentication`, `UseAuthorization`, and the Development-only seeder call.
53. Add `Api/Endpoints/Auth/AuthController.cs` — `POST /auth/login`, `POST /auth/refresh`, both `[AllowAnonymous]`, each wrapped in try/catch delegating to `ExceptionMapper`.
54. Add `Api/Endpoints/Auth/Dtos/` — `LoginRequest`, `RefreshRequest`, `AuthResponse` with field names exactly `accessToken`, `refreshToken`, `expiresIn`.
55. Add `Api/Endpoints/Workouts/WorkoutsController.cs` — `GET /workouts`, `[Authorize]`, reads `sub` and role claims, injects only `GetAllWorkoutsUseCase`.
56. Add `Api/Endpoints/Workouts/Dtos/` — `WorkoutResponse`, `WorkoutExerciseResponse`, `SetResponse`, `SetTargetResponse` matching the shape in plan §7, with `MaxReps` serialized without `value`.

## Phase 8 — Integration tests

57. Create `test/WeightTracker.Integration` (xUnit, `Microsoft.AspNetCore.Mvc.Testing`, `Npgsql`, `FluentMigrator.Runner.Postgres`, FluentAssertions) and register it in `WeightTracker.slnx`.
58. Add the fixture: point at `weight-tracker-test`, apply migrations, truncate between tests, re-seed, override the connection string and signing key via `WebApplicationFactory`.
59. Add an auth helper that logs a seeded user in and returns a bearer token.
60. Write the auth tests: anonymous `GET /workouts` → 401; valid login → 200 with all three fields; wrong password → 401; refresh returns a new pair; replaying the old refresh token → 401.
61. Write the scoping tests: `USER` sees `...0021` and `...0022`; `TRAINER` sees `...0022` and `...0023`; `ADMIN` sees all three.
62. Write the response-shape test, including nested sets and the `MaxReps` target.
63. Add a migrations-from-empty test.

## Phase 9 — Verification and review

64. `dotnet build WeightTracker.slnx` with `TreatWarningsAsErrors` clean.
65. `dotnet test` — both suites green, Postgres running.
66. Review each capability against the checkpoints in plan §13:
    - 66a. Shared identifiers (`Id`, `Email`) — checkpoint 1
    - 66b. `User` aggregate — checkpoint 2
    - 66c. `Workout` ownership — checkpoint 3
    - 66d. Persistence and migrations — checkpoint 4
    - 66e. Mappers — checkpoint 5
    - 66f. Auth (login + refresh) — checkpoint 6
    - 66g. `GET /workouts` — checkpoint 7
    - 66h. `ExceptionMapper` — checkpoint 8
    - 66i. Module classification — checkpoint 9
    - 66j. Test coverage — checkpoint 10
67. Update `.stpr/template/README.md` to register the new canonical references (repository implementation, mapper, controller, `ExceptionMapper`, migration, seed, integration bootstrap), and correct the entry that calls `Workouts` a catalog reference — checkpoint 11.
68. Run `.stpr/review/CHECKLIST_REVISION.md` — this change is large enough to warrant it.
69. Optional, flagged in plan §13: rename `ExerciseNotInWorkputException.cs` to `ExerciseNotInWorkoutException.cs` in both `src/` and `.stpr/template/`.
