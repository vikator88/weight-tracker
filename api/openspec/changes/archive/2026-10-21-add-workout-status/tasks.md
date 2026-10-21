# Tasks

## 1. Domain: status on the Workout aggregate

- [x] 1.1 Add `WorkoutStatus` enum (`DRAFT = 1, PUBLISHED = 2, STARTED = 3, FINISHED = 4`) in `WeightTracker.Domain/Workouts`, mirroring `Effort.cs`. Verify that `dotnet build WeightTracker.slnx` succeeds.
- [x] 1.2 Add `InvalidWorkoutStatusException` and `InvalidWorkoutStatusTransitionException` in `WeightTracker.Domain/Exceptions`, following an existing domain exception. Verify the build succeeds.
- [x] 1.3 Add `Status` to `Workout`: `Create` sets `DRAFT`, and `Rehydrate` takes a `WorkoutStatus` and rejects undefined values. Add `ChangeStatus` (business graph), `OverrideStatus` (no graph), `IsLocked` and `IsHiddenFrom(Id callerId)` (design D2, D5 and D6). Verify the build succeeds.
- [x] 1.4 Update every `Workout.Rehydrate` call (mapper, seeder, tests) to pass a status. Verify the build succeeds with warnings as errors.
- [x] 1.5 Extend `test/WeightTracker.Unit/Domain/Workouts/WorkoutTests.cs`, using Arrange/Act/Assert. Cover: `Create` gives `DRAFT`; each of the 4 valid transitions; every invalid pair, including the same status and the transitions out of `FINISHED`, which throw `InvalidWorkoutStatusTransitionException`; an undefined target throws `InvalidWorkoutStatusException` from both `ChangeStatus` and `OverrideStatus`; `OverrideStatus` skips the graph; `IsLocked` for each status; `IsHiddenFrom` for every combination of draft, trainer and caller. Verify that `dotnet test test/WeightTracker.Unit` passes.
- [x] 1.6 Review: compare `Workout.cs` and `WorkoutStatus.cs` against the template references (`.stpr/template` copies of `Workout.cs` and `Effort.cs`) and `STPR_INVARIANTS.md`. Verify that no ad hoc validation was added and that the action methods guard the transition semantics.

## 2. Persistence: status column, entity, mapper and seed

- [x] 2.1 Add the FluentMigrator migration `AddStatusToWorkouts_<yyyyMMddHHmm>`, modelled on `AddEffortToWorkoutExercises_202610131437`. It adds a non-null `int` with default `1`, drops the default, and `Down()` drops the column. Verify that the integration `MigrationsTests` pass, including the down migration.
- [x] 2.2 Add `Status` (`int`) to `WorkoutEntity` and map the `status` column as required in `WeightTrackerDbContext`. Verify that the EF column name matches the migration.
- [x] 2.3 Update `WorkoutMapper`: write `(int)Status` and read it through `MapStatus`, which throws `PersistenceMappingException` on unknown values. Extend `test/WeightTracker.Unit/Infra/Mappers` with a round-trip test for each status and an unknown-value test. Verify that the unit tests pass.
- [x] 2.4 Update the seed (design D9): prescribed workouts are `PUBLISHED`, self-planned ones are `DRAFT`, and add `PrescribedDraftWorkoutId` and `PrescribedStartedWorkoutId` to `SeedData` and `DatabaseSeeder`. Verify that `dotnet run --project src/WeightTracker.Api` in Development seeds without errors. (Verified against a throwaway database, because the development database already holds users and the seeder skips it.)
- [x] 2.5 Extend `test/WeightTracker.Integration/Repositories/WorkoutRepositoryTests.cs` so status persists and reloads after a save. Verify that it passes with Docker running.
- [x] 2.6 Review: compare the migration, entity and mapper with their template references. Verify that `Down()` reverses `Up()` and that no EF Core migration was generated.

## 3. Application: status change use case and authorizer

- [x] 3.1 Add `WorkoutStatusAuthorizer` in `WeightTracker.Application/Services`, modelled on `WorkoutEditAuthorizer` (design D3). Add `test/WeightTracker.Unit/Application/Workouts/WorkoutStatusAuthorizerTests.cs` covering every row of the role table, including the unrelated trainer and user. Verify that the unit tests pass.
- [x] 3.2 Add `ChangeWorkoutStatusUseCase` in `WeightTracker.Application/UseCases/Workouts`, modelled on `UpdateWorkoutExerciseUseCase` (design D4). Admins go through `OverrideStatus` and everyone else through `ChangeStatus`. Verify the build succeeds.
- [x] 3.3 Add `ChangeWorkoutStatusUseCaseTests`, mirroring `UpdateWorkoutExerciseUseCaseTests`. Cover: `404` when not found; `403` from the authorizer with no save; admin override; a non-admin graph violation that propagates the domain exception without saving; and success that saves and commits. Verify that the unit tests pass.
- [x] 3.4 Register `WorkoutStatusAuthorizer` and `ChangeWorkoutStatusUseCase` in `Program.cs`. Verify that the API starts.
- [x] 3.5 Review: compare the use case and authorizer with `UpdateWorkoutExerciseUseCase` and `WorkoutEditAuthorizer`. Verify that naming, constructor shape and XML exception docs match.

