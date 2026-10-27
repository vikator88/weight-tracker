## Change

Add exercise translations and replace the stored body part with a muscle

## Objective

An exercise name is a single Spanish string today, and the exercise stores a coarse `BodyPart`. We want:

- exercise names stored per language in a new `ExerciseTranslations` table, so the catalog can be served in several languages
- `Muscle` as the stored classification of an exercise. `BodyPart` stays on the aggregate and in the API, but it is derived from the muscle and is not persisted

There is no data in `Exercises` yet, so dropping columns loses nothing. Only the seeds change.

## Capabilities

- Store and restore an exercise with its list of translations (`LanguageCode`, `Name`)
- Add or replace a translation for a language, and remove a translation for a language
- Store `Muscle` on the exercise and derive `BodyPart` from it through a muscle-to-body-part mapper
- Store an optional `FreeExerciseDbId` on the exercise, linking it to its entry in `infra/exercises.json`
- Return both `Muscle` and `BodyPart`, plus the translations, from the exercise API and from the workout responses that embed exercise data

## Module Classification

- Exercises -> mutable aggregate (exposed through GET/POST/PUT/DELETE, state changes) -> `WeightTracker.Domain/Workouts` (template)
- ExerciseTranslation -> inner entity of the Exercise aggregate, has no repository of its own -> `WeightTracker.Domain/Workouts/WorkoutExercise.cs`
- Workouts -> mutable aggregate, only affected in how its responses read exercise data -> no domain change

## Canonical Template Mapping

- Domain: Exercise -> `src/WeightTracker.Domain/Exercises/Exercise.cs` -> the aggregate we are changing (template copy in `.stpr/template/src/WeightTracker.Domain/Exercises/Exercise.cs` must be kept identical)
- Domain: ExerciseTranslation -> `WeightTracker.Domain/Workouts/WorkoutExercise.cs` -> nearest inner entity owned by an aggregate
- Domain: translation methods on Exercise -> `WeightTracker.Domain/Workouts/Workout.cs` (add/remove of `WorkoutExercise`) -> nearest aggregate managing a child collection
- Domain: LanguageCode -> `src/WeightTracker.Domain/Exercises/Url.cs` -> nearest value object in the folder
- Domain: FreeExerciseDbId -> `src/WeightTracker.Domain/Exercises/Url.cs` -> nearest string value object in the folder
- Domain: Muscles -> `src/WeightTracker.Domain/Exercises/Muscles.cs` (already exists, untracked) -> enum used as input, mirrors `BodyParts.cs`
- Domain: muscle-to-body-part mapper -> `src/WeightTracker.Domain/Exercises/` (new static class, no closer reference) -> pure domain rule, total over every `Muscles` value
- Application: Create/Update exercise -> `src/WeightTracker.Application/UseCases/Exercises/CreateExerciseUseCase.cs`, `UpdateExerciseUseCase.cs`, `ExerciseDTO.cs` -> the use cases being changed
- Infrastructure: entities -> `src/WeightTracker.Infra/Entities/ExerciseEntity.cs` (changed) and new `ExerciseTranslationEntity.cs`, mirroring `WorkoutExerciseEntity.cs`
- Infrastructure: mapper -> `src/WeightTracker.Infra/Mappers/ExerciseMapper.cs`, following `WorkoutMapper.cs` -> multi-table aggregate mapper
- Infrastructure: repository -> `src/WeightTracker.Infra/Repositories/ExerciseRepository.cs`, following the child-collection sync in `WorkoutRepository.cs` -> insert/update/delete of child rows
- Infrastructure: DbContext -> `src/WeightTracker.Infra/Persistence/WeightTrackerDbContext.cs` -> new `ExerciseTranslations` set and configuration
- Infrastructure: seed -> `src/WeightTracker.Infra/Seed/DatabaseSeeder.cs`, `SeedData.cs` -> exercises seeded with a muscle and translations
- Migration -> new `src/WeightTracker.Migrations/Migrations/<timestamp>_AddExerciseTranslationsAndMuscle.cs`, following `202610161647_AddSuperSeriesIdToWorkoutExercises.cs` (alter table) and `202610080938_CreateWorkoutExercisesTable.cs` (child table with FK) -> one migration for the whole change
- API: exercises -> `src/WeightTracker.Api/Endpoints/Exercises/ExercisesController.cs`, `Dtos/ExerciseRequest.cs`, `Dtos/ExerciseResponse.cs`
- API: workouts -> `src/WeightTracker.Api/Endpoints/Workouts/Dtos/WorkoutResponse.cs` -> currently reads `exercise.Name` and `exercise.BodyPart`
- API: errors -> `src/WeightTracker.Api/Common/ExceptionMapper.cs` -> new domain exceptions map to `400`

