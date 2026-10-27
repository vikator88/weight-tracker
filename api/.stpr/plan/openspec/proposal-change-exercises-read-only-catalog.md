## Change

Turn exercises into a read-only catalog

## Objective

Exercises are now imported from the free exercise dataset (`ImportFreeExerciseCatalog_202610271200`), and from now on they are maintained directly in the database. The API's create, update and delete flows for exercises, and the aggregate methods behind them, are no longer used. Keeping them means maintaining write paths nobody uses and leaves a second source of truth for catalog data. This change removes every exercise write path and keeps only the reads.

## Capabilities

- `GET /exercises` and `GET /exercises/{id}` keep working exactly as today: anonymous, same response shape (`id`, `freeExerciseDbId`, `videoUrl`, `muscle`, `bodyPart`, `translations`)
- Workouts keep referencing exercises by id, and `GET /workouts` keeps embedding their translations, muscle and body part
- `POST /exercises`, `PUT /exercises/{id}` and `DELETE /exercises/{id}` are removed
- The `Exercise` aggregate can only be restored from storage. It can no longer be created or changed through the domain

## Module Classification

- Exercises -> read-only catalog -> `src/WeightTracker.Application/UseCases/Exercises` (`ExerciseUseCase`, the skeleton-backed read-only reference in `.stpr/template/README.md`)
  - This catalog is **truly read-only, not just publicly read-only**. After this change nothing in the system changes exercise state: no endpoint, no use case and no domain behavior writes it. Changes happen only outside the application, through migrations or direct database edits. So the mandatory stop cases about modules that are "read-only only because users can't edit them" don't apply
- ExerciseTranslation -> part of the read-only catalog, loaded together with its exercise. It has no behavior of its own
- Workouts -> mutable aggregate, unchanged. It references exercises by id only

## Canonical Template Mapping

- Application: exercise reads -> `src/WeightTracker.Application/UseCases/Exercises/GetAllExercisesUseCase.cs`, `GetExerciseByIdUseCase.cs` -> the read-only use case reference; both stay unchanged
- Application: repository contract -> `src/WeightTracker.Application/Interfaces/IExerciseRepository.cs` -> reduced to the read signatures of the contract table (`GetAll`, `GetAllByIds`, `GetById`)
- Domain: Exercise -> `src/WeightTracker.Domain/Exercises/Exercise.cs` -> keeps `Rehydrate` and read-only properties only. Its template copy in `.stpr/template/src` is updated to match
- Domain: ExerciseTranslation -> `src/WeightTracker.Domain/Exercises/ExerciseTranslation.cs` -> keeps `Rehydrate` only
- Infrastructure: repository -> `src/WeightTracker.Infra/Repositories/ExerciseRepository.cs` -> keeps the read queries with translations included
- Infrastructure: mapper -> `src/WeightTracker.Infra/Mappers/ExerciseMapper.cs` -> keeps `MapToDomain`, and keeps `MapToEntity` because `DatabaseSeeder` uses it
- Infrastructure: seed -> `src/WeightTracker.Infra/Seed/DatabaseSeeder.cs` -> keeps building the four seeded exercises with `Exercise.Rehydrate`; writing them directly through the `DbContext` is the access the seed is allowed to have
- API: controller -> `src/WeightTracker.Api/Endpoints/Exercises/ExercisesController.cs` -> keeps the two anonymous `GET` actions only
- API: errors -> `src/WeightTracker.Api/Common/ExceptionMapper.cs` -> drops the mappings of removed exceptions

## State And Mutability

- No exercise or translation state changes because of system behavior. The only writers are FluentMigrator migrations, direct database edits and the seeder (development and tests only)
- No schema change and no migration. The `Exercises` and `ExerciseTranslations` tables, their constraints and the imported catalog stay as they are
- The `Exercise` aggregate keeps `Id`, `FreeExerciseDbId`, `VideoUrl`, `Muscle`, the derived `BodyPart` and `Translations`, all read-only (no public or internal setters)
- `Rehydrate` keeps validating what it restores: a defined muscle and no duplicate language. Bad stored data still surfaces as `PersistenceMappingException` through the mapper

## Business Rules

- **Removed from the domain:**
  - `Exercise.Create`, `Exercise.Update`, `Exercise.SetTranslation`, `Exercise.RemoveTranslation`, and the `VideoUrl` and `Muscle` setters
  - `ExerciseTranslation.Create` and `ExerciseTranslation.Rename`
- **Kept in the domain:**
  - `Exercise.Rehydrate`, `ExerciseTranslation.Rehydrate`, `MuscleBodyParts`
  - The value objects `ExerciseName`, `LanguageCode`, `FreeExerciseDbId` and `Url`, used when rehydrating
  - `InvalidMuscleException` and `DuplicateExerciseTranslationException`, still thrown by `Rehydrate`
