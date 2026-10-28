# Tasks

## 1. Domain

- [x] 1.1 Add `ExerciseTypes` (`STRENGTH = 1`, `CARDIO = 2`, `STRETCHING = 3`) in `Domain/Exercises`, following `Muscles`, and `InvalidExerciseTypeException(int)` in `Domain/Exceptions`, following `InvalidMuscleException`. Verify that `dotnet build WeightTracker.slnx` succeeds.
- [x] 1.2 Add the mandatory `Exercise.Type` and a `type` parameter after `muscle` in `Exercise.Rehydrate`, validated by an `EnsureDefined` overload, with XML docs including `<exception>`. Update every `Exercise.Rehydrate` call site in `src` and `test`, passing `ExerciseTypes.STRENGTH` where the test is not about type. Verify that the solution builds.
- [x] 1.3 In `test/WeightTracker.Unit/Domain/Exercises/ExerciseTests.cs`, using Arrange/Act/Assert, cover: the type given is kept; an undefined type is rejected with `InvalidExerciseTypeException`; muscle and body part do not depend on type (a `CARDIO` exercise with `QUADRICEPS` still reports `LEGS`). Verify that `dotnet test test/WeightTracker.Unit` passes.
- [x] 1.4 Copy `src/WeightTracker.Domain/Exercises/Exercise.cs` to its `.stpr/template/src` counterpart, along with any other changed curated file. Verify that the parity loop from AGENTS.md prints no `DRIFT`.
- [x] 1.5 Review the domain change against `Exercise.Muscle` and `STPR_INVARIANTS.md`: input is validated before use, the shared enum convention is followed, and the module is still a read-only catalog. Record any deviation, or confirm there is none.

## 2. Catalog data and migration

- [x] 2.1 Add `"type"` right after `"muscle"` to every entry in `src/WeightTracker.Migrations/Data/ExerciseCatalog.json`, using a one-off script that is not committed. The script derives it from `infra/exercises.json` `category`: `cardio` → `cardio`; `stretching` → `stretching`; `strength`, `powerlifting`, `olympic weightlifting`, `strongman` and `plyometrics` → `strength`. Verify that `git diff --stat` shows 876 insertions and no deletions, and that the counts are 739 / 14 / 123.
- [x] 2.2 Add migration `AddTypeToExercises_<timestamp>`, later than every existing migration, with its own catalog reader and a public `TypeValues` dictionary. `Up()` validates every `type` string, adds a nullable `Type` column, updates every catalog row by id, then makes the column non-null. `Down()` drops the column. Verify that the build succeeds.
- [x] 2.3 Add tests to `test/WeightTracker.Integration/Migrations/MigrationsTests.cs`, following the `AddEffort_*` tests:
  - After `MigrateUp`, no exercise row has a null `Type`, and the per-type counts are 739 / 14 / 123.
  - `Running_Treadmill` is type `2` and keeps muscle `14`.
  - Migrating down to the previous version removes the `Type` column.

  Verify that the migration tests pass with Docker running.
- [x] 2.4 Review the migration against `202610270035_AddExerciseTranslationsAndMuscle.cs` and `202610271200_ImportFreeExerciseCatalog.cs`: FluentMigrator only, a `Down()` that reverses `Up()`, and the import migration left unchanged.

## 3. Persistence and seed

- [x] 3.1 Add `ExerciseEntity.Type` (`int`), map it in `WeightTrackerDbContext` with `.HasColumnName("Type")`, and read and write it in `ExerciseMapper` with a `MapType` that throws `PersistenceMappingException`, mirroring `MapMuscle`. Verify that the build succeeds.
- [x] 3.2 Extend the mapper tests in `test/WeightTracker.Unit/Infra/Mappers/AggregateMapperTests.cs`: `Type` round-trips, and an unknown stored type throws `PersistenceMappingException`. Verify that the unit suite passes.
- [ ] 3.3 Give `DatabaseSeeder.SeededExercise` a type argument and pass `STRENGTH` for the four existing seeded exercises. Add `Running_Treadmill` (`CARDIO`, `QUADRICEPS`) and `90_90_Hamstring` (`STRETCHING`, `HAMSTRINGS`) to `SeedData` and the seeder, with ids, dataset ids and translation names copied exactly from the catalog. Verify that `dotnet run` in Development still starts, and that the seeder inserts nothing extra in a migrated database.
- [x] 3.4 Review the persistence and seed changes against how `Muscle` is stored, mapped and seeded. Confirm the EF configuration matches the migration column.

## 4. Api

- [x] 4.1 Add `string Type` after `BodyPart` in `ExerciseResponse` and `WorkoutExerciseResponse`, set from `exercise.Type.ToString()`, and update their XML docs. Verify that the build succeeds.
- [x] 4.2 Update `test/WeightTracker.Integration/Exercises/ExerciseReadEndpointsTests.cs`:
  - The list now expects the two new seeded ids.
  - `GET /exercises/{id}` returns `type` `STRENGTH` for Sentadilla, `CARDIO` for Running, Treadmill (with `muscle` `QUADRICEPS` and `bodyPart` `LEGS`), and `STRETCHING` for 90/90 Hamstring.
  - Every item of `GET /exercises` carries a `type`.

  Extend `ExerciseJson` if needed. Verify that the exercise integration tests pass.
- [x] 4.3 Assert `type` `STRENGTH` on the Press Banca entry of `GET /workouts` in the closest existing test, `WorkoutResponseShapeTests` or `WorkoutScopingTests`, and update any shape test that lists entry fields. Verify that the workout integration tests pass.
- [x] 4.4 Review the DTOs and endpoint tests against the existing `muscle` and `bodyPart` fields for parity. Confirm `GET /workouts/{id}` is unchanged.

## 5. Verification

- [x] 5.1 With Docker running, run `dotnet build WeightTracker.slnx` and `dotnet test WeightTracker.slnx`. Verify that the build has no warnings and that both tiers are green.
- [x] 5.2 Run the AGENTS.md quality checklist and the template parity loop. Verify that every item holds and that there is no `DRIFT` output.

## Workflow follow-up

- Archive the change after review, so the `exercises` delta is merged into the main spec.
- Regenerate the frontend API contract when the apps need `type`.
