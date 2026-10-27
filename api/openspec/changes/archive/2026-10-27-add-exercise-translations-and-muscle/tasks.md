# Tasks

Changing the `Exercise` signatures breaks every caller at once, and warnings are errors. Until group 5 compiles the solution, a group is checked by building its own project (`dotnet build src/<Project>`) and by updating its tests so they compile. Those tests are run in the first group that builds the whole solution.

## 1. Domain

- [x] 1.1 Add `LanguageCode` (trim, lowercase, two ASCII letters, `MaxLength = 2`) and `InvalidLanguageCodeException`, mirroring `Url.cs`, with `test/WeightTracker.Unit/Domain/Exercises/LanguageCodeTests.cs` covering valid, normalized, empty, three-letter and non-letter inputs; verify `dotnet build src/WeightTracker.Domain` succeeds
- [x] 1.2 Add `FreeExerciseDbId` (trim, 1 to 128 characters, `MaxLength = 128`) and `InvalidFreeExerciseDbIdException`, with `FreeExerciseDbIdTests` covering trimmed, empty, whitespace-only, 128- and 129-character inputs; verify the domain project builds
- [x] 1.3 Add `InvalidMuscleException` and `MuscleBodyParts.BodyPartOf` with the mapping from `specs/exercises/spec.md`, plus a test that enumerates every `Muscles` value and asserts each expected body part; verify the domain project builds
- [x] 1.4 Add the `ExerciseTranslation` entity (`Create`, `Rehydrate`, internal `Rename`) mirroring `WorkoutExercise.cs`, and `DuplicateExerciseTranslationException`; verify the domain project builds
- [x] 1.5 Rework `Exercise`: remove `Name`, `_bodyPart` and `EnsureDefined(BodyParts)`; add `FreeExerciseDbId`, validated `Muscle`, derived `BodyPart` and `Translations`; new `Create`, `Rehydrate` and `Update` signatures; `SetTranslation` and `RemoveTranslation`. Delete `InvalidBodyPartException`, and update the `ExerciseName` doc comment. Verify the domain project builds
- [x] 1.6 Rewrite `test/WeightTracker.Unit/Domain/Exercises/ExerciseTests.cs`, following `WorkoutTests.cs` with Arrange/Act/Assert: create, rehydrate with translations, rehydrate rejecting a duplicate language, update, an invalid muscle that leaves state unchanged, derived body part, `SetTranslation` add and rename (same `Id`), `RemoveTranslation` present and absent (no throw). Update `EntityEqualityTests`, `WorkoutTests` and `WorkoutExerciseTests` wherever they build an `Exercise`. Verify the files compile against the new domain
- [x] 1.7 Review the domain against `WeightTracker.Domain/Workouts` (`Workout.cs`, `WorkoutExercise.cs`) and `Url.cs`: private child list exposed read-only, child mutations only through the root, `Id.New()` only in `Create`. Note any deviation in the change

## 2. Application

- [x] 2.1 Replace `ExerciseDTO` with `(FreeExerciseDbId?, VideoUrl?, Muscle, Translations)` and add `ExerciseTranslationDTO`; verify `dotnet build src/WeightTracker.Application` reaches only the expected call-site errors
- [x] 2.2 Add `ExistsByFreeExerciseDbId` to `IExerciseRepository`, and add `FreeExerciseDbIdAlreadyExistsException` (Application); verify the project builds
- [x] 2.3 Update `CreateExerciseUseCase`: build value objects, reject duplicate normalized language codes, pre-check `FreeExerciseDbId`, `Create`, `SetTranslation` each entry, `Save`, commit once. Update `CreateExerciseUseCaseTests` for success, duplicate dataset id (no save, no commit), duplicate language, invalid muscle and invalid language code. Verify the project builds
- [x] 2.4 Update `UpdateExerciseUseCase`: `Update(videoUrl, muscle)`, remove the stored languages missing from the request, `SetTranslation` each entry, ignore `FreeExerciseDbId`. Update `UpdateExerciseUseCaseTests` for rename, add and remove, empty list, unchanged dataset id, not found and duplicate language. Verify the project builds
- [x] 2.5 Fix the remaining Application call sites and their tests (`GetExerciseByIdUseCaseTests`, `DeleteExerciseUseCaseTests` and the workout use case tests that build exercises). Verify `dotnet build src/WeightTracker.Application` succeeds
- [x] 2.6 Review the use cases against `CreateExerciseUseCase` as it was before this change and against `UpdateWorkoutExercisesUseCase`: input is validated before any mutation and `SaveChangesAsync` is called once

## 3. Persistence and migration

