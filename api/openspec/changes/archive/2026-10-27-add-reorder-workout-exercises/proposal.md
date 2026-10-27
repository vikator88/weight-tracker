# Proposal

## Why

A workout keeps its exercises in the order they must be performed, but the only way to change that order today is to remove exercises and add them back, which also throws away their sets and superseries. Trainers, and athletes on their own plans, need to rearrange a workout in one call. Source brief: `.stpr/plan/openspec/proposal-change-add-reorder-workout-exercises.md`.

## What Changes

- New route `PUT /workouts/{workoutId}/exercises/order` with body `{ "exerciseIds": [ ... ] }`, the catalog exercise ids of the workout in the new order. Returns `200 OK` with the workout detail.
- The `Workout` aggregate gains a reorder action. It accepts the list only when it contains exactly the exercises already in the workout: the same count, each id once, and no unknown id. Otherwise it refuses and the order stays as it was.
- A mismatching list is rejected with `409 Conflict` through a new domain exception, mapped explicitly in `ExceptionMapper`.
- The workout edit rule applies: admin always, the prescribing trainer if there is one, otherwise the owner. Started or finished workouts are locked for non-admins.
- Persistence needs no change. Order is already stored in `workout_exercises.Position` and reconciled by the repository. There is no migration.

## Capabilities

### New Capabilities

_None._

### Modified Capabilities

- `workout-exercises`: adds requirements for reordering the exercises of a workout, the exact-match rule, and auth and permission handling for the new route.
- `workout-editing`: the lock on started and finished workouts lists reordering exercises among the editing operations.

## Template And Module Mapping

- Workouts -> mutable aggregate -> `.stpr/template/src/WeightTracker.Domain/Workouts/Workout.cs`. The reorder action follows `Workout.CreateSuperSeries`: it validates the whole input before mutating anything.
- Application -> use case -> `CreateSuperSeriesUseCase`: load the workout, call `WorkoutEditAuthorizer.EnsureCanEdit`, run the aggregate action, save, commit.
- Api -> endpoint -> `WorkoutsController.ChangeStatus`: a PUT on a workout sub-resource that returns `WorkoutDetailResponse`. Request DTO follows `CreateSuperSeriesRequest`.
- No deviations from the template or invariants. No conflicts were found.

## Business Identifiers And Mutability

- No identifier is created or changed. `Workout.Id`, `WorkoutExercise.Id` and `WorkoutExercise.ExerciseId` remain immutable. Only the position of the entries changes.

## Acceptance Criteria

- A valid permutation is stored and reads back through `GET /workouts/{id}` in the new order. Effort, sets, entry ids and superseries are unchanged.
- A list with missing, extra, unknown or repeated ids returns `409` and leaves the order unchanged.
- The route returns `401` without a token and `401` with an invalid token, `403` for a caller the edit rule rejects, `404` for an unknown workout, `400` when `exerciseIds` is missing, and `409` for a locked workout and a non-admin caller.

## Out Of Scope

- Moving a single exercise up or down. The full order is always sent.
- Keeping superseries members next to each other. No such rule exists today and none is added.
- Frontend and E2E tests.

## Impact

- Domain: `Workout.cs` (and its curated copy in `.stpr/template/src`), plus a new exception in `Domain/Exceptions`.
- Application: new `ReorderWorkoutExercisesUseCase`.
- Api: new request DTO, a new action in `WorkoutsController`, a new `ExceptionMapper` entry, and registration in `Program.cs`.
- Tests: unit tests for the domain and the use case, and an integration test class for the route.
- The OpenAPI document gains the new route, so the frontend's generated contract can be regenerated later.
