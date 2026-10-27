# Design

## Context

See `proposal.md` for the reasons and `specs/exercises/spec.md` for the behavior. This section only describes the current state.

- `ExercisesController` inherits `AuthorizedController`. Its two `GET` actions are `[AllowAnonymous]`, and `POST`/`PUT`/`DELETE` are `[RequireRole(Role.ADMIN)]`, backed by `CreateExerciseUseCase`, `UpdateExerciseUseCase` and `DeleteExerciseUseCase`
- The `Exercise` aggregate has `Create`, `Rehydrate`, `Update`, `SetTranslation` and `RemoveTranslation`, plus public setters on `VideoUrl` and `Muscle`. `ExerciseTranslation` has `Create`, `Rehydrate` and an internal `Rename`
- `ExerciseRepository` implements `GetAll`, `GetAllByIds`, `GetById`, `ExistsByFreeExerciseDbId`, `Save` (with a translation reconcile) and `Delete`. `WorkoutRepository.ExistsByExerciseId` exists only for the delete use case
- `DatabaseSeeder` builds the four seeded exercises with `Exercise.Rehydrate` and inserts them with `ExerciseMapper.MapToEntity` when they are missing
- These files have copies in `.stpr/template/src` that must stay byte-identical: `Exercise.cs`, `IExerciseRepository.cs` and `IWorkoutRepository.cs`

## Goals / Non-Goals

**Goals:**
- Leave no exercise write path in the domain, application or API
- Keep both `GET` responses and the `GET /workouts` embedding byte-for-byte unchanged

**Non-Goals:**
- Changing `ExerciseRepository` reads, `ExerciseMapper.MapToDomain`, or the response DTOs
- Changing the seed, the catalog migration or the schema

## Decisions

### Domain (`WeightTracker.Domain/Exercises`)
- **`Exercise`:**
  - Delete `Create`, `Update`, `SetTranslation` and `RemoveTranslation`
  - `VideoUrl` and `Muscle` become get-only. `Muscle` keeps its validation in the constructor (`EnsureDefined`), so `Rehydrate` still rejects an undefined muscle
  - `Translations` stays a snapshot of a private list, which is now `IReadOnlyList` built once
  - `Rehydrate` keeps its signature and its duplicate-language check
  - Keep `AggregateRoot` as the base type. Equality by `Id` is what the workout reads rely on (`IReadOnlyDictionary<Id, Exercise>`), and a new base type for catalogs would be a pattern the template doesn't have
  - `IsNew` is always `false` after this change. It stays because it belongs to the base type
- **`ExerciseTranslation`:** delete `Create` and `Rename`. `Name` becomes get-only
- **Value objects and exceptions:** unchanged. `ExerciseName`, `LanguageCode`, `FreeExerciseDbId`, `Url` and `MuscleBodyParts` are all still used by rehydration, and so are `InvalidMuscleException` and `DuplicateExerciseTranslationException`
- **Alternative considered:** keeping `Create` for the seeder. Rejected, because the seeder already uses `Rehydrate` with fixed ids, and a public `Create` would re-open a write path

### Application (`WeightTracker.Application`)
- **Delete these files:**
  - `UseCases/Exercises/CreateExerciseUseCase.cs`, `UpdateExerciseUseCase.cs`, `DeleteExerciseUseCase.cs`
  - `UseCases/Exercises/ExerciseDTO.cs`, `ExerciseTranslationParser.cs`
  - `Exceptions/FreeExerciseDbIdAlreadyExistsException.cs`, `Exceptions/ExerciseInUseException.cs`
- **`IExerciseRepository`:** keep `GetAll`, `GetAllByIds` and `GetById`
- **`IWorkoutRepository`:** remove `ExistsByExerciseId`. Nothing else calls it, and dead contract methods would be implemented and tested for nothing
- **Unchanged:** `GetAllExercisesUseCase` and `GetExerciseByIdUseCase`

