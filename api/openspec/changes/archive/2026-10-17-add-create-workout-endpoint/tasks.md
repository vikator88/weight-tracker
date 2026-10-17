# Tasks

## 1. Application: create-workout use case

- [x] 1.1 Add `Application/Exceptions/InvalidWorkoutAssignmentException(string reason)` deriving from `ApplicationLayerException` (design D4). Verify the build passes.
- [x] 1.2 Add `UseCases/Workouts/CreateWorkoutUseCase` with `Execute(Id callerId, Role callerRole, Id? userId, Id? trainerId, DateTime workoutDate, CancellationToken)` returning `Workout`. Inject `IWorkoutRepository`, `IUserRepository` and `IUnitOfWork`. Follow the check order in design D3: role-dependent `400` checks, owner `GetById` → `UserNotFoundException`, trainer → non-`USER` owner → `UnauthorizedOperationException("POST /workouts")`, admin `trainerId` → `IsTrainerOrAdmin`, then `Workout.Create` → `Save` → one `SaveChangesAsync`. Document every thrown exception in XML docs, as `CreateSuperSeriesUseCase` does. Verify the build passes.
- [x] 1.3 Add `test/WeightTracker.Unit/Application/Workouts/CreateWorkoutUseCaseTests.cs`, mirroring `CreateSuperSeriesUseCaseTests` (strict mocks, an `AUseCase()` factory, labelled Arrange/Act/Assert, one Act). Cover:
  - `USER`: success without ids (owner = caller, trainer `null`, no user lookup), success with their own `userId`, `400` for a foreign `userId`, `400` for any `trainerId`.
  - `TRAINER`: success (owner = `userId`, trainer = caller), `400` for a missing `userId`, `400` for `userId` == caller, `400` for any `trainerId`, `404` for an unknown owner, `403` for an owner with the `TRAINER` role, `403` for an owner with the `ADMIN` role.
  - `ADMIN`: success without a trainer, success with a trainer, `400` for a missing `userId`, `400` for `userId` == caller, `404` for an unknown owner, `400` when `IsTrainerOrAdmin` is false, `TrainerCannotBeWorkoutOwnerException` when `trainerId == userId`.

  Every rejection asserts that `Save` and `SaveChangesAsync` were never called, and every `400` body-check case asserts that `IUserRepository` was never called. Verify `dotnet test test/WeightTracker.Unit` is green.
- [x] 1.4 Review: compare `CreateWorkoutUseCase` with `CreateSuperSeriesUseCase` (shape, explicit caller passing, cancellation, single commit) and check that it creates state only through `Workout.Create`. Check every spec requirement in `specs/workout-creation/spec.md` against a unit test.

## 2. Api: endpoint, DTO, mapping and wiring

- [x] 2.1 Add `Endpoints/Workouts/Dtos/CreateWorkoutRequest` (`Guid? UserId`, `Guid? TrainerId`, `[Required] DateOnly? WorkoutDate`), with XML docs stating which roles may send `userId` and `trainerId` (design D1). Verify the build passes.
- [x] 2.2 Add the `Create` action (`[HttpPost]`) to `WorkoutsController`. Inject `CreateWorkoutUseCase`, reuse `TryReadCaller`, convert the date with `ToDateTime(TimeOnly.MinValue)`, and return `StatusCode(201, WorkoutResponse.From(workout, new Dictionary<Id, Exercise>()))` (design D5). Route the catch through `_exceptionMapper.Map` and declare `[ProducesResponseType]` for `201`, `400`, `401`, `403` and `404`. Verify the build passes.
- [x] 2.3 Map `InvalidWorkoutAssignmentException` to `400` in `ExceptionMapper`, and add its case to `test/WeightTracker.Integration/Common/ExceptionMapperTests.cs`. Verify that test is green.
- [x] 2.4 Register `CreateWorkoutUseCase` in `Program.cs` next to the other workout use cases. Verify the API starts and `/openapi/v1.json` lists `POST /workouts` with a `Bearer` requirement and a `date`-formatted `workoutDate`.
- [x] 2.5 Review: compare the action and DTO with `WorkoutsController.CreateSuperSeries` and `CreateSuperSeriesRequest` (only structural validation in the DTO, response built by a DTO factory, `ExceptionMapper` in the catch).

## 3. Integration tests: `POST /workouts`

- [x] 3.1 Add `test/WeightTracker.Integration/Workouts/WorkoutCreateEndpointTests.cs`, mirroring `SuperSeriesCreateEndpointTests` and `AuthEndpointsTests`. Authenticate only through `AuthenticatedClientFor`, use only the seeded users, and add no seed data. Cover:
  - `401` with no token and `401` with an invalid token.
  - `400` for a missing `workoutDate` and `400` for a non-date `workoutDate`.
  - `USER`: `201` without ids (`userId` = `SeedData.UserId`, `trainerId` `null`, empty `exercises`, `workoutDate` echoed), `400` for a foreign `userId`, `400` for any `trainerId`.
  - `TRAINER`: `201` for `SeedData.UserId` (`trainerId` = `SeedData.TrainerId`), `400` for a missing `userId`, `400` for their own id, `400` for any `trainerId`, `403` for `SeedData.AdminId` as owner, `404` for an unknown `userId`.
  - `ADMIN`: `201` for `SeedData.UserId` without a trainer, `201` with `SeedData.TrainerId` as trainer, `400` for a missing `userId`, `400` for their own id, `400` for a `USER` as `trainerId`, `400` for `trainerId == userId`, `404` for an unknown `userId`.

  For each `201`, assert through `GET /workouts` that the owner sees the new id. For the trainer case, also assert that the trainer sees it. For the admin case without a trainer, assert that the seeded trainer does not. For each `400`, `403` and `404`, assert that the admin's `GET /workouts` count is unchanged. Verify the tests are green.
- [x] 3.2 Review: check that every scenario in `specs/workout-creation/spec.md` is covered by at least one unit or integration test, and that the route has its own `401`/`401`/`400`/`403`/`404` cases as AGENTS.md requires.

## 4. Final verification

- [x] 4.1 Run `dotnet build WeightTracker.slnx` (warnings are errors) and the full `dotnet test WeightTracker.slnx` with Docker running. Verify both succeed.
- [x] 4.2 Run the template parity check from `AGENTS.md` and verify it prints nothing. No template file is expected to change.
- [x] 4.3 Walk the `AGENTS.md` quality checklist and verify every item is satisfied or explicitly marked not applicable (no migration, no seed, no workout edit).

## Workflow follow-up

- Archive the change after review, which adds `workout-creation` to `openspec/specs/`.
