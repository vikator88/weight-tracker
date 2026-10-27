# Tasks

Removal goes from the outside in (API, Application, Infrastructure, Domain), so each group deletes the callers before the code they call. The whole solution builds after every group, and each group removes or rewrites its own tests in the same step.

## 1. API

- [x] 1.1 Remove the `Create`, `Update` and `Delete` actions, their three use case fields and constructor parameters from `ExercisesController`, and update its `[AllowAnonymous]` remark. Delete `Endpoints/Exercises/Dtos/ExerciseRequest.cs`. Delete `ExerciseCreateEndpointTests.cs`, `ExerciseUpdateEndpointTests.cs` and `ExerciseDeleteEndpointTests.cs`. Verify `dotnet build WeightTracker.slnx` gets past the API project
- [x] 1.2 In `ExerciseReadEndpointsTests`, replace the empty-translations case so it inserts an `ExerciseEntity` without translations through `WeightTrackerDbContext` instead of `POST`. Add the three "Exercises cannot be written through the API" scenarios with an ADMIN token, each asserting a non-success status and an unchanged `GET`. Verify the file compiles
- [x] 1.3 Review the controller against `WorkoutsController` and the read-only reference: both actions anonymous, every catch through `ExceptionMapper`, no write wiring left

## 2. Application

- [x] 2.1 Delete `CreateExerciseUseCase`, `UpdateExerciseUseCase`, `DeleteExerciseUseCase`, `ExerciseDTO.cs` and `ExerciseTranslationParser.cs`, and their registrations in `Program.cs`. Delete `CreateExerciseUseCaseTests`, `UpdateExerciseUseCaseTests` and `DeleteExerciseUseCaseTests`. Verify `dotnet build WeightTracker.slnx` gets past the Application and API projects
- [x] 2.2 Delete `FreeExerciseDbIdAlreadyExistsException` and `ExerciseInUseException`, remove them from `ExceptionMapper`, and remove their cases from `ExceptionMapperTests`. Verify the solution builds
- [x] 2.3 Reduce `IExerciseRepository` to `GetAll`, `GetAllByIds` and `GetById`, and remove `ExistsByExerciseId` from `IWorkoutRepository`. Verify the Application project builds; Infra errors are expected until 3.1
- [x] 2.4 Review the remaining exercise use cases against `GetAllExercisesUseCase` (read-only reference), with no other use case touching exercises

## 3. Infrastructure

- [x] 3.1 Remove `Save`, `Delete`, `ExistsByFreeExerciseDbId` and `Reconcile` from `ExerciseRepository`, and `ExistsByExerciseId` from `WorkoutRepository`. Remove the matching cases from `ExerciseRepositoryTests` and the `ExistsByExerciseId` theory from `WorkoutRepositoryTests`. Verify `dotnet build WeightTracker.slnx` gets past Infra
- [x] 3.2 Confirm `ExerciseMapper`, `DatabaseSeeder` and `WeightTrackerDbContext` need no change, and that `AggregateMapperTests` still round-trips. Verify by building
- [x] 3.3 Review `ExerciseRepository` against `WorkoutRepository` reads (`WithAggregateTables`, `AsNoTracking`, mapping inside the repository)

## 4. Domain

- [x] 4.1 Remove `Create`, `Update`, `SetTranslation`, `RemoveTranslation` and the `VideoUrl`/`Muscle` setters from `Exercise`, keeping `Rehydrate` with its muscle and duplicate-language checks. Remove `Create` and `Rename` from `ExerciseTranslation`, making `Name` get-only. Verify `dotnet build WeightTracker.slnx` succeeds with 0 warnings
- [x] 4.2 Rewrite `ExerciseTests` around `Rehydrate`: fields and translations restored, `IsNew` false, duplicate language rejected, undefined muscle rejected, body part derived from the stored muscle. Move any other unit test still using a removed method to `Rehydrate`. Verify `dotnet test test/WeightTracker.Unit` is green
- [x] 4.3 Review the aggregate against the read-only catalog classification: no public or internal mutator left, and `Id.New()` no longer used for exercises

## 5. Final verification

- [x] 5.1 Sync the `.stpr/template/src` copies of `Exercise.cs`, `IExerciseRepository.cs` and `IWorkoutRepository.cs`. Verify the drift check in `AGENTS.md` prints nothing
- [x] 5.2 Verify that `grep -rn "CreateExerciseUseCase\|UpdateExerciseUseCase\|DeleteExerciseUseCase\|SetTranslation\|RemoveTranslation\|ExistsByFreeExerciseDbId\|ExistsByExerciseId" src test` finds nothing outside `bin`/`obj`
- [x] 5.3 With Docker running, verify `dotnet test WeightTracker.slnx` is fully green, with no skipped integration tests
- [x] 5.4 Walk the `AGENTS.md` quality checklist and verify `openspec validate make-exercises-read-only-catalog --strict` passes

## Workflow follow-up

- Archive the change after review and sync `specs/exercises` into `openspec/specs`
- After the sync, update the `## Purpose` of `openspec/specs/exercises/spec.md` by hand to drop "and written by admins". A delta cannot change Purpose
