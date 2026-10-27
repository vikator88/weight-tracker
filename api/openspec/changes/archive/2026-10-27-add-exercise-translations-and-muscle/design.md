# Design

## Context

See `proposal.md` for the reasons and `specs/exercises/spec.md` for the behavior. This section only describes the current state.

- `Exercise` (`Domain/Exercises/Exercise.cs`) is an aggregate root with `Name`, `VideoUrl` and a validated `BodyPart`, persisted to one table, `Exercises`, through `ExerciseMapper` and `ExerciseRepository`. `Muscles.cs` already exists in the folder and is unused
- `Workout` is the template's aggregate with a child collection: a private `List<WorkoutExercise>` exposed as `IReadOnlyList`, children built with `Create`/`Rehydrate`, and mutations that go through aggregate methods. `WorkoutRepository` loads children with `Include` and reconciles them on save by entity `Id`
- `POST`, `PUT` and `DELETE /exercises` require `ADMIN`. Both `GET` routes are `[AllowAnonymous]`
- `GET /workouts` builds `WorkoutExerciseResponse` from the exercise (`Name`, `BodyPart`). `GET /workouts/{id}` carries no exercise data
- Uniqueness pre-checks use `ExistsBy<Criteria>` on the repository (`IUserRepository.ExistsByEmail`)

## Goals / Non-Goals

**Goals:**
- Mirror `Workout`/`WorkoutExercise` for `Exercise`/`ExerciseTranslation`, and `WorkoutRepository.Reconcile` for persisting translations
- Keep `BodyPart` a pure function of `Muscle` in the domain, so no layer can store or accept one that disagrees with the muscle

**Non-Goals:**
- Changing exercise authorization, delete semantics or the `ExerciseInUseException` rule
- Choosing a translation by caller language
- Any frontend or backoffice change

## Decisions

### Domain (`WeightTracker.Domain/Exercises`)

- **`ExerciseTranslation : Entity`** with `Id`, `LanguageCode LanguageCode` and `ExerciseName Name { get; private set; }`. It has `static Create(LanguageCode, ExerciseName)` (`Id.New()`), `static Rehydrate(Id, LanguageCode, ExerciseName)`, and `internal void Rename(ExerciseName)`, so only the aggregate can change it, as with `WorkoutExercise.Replace`. It has no `ExerciseId` property: like `WorkoutExercise` it is reached through its root, and the foreign key exists only on the persistence row
- **`ExerciseName`** is reused unchanged as the translation name (3 to 150 characters). Its doc comment that points to `exercises.name` is updated to `ExerciseTranslations.Name`
- **`LanguageCode`** is a sealed record value object like `Url`. `From` trims and lowercases the value and accepts exactly two ASCII letters `a`–`z`. `MaxLength = 2`. Otherwise it throws `InvalidLanguageCodeException`. It is not checked against a list of real ISO codes: the format is enough, and a full list would be a maintenance burden with no rule that depends on it. A shared `Common` value object was ruled out because nothing else uses language codes
- **`FreeExerciseDbId`** is a sealed record value object like `Url`. `From` trims the value and rejects empty or longer than `MaxLength = 128`, throwing `InvalidFreeExerciseDbIdException`. There is no format check beyond that, because the dataset ids are free text (`3_4_Sit-Up`, `Barbell_Bench_Press_-_Medium_Grip`)
- **`MuscleBodyParts`** is a static class with `BodyParts BodyPartOf(Muscles muscle)`, written as a `switch` expression with one arm per muscle and a throwing default for undefined values. The domain owns the mapping because it is a business rule, and the API only reads the result. A dictionary was considered, but the `switch` makes a missing arm visible in review, and the unit test enumerates `Enum.GetValues<Muscles>()` to catch it
- **`Exercise`**:
  - Fields: `FreeExerciseDbId? FreeExerciseDbId { get; }` (no setter), `Url? VideoUrl`, `Muscles Muscle` (validated setter, the same pattern as the current `BodyPart`), `BodyParts BodyPart => MuscleBodyParts.BodyPartOf(Muscle)`, and a private `List<ExerciseTranslation>` exposed as `IReadOnlyList<ExerciseTranslation> Translations`
  - `Create(FreeExerciseDbId?, Url?, Muscles)` starts with no translations. `Rehydrate(Id, FreeExerciseDbId?, Url?, Muscles, IEnumerable<ExerciseTranslation>)` throws `DuplicateExerciseTranslationException` when the stored list repeats a language, so a corrupt load fails loudly. `Update(Url?, Muscles)` validates the muscle before it assigns anything
  - `SetTranslation(LanguageCode, ExerciseName)` renames the existing translation for that code, keeping its `Id`, or adds `ExerciseTranslation.Create`. `RemoveTranslation(LanguageCode)` removes it when present and otherwise does nothing
  - `_bodyPart`, `EnsureDefined(BodyParts)` and `InvalidBodyPartException` are removed. `BodyParts` itself stays