- **Removed from the application:** `CreateExerciseUseCase`, `UpdateExerciseUseCase`, `DeleteExerciseUseCase`, `ExerciseDTO`/`ExerciseTranslationDTO`, `ExerciseTranslationParser`, `FreeExerciseDbIdAlreadyExistsException` and `ExerciseInUseException`. Their registrations in `Program.cs` go too
- **Removed from the repository contracts:**
  - `IExerciseRepository.Save`, `Delete` and `ExistsByFreeExerciseDbId`, with their implementations, including the translation reconcile
  - `IWorkoutRepository.ExistsByExerciseId`, whose only caller is `DeleteExerciseUseCase`
- **Removed from the API:** `ExerciseRequest`/`ExerciseTranslationRequest`, and the `Create`, `Update` and `Delete` actions. `ExerciseResponse` stays
- **Kept in the API:** `ExceptionMapper` keeps the mappings of the remaining domain exceptions, including the value-object ones that can only be reached through rehydration (`InvalidLanguageCodeException`, `InvalidFreeExerciseDbIdException`, `InvalidExerciseNameException`, `InvalidUrlException`, `InvalidMuscleException`). The invariants require every custom exception to keep an explicit mapping

## Roles And Visibility

- Both `GET` routes stay `[AllowAnonymous]`, with no role restriction
- No role can create, update or delete exercises through the API anymore, ADMIN included. Requests to the removed routes get the framework's default response for an unmatched route (`404` for a removed path, `405` for a removed verb on an existing path). No custom handling is added
- `ExercisesController` keeps inheriting `AuthorizedController`, so a protected route added later gets the caller resolution by default

## Business Identifiers And Mutability

- `Exercise.Id`: immutable, assigned by the catalog migration or the seed, never generated by the application
- `Exercise.FreeExerciseDbId`: unique and immutable from the application's point of view. Only database edits outside the system may change it
- `ExerciseTranslation.Id` and (`ExerciseId`, `LanguageCode`): immutable from the application's point of view, with uniqueness enforced by `UQ_ExerciseTranslations_ExerciseId_LanguageCode`

## Out Of Scope

- Any schema or data change, including new exercises or translations
- Filtering, searching or choosing a translation by language on the `GET` routes
- An admin tool or script for editing the catalog in the database
- Frontend or backoffice changes. Any client screen that creates, edits or deletes exercises stops working and is handled in its own change

## Notes

- **OpenSpec:** the `exercises` main spec (`openspec/specs/exercises/spec.md`) has to lose these requirements:
  - "Admins create exercises with muscle, translations and dataset id"
  - "Admins replace an exercise's muscle, video and translations"
  - "The dataset id is immutable after creation"
  - "Dataset ids are unique among exercises"
  - "Exercise writes validate their input"
  - "Language codes and dataset ids are normalized"

  Scenarios in other requirements that create or update exercises (for example "Changing the muscle changes the body part" and "Body part in the request is ignored") are rewritten in terms of stored data. The `Purpose` loses "written by admins".
- **Tests removed:**
  - `ExerciseCreateEndpointTests`, `ExerciseUpdateEndpointTests`, `ExerciseDeleteEndpointTests`
  - `CreateExerciseUseCaseTests`, `UpdateExerciseUseCaseTests`, `DeleteExerciseUseCaseTests`
  - The repository `Save`/`Delete`/`ExistsByFreeExerciseDbId` cases in `ExerciseRepositoryTests`
  - The `WorkoutRepositoryTests.ExistsByExerciseId` cases
  - The `ExceptionMapperTests` cases for removed exceptions
- **Tests rewritten:**
  - `ExerciseTests` covers `Rehydrate`, the derived body part and the duplicate-language rejection
  - `EntityEqualityTests` and any test that used `Exercise.Create` move to `Rehydrate`
  - `ExerciseReadEndpointsTests` replaces the case that created an exercise through `POST` (empty translations) with one based on stored data, or drops it if no seeded exercise lacks translations
- **Tests added:** `ExerciseReadEndpointsTests` asserts that `POST /exercises`, `PUT /exercises/{id}` and `DELETE /exercises/{id}` are no longer served, even with an ADMIN token
- **Unchanged:** `AggregateMapperTests` keeps its round trip, since the seed still uses `MapToEntity`
- **Template parity:** `.stpr/template` copies of `Exercise.cs` and `IExerciseRepository.cs` are updated with `src`, and so is `IWorkoutRepository.cs` if it has a copy
- No deviations from the mapped references are allowed