- [x] 3.1 Add migration `<yyyyMMddHHmm>_AddExerciseTranslationsAndMuscle` as described in `design.md`, with a `Down()` that reverses it. Verify `dotnet build src/WeightTracker.Migrations` succeeds
- [x] 3.2 Update `ExerciseEntity`, add `ExerciseTranslationEntity`, and configure both in `WeightTrackerDbContext` with explicit names, lengths taken from the value object constants, `ValueGeneratedNever()`, cascade and both unique indexes. Verify the configuration matches the migration column for column
- [x] 3.3 Rewrite `ExerciseMapper` for both tables, with `MapMuscle` and `PersistenceMappingException` on bad stored data. Update `test/WeightTracker.Unit/Infra/Mappers/AggregateMapperTests.cs` for a round-trip with translations and dataset id, and for an unknown muscle, an invalid stored language code and duplicate stored languages. Verify the infra project builds
- [x] 3.4 Update `ExerciseRepository`: `Include(Translations)` on every read, `ExistsByFreeExerciseDbId`, and a `Save` that reconciles translations as `WorkoutRepository.Reconcile` does and never copies `FreeExerciseDbId` on update. Verify the infra project builds
- [x] 3.5 Update `SeedData` (eight translation ids) and `DatabaseSeeder` (muscle, dataset id, `es`/`en` translations per the brief). Verify `dotnet build src/WeightTracker.Infra` succeeds
- [x] 3.6 Review the mapper and repository against `WorkoutMapper.cs` and `WorkoutRepository.cs`, and the migration against `202610080938_CreateWorkoutExercisesTable.cs` and `202610161647_AddSuperSeriesIdToWorkoutExercises.cs`

## 4. API

- [x] 4.1 Update `ExerciseRequest` (with `ExerciseTranslationRequest`) and `ExerciseResponse` (with `ExerciseTranslationResponse`) to the shapes in `design.md`, and add `ProducesResponseType(409)` to `POST /exercises`. Verify `dotnet build src/WeightTracker.Api` succeeds
- [x] 4.2 Update `WorkoutExerciseResponse` to carry `Translations` and `Muscle` in place of `Name`. Verify the API project builds
- [x] 4.3 Update `ExceptionMapper`: four new `400` mappings, one new `409`, and remove `InvalidBodyPartException`. Update `test/WeightTracker.Integration/Common/ExceptionMapperTests.cs` to match. Verify the API project builds
- [x] 4.4 Review the controller and DTOs against the existing exercise and workout endpoints: no new wiring, structural validation only, every catch delegates to `ExceptionMapper.Map`

## 5. Integration tests

- [x] 5.1 Build the whole solution and run the unit suite. Verify `dotnet build WeightTracker.slnx` succeeds and `dotnet test test/WeightTracker.Unit` is green, which runs the tests written in groups 1–3
- [x] 5.2 Update `test/WeightTracker.Integration/Migrations/MigrationsTests.cs` for the new columns, the dropped columns, the translations table, both unique indexes and a clean `Down()`. Verify the tests pass
- [x] 5.3 Update `Repositories/ExerciseRepositoryTests.cs` for insert with translations, rename keeping the row id, remove deleting the row, dataset id round-trip, `ExistsByFreeExerciseDbId`, and cascade on delete. Verify the tests pass
- [x] 5.4 Update `Exercises/ExerciseReadEndpointsTests.cs` for the new response shape, the seeded Sentadilla values and the absent `name`. Verify the tests pass
- [x] 5.5 Update `Exercises/ExerciseCreateEndpointTests.cs` (create with and without translations or dataset id, body part ignored, `409` duplicate dataset id, `400` for an invalid muscle, a bad language code, a duplicate language and a 129-character dataset id, normalization), keeping the existing `401`/`403` matrix. Verify the tests pass
- [x] 5.6 Update `Exercises/ExerciseUpdateEndpointTests.cs` (rename, add and remove, empty list, unchanged dataset id, a muscle change that changes the body part, `400` for a duplicate language with nothing stored, `404`), keeping the existing auth matrix, and adjust `ExerciseDeleteEndpointTests.cs` to the new seeds. Verify the tests pass
- [x] 5.7 Update the workout integration tests that read exercise `name` or `bodyPart` (`WorkoutResponseShapeTests` and the other `Workouts/*` tests). `GET /workouts` asserts `translations`, `muscle`, `bodyPart` and no `name`; `GET /workouts/{id}` still carries no exercise data. Verify the tests pass

## 6. Final verification

- [x] 6.1 Sync the `.stpr/template` copies (`Exercise.cs`, `BodyParts.cs`, new `Muscles.cs`, and `Workout.cs`/`WorkoutTests.cs` if they changed). Verify the drift check in `AGENTS.md` prints nothing
- [x] 6.2 With Docker running, verify `dotnet test WeightTracker.slnx` is fully green, with no skipped integration tests
- [x] 6.3 Walk the quality checklist in `AGENTS.md` against the change, and verify `openspec validate add-exercise-translations-and-muscle --strict` passes

## Workflow follow-up

- Archive the change once review is done, then sync `specs/exercises` into `openspec/specs`
