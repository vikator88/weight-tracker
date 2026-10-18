# Design

## Context

- `Workout` (template-backed, mutable aggregate root) already has `AddExercise(exerciseId, effort, sets)` and `RemoveExercise(exerciseId)`. Both key on the **catalog** exercise id. `RemoveExercise` already handles a superseries that would be left with one member. It has no update path, and `WorkoutExercise.Effort` and `_sets` cannot be changed after creation.
- `SetPrescription(count, target)` and `SetTarget.{Reps, Duration, MaxReps}` are records with no validation. Nothing stops an empty set list.
- `WorkoutRepository.Save` → `Reconcile` already drops entries missing from the aggregate (sets cascade with the entry) and clears and re-adds the sets of every surviving entry. `WorkoutMapper.MapToEntity` gives each set row a new `Id.New()`. So a domain update already produces new set row identities, and Infra needs no persistence change.
- `CreateSuperSeriesUseCase` is the reference shape for editing a workout. It loads the workout (404), calls `WorkoutEditAuthorizer.EnsureCanEdit` (403), runs the aggregate action, then `Save` and one `SaveChangesAsync`.
- Responses already render effort with `Effort.ToString()` and target types with string literals in `SetTargetResponse`. `WorkoutMapper` keeps private `RepsTarget`/`DurationTarget`/`MaxRepsTarget` constants.
- No global `JsonStringEnumConverter` is registered.

## Goals / Non-Goals

**Goals:**
- Three protected routes that add, replace and remove a workout exercise, with every business rule in the aggregate and permission in `WorkoutEditAuthorizer`.
- One shared source for the set target type names.
- Domain validation of set prescriptions.

**Non-Goals:**
- Reordering entries, changing a workout's date, owner or trainer.
- Changing the workout edit rule. The source doc's "redefinition" is the current rule.
- Schema changes, migrations, or new seed rows beyond what the tests strictly need.
- Changing how responses serialize effort.

## Decisions

### Layer mapping

| Layer | Change |
| --- | --- |
| Domain `Workouts/` | `SetTargetTypes` (new, `public static class` with `public const string Reps = "Reps"`, `Duration`, `MaxReps`). `Workout.UpdateExercise(Id exerciseId, Effort effort, IEnumerable<SetPrescription> sets)`. `WorkoutExercise` gains `internal void Replace(Effort, IEnumerable<SetPrescription>)`, and `Effort` gets a private setter. Validation in `SetPrescription`, `SetTarget.Reps`, `SetTarget.Duration` and the `WorkoutExercise` constructor. |
| Domain `Exceptions/` | `InvalidSetPrescriptionException` (count or target `< 1`) and `WorkoutExerciseRequiresSetsException` (empty list). Both extend `DomainException`. |
| Application `UseCases/Workouts/` | `AddWorkoutExerciseUseCase`, `UpdateWorkoutExerciseUseCase`, `RemoveWorkoutExerciseUseCase`. |
| Application `Exceptions/` | `ExerciseIdMismatchException` (route vs body on PUT). |
| Infra | `WorkoutMapper` replaces its private constants with `SetTargetTypes`. No repository, EF or migration change. |
| Api `Endpoints/Workouts/` | Three actions on `WorkoutsController`. `Dtos/AddOrUpdateExerciseRequest.cs` with nested `SetPrescriptionRequest`. `SetTargetResponse` uses `SetTargetTypes`. |
| Api `Common/ExceptionMapper` | `InvalidSetPrescriptionException`, `WorkoutExerciseRequiresSetsException` and `ExerciseIdMismatchException` → 400. Existing mappings already cover `ExerciseNotFoundException` and `ExerciseNotInWorkoutException` → 404 and `ExerciseAlreadyInWorkoutException` → 409. |
| `Program.cs` | Register the three use cases as scoped. |

Cross-module dependency: `AddWorkoutExerciseUseCase` reads `IExerciseRepository.GetById` to check that the catalog reference exists. That rule spans two aggregates, so it belongs in Application. The Workouts domain still references `Exercise` by `Id` only.

### Aggregate update replaces in place

`Workout.UpdateExercise` finds the entry by catalog `ExerciseId` (`ExerciseNotInWorkoutException` if it is missing). When the entry has a `SuperSeriesId` and the new total set count differs from the totals of the other members, it throws `SuperSeriesSetCountMismatchException` **before** mutating anything. Otherwise it calls `entry.Replace(effort, sets)`. The entry id, position and superseries are kept. Remove-then-add was rejected because it would move the entry to the end and silently break its superseries.

`Replace` validates in the same order as the constructor (`Enum.IsDefined`, non-empty sets) before it assigns, so a failed update leaves the entry untouched. `ExerciseId` stays get-only: it is immutable.

### Set prescription validation lives in the value objects

- `SetPrescription`: `count < 1` → `InvalidSetPrescriptionException`.
- `SetTarget.Reps` / `SetTarget.Duration`: `value < 1` → `InvalidSetPrescriptionException`. `Duration` stays in seconds.
- `WorkoutExercise` constructor (covers `Create`, `Rehydrate` and, through `Replace`, update): an empty set list → `WorkoutExerciseRequiresSetsException`.

Validating in the records means a bad value can never be constructed, on any path. Rehydrate failures are already wrapped by `WorkoutMapper` as `PersistenceMappingException`. Seed data only uses positive values, so this has no effect on it. `SeedDataTests` stays green, and the implementer must run it to confirm.

### Use case shapes (mirror `CreateSuperSeriesUseCase`)