## 4. Application: edit lock

- [x] 4.1 Add `WorkoutLockedException` in `WeightTracker.Application/Exceptions`. In `WorkoutEditAuthorizer.EnsureCanEdit`, throw it after the permission check when the caller is not an admin and `workout.IsLocked` is true (design D5). Verify the build succeeds.
- [x] 4.2 Extend the existing `WorkoutEditAuthorizer` unit tests, or add them following the closest test. Cover: a `STARTED` or `FINISHED` workout throws for the allowed non-admin; admin passes; an unauthorized caller on a locked workout still gets `UnauthorizedOperationException`; `DRAFT` and `PUBLISHED` pass. Verify that the unit tests pass.

## 5. Application: draft visibility

- [x] 5.1 In `GetWorkoutByIdUseCase`, reject a workout for which `IsHiddenFrom(callerId)` is true for non-admins with `UnauthorizedOperationException`. Extend `GetWorkoutByIdUseCaseTests` with the prescribed draft (rejected), the self-planned draft (allowed) and the trainer reading their draft (allowed). Verify that the unit tests pass.
- [x] 5.2 In `GetAllWorkoutsUseCase`, filter the TRAINER and USER scopes with `!IsHiddenFrom(callerId)` before collecting exercise ids. Extend `GetAllWorkoutsUseCaseTests`, asserting what is *excluded* as well as what is returned. Verify that the unit tests pass.
- [x] 5.3 Review: compare both use cases with their pre-change structure. Verify that visibility for every role stays exactly as the `workout-retrieval` delta states.

## 6. API: route, DTOs and error mapping

- [x] 6.1 Add `ChangeWorkoutStatusRequest` (a required `WorkoutStatus?` with `JsonStringEnumConverter`), modelled on `AddOrUpdateExerciseRequest`. Add `Status` to `WorkoutResponse` and `WorkoutDetailResponse`. Verify the build succeeds.
- [x] 6.2 Add `PUT /workouts/{workoutId:guid}/status` to `WorkoutsController`, modelled on `UpdateExercise`, returning `200` with `WorkoutDetailResponse`. Verify that every catch delegates to `ExceptionMapper.Map`.
- [x] 6.3 Map `InvalidWorkoutStatusException` → `400`, and `InvalidWorkoutStatusTransitionException` and `WorkoutLockedException` → `409`, in `ExceptionMapper`. Extend `test/WeightTracker.Integration/Common/ExceptionMapperTests.cs` for the three exceptions. Verify that the tests pass.
- [x] 6.4 Update `OpenApiDocumentTests` if they enumerate routes or schemas. Verify that the new route and the `status` field appear in `/openapi/v1.json`.

## 7. Integration tests

- [x] 7.1 Add `test/WeightTracker.Integration/Workouts/WorkoutStatusChangeEndpointTests.cs`, mirroring `WorkoutExerciseUpdateEndpointTests.cs` and authenticating only through `AuthenticatedClientFor`. Cover: `401` with no token and with an invalid token; `400` for a missing or unknown status; `404`; every allowed and forbidden row for admin, trainer, prescribed athlete, self-planned athlete, unrelated trainer and unrelated user; `409` graph violations; and persistence, checked with a follow-up `GET`. Verify that the tests pass with Docker running.
- [x] 7.2 Add edit-lock cases to each edit route test class (`WorkoutExerciseAddEndpointTests`, `WorkoutExerciseUpdateEndpointTests`, `WorkoutExerciseRemoveEndpointTests`, `SuperSeriesCreateEndpointTests`, `SuperSeriesBreakEndpointTests`). Use `PrescribedStartedWorkoutId` for the trainer `409` and the admin bypass, and an Arrange step through the admin status endpoint for a `FINISHED` self-planned `409`. Verify that each route carries its own cases and that the tests pass.
- [x] 7.3 Extend `WorkoutGetByIdEndpointTests` and `WorkoutScopingTests` with the draft visibility cases from the `workout-retrieval` delta, asserting excluded ids in lists. Update any list counts that change because of the new seeds. Verify that the tests pass.
- [x] 7.4 Extend `WorkoutCreateEndpointTests` and `WorkoutResponseShapeTests`: creation returns `status` `DRAFT`, a prescribed draft is absent from the owner's `GET /workouts`, and the response shapes include `status`. Verify that the tests pass.

## 8. Final verification

- [x] 8.1 Run `dotnet build WeightTracker.slnx` and `dotnet test WeightTracker.slnx` with Docker running. Verify that both pass with no skipped integration tests.
- [x] 8.2 Run the `.stpr/template` drift check from `AGENTS.md`. Verify that there is no `DRIFT:` output, or update the template copies on purpose if a canonical file changed.
- [x] 8.3 Walk the AGENTS.md quality checklist and `.stpr/review/CHECKLIST_REVISION.md`. Verify that every item holds for this change.

## Workflow follow-up

- Archive the change after review, with `/opsx:archive`.