### Application (`WeightTracker.Application`)

- `ExerciseDTO(string? FreeExerciseDbId, string? VideoUrl, Muscles Muscle, IReadOnlyList<ExerciseTranslationDTO> Translations)` with `ExerciseTranslationDTO(string LanguageCode, string Name)`
- The use cases convert every input to value objects before touching the aggregate, so invalid input never leaves a half-mutated aggregate. They also reject a repeated language code after normalization with `DuplicateExerciseTranslationException` (the same domain exception, mapped to `400`). Normalizing first means `ES` and `es` count as duplicates
- **Create**: when `FreeExerciseDbId` is given, call `IExerciseRepository.ExistsByFreeExerciseDbId` and throw `FreeExerciseDbIdAlreadyExistsException` (Application, `409`) when it returns true. Then `Exercise.Create`, `SetTranslation` for each entry, `Save` and a single `SaveChangesAsync`
- **Update**: load the exercise or throw `ExerciseNotFoundException`, then `Update(videoUrl, muscle)`. Call `RemoveTranslation` for each stored language that is not in the request and `SetTranslation` for each entry, then `Save` and commit. `dto.FreeExerciseDbId` is ignored. A separate update DTO was considered, but the existing create and update use cases already share one DTO, and the spec says the field is ignored
- **`IExerciseRepository`** gains `Task<bool> ExistsByFreeExerciseDbId(FreeExerciseDbId, CancellationToken)`, following the contract table

### Infrastructure (`WeightTracker.Infra`)

- `ExerciseEntity` drops `Name` and `BodyPart`, adds `string? FreeExerciseDbId`, `int Muscle` and `List<ExerciseTranslationEntity> Translations`
- New `ExerciseTranslationEntity { Guid Id; Guid ExerciseId; string LanguageCode; string Name }`
- `WeightTrackerDbContext` configures `ExerciseTranslations` with explicit column names, `HasMaxLength(LanguageCode.MaxLength)` and `HasMaxLength(ExerciseName.MaxLength)`, `Id.ValueGeneratedNever()`, `HasMany(Translations).WithOne().HasForeignKey(ExerciseId).OnDelete(Cascade)` (this navigation stays inside the aggregate) and a unique index on (`ExerciseId`, `LanguageCode`). `Exercises.FreeExerciseDbId` gets `HasMaxLength(FreeExerciseDbId.MaxLength)` and a unique index. EF does not create the schema, so these indexes only describe the constraints the migration creates
- `ExerciseMapper` maps both tables in both directions and translates enum and value-object failures into `PersistenceMappingException`, as it does today (`MapMuscle` replaces `MapBodyPart`)
- `ExerciseRepository`:
  - Every read `Include`s `Translations`, including `GetAllByIds`, which serves `GET /workouts`
  - `Save` for an existing row loads it with `Include`, copies the scalar fields and reconciles translations by `Id` as `WorkoutRepository.Reconcile` does: detach the dropped rows, update `Name` on the kept ones, add the new ones. `FreeExerciseDbId` is not copied on update, because it is immutable
  - An exercise that removes `es` and adds `es` again in one request is impossible, because `SetTranslation` keeps the `Id`. A rename never deletes and reinserts a row, so the unique index cannot collide within one commit
  - Deletes cascade to translations in the database

### Migration (`WeightTracker.Migrations`)

