## Change

Reorder the exercises of a workout

## Objective

A workout keeps its exercises in the order they must be performed, but today the only way to change that order
is to remove exercises and add them again. Trainers (and athletes on their own plans) need to move exercises
around in one call, sending the full order they want.

## Capabilities

- Reorder every exercise of a workout in a single request by sending the complete list of catalog exercise ids in the desired order.
- Refuse the reorder when the list is not exactly the set of exercises the workout already holds.

## Module Classification

- Workouts -> mutable aggregate -> `.stpr/template/` workout reference (`Domain/Workouts/Workout.cs`)
- Application: Workouts -> use case -> `WeightTracker.Application/UseCases/Workouts`
- Api: Workouts -> endpoint -> `WeightTracker.Api/Endpoints/Workouts`

## Canonical Template Mapping

- `Workout.ReorderExercises` -> `Workout.CreateSuperSeries` -> action method over a list of entry identifiers, validated before mutating
- `ReorderWorkoutExercisesUseCase` -> `CreateSuperSeriesUseCase` -> load, `WorkoutEditAuthorizer.EnsureCanEdit`, aggregate action, save
- `PUT /workouts/{workoutId}/exercises/order` -> `WorkoutsController.ChangeStatus` -> PUT on a workout sub-resource returning `WorkoutDetailResponse`
- `ReorderWorkoutExercisesRequest` -> `CreateSuperSeriesRequest` -> structural `[Required]` list of guids

## State And Mutability

- The order of `WorkoutExercise` entries inside the `Workout` aggregate changes. Nothing else does: effort, sets and superseries membership are untouched.
- Order is already persisted in `workout_exercises.Position`, written by `WorkoutMapper` from the aggregate list order and reconciled by `WorkoutRepository`. No migration and no schema change.

## Business Rules

- The reorder is accepted only when the received list has the same number of ids as the workout has exercises and contains exactly the same exercise ids, each once.
- A list with missing, extra, unknown or duplicated ids is refused and the workout keeps its current order (validate before mutating).
- Ids are catalog `exerciseId` values, consistent with `PUT/DELETE /workouts/{workoutId}/exercises/{exerciseId}`. A catalog exercise appears at most once per workout, so it identifies the entry unambiguously.
- Reordering is a content edit: a started or finished workout is locked for everyone but an admin.

## Roles And Visibility

- Workout edit rule from `STPR_INVARIANTS.md`: admin always; the prescribing trainer if the workout has one; otherwise the owner. Enforced through `WorkoutEditAuthorizer.EnsureCanEdit`.

## Business Identifiers And Mutability

- No identifier is created or changed. `WorkoutExercise.Id` and `ExerciseId` stay immutable.

## Errors

- Mismatching list -> new domain exception `WorkoutExerciseOrderMismatchException` -> `409 Conflict` (the caller's view of the workout is stale), mapped explicitly in `ExceptionMapper`.
- Missing workout -> `404`. Caller not allowed -> `403`. Locked workout -> `409`. Missing body or `exerciseIds` -> `400`.

## Tests

- Unit (domain): reorder succeeds and keeps effort, sets and superseries; refused on fewer ids, more ids, unknown id, duplicated id; order unchanged after a refusal; empty list on an empty workout is accepted.
- Unit (use case): not found, unauthorized, locked, success saves and commits.
- Integration: success persists the new order (read back via `GET /workouts/{id}`); `401` no token; `401` invalid token; `403` owner of a prescribed workout and unrelated trainer; `404` missing workout; `400` missing `exerciseIds`; `409` mismatching list; `409` locked workout for non-admin; admin can reorder a locked workout.

## Out Of Scope

- Partial moves (move one exercise up/down). The full order is always sent.
- Keeping superseries members contiguous. No such rule exists today and none is added here.
- Frontend and E2E tests.

## Notes

- Endpoint: `PUT /workouts/{workoutId}/exercises/order`, body `{ "exerciseIds": ["guid1", "guid2", ...] }`, response `200` with `WorkoutDetailResponse`.
- No deviations from the mapped references are allowed.
