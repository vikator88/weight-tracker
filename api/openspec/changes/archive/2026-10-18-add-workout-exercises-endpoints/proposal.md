# Proposal

## Why

A workout can be created and read, and its exercises can be grouped into superseries, but there is no way to put exercises into a workout through the API. Without that, a new workout stays empty. This change adds the endpoints that add, replace and remove a workout's exercises together with their effort and set prescriptions.

Source plan: `.stpr/plan/openspec/proposal-change-add-workout-exercises-endpoints.md`.

## What Changes

- `POST /workouts/{id}/exercises` adds a catalog exercise to the end of the workout, with an effort and an ordered list of set prescriptions (for example `3x15`, or `2x12` then `2x10`). Returns `201` with the new entry.
- `PUT /workouts/{id}/exercises/{exerciseId}` replaces the entry's effort and **all** of its set prescriptions. It takes the same body as `POST` and returns `200` with the updated entry. The stored set rows are always deleted and reinserted, even when the new values match the old ones.
- `DELETE /workouts/{id}/exercises/{exerciseId}` removes the entry and its set prescriptions. Returns `204`. (The source doc lists this route as `EDIT`; we read that as `DELETE`.)
- One request DTO, `AddOrUpdateExerciseRequest`: `exerciseId` (catalog id), `effort` (enum name such as `"HIGH"` or its number such as `5`), and `sets: [{ count, targetType: Reps|Duration|MaxReps, target }]`.
- New public constants class in `WeightTracker.Domain/Workouts` for the set target type names `Reps`, `Duration` and `MaxReps`. `WorkoutMapper` (instead of its private constants), `SetTargetResponse` and request validation all use it.
- Domain additions (they **conflict with the source doc**, which says nothing changes in Domain; see Conflicts):
  - `Workout.UpdateExercise(...)` changes the effort and replaces the sets of an existing entry.
  - Set prescriptions are validated: at least one prescription, `count >= 1`, and a target value `>= 1` for `Reps` and `Duration`.
- All three use cases enforce the existing workout edit rule through `WorkoutEditAuthorizer`: admin always, the prescribing trainer if there is one, otherwise the owner.

## Conflicts And Resolved Ambiguities

These were open questions in the source doc. Each one is settled with the default below. Revise this proposal if a default is wrong.

1. **"Nothing to change in Domain"** does not hold. `WorkoutExercise.Effort` and its sets are immutable, and the aggregate has no update path. The aggregate is the gate for business rules (AGENTS.md, Adding a New Endpoint), so the update has to go through a new aggregate method. It must not be done in the use case.
2. **Route `{exerciseId}` is the catalog exercise id**, not the workout-entry id. This matches `Workout.RemoveExercise` and the rule that an exercise appears only once per workout. Superseries routes still use entry ids. On `PUT`, a body `exerciseId` that differs from the route value is rejected with `400`. To swap the exercise, delete the entry and add the new one.
3. **Update replaces in place.** The entry keeps its id, position and superseries membership. Only the effort and sets change.
4. **Superseries and update:** when the updated entry belongs to a superseries and its total set count changes, the update is rejected (`400`, `SuperSeriesSetCountMismatchException`), as `CreateSuperSeries` does.
5. **`Duration` target is in seconds**, as `SetTarget.Duration` already documents. The source doc says minutes; the domain meaning wins.
6. **`MaxReps` ignores `target`.** Any value is accepted and none is stored.
7. **Owner role redefinition** already matches the invariant and the `workout-editing` spec. Nothing about it changes. "An athlete is an owner only when there is no trainer" is the existing rule.

## Capabilities

### New Capabilities

- `workout-exercises`: adding, replacing and removing the exercises of an existing workout, with their effort and set prescriptions, including validation of set prescriptions. Canonical references:
  - Domain: `Workouts` (template-backed, `.stpr/template/src/WeightTracker.Domain/Workouts`), aggregate root with value objects, **mutable**.
  - Use cases: `CreateSuperSeriesUseCase` / `BreakSuperSeriesUseCase` (skeleton-backed), the edit-a-workout shape with `WorkoutEditAuthorizer.EnsureCanEdit`.
  - Controller: `WorkoutsController` (skeleton-backed). Errors: `ExceptionMapper` (skeleton-backed).
  - Tests: `AuthEndpointsTests` for the 401 matrix, `SuperSeriesCreateEndpointTests` / `SuperSeriesBreakEndpointTests` as the closest per-route edit tests, and `WorkoutTests` for aggregate unit tests (template-backed).

### Modified Capabilities

- None. `workout-editing` already defines the permission rule these endpoints apply. `workout-superseries` is unchanged; how updates and removals interact with superseries is specified in `workout-exercises`.

## Identifiers And Mutability

- `Workout.Id`, `WorkoutExercise.Id`: system-generated through `Id.New()`, **immutable**.
- `WorkoutExercise.ExerciseId` (catalog reference, unique within a workout): **immutable**. `PUT` cannot change it.
- `Effort` and set prescriptions of an entry: **mutable**, through `Workout.UpdateExercise` only.

## Visibility

All three routes are protected (`[Authorize]` through `AuthorizedController`). There is no role attribute, because permission is decided per workout by `WorkoutEditAuthorizer` (`403`). Responses expose the entry only (`WorkoutExerciseDetailResponse`), never domain objects.

## Acceptance Criteria

- Each route covers `401` without a token, `401` with an invalid token, `401` for a token whose user no longer exists, `403` for the owner of a prescribed workout and for a non-owner trainer, `404` for an unknown workout, and `400` for structural errors. `POST` also covers `404` for an unknown catalog exercise and `409` for a duplicate. `PUT` and `DELETE` also cover `404` for an exercise that is not in the workout.
- Each route has success cases for the prescribing trainer, the owner of a self-created workout, and an admin.
- `DELETE` leaves no set rows behind for the removed entry. This is checked against the database.
- `PUT` replaces the set rows even when only the effort changes (the old set row identities are gone and the values are equal).
- An E2E flow (create workout → add exercises → get by id) returns the added entries in order with their sets.
- Unit tests cover the new aggregate behaviour, set prescription validation, the three use cases, and the mapper using the shared constants.
- Full `dotnet test WeightTracker.slnx` passes with Docker running, and `.stpr/template` parity holds.

## Impact

- **Domain**: `Workout`, `WorkoutExercise`, `SetPrescription`, `SetTarget`, the new target-type constants class, and new exceptions for invalid set prescriptions. The template copies of the changed Workouts files must be re-synced.
- **Application**: `AddWorkoutExerciseUseCase`, `UpdateWorkoutExerciseUseCase`, `RemoveWorkoutExerciseUseCase`, and one new exception for a route/body exercise id mismatch.
- **Infra**: `WorkoutMapper` uses the shared constants. No migration and no schema change; `WorkoutRepository.Reconcile` already replaces set rows and drops removed entries.
- **Api**: three actions on `WorkoutsController`, `AddOrUpdateExerciseRequest` (with a property-level enum converter for `effort`), `ExceptionMapper` mappings, and `Program.cs` registrations.
- **Tests**: unit and integration, plus template parity for `WorkoutTests.cs`. Existing seed data (owned, trained, trainer-own and pair workouts, and the unused `CurlBiceps` exercise) is expected to be enough.
