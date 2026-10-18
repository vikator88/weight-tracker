# Tasks

## 1. Read use case

- [x] 1.1 Add `GetWorkoutByIdUseCase` in `WeightTracker.Application/UseCases/Workouts` with the role scope check; verify it builds with `dotnet build WeightTracker.slnx`
- [x] 1.2 Register the use case in `Program.cs`; verify the API starts and resolves it (covered by the integration tests in group 3)
- [x] 1.3 Add unit tests in `test/WeightTracker.Unit/Application/Workouts` (mocked repository, Arrange/Act/Assert): admin any, trainer owned, trainer prescribed, trainer unrelated -> `UnauthorizedOperationException`, user own, user other -> `UnauthorizedOperationException`, missing -> `WorkoutNotFoundException`; verify `dotnet test test/WeightTracker.Unit` passes
- [x] 1.4 Review `GetWorkoutByIdUseCase` against `GetAllWorkoutsUseCase` and `ExerciseUseCase`; verify naming, structure and no domain logic leaked into the use case

## 2. Endpoint and response

- [x] 2.1 Add `WorkoutDetailResponse` (and entry DTO without `name` / `bodyPart`) in `Endpoints/Workouts/Dtos`; verify it maps ids, sets, effort and superseries
- [x] 2.2 Add `GetById` (`GET {id:guid}`) to `WorkoutsController` with `ProducesResponseType` for 200, 401, 403, 404 and the catch routed through `ExceptionMapper`; verify with the integration tests in group 3
- [x] 2.3 Change `Create` to `CreatedAtAction(nameof(GetById), ...)` and update its XML remarks, plus the superseries remarks only if they claim a single-workout route is missing; verify `Location` ends with `/workouts/{id}`
- [x] 2.4 Review the controller against `WorkoutsController` / `ExercisesController.GetById`; verify parity and run the `.stpr/template` drift check from AGENTS.md

## 3. Integration tests

- [x] 3.1 Add `WorkoutGetByIdEndpointTests` in `test/WeightTracker.Integration/Workouts` using `AuthenticatedClientFor`: 401 no token, 401 invalid token, 404 missing id, 404 malformed id, 200 body shape with exercises/sets/superseries and no `name`/`bodyPart`, 200 empty workout; verify they pass
- [x] 3.2 Add the scoped cases to `WorkoutScopingTests` style coverage (or a sibling file): admin any, trainer owned, trainer prescribed, trainer unrelated 403, user own, user other 403 (including one where their trainer is set) asserting no content leaks; verify they pass
- [x] 3.3 Extend `WorkoutCreateEndpointTests` with the `Location` header scenario and follow it as the same caller; verify it returns 200 with the created workout
- [x] 3.4 Review each new test for exactly one Act and labelled Arrange/Act/Assert phases, and compare against `AuthEndpointsTests` and `WorkoutScopingTests`
- [x] 3.5 Run `dotnet build WeightTracker.slnx` and the full `dotnet test WeightTracker.slnx` with Docker running; verify all green

## Workflow follow-up

- Archive the change after review (syncs `workout-retrieval` and the `workout-creation` delta into `openspec/specs`).
