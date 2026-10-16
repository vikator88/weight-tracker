# Tasks

## 1. Domain: superseries members by entry id

- [x] 1.1 Add `WorkoutExerciseNotInWorkoutException(Id workoutExerciseId, Id workoutId)` in `Domain/Exceptions`, deriving from `DomainException`. Verify the build passes.
- [x] 1.2 Change `Workout.CreateSuperSeries` to look members up by `WorkoutExercise.Id` and throw the new exception for an unknown id. Rename the parameter and update the XML docs (design D1). Verify the build passes.
- [x] 1.3 Make `ExerciseAlreadyInSuperSeriesException` and `DuplicateExerciseInSuperSeriesException` take and print the entry id ("Workout exercise '…'"). Verify the build passes.
- [x] 1.4 Update the superseries cases in `test/WeightTracker.Unit/Domain/Workouts/WorkoutTests.cs` to pass entry ids. Add "catalog exercise id is rejected" and "unknown entry id throws `WorkoutExerciseNotInWorkoutException`". Arrange/Act/Assert, one Act. Verify `dotnet test test/WeightTracker.Unit` is green.
- [x] 1.5 Review: compare `Workout.cs` and `WorkoutTests.cs` against the `Workouts`/`WorkoutTests` template references. Copy both to `.stpr/template/` and verify the parity check reports no `DRIFT` for them.

## 2. Application: workout edit rule

- [x] 2.1 Add `Application/Services/WorkoutEditAuthorizer` with `EnsureCanEdit(Workout, Id callerId, Role callerRole, string operation)` implementing design D2. Verify the build passes.
- [x] 2.2 Add `test/WeightTracker.Unit/Application/Services/WorkoutEditAuthorizerTests.cs` covering admin on prescribed and self-created workouts, the prescribing trainer, the owner of a prescribed workout (rejected), another trainer on a prescribed workout (rejected), a `USER` owner and a `TRAINER` owner of a self-created workout, and a non-owner trainer on a self-created workout (rejected). Verify the tests are green.
- [x] 2.3 Review: check the service against the invariant "Business authorization rules" in `STPR_INVARIANTS.md` and the `workout-editing` spec, scenario by scenario.

## 3. Application: use cases

- [x] 3.1 Add `Application/Exceptions/WorkoutNotFoundException(Id workoutId)` deriving from `ApplicationLayerException`. Verify the build passes.
- [x] 3.2 Add `UseCases/Workouts/CreateSuperSeriesUseCase` (design D3): load → 404 → authorize → `CreateSuperSeries` → `Save` → one `SaveChangesAsync`. It returns the `SuperSeriesId`. Verify the build passes.
- [x] 3.3 Add `UseCases/Workouts/BreakSuperSeriesUseCase` (design D3): `SuperSeriesId.From` first, then load → 404 → authorize → `BreakSuperSeries` → `Save` → commit. Verify the build passes.
- [x] 3.4 Add `test/WeightTracker.Unit/Application/Workouts/CreateSuperSeriesUseCaseTests.cs` and `BreakSuperSeriesUseCaseTests.cs`, mirroring `UpdateExerciseUseCaseTests` (mocked repository and unit of work). Cover success (saved and committed once), missing workout, rejected caller, and a domain error. Each failure case must show that nothing is saved or committed. Break also gets an invalid id case that never touches the repository. Verify the tests are green.
- [x] 3.5 Review: compare both use cases with `UpdateExerciseUseCase` and `GetAllWorkoutsUseCase` (shape, caller passing, cancellation, single commit).

## 4. Api: endpoints and response shape

