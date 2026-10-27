# Design

## Context

See proposal.md (Why). The `Workout` aggregate keeps its `WorkoutExercise` entries in a private list, and that list's order is the performance order. `WorkoutMapper` writes each entry's list index to `workout_exercises.Position` and reads entries back ordered by it. `WorkoutRepository.Reconcile` updates `Position` on entries that already exist. The only unique constraint on `workout_exercises` is `(WorkoutId, ExerciseId)`, so nothing at the database level blocks swapping positions inside one commit.

The per-exercise routes (`PUT/DELETE /workouts/{workoutId}/exercises/{exerciseId}`) identify entries by catalog exercise id. The superseries routes use workout entry ids.

## Goals / Non-Goals

**Goals:**
- Enforce the exact-match rule in the aggregate, so no caller can store a partial or invalid order.
- Reuse the existing persistence path with no schema or repository changes.

**Non-Goals:**
- Changing how `Position` is stored or reconciled.
- Any contiguity rule for superseries.

## Decisions

### Layer mapping

- **Domain**: `Workout.ReorderExercises(IEnumerable<Id> exerciseIds)`. It materializes the input, then checks three things: the count equals `_exercises.Count`, there are no duplicates, and every id matches an entry's `ExerciseId`. Any failure throws `WorkoutExerciseOrderMismatchException(workoutId)` before `_exercises` is touched. On success it rebuilds `_exercises` from the existing entry instances in the requested order, so ids, effort, sets and superseries carry over. New `WorkoutExerciseOrderMismatchException : DomainException` goes in `Domain/Exceptions`. The curated copy `.stpr/template/src/.../Workout.cs` is updated so the parity check stays green.
- **Application**: `ReorderWorkoutExercisesUseCase.Execute(callerId, callerRole, workoutId, IReadOnlyList<Id> exerciseIds, ct)`. It mirrors `CreateSuperSeriesUseCase`: `GetById` or `WorkoutNotFoundException`, then `WorkoutEditAuthorizer.EnsureCanEdit(..., "PUT /workouts/{workoutId}/exercises/order")`, the aggregate action, `Save`, and `SaveChangesAsync`. It returns the `Workout`. It is registered in `Program.cs`.
- **Api**: `ReorderWorkoutExercisesRequest { [Required] IReadOnlyList<Guid>? ExerciseIds }`, plus a `WorkoutsController.ReorderExercises` action on `[HttpPut("{workoutId:guid}/exercises/order")]` that returns `Ok(WorkoutDetailResponse.From(workout))`. Its catch delegates to `ExceptionMapper.Map`. `ExceptionMapper` maps `WorkoutExerciseOrderMismatchException` to `409` explicitly.
- **Infrastructure**: no change.

Cross-module dependencies: none beyond Workouts. Catalog exercises are not looked up, because membership in the workout is the only check needed.

### Catalog exercise ids rather than entry ids

The order is expressed in catalog exercise ids, consistent with the per-exercise routes. A catalog exercise is unique per workout, so it identifies the entry unambiguously.
Alternative: use entry ids like superseries. This was rejected because the brief asks for "exercise ids" and the neighbouring `/exercises` routes already use catalog ids.

### `409 Conflict` for a mismatching list

The body is structurally valid. It disagrees with the current state of the workout, which typically means the client's view is stale.
Alternative: `400`, which the `DomainException` fallback would give. This was rejected because `409` tells the client to re-read the workout. The mapping is explicit, as the AGENTS checklist requires.

### One exception for every mismatch kind

Missing, extra, unknown and repeated ids all mean the same thing to the caller, so a single exception and message keep the API surface small.

### `PUT` on a sub-resource returning the workout detail

The request replaces the whole order, which makes it idempotent, so `PUT` fits. Returning `WorkoutDetailResponse` lets the client render the result without another `GET`, as `PUT /workouts/{id}/status` already does.

### Route `.../exercises/order` and the `{exerciseId:guid}` routes

`order` is not a GUID, so it cannot collide with `PUT /workouts/{workoutId}/exercises/{exerciseId:guid}`.

## Risks / Trade-offs

- [Superseries members can end up apart after a reorder] → This is accepted and out of scope. The brief adds no such rule, and it can be added later in the aggregate without changing the route.
- [Concurrent edits can race: add an exercise, then reorder with a stale list] → The exact-match rule turns this into a `409` instead of silently dropping an exercise.
- [Template drift] → `Workout.cs` is a curated template file. Copy it after editing and run the parity loop from AGENTS.md.

## Migration Plan

No schema change and no migration. To roll back, revert the code. Orders that were already stored remain valid positions.

## Open Questions

None.
