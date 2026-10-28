# Proposal

## Why

Nothing in the catalog tells strength, cardio and stretching exercises apart. Treadmills, bikes and rowing machines come back as `QUADRICEPS` / `LEGS`, and stretches come back under the muscle they stretch. Clients need the kind of exercise to filter the catalog and to present workouts. Muscle and body part cannot express it, so it becomes its own property. Source brief: `.stpr/plan/openspec/proposal-change-add-exercise-type.md`.

## What Changes

- New domain enum `ExerciseTypes` (`STRENGTH = 1`, `CARDIO = 2`, `STRETCHING = 3`) and a mandatory `Exercise.Type`. An undefined value is rejected with a new `InvalidExerciseTypeException`, the same way `Muscle` is.
- `Type` is independent: there is no mapping to or from muscle or body part, and the existing muscle and body part of every exercise stay as they are.
- `ExerciseCatalog.json` gains a lowercase `type` on all 876 entries, derived once from the source dataset's `category`:
  - `cardio` → cardio (14)
  - `stretching` → stretching (123)
  - `strength`, `powerlifting`, `olympic weightlifting`, `strongman` and `plyometrics` → strength (739)
- A new FluentMigrator migration adds a non-null `Type` column to `Exercises` and fills it from the catalog. Its `Down()` drops the column.
- Additive, non-breaking response change: `type` is returned, by name, by `GET /exercises`, `GET /exercises/{id}` and in every `GET /workouts` entry: every response that already carries `muscle` and `bodyPart`.

## Capabilities

### New Capabilities

_None._

### Modified Capabilities

- `exercises`: exercise reads and workout listings expose `type`, and every exercise has exactly one mandatory, independent type taken from the catalog.

## Template And Module Mapping

- Exercises -> read-only catalog (truly catalog-like: changed only by migrations or direct database edits) -> `.stpr/template/src/WeightTracker.Domain/Exercises/` (`Exercise.cs`, `Muscles.cs`). `Type` mirrors `Muscle`: a stored `int`, validated in the factory, and mapped in `ExerciseMapper`.
- Migration -> `202610270035_AddExerciseTranslationsAndMuscle.cs` (adds a column) and `202610271200_ImportFreeExerciseCatalog.cs` (reads the embedded catalog and maps dataset strings with a hand-aligned dictionary).
- Api -> `ExerciseResponse` and `WorkoutExerciseResponse` -> they already expose `muscle` and `bodyPart` by name.
- Workouts aggregate -> untouched. Only the list response, which embeds catalog exercise data, gains the field.
- No conflicts with the template or invariants, and no deviations.

## Business Identifiers And Mutability

- No identifier is added or changed. `Exercise.Id` and `FreeExerciseDbId` stay immutable.
- `Type` is not an identifier. It is immutable through the API and changes only by migration or a direct database edit.

## Acceptance Criteria

- Every exercise read through the API has a `type` of `STRENGTH`, `CARDIO` or `STRETCHING`.
- After migrating, every catalog row has a non-null type, and the counts match the catalog: 739 / 14 / 123.
- Muscle and body part of every exercise are unchanged. For example, Running, Treadmill is still `QUADRICEPS` / `LEGS`, now with `type` `CARDIO`.
- The migration's `Down()` drops the column. The full suite passes with Docker running.

## Out Of Scope

- Filtering `GET /exercises` by type.
- Any type ↔ muscle or body part rule, and re-labelling the muscle of cardio or stretching exercises.
- Frontend contract regeneration and frontend changes.
- Type-specific set targets, such as duration-only sets for cardio.

## Impact

- Domain: `ExerciseTypes.cs`, `Exercise.cs` (and its template copy), and `InvalidExerciseTypeException`.
- Infra: `ExerciseEntity`, `WeightTrackerDbContext`, `ExerciseMapper`, `SeedData` and `DatabaseSeeder`.
- Migrations: new migration and `Data/ExerciseCatalog.json`.
- Api: `ExerciseResponse` and `WorkoutExerciseResponse`.
- Tests: every unit-test call to `Exercise.Rehydrate` gains a type argument, plus new domain, mapper, endpoint and migration tests.
- The OpenAPI document changes. The frontend's generated contract will need regenerating in a later change.