- One migration, `<yyyyMMddHHmm>_AddExerciseTranslationsAndMuscle`, following the style of the existing files and the PascalCase naming of the recent rename
- `Up()`:
  - On `Exercises`, delete `Name` and `BodyPart`, and add `Muscle` (`int`, not null) and `FreeExerciseDbId` (`string(128)`, nullable)
  - Add the unique constraint `UQ_Exercises_FreeExerciseDbId`. Postgres treats nulls as distinct, so exercises without a dataset id never conflict and no filtered index is needed
  - Create `ExerciseTranslations` with `PK_ExerciseTranslations`, `FK_ExerciseTranslations_Exercise` (cascade on delete), `LanguageCode` `string(2)`, `Name` `string(150)`, and the unique constraint `UQ_ExerciseTranslations_ExerciseId_LanguageCode`. This follows the `UQ_` constraints of `CreateWorkoutExercisesTable`
- `Down()`: drop `ExerciseTranslations`, the constraint, `FreeExerciseDbId` and `Muscle`, then re-add `Name` `string(150)` and `BodyPart` `int`, both not null with defaults of `''` and `0` so it also runs on a non-empty table. The defaults are a rollback-only concession: the original columns had none
- No data migration. The table is empty, and the brief accepts the data loss

### API (`WeightTracker.Api`)

- `ExerciseRequest`: `[Required][EnumDataType(typeof(Muscles))] Muscles Muscle`, `string? VideoUrl`, `string? FreeExerciseDbId`, and `List<ExerciseTranslationRequest> Translations = []`, where `ExerciseTranslationRequest` has `[Required] LanguageCode` and `[Required] Name`. Only structural validation lives here; formats and duplicates are enforced in the domain and the use case
- `ExerciseResponse(Guid Id, string? FreeExerciseDbId, string? VideoUrl, string Muscle, string BodyPart, IReadOnlyList<ExerciseTranslationResponse> Translations)`
- `WorkoutExerciseResponse` replaces `Name` with `Translations` and adds `Muscle`
- `ExceptionMapper`: `InvalidMuscleException`, `InvalidLanguageCodeException`, `InvalidFreeExerciseDbIdException` and `DuplicateExerciseTranslationException` map to `400`. `FreeExerciseDbIdAlreadyExistsException` maps to `409`. The `InvalidBodyPartException` mapping is removed
- No route or authorization changes. `ProducesResponseType(409)` is added to `POST /exercises`

### Seed

- `SeedData` gains deterministic ids for eight translations (`es` and `en` for each of the four exercises)
- `DatabaseSeeder` builds each exercise with `Exercise.Rehydrate(..., FreeExerciseDbId.From(...), null, muscle, [ExerciseTranslation.Rehydrate(...)])`, using the values in the brief

### Template parity

- `.stpr/template/src/WeightTracker.Domain/Exercises/Exercise.cs` is copied from `src` after the change, and `Muscles.cs` is added to the template. `BodyParts.cs` already carries a local uncommitted change in `src`, so its template copy is synced too
- `Workout.cs` and `WorkoutTests.cs` have template copies. If either changes because of the new `Exercise` signatures, its copy is updated byte for byte

## Risks / Trade-offs

- [The frontend and backoffice break on the new shapes] → This is called out as **BREAKING** in the proposal. Those clients are updated in their own change
- [A race between two creates with the same `FreeExerciseDbId` passes the pre-check] → The unique index rejects the second commit. That surfaces as a `500` rather than a `409`, which is acceptable for an admin-only route with no expected concurrency
- [`LanguageCode` accepts well-formed codes that are not real languages, such as `zz`] → Accepted. Nothing depends on the language being real
- [The mapping has two judgment calls (`TRAPS → BACK`, `LOWER_BACK → BACK`)] → They are stated in the spec scenario. Changing them later only touches `MuscleBodyParts` and its test, since body part is not stored

## Migration Plan

1. Deploy the migration. `Up()` runs on an empty `Exercises` table
2. Run the seeder in Development
3. Rollback: `dotnet run --project src/WeightTracker.Migrations -- "<conn>" --down` or a down to the previous version. Translations, muscles and dataset ids are lost