- [x] 4.1 Add `Endpoints/Workouts/Dtos/CreateSuperSeriesRequest` (`[Required] IReadOnlyList<Guid> ExerciseIds`) and `SuperSeriesResponse(int Id)` with `static From(SuperSeriesId)`. Verify the build passes.
- [x] 4.2 Add `CreateSuperSeries` and `BreakSuperSeries` actions to `WorkoutsController` (design D4). Each reuses `TryReadCaller`, routes every catch through `ExceptionMapper.Map` and declares its `[ProducesResponseType]` set. The create action's summary states that `exerciseIds` are workout exercise ids. Verify the build passes.
- [x] 4.3 Map `WorkoutNotFoundException` and `WorkoutExerciseNotInWorkoutException` to `404` in `ExceptionMapper`, and add their cases to `test/WeightTracker.Integration/Common/ExceptionMapperTests.cs`. Verify the tests are green.
- [x] 4.4 Register `WorkoutEditAuthorizer`, `CreateSuperSeriesUseCase` and `BreakSuperSeriesUseCase` in `Program.cs`. Verify the API starts and `/openapi/v1.json` lists both routes with a `Bearer` requirement.
- [x] 4.5 Add `Id` and `SuperSeriesId` to `WorkoutExerciseResponse`, with `superSeriesId` serialized as `null` when ungrouped. Extend `WorkoutResponseShapeTests` to assert both fields. Verify the test is green.
- [x] 4.6 Review: compare the new actions and DTOs with `WorkoutsController.GetAll` and `ExercisesController` (DTO factories, response declarations, `ExceptionMapper` in every catch).

## 5. Seed data

- [x] 5.1 Add the four workouts and their entry ids from design D5 to `SeedData` and `DatabaseSeeder`, built through `Workout.Rehydrate`/`WorkoutExercise.Rehydrate`. Verify the API seeds the development database on startup without errors.
- [x] 5.2 Update `WorkoutScopingTests` to the new expected sets, keeping the exclusion assertions (the trainer never sees either self-created workout). Copy the file to `.stpr/template/test/...`. Grep the integration suite for other seed-count assumptions and fix them. Verify those suites are green and the parity check reports no drift.
- [x] 5.3 Review: compare the seeder changes with the existing `DatabaseSeeder` shape (fixed ids, aggregates built through the domain).

## 6. Integration tests: endpoints

- [x] 6.1 Add `test/WeightTracker.Integration/Workouts/SuperSeriesCreateEndpointTests.cs`, mirroring `ExerciseUpdateEndpointTests`/`AuthEndpointsTests` and logging in via `AuthenticatedClientFor`. Cover: 401 no token, 401 invalid token, 201 for the prescribing trainer (then `GET /workouts` shows both entries grouped and the others not), 201 for the owner of a self-created workout, 201 for admin, 403 for the owner of a prescribed workout, 403 for a trainer on a self-created workout, 404 unknown workout, 404 entry from another workout, 409 entry already grouped, 400 single id, 400 duplicate id, 400 set-total mismatch, 400 missing `exerciseIds`. Every 403 case must also assert that nothing was grouped. Verify the tests are green.
- [x] 6.2 Add `SuperSeriesBreakEndpointTests.cs` with the same pattern. Cover: 401 no token, 401 invalid token, 204 for the prescribing trainer (then `GET /workouts` shows the same entries, in the same order, ungrouped), 204 for the owner of a self-created workout, 204 for admin, 403 for the owner of a prescribed workout, 403 for a trainer on a self-created workout, 404 unknown workout, 404 unknown superseries, 400 for superseries id `0`. Every 403 case must also assert that the superseries is intact. Verify the tests are green.
- [x] 6.3 Review: check every scenario in `specs/workout-superseries/spec.md` and `specs/workout-editing/spec.md` is covered by at least one test (unit or integration), and that each route has its own 401/401/403/404/400 cases.

## 7. Template map and final verification

- [x] 7.1 Add `WorkoutEditAuthorizer` to `.stpr/template/README.md` as a skeleton-backed "Business authorization service" reference for future workout-editing use cases. Verify that the README links resolve.
- [x] 7.2 Run `dotnet build WeightTracker.slnx` (warnings are errors) and the full `dotnet test WeightTracker.slnx` with Docker running. Verify both succeed.
- [x] 7.3 Run the template parity check from `AGENTS.md` and verify it prints nothing.
- [x] 7.4 Walk the `AGENTS.md` quality checklist (including the new workout edit rule item) and `.stpr/review/CHECKLIST_REVISION.md`. Verify every item is satisfied or explicitly justified.

## Workflow follow-up

- Archive the change after review, which syncs `workout-editing` (new) and `workout-superseries` (modified) into `openspec/specs/`.
