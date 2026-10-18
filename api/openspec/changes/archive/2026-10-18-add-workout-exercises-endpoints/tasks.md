# Tasks

## 1. Shared set target type names

- [x] 1.1 Add `WeightTracker.Domain/Workouts/SetTargetTypes.cs` (`public static class`, `public const string Reps`, `Duration`, `MaxReps`) and verify `dotnet build WeightTracker.slnx` succeeds
- [x] 1.2 Replace the private `RepsTarget`/`DurationTarget`/`MaxRepsTarget` in `WorkoutMapper` with `SetTargetTypes`. Verify that `WorkoutMapperTests` pass and that a round-trip case for each of the three target types exists or is added
- [x] 1.3 Use `SetTargetTypes` in `SetTargetResponse.From` instead of string literals, and verify `WorkoutResponseShapeTests` still pass

## 2. Domain: set prescription validation

- [x] 2.1 Add `InvalidSetPrescriptionException` and `WorkoutExerciseRequiresSetsException` (extend `DomainException`) in `Domain/Exceptions` and verify the build
- [x] 2.2 Validate `count >= 1` in `SetPrescription` and `value >= 1` in `SetTarget.Reps`/`SetTarget.Duration`. Add `SetPrescriptionTests` and `SetTargetTests` under `test/WeightTracker.Unit/Domain/Workouts` covering 0, negative and 1, in Arrange/Act/Assert, and verify they pass
- [x] 2.3 Reject an empty set list in the `WorkoutExercise` constructor. Add the case to `WorkoutExerciseTests` and verify that it passes and that `SeedDataTests` still passes

## 3. Domain: update an entry in place

- [x] 3.1 Add `internal WorkoutExercise.Replace(Effort, IEnumerable<SetPrescription>)` that validates before assigning (give `Effort` a private setter, keep `ExerciseId` get-only), and verify the build
- [x] 3.2 Add `Workout.UpdateExercise(Id exerciseId, Effort effort, IEnumerable<SetPrescription> sets)`: throw `ExerciseNotInWorkoutException` if the exercise is absent, and `SuperSeriesSetCountMismatchException` if the new total differs from the other superseries members, before any mutation. Verify the build
- [x] 3.3 Extend `WorkoutTests` with UpdateExercise cases (replaces effort and sets; keeps entry id, position and superseries; absent exercise; superseries mismatch leaves the entry unchanged; same total in a superseries accepted; invalid effort leaves the entry unchanged) and verify they pass
- [x] 3.4 Add a `WorkoutTests` case confirming `RemoveExercise` drops the entry's sets with it (the entry is gone from `Exercises`) and verify it passes
- [x] 3.5 Copy the changed `Workout.cs`, `WorkoutExercise.cs`, `SetPrescription.cs`, `SetTarget.cs` and `WorkoutTests.cs` into `.stpr/template`, then run the parity loop from `AGENTS.md` and verify that it prints no `DRIFT`
- [x] 3.6 Review: compare the domain changes against the `Workouts` template reference (Create/Rehydrate/IsNew untouched, value objects as records, no `Guid.NewGuid`), and record any deviation in this file

## 4. Application: use cases

- [x] 4.1 Add `ExerciseIdMismatchException` (extends `ApplicationLayerException`) in `Application/Exceptions` and verify the build
- [x] 4.2 Implement `AddWorkoutExerciseUseCase` (load → `EnsureCanEdit` → `IExerciseRepository.GetById` → `AddExercise` → `Save` → one `SaveChangesAsync`, returns the new entry). Add `AddWorkoutExerciseUseCaseTests` mirroring `CreateSuperSeriesUseCaseTests` (404 workout, 403, 404 catalog exercise, 409 duplicate, happy path verifies Save and commit, no commit on failure) and verify they pass
- [x] 4.3 Implement `UpdateWorkoutExerciseUseCase` (mismatch check first → load → authorize → `UpdateExercise` → save → commit, returns the entry). Add `UpdateWorkoutExerciseUseCaseTests` (mismatch 400, 404 workout, 403, 404 exercise not in workout, happy path, no commit on failure) and verify they pass
- [x] 4.4 Implement `RemoveWorkoutExerciseUseCase` (load → authorize → `RemoveExercise` → save → commit). Add `RemoveWorkoutExerciseUseCaseTests` (404 workout, 403, 404 exercise not in workout, happy path, no commit on failure) and verify they pass
- [x] 4.5 Review: compare the three use cases against `CreateSuperSeriesUseCase` (single `Execute`, authorizer not inlined, one commit, XML `<exception>` docs) and record any deviation in this file