- **Add** `Execute(callerId, callerRole, workoutId, exerciseId, effort, sets, ct)` → `WorkoutExercise`. Steps: load workout (404) → `EnsureCanEdit(..., "POST /workouts/{workoutId}/exercises")` (403) → `_exercises.GetById` (`ExerciseNotFoundException`, 404) → `workout.AddExercise` → `Save` → `SaveChangesAsync`. Returns the new entry, `workout.Exercises.Last()`.
- **Update** `Execute(callerId, callerRole, workoutId, routeExerciseId, bodyExerciseId, effort, sets, ct)` → `WorkoutExercise`. The id mismatch check comes first and throws `ExerciseIdMismatchException`. Then: load (404) → authorize (403) → `workout.UpdateExercise` → save → commit. Returns the entry. The catalog existence check is skipped, because the exercise is already in the workout.
- **Remove** `Execute(callerId, callerRole, workoutId, exerciseId, ct)`. Steps: load (404) → authorize (403) → `workout.RemoveExercise` → save → commit.

None of the use cases restate the edit rule inline.

### API contract

- Routes use the existing `{workoutId:guid}` naming. `POST {workoutId:guid}/exercises` → `201` + `WorkoutExerciseDetailResponse` + `Location` from `CreatedAtAction(nameof(GetById), new { id = workoutId })`. `PUT {workoutId:guid}/exercises/{exerciseId:guid}` → `200` + `WorkoutExerciseDetailResponse`. `DELETE` same route → `204`.
- `[ProducesResponseType]` on each action: 400, 401, 403, 404 on all three, and 409 on POST.
- `AddOrUpdateExerciseRequest` takes structural validation only:
  - `[Required] Guid? ExerciseId`
  - `[Required, JsonConverter(typeof(JsonStringEnumConverter<Effort>))] Effort? Effort`. The converter reads names and integers alike, and an undefined integer reaches the domain and fails as `InvalidEffortException` (400). Putting the converter on the property leaves every other enum's serialization as it is.
  - `[Required, MinLength(1)] IReadOnlyList<SetPrescriptionRequest>? Sets`
  - `SetPrescriptionRequest`: `[Required] int? Count`, `[Required, AllowedValues(SetTargetTypes.Reps, SetTargetTypes.Duration, SetTargetTypes.MaxReps)] string? TargetType`, `int? Target` (ignored for `MaxReps`).
  - `ToDomain()` on `SetPrescriptionRequest` builds the `SetPrescription` and switches on the constants. This keeps conversion next to the DTO, the way `From` sits next to the response DTOs. The domain still owns the value rules (count and target `>= 1`).
- The structural `MinLength(1)` and the domain's `WorkoutExerciseRequiresSetsException` both return 400. The domain rule is still needed, because domain code must not trust its callers.

### Identifier roles (POST / PUT / DELETE)

- `workoutId` (route): aggregate lookup → 404.
- `exerciseId` (route, PUT/DELETE): catalog id of an entry already in the workout → `ExerciseNotInWorkoutException` 404. This is not a superseries entry id.
- `exerciseId` (body): POST → catalog existence check (404) and duplicate check in the aggregate (409). PUT → must equal the route value (400).

### Tests

- **Unit, Domain** (`Domain/Workouts`): `WorkoutTests` gains UpdateExercise cases (replaces effort and sets, keeps id, position and superseries, rejects an absent exercise, rejects a superseries total mismatch without mutating, accepts the same total). `WorkoutExerciseTests` gets the empty-sets case. New `SetPrescriptionTests` / `SetTargetTests` cover count and value bounds. Every test is labelled Arrange / Act / Assert.
- **Unit, Application** (`Application/Workouts`): one file per use case, mirroring `CreateSuperSeriesUseCaseTests`. Cover 404, 403 through the authorizer, catalog 404 (add), mismatch 400 (update), happy path verifying `Save` and one `SaveChangesAsync`, and no commit on failure.
- **Unit, Infra** (`Infra/Mappers`): `WorkoutMapperTests` round-trips all three target types through `SetTargetTypes`.
- **Integration** (`Workouts/`): `WorkoutExerciseAddEndpointTests`, `WorkoutExerciseUpdateEndpointTests`, `WorkoutExerciseRemoveEndpointTests`, structured like `SuperSeriesBreakEndpointTests`, with the full per-route matrix from the spec. DB-level assertions (set rows removed on DELETE, set row ids replaced on PUT) go through `Factory.WithScope` and read `WeightTrackerDbContext.WorkoutExerciseSets`, so no ApiFactory change is needed. `WorkoutExerciseFlowTests` covers create → add two → get by id.
- **Seed**: the existing data is enough. `OwnedWorkout` (self-created, Press Banca) accepts Sentadilla or Plancha. `TrainedWorkout` (prescribed) is the 403-owner case. `TrainerOwnWorkout` is the 403 non-owner-trainer case. The superseries workouts cover the update-total and remove-member cases. `CurlBiceps` is free to add anywhere.

### Template parity

The change edits template-backed files `Workout.cs`, `WorkoutExercise.cs`, `SetPrescription.cs`, `SetTarget.cs` and `test/.../WorkoutTests.cs`. Their `.stpr/template` copies must be updated byte-identically. `SetTargetTypes.cs` and the new exceptions stay skeleton-only.

## Risks / Trade-offs

- **Validation in value-object constructors could break rehydration** of existing rows with `count = 0`. This is mitigated because seed and production data are created only through the domain and have no such rows. If a bad row did exist, reads would fail with a 500 `PersistenceMappingException` rather than return corrupt data.
- **Catalog id as the route key** differs from the superseries routes, which use entry ids. API consumers must know which id each route takes. This is documented in the action `<summary>`s, as the superseries action already does.
- **`MaxReps` silently ignoring `target`** is lenient. A client that sends a non-zero target gets no error. Requiring `0` would make this stricter, but the source doc lists `0` only as the expected value, not as a rule.
- **Effort-only PUT rewrites every set row.** This is intended (source doc) and costs little at workout scale.