### Infrastructure (`WeightTracker.Infra`)
- **`ExerciseRepository`:** remove `Save`, `Delete`, `ExistsByFreeExerciseDbId` and `Reconcile`. Keep `WithAggregateTables` and the three reads
- **`WorkoutRepository`:** remove `ExistsByExerciseId`
- **`ExerciseMapper`:** unchanged. `MapToEntity` is still needed by `DatabaseSeeder`, so the round-trip tests stay meaningful
- **Unchanged:** `WeightTrackerDbContext`, the entities and the migrations. The `ExerciseTranslations` cascade from `Exercises` stays, because database edits rely on it

### API (`WeightTracker.Api`)
- **`ExercisesController`:**
  - Remove the `Create`, `Update` and `Delete` actions and their three use case dependencies
  - Keep inheriting `AuthorizedController`, so a protected route added later gets the caller resolution, as the brief requires
  - Update the remark about `[AllowAnonymous]` per action so it no longer mentions write actions
- **Removed routes:** with no action matching, ASP.NET Core answers `POST /exercises` with `405 Method Not Allowed` (the path still matches the `GET` action), and `PUT`/`DELETE /exercises/{id}` with `405` as well (the path still matches `GET /exercises/{id}`). The spec only requires "not a success status", so tests assert a non-success status and an unchanged catalog, not a specific code
- **Delete:** `Dtos/ExerciseRequest.cs` (both records)
- **`ExceptionMapper`:** remove `FreeExerciseDbIdAlreadyExistsException` and `ExerciseInUseException` from the `409` arm. Every remaining domain exception keeps its mapping
- **`Program.cs`:** remove the three use case registrations

### Tests
- **Removed:**
  - Unit: `Application/Exercises/CreateExerciseUseCaseTests.cs`, `UpdateExerciseUseCaseTests.cs`, `DeleteExerciseUseCaseTests.cs`
  - Integration: `Exercises/ExerciseCreateEndpointTests.cs`, `ExerciseUpdateEndpointTests.cs`, `ExerciseDeleteEndpointTests.cs`
- **`ExerciseTests`:** reduced to rehydration:
  - Fields and translations restored, and `IsNew` false
  - Duplicate-language rejection and undefined-muscle rejection
  - Body part derived for each muscle used
  - `Translations` is a snapshot. Its old test mutated through `SetTranslation`, so it now asserts the returned list cannot be cast back and changed, or is dropped if that adds nothing
- **`ExerciseRepositoryTests`:** remove the `Save`, `ExistsByFreeExerciseDbId` and `Delete` cases. Keep the `GetById` and `GetAllByIds` cases
- **`WorkoutRepositoryTests`:** remove the `ExistsByExerciseId` theory
- **`ExceptionMapperTests`:** remove the `409` case for `FreeExerciseDbIdAlreadyExistsException`, and the `ExerciseInUseException` case if there is one
- **`ExerciseReadEndpointsTests`:**
  - The "empty translations" case inserts an `ExerciseEntity` without translations directly through `WeightTrackerDbContext`, instead of calling the removed `POST`. That is how catalog data is maintained now, and `WorkoutExerciseEndpointTest` already reaches the context from a test
  - New cases cover the "Exercises cannot be written through the API" scenarios with an ADMIN token, asserting a non-success status and an unchanged `GET`
- **`OpenApiDocumentTests`:** the cases for the removed `POST`/`PUT`/`DELETE` operations are replaced by one that asserts both exercise paths document only `get` (found during apply)
- **Kept:** the `GET` routes are anonymous, so no `401` matrix applies to them, as `AGENTS.md` says for public endpoints

### Template parity
- After the change, copy `Exercise.cs`, `IExerciseRepository.cs` and `IWorkoutRepository.cs` into `.stpr/template/src`. The drift check in `AGENTS.md` must print nothing

## Risks / Trade-offs

- [Clients still call the removed routes] → They get `405` instead of a success. That is called out as BREAKING in the proposal, and the client updates are a separate change
- [`IsNew` and `AggregateRoot` remain on a catalog type] → Accepted, to keep equality and avoid a new base type. Nothing reads `IsNew` for exercises after this change
- [Deleting an exercise in the database while a workout references it] → `FK_WorkoutExercises_Exercise` is restricted and rejects it. That protection used to be the `409` from `ExerciseInUseException`; the database constraint now provides it

## Migration Plan

- Code-only change, no database migration. Deploying it removes the write routes. Rolling back means redeploying the previous build, with no data impact
