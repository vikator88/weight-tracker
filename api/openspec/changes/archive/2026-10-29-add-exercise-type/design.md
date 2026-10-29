# Design

## Context

See proposal.md (Why). Today `Exercise` stores `Muscle` as an `int` and derives `BodyPart` from it. The catalog is inserted by `ImportFreeExerciseCatalog_202610271200`, which reads the embedded `Data/ExerciseCatalog.json` (876 entries with fixed ids) with `System.Text.Json` web defaults. Those defaults ignore unknown properties. The migrations project references no other project, so dataset strings are mapped to enum values through `MuscleValues`, a dictionary aligned by hand.

Integration tests truncate the database and reseed it through `DatabaseSeeder`. It rebuilds the four seeded catalog exercises with `Exercise.Rehydrate`, and only those four, all of them strength, exist during integration tests. Production and development databases already hold the 876 catalog rows.

`Exercise.Rehydrate` is the only factory. It is called by `ExerciseMapper`, `DatabaseSeeder`, and about 20 unit tests.

## Goals / Non-Goals

**Goals:**
- Make `Type` mandatory from the domain down to the database (a non-null column), set for every existing row by the migration.
- Keep the catalog JSON the single source of each exercise's type.

**Non-Goals:**
- Changing `ImportFreeExerciseCatalog_202610271200` or the meaning of any existing migration.
- Any query or filter by type.

## Decisions

### Layer mapping

- **Domain**:
  - `ExerciseTypes` enum, next to `Muscles`: `STRENGTH = 1`, `CARDIO = 2`, `STRETCHING = 3`.
  - `Exercise.Type { get; }`.
  - `Exercise.Rehydrate(id, freeExerciseDbId, videoUrl, muscle, type, translations)`: `type` goes right after `muscle` and is validated by an `EnsureDefined` overload that throws `InvalidExerciseTypeException(int)`. That exception follows `InvalidMuscleException` and falls back to `400` through the `DomainException` branch of `ExceptionMapper`. It cannot be reached from a request because the catalog is read-only, so it gets no explicit mapping.
  - The curated copy `.stpr/template/src/WeightTracker.Domain/Exercises/Exercise.cs` is updated with it.
- **Infrastructure**:
  - `ExerciseEntity.Type` (`int`) and `.HasColumnName("Type")` in `WeightTrackerDbContext`.
  - `ExerciseMapper` writes `(int)exercise.Type` and reads it through `MapType`, which throws `PersistenceMappingException` like `MapMuscle` does.
  - `DatabaseSeeder.SeededExercise` takes a type. Every seeded exercise passes the catalog's type.
- **Migrations**:
  - New `AddTypeToExercises_<timestamp>`. `Up()` does four things, in order:
    1. `Alter.Table("Exercises").AddColumn("Type").AsInt32().Nullable()`.
    2. For each catalog entry, `Update.Table("Exercises").Set(new { Type }).Where(new { Id })`.
    3. `Alter.Column("Type").OnTable("Exercises").AsInt32().NotNullable()`.
    4. It throws on an unknown or missing `type` string before writing anything.
  - It reads the catalog through its own `LoadCatalogTypes()` with a private record `(Guid Id, string FreeExerciseDbId, string Type)`, and maps strings through a public `TypeValues` dictionary (`strength` → 1, `cardio` → 2, `stretching` → 3), aligned by hand with `ExerciseTypes`.
  - `Down()` drops the column.
- **Catalog data**: every entry in `ExerciseCatalog.json` gains `"type"` right after `"muscle"`, as a lowercase string. A one-off script that is not committed generates it from `infra/exercises.json` `category`, matched on `freeExerciseDbId`. The file keeps its current 2-space formatting and non-ASCII characters, so the diff only adds one line per entry.
- **Api**:
  - `ExerciseResponse` and `WorkoutExerciseResponse` gain `string Type` after `BodyPart`, set from `exercise.Type.ToString()`, and the `<param>` docs are updated.
  - No new route, no new exception mapping.
- **Seed**: `SeedData` gains two catalog exercises so integration tests can reach the non-strength types:
  - `Running_Treadmill` (cardio): exercise id `01a17aa9-4cb7-7fb2-9840-4559ec09792f`, translation ids `01a17aa9-4cb8-712e-b94a-4d77d3c3e8e4` / `01a17aa9-4cb9-7dc3-b962-0bbc6e86d24b`.
  - `90_90_Hamstring` (stretching): exercise id `01a17aa9-4585-71c5-a350-6a9c03107e96`.
  - Ids, dataset ids, muscles and names are copied from the catalog, as `SeedData` requires. The seeder already skips rows that exist in a migrated database.

Cross-module dependencies: Workouts reads `Exercise` only to build `WorkoutExerciseResponse`. The Workout aggregate does not change.

### A backfill migration instead of editing the import migration

Applied migrations are never edited. Development and production databases already ran `202610271200`, so a later migration is the only way to add a column and fill existing rows. It also works on a fresh database, where the import has just run. Adding the column as nullable, filling it, then requiring it avoids a database default that would hide a missing type.
Alternative: add the column with a default of `STRENGTH`, then update the cardio and stretching rows. Rejected because a wrong or missing entry would silently become strength.

### The migration keeps its own catalog reader

`ImportFreeExerciseCatalog_202610271200.CatalogExercise` stays unchanged so the import keeps its meaning. The new migration deserializes only the fields it needs.

### `Type` goes after `muscle` in `Rehydrate`

The two catalog classification fields stay together. Every caller is a positional call, so the compiler finds them all.

## Risks / Trade-offs

- [A row in `Exercises` that is not in the catalog (for example a manual database edit) would make step 3 fail] → The failure is wanted, because the type is mandatory. The migration log shows the null row, which is fixed by hand and then migrated again.
- [The catalog JSON and `TypeValues` drift from `ExerciseTypes`] → A migration test checks per-type counts after migrating and that every row is non-null. The existing pattern (`MuscleValues`) has the same trade-off.
- [About 20 unit-test call sites change] → The changes are mechanical. Each passes `ExerciseTypes.STRENGTH` unless the test is about the type.
- [Clients that strictly validate responses see a new field] → The field is additive. Regenerating the frontend contract is a follow-up.

## Migration Plan

Deploy runs the new migration. It adds the column, fills it from the embedded catalog, then makes it required. To roll back, run `--down` to that version, which drops the column, then revert the code. No data is lost, because the type comes from the catalog file.

## Open Questions

None. Plyometrics → `STRENGTH` was confirmed by the user.
