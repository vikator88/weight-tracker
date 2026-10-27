# Tasks

## 1. Domain

- [x] 1.1 Add `WorkoutExerciseOrderMismatchException : DomainException` in `Domain/Exceptions`, following `WorkoutExerciseNotInWorkoutException`. Verify that `dotnet build WeightTracker.slnx` succeeds.
- [x] 1.2 Add `Workout.ReorderExercises(IEnumerable<Id> exerciseIds)`. It validates the count, duplicates and membership before mutating, then rebuilds the list from the existing entries. Add XML docs with `<exception>`. Verify with the 1.3 tests.
- [x] 1.3 Add unit tests in `test/WeightTracker.Unit/Domain/Workouts/WorkoutTests.cs` with Arrange/Act/Assert phases. Cover: a permutation is applied; entry id, effort, sets and superseries are kept; an empty order on an empty workout is accepted; missing, extra, unknown and repeated ids are rejected with the order unchanged; entry ids instead of catalog ids are rejected. Verify that `dotnet test test/WeightTracker.Unit` passes.
- [x] 1.4 Copy `src/WeightTracker.Domain/Workouts/Workout.cs` to `.stpr/template/src/WeightTracker.Domain/Workouts/Workout.cs`. Verify that the parity loop from AGENTS.md prints no `DRIFT`.
- [x] 1.5 Review the domain change against `Workout.CreateSuperSeries` and `STPR_INVARIANTS.md`: validation before mutation, and the action protects its semantics. Record any deviation, or confirm there is none.

## 2. Application

- [x] 2.1 Add `ReorderWorkoutExercisesUseCase`, mirroring `CreateSuperSeriesUseCase`. It loads the workout or throws `WorkoutNotFoundException`, calls `WorkoutEditAuthorizer.EnsureCanEdit` with operation `"PUT /workouts/{workoutId}/exercises/order"`, reorders, saves, commits, and returns the `Workout`. Verify that the build succeeds.
- [x] 2.2 Add `test/WeightTracker.Unit/Application/Workouts/ReorderWorkoutExercisesUseCaseTests.cs`, mirroring `CreateSuperSeriesUseCaseTests` with strict mocks. Cover: success saves and commits once; the owner reorders a workout without a trainer; an admin reorders a started workout; unknown workout; owner of a prescribed workout; another trainer; the prescribing trainer on a started workout (`WorkoutLockedException`); a mismatching order is propagated. Every failure case asserts nothing is saved or committed and the order is unchanged. Verify that the unit suite passes.
- [x] 2.3 Review the use case and its tests against `CreateSuperSeriesUseCase` and its tests for structure and coverage parity.

## 3. Api

- [x] 3.1 Add `ReorderWorkoutExercisesRequest` in `Endpoints/Workouts/Dtos` with `[Required] IReadOnlyList<Guid>? ExerciseIds`, mirroring `CreateSuperSeriesRequest`. Verify that the build succeeds.
- [x] 3.2 Add the `WorkoutsController.ReorderExercises` action on `[HttpPut("{workoutId:guid}/exercises/order")]`. It has `ProducesResponseType` for 200/400/401/403/404/409, returns `Ok(WorkoutDetailResponse.From(workout))`, and its catch delegates to `ExceptionMapper.Map`. Verify that the build succeeds.
- [x] 3.3 Map `WorkoutExerciseOrderMismatchException` to `409 Conflict` in `ExceptionMapper`, and register `ReorderWorkoutExercisesUseCase` in `Program.cs`. Verify that the build succeeds and that `ExceptionMapperTests` still pass.
- [x] 3.4 Add `test/WeightTracker.Integration/Workouts/WorkoutExerciseReorderEndpointTests.cs`, extending `WorkoutExerciseEndpointTest` and mirroring `WorkoutExerciseRemoveEndpointTests`. Use seeded workouts and `AuthenticatedClientFor` only. Cover: `401` with no token; `401` with an invalid token; `200` for the prescribing trainer, with the new order persisted and read back, the entry rows kept and the sets kept; `200` for the owner of a self-created workout; `200` for an admin; `403` for the owner of a prescribed workout with the order unchanged; `403` for a trainer on a self-created workout they don't own; `404` for an unknown workout; `400` for a missing `exerciseIds`; `409` for a missing, unknown and repeated id, with the order unchanged; `409` for the prescribing trainer on a started workout; `200` for an admin on a started workout.
- [x] 3.5 Review the controller action, DTO and integration tests against `ChangeStatus`, `CreateSuperSeriesRequest` and `WorkoutExerciseRemoveEndpointTests` for parity. Confirm the auth matrix is complete for the new route.

## 4. Verification

- [x] 4.1 With Docker running, run `dotnet build WeightTracker.slnx` and `dotnet test WeightTracker.slnx`. Verify that both succeed with no warnings and that both test tiers are green.
- [x] 4.2 Run the AGENTS.md quality checklist and the template parity loop. Verify that every item holds and that there is no `DRIFT` output.

## Workflow follow-up

- Archive the change after review, so the `workout-exercises` and `workout-editing` deltas are merged into the main specs.
- Regenerate the frontend API contract when the backoffice needs the new route.
