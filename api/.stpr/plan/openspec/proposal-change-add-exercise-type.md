## Change

Add a mandatory type (STRENGTH, CARDIO, STRETCHING) to every exercise

## Objective

The catalog mixes strength, cardio and stretching exercises, and nothing tells them apart. Today cardio machines such as treadmill, bike and rowing come back as quadriceps / `LEGS`, and stretches come back under the muscle they stretch. Clients need the kind of exercise to filter the catalog and to show workouts properly. Muscle and body part cannot express this, so the type is a separate, independent property.

## Capabilities

- Every exercise has a mandatory `Type`, one of `STRENGTH`, `CARDIO` or `STRETCHING`.
- `ExerciseCatalog.json` declares the type of every exercise, and the database is populated from it.
- Every API response that carries `muscle` and/or `bodyPart` also carries `type`.

## Module Classification

- Exercises -> read-only catalog -> `.stpr/template/src/WeightTracker.Domain/Exercises/` (`Exercise.cs`, `Muscles.cs`, `BodyParts.cs`). The catalog is truly catalog-like: it changes only through migrations or direct database edits, and no endpoint writes it.
- Workouts -> mutable aggregate -> not modified. Only the workout list response, which embeds catalog exercise data, gains the field.

## Canonical Template Mapping

- `ExerciseTypes` enum -> `.stpr/template/src/WeightTracker.Domain/Exercises/Muscles.cs` -> a closed, plural-named enum with explicit numeric values
- `Exercise.Type` -> `Exercise.Muscle` in `.stpr/template/src/WeightTracker.Domain/Exercises/Exercise.cs` -> a mandatory enum property validated with `EnsureDefined` in `Create`/`Rehydrate`, throwing a dedicated domain exception
- `InvalidExerciseTypeException` -> `src/WeightTracker.Domain/Exceptions/InvalidMuscleException.cs` -> the same shape for an undefined enum value
- Persistence (`ExerciseEntity.Type`, EF config, mapper) -> how `Muscle` is stored as an `int` column -> same storage semantics
- Migration -> `202610270035_AddExerciseTranslationsAndMuscle.cs` (adds the `Muscle` column) and `202610271200_ImportFreeExerciseCatalog.cs` (reads the embedded catalog and maps dataset strings with a hand-aligned dictionary)
- DTOs -> `ExerciseResponse` and `WorkoutExerciseResponse` -> they already expose `muscle` and `bodyPart` by name

## State And Mutability

- No new mutable business state. `Type` is stored, unlike `BodyPart`, which is derived. It is set when an exercise is created or rehydrated and never changes through the API.
- The Exercises module stays a read-only catalog.

## Business Rules

- `Type` is mandatory for every exercise, just like `Muscle`. An undefined value is rejected in the domain with `InvalidExerciseTypeException`.
- `Type` is independent: there is no mapping between type and muscle or body part, in either direction. Muscle and body part stay as they are for every exercise, cardio and stretching included.
- Enum values: `STRENGTH = 1`, `CARDIO = 2`, `STRETCHING = 3`. API responses return `type` by name, like `muscle` and `bodyPart`.

## Catalog Data

- Every entry in `src/WeightTracker.Migrations/Data/ExerciseCatalog.json` gains a `type` property next to `muscle`, as a lowercase dataset-style string: `"strength"`, `"cardio"` or `"stretching"`.
- Values are derived once from the source dataset's `category` in `infra/exercises.json`, matched on `freeExerciseDbId` (all 876 match):
  - `cardio` (14) -> `cardio`
  - `stretching` (123) -> `stretching`
  - `strength` (584), `powerlifting` (38), `olympic weightlifting` (35), `strongman` (21), `plyometrics` (61) -> `strength`
- After generation, the catalog contains 739 `strength`, 14 `cardio` and 123 `stretching` entries. Every entry must have a type.

## Persistence

- A new FluentMigrator migration adds an `int` `Type` column to `Exercises`:
  - Add it as nullable.
  - Update every catalog row by its fixed id from the embedded `ExerciseCatalog.json`, mapping strings through a `TypeValues` dictionary aligned by hand with `ExerciseTypes`, the same way `MuscleValues` is.
  - Fail on an unknown string or a missing type.
  - Then make the column non-nullable.
  - `Down()` drops the column.
- The migration must work on a fresh database (where the import migration runs first and inserts no type) and on an existing one. No EF Core migration is generated.
- `ImportFreeExerciseCatalog_202610271200` stays unchanged. Its deserializer must keep ignoring the new `type` property.
- `SeedData` and any test fixture that builds exercises must provide a type that matches the catalog.

## Roles And Visibility

- No change. Exercise reads stay anonymous, and the workout list keeps its current scoping.

## Business Identifiers And Mutability

- No identifier is added or changed. `Exercise.Id` and `FreeExerciseDbId` stay immutable.
- `Type` is not an identifier. It is immutable through the API, and changed only by a migration or a direct database edit.

## Api Contract

- `ExerciseResponse` (`GET /exercises`, `GET /exercises/{id}`) gains `type`.
- `WorkoutExerciseResponse` (entries of `GET /workouts`) gains `type`.
- `WorkoutExerciseDetailResponse` and `GET /workouts/{id}` carry no muscle or body part, so they stay unchanged.

## Tests

- Unit:
  - `Exercise` rejects an undefined type and keeps the type it is given (`ExerciseTests`).
  - The mapper round-trips `Type`.
  - Mapping of the new response fields.
- Integration:
  - `GET /exercises` and `GET /exercises/{id}` return `type`, with one cardio, one stretching and one strength exercise asserted.
  - `GET /workouts` entries return `type`.
  - Migration test: after migrating, every catalog row has a non-null type and the counts per type match the catalog.
  - The migration's `Down()` reverses it.
- Run the full `dotnet test WeightTracker.slnx` with Docker running.

## Out Of Scope

- Filtering `GET /exercises` by type.
- Any mapping between type and muscle or body part, and any change to the existing muscle or body part of cardio or stretching exercises.
- Regenerating the frontend contract and any frontend changes.
- Type-specific set targets, for example duration-only sets for cardio.

## Notes

- Enum naming follows the plural convention of `Muscles` and `BodyParts`: `ExerciseTypes`. The property is `Exercise.Type`.
- `Exercise.cs` and possibly other files are curated copies under `.stpr/template/src`. Update them together and keep the parity check clean.
- Confirmed: `plyometrics` maps to `strength` (explosive power work is programmed with strength).
- No deviations from the mapped template references are allowed.
