# Proposal

## Why

An exercise stores one name, in Spanish, so the catalog can't be served in more than one language. It also stores only a coarse `BodyPart`, and it has no link to the free exercise dataset in `infra/exercises.json` that the catalog will be built from. The `Exercises` table holds no data yet, so this is the cheapest moment to change its shape.

Source brief: `.stpr/plan/openspec/proposal-change-add-exercise-translations-and-muscle.md`.

## What Changes

- New `ExerciseTranslations` table and `ExerciseTranslation` inner entity of the `Exercise` aggregate (`Id`, `LanguageCode`, `Name`), at most one per language
- New aggregate methods `SetTranslation(LanguageCode, ExerciseName)` (add or replace) and `RemoveTranslation(LanguageCode)` (no-op when absent)
- New `Muscle` on the exercise, stored and validated against `Muscles`. `BodyPart` stays on the aggregate and in responses but is derived from `Muscle` through a domain mapper and is never stored or accepted
- New optional, unique, immutable `FreeExerciseDbId` (string, 1 to 128 characters) holding the `id` of the matching `infra/exercises.json` entry
- New value objects `LanguageCode` (lowercase ISO 639-1 two-letter code) and `FreeExerciseDbId`
- **BREAKING** `Exercises` drops `Name` and `BodyPart`, adds `Muscle` and `FreeExerciseDbId`. There is no data to lose
- **BREAKING** `Exercise` constructor, `Create` and `Update` no longer take a name and take `Muscles` instead of `BodyParts`. `Create` also takes `FreeExerciseDbId?`. `Rehydrate` takes `FreeExerciseDbId?`, `Muscles` and the list of translations
- **BREAKING** `POST`/`PUT /exercises` request: `name` and `bodyPart` are replaced by `muscle` and `translations`, and `freeExerciseDbId` is added (used only on `POST`). The response replaces `name` with `translations` and adds `muscle` and `freeExerciseDbId`
- **BREAKING** `GET /workouts` entries replace the embedded exercise `name` with `translations` and add `muscle` next to `bodyPart`
- New errors: `400` for an invalid muscle, language code or `FreeExerciseDbId` and for duplicate languages in a request. `409` for a `FreeExerciseDbId` already used by another exercise. `InvalidBodyPartException` is removed, since body part is no longer an input
- Seeds give the four seeded exercises a muscle, `es` and `en` translations and a `FreeExerciseDbId`

## Capabilities

### New Capabilities

- `exercises`: the exercise definition read and written through `/exercises`: its translations, muscle, derived body part, free exercise dataset id, and how that data is embedded in workout listings. Canonical template reference: `WeightTracker.Domain/Workouts` (aggregate with an inner entity and value objects), with `WorkoutRepository` and `WorkoutMapper` for the multi-table persistence

### Modified Capabilities

None. `workout-retrieval` already excludes exercise data from `GET /workouts/{id}`, and that stays true.

## Module Classification

- Exercises -> mutable aggregate. Its public reads are anonymous, but admins create, update and delete it, so it is not a read-only catalog. Reference: `WeightTracker.Domain/Workouts`
- ExerciseTranslation -> inner entity of the Exercise aggregate, with no repository. Reference: `WeightTracker.Domain/Workouts/WorkoutExercise.cs`
- Workouts -> mutable aggregate, unchanged in the domain. Only the `GET /workouts` response shape changes

## Business Identifiers And Mutability

- `ExerciseTranslation.Id`: technical identifier from `Id.New()`, immutable. Replacing a name keeps the `Id`
- (`ExerciseId`, `LanguageCode`): unique key of a translation, immutable. Changing a translation's language means removing it and setting a new one
- `Exercise.FreeExerciseDbId`: optional, unique when present, immutable after creation. It is a string from an external dataset, so it does not use `Id`. The brief approves this exception

## Conflicts With Template Or Invariants

- The invariants ask that identifier-specific abstractions exist only when the plan requires a specific format. `FreeExerciseDbId` and `LanguageCode` each have one (a string of up to 128 characters, and a two-letter lowercase code), so they are separate value objects
- The invariants warn against treating foreign identifiers as uniqueness pre-checks. `FreeExerciseDbId` is not a reference to another aggregate: the brief requires it to be unique among exercises, so the pre-check on create is part of the plan rather than an implicit rule
- No other deviations from the mapped references are allowed

## Impact

- Domain: `Domain/Exercises` (`Exercise`, new `ExerciseTranslation`, `LanguageCode`, `FreeExerciseDbId`, muscle-to-body-part mapper; `Muscles` already exists), `Domain/Exceptions`
- Application: `UseCases/Exercises` (create, update, DTO), `Interfaces/IExerciseRepository`, `Exceptions`
- Infrastructure: `ExerciseEntity`, new `ExerciseTranslationEntity`, `ExerciseMapper`, `ExerciseRepository`, `WeightTrackerDbContext`, `Seed/DatabaseSeeder` and `SeedData`
- Migrations: one new FluentMigrator migration with a reversible `Down()`
- API: `ExerciseRequest`, `ExerciseResponse`, `WorkoutResponse`, `ExceptionMapper`
- Template parity: `.stpr/template` copies of `Exercise.cs`, `BodyParts.cs` and any other changed copied file are updated, and `Muscles.cs` is added
- Clients: the frontend and backoffice read `name` and send `bodyPart` today, so they have to follow the new shape. That work is not part of this change
- Tests: unit and integration suites that build, map, persist or read exercises