## State And Mutability

- `Exercises` table: drop `Name`, drop `BodyPart`, add `Muscle` (`int`, not null), add `FreeExerciseDbId` (string, length 128, nullable) with a unique index that ignores nulls
- New `ExerciseTranslations` table: `Id` (guid, PK, `ValueGeneratedNever`), `ExerciseId` (guid, FK to `Exercises.Id`, cascade delete), `LanguageCode` (string), `Name` (string, length aligned with `ExerciseName.MaxLength` = 150). Unique index on (`ExerciseId`, `LanguageCode`)
- `Exercise` aggregate:
  - `Muscle` is new, settable and persisted, validated as a defined `Muscles` value
  - `BodyPart` becomes read-only and derived from `Muscle` through the mapper. It is never stored and never received as input
  - `FreeExerciseDbId` is new, optional, persisted, and set only at creation
  - `Name` is removed. `Translations` is a new read-only collection of `ExerciseTranslation`
- Constructor, `Create` and `Update` stop receiving a name and receive `Muscles` instead of `BodyParts`. The constructor and `Create` also receive `FreeExerciseDbId?`; `Update` does not
- `Rehydrate(Id id, FreeExerciseDbId? freeExerciseDbId, Url? videoUrl, Muscles muscle, IEnumerable<ExerciseTranslation> translations)`
- `ExerciseTranslation` is a new entity in `Domain/Exercises` with `Id`, `LanguageCode` and `Name` (`ExerciseName`). The `ExerciseId` exists only as a column; the entity is reached through its aggregate
- Repository save synchronises the child rows: inserts translations new to the aggregate, updates the name of existing ones, deletes rows whose language is no longer in the aggregate
- Migration `Down()` drops `ExerciseTranslations`, drops the `FreeExerciseDbId` index and column, drops `Muscle` and re-adds `Name` and `BodyPart` as they were. Data is not preserved in either direction

## Business Rules

- `SetTranslation(LanguageCode languageCode, ExerciseName name)` adds a translation when the language has none, and replaces the name when it already has one. The name proposed in the request (`InsertOrUpdateTranslation`) is replaced by `SetTranslation`, since "set" already means upsert and matches `RemoveTranslation`
- `RemoveTranslation(LanguageCode languageCode)` removes the translation for that language and does nothing when there is none. It never throws
- An exercise has at most one translation per language code. The aggregate guarantees it; the unique index backs it up
- A new translation gets its identifier from `Id.New()`. Replacing the name keeps the existing translation `Id`
- An exercise may have zero translations
- `LanguageCode` is a lowercase ISO 639-1 two-letter code (`es`, `en`). It is trimmed and lowercased on input; anything else throws `InvalidLanguageCodeException`
- `FreeExerciseDbId` is the `id` of an entry in `infra/exercises.json` (for example `Barbell_Squat`). It is trimmed, and it must not be empty or longer than 128 characters, or `InvalidFreeExerciseDbIdException` is thrown. The longest id in the file today is 58 characters; 128 leaves headroom. The domain does not check that the id exists in the file
- Two exercises cannot share a `FreeExerciseDbId`. A duplicate on create is a `409` (`FreeExerciseDbIdAlreadyExistsException` in Application, checked through the repository before saving; the unique index backs it up)
- `Muscle` must be a defined `Muscles` value or `InvalidMuscleException` is thrown, before any state is changed
- `BodyPart` always equals the mapper output for the current `Muscle`. The mapper covers every `Muscles` value:

  | Muscle | BodyPart |
  | --- | --- |
  | ABDOMINALS | CORE |
  | ABDUCTORS, ADDUCTORS, CALVES, GLUTES, HAMSTRINGS, QUADRICEPS | LEGS |
  | BICEPS, FOREARMS, TRICEPS | ARMS |
  | CHEST | CHEST |
  | LATS, LOWER_BACK, MIDDLE_BACK, TRAPS | BACK |
  | NECK | NECK |
  | SHOULDERS | SHOULDERS |