## 5. API: DTO, controller, wiring

- [x] 5.1 Add `Dtos/AddOrUpdateExerciseRequest.cs` with nested `SetPrescriptionRequest` (`[Required]`, property-level `JsonStringEnumConverter<Effort>`, `[MinLength(1)]` on `Sets`, `[AllowedValues]` from `SetTargetTypes`, `ToDomain()`), and verify the build
- [x] 5.2 Map `InvalidSetPrescriptionException`, `WorkoutExerciseRequiresSetsException` and `ExerciseIdMismatchException` to 400 in `ExceptionMapper`. Verify the build, and confirm by reading `ExceptionMapper` that no new custom exception falls through to 500
- [x] 5.3 Add the `POST {workoutId:guid}/exercises` (201 + Location to GetById), `PUT {workoutId:guid}/exercises/{exerciseId:guid}` (200) and `DELETE` (204) actions on `WorkoutsController`, each with a `try/catch → ExceptionMapper.Map`, `[ProducesResponseType]` for every status, and a `<summary>` stating that `exerciseId` is the catalog id. Verify the build
- [x] 5.4 Register the three use cases in `Program.cs`, then start the API and verify the three routes appear in `/openapi/v1.json`
- [x] 5.5 Review: compare the controller and DTO against `WorkoutsController` and `CreateSuperSeriesRequest` (structural validation only, `CurrentUser` from `AuthorizedController`, no domain objects in responses) and record any deviation in this file

## 6. Integration tests, one file per route

- [x] 6.1 Add `test/WeightTracker.Integration/Workouts/WorkoutExerciseAddEndpointTests.cs` structured like `SuperSeriesBreakEndpointTests`: no token, garbage token and deleted-user 401s; 201 for the prescribing trainer, the self-created owner and an admin (read back through GET); 403 for the owner of a prescribed workout and a non-owner trainer, each with the workout unchanged; 404 unknown workout; 404 unknown catalog exercise; 409 duplicate; 400 for empty sets, missing effort, unknown target type, count 0 and effort 9; effort accepted as a name and as a number; MaxReps ignores target. Verify they pass
- [x] 6.2 Add `WorkoutExerciseUpdateEndpointTests.cs`: the same 401/403/404 matrix; 404 exercise not in workout; 400 route/body mismatch; 200 replacing effort and sets while keeping entry id and position; an effort-only update where `Factory.WithScope` shows the old `workout_exercise_sets` ids are gone and the new rows are equal in value; superseries total change → 400 with the entry unchanged; same total in a superseries → 200. Verify they pass
- [x] 6.3 Add `WorkoutExerciseRemoveEndpointTests.cs`: the same 401/403/404 matrix; 404 exercise not in workout; 204 with the entry gone through GET and no `workout_exercise_sets` rows left for the removed entry id (through `Factory.WithScope`); removing a member of a two-member superseries leaves the other member ungrouped. Verify they pass
- [x] 6.4 Add `WorkoutExerciseFlowTests.cs`: create a workout → add two exercises → `GET /workouts/{id}` returns both in insertion order with their effort and sets. Verify it passes
- [x] 6.5 Review: compare the new test files against `AuthEndpointsTests` and `SuperSeriesBreakEndpointTests` (auth only through `AuthenticatedClientFor`, labelled phases with a single Act, no dependence on other tests' writes, no seed changes) and record any deviation in this file

## 7. Final verification

- [x] 7.1 Run `dotnet build WeightTracker.slnx` and verify it reports zero warnings and zero errors
- [x] 7.2 With Docker running, run the full `dotnet test WeightTracker.slnx` and verify both unit and integration suites pass
- [x] 7.3 Re-run the `.stpr/template` parity loop and verify that it prints no `DRIFT`
- [x] 7.4 Walk the `AGENTS.md` quality checklist and verify every item applies or is explicitly justified

## Workflow follow-up

- Archive the change after review, which syncs `workout-exercises` into `openspec/specs/`.