## Roles And Visibility

- No change. Exercise endpoints keep their current authorization, and translations follow the same rules as the exercise that owns them

## Business Identifiers And Mutability

- `ExerciseTranslation.Id` is a technical identifier generated with `Id.New()`, immutable after creation
- `Exercise.FreeExerciseDbId` is an optional unique external identifier, immutable after creation. It is a string from an external dataset, so it does not use `Id`; this exception is approved here
- (`ExerciseId`, `LanguageCode`) is the unique business key of a translation and is immutable. Changing the language of a translation means removing it and setting a new one

## Out Of Scope

- Dedicated translation endpoints (for example `PUT /exercises/{id}/translations/{languageCode}`)
- Picking a single name from `Accept-Language` or a query parameter. Responses return every translation
- Importing `../infra/exercises.json` into the catalog, and validating `FreeExerciseDbId` against it
- Changing or clearing `FreeExerciseDbId` after creation
- Storing secondary muscles. One muscle per exercise

## Notes

- API shape:
  - `ExerciseRequest` replaces `Name` and `BodyPart` with `Muscle` and `Translations: [{ languageCode, name }]`, and adds an optional `FreeExerciseDbId` that only `POST` uses. `PUT` ignores it, since the value is immutable. `POST` calls `SetTranslation` for each entry. `PUT` makes the stored set match the request: it calls `SetTranslation` for each entry and `RemoveTranslation` for languages not in the request. Duplicate language codes in one request are a `400`
  - `ExerciseResponse` becomes `{ id, freeExerciseDbId, videoUrl, muscle, bodyPart, translations: [{ languageCode, name }] }`, with enum values as strings as today
  - In `WorkoutResponse`, the embedded exercise replaces `name` with `translations` and adds `muscle` next to `bodyPart`
- Exceptions: add `InvalidMuscleException`, `InvalidLanguageCodeException` and `InvalidFreeExerciseDbIdException` in `Domain/Exceptions`, mapped to `400`, and `FreeExerciseDbIdAlreadyExistsException` in `Application/Exceptions`, mapped to `409` in `ExceptionMapper`. `InvalidBodyPartException` is no longer reachable and is removed together with its mapping and tests
- `ExerciseMapper.MapToDomain` throws `PersistenceMappingException` for an unknown stored muscle or an invalid stored translation, as it does today for body parts
- Repository reads (`GetById`, `GetAll`, the exercise lookup used by workout reads) load translations
- Seeds: the four seeded exercises keep their `SeedData` ids, get a `FreeExerciseDbId` (`Barbell_Bench_Press_-_Medium_Grip`, `Barbell_Squat`, `Plank`, `Barbell_Curl`, all present in the file), a muscle (Press Banca -> CHEST, Sentadilla -> QUADRICEPS, Plancha -> ABDOMINALS, Curl de Bíceps -> BICEPS) and `es` and `en` translations with deterministic ids in `SeedData`. Plancha moves from BACK to CORE, so tests asserting its body part change with it
- The `.stpr/template` copies of `Exercise.cs` and `BodyParts.cs` are updated together with `src`, and `Muscles.cs` is added to the template, so the parity check stays clean
- Tests:
  - Unit: `Domain/Exercises/ExerciseTests` (create, rehydrate, update, muscle validation, derived body part, `SetTranslation` add and replace, `RemoveTranslation` present and absent), new `LanguageCodeTests` and `FreeExerciseDbIdTests`, new mapper test covering every `Muscles` value, `Application/Exercises/*UseCaseTests`, `Infra/Mappers/AggregateMapperTests` round-trip with translations, plus every test building an `Exercise` (`WorkoutTests`, `EntityEqualityTests`, workout use case tests)
  - Integration: `Repositories/ExerciseRepositoryTests` (insert, rename, remove translation rows, `FreeExerciseDbId` round-trip and uniqueness lookup), `Exercises/*EndpointTests` (new request and response shape, `400` for invalid muscle, invalid language code, duplicate languages and invalid `FreeExerciseDbId`; `409` for a duplicate `FreeExerciseDbId` on `POST`; `PUT` leaves it unchanged), `Workouts/WorkoutResponseShapeTests` and other workout tests reading exercise name or body part, `Migrations/MigrationsTests` (up and down)
  - Existing auth matrices for exercise routes stay as they are
- No deviations from the mapped template references are allowed
