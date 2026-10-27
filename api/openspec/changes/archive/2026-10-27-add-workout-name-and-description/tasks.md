# Tasks

Changing the `Workout.Create`/`Rehydrate` signatures breaks every caller at once, and warnings are errors. Until group 5 compiles the whole solution, each group is checked by building its own project (`dotnet build src/<Project>`) and by updating its tests so they compile. Those tests run in the first group that builds the whole solution.

## 1. Domain

- [x] 1.1 Add `WorkoutName` (trim, 1–256 chars, `MaxLength = 256`) and `InvalidWorkoutNameException : DomainException`, mirroring `Common/Email.cs`. Add `test/WeightTracker.Unit/Domain/Workouts/WorkoutNameTests.cs` covering trimmed, null, empty, whitespace-only, 256 and 257 characters. Verify `dotnet build src/WeightTracker.Domain` succeeds
- [x] 1.2 Add `WorkoutDescription` (`From` returns `null` for null/blank, trims, max 1024, `MaxLength = 1024`) and `InvalidWorkoutDescriptionException : DomainException`. Add `WorkoutDescriptionTests` covering null, blank to `null`, trimmed, 1024 and 1025 characters. Verify the domain project builds
- [x] 1.3 Extend `Workout`: `Name`/`Description` properties, new `Create(userId, trainerId, name, description, workoutDate)` and `Rehydrate(..., name, description, ...)` parameters, `UpdateDetails(name, description, workoutDate)` keeping only the date part, and `Reassign(userId, trainerId)`, which throws `TrainerCannotBeWorkoutOwnerException` before mutating. Change the `UserId` doc comment to say it changes only through `Reassign`. Verify the domain project builds
- [x] 1.4 Update `test/WeightTracker.Unit/Domain/Workouts/WorkoutTests.cs` with Arrange/Act/Assert: create and rehydrate carry name and description; `UpdateDetails` replaces all three values, drops the time, and keeps status and exercises; `Reassign` changes owner and trainer and clears the trainer; `Reassign` with trainer equal to owner throws and leaves state unchanged. Use one shared test default name for tests that do not care about it, and update `WorkoutExerciseTests`/`EntityEqualityTests` call sites. Verify the files compile against the new domain
- [x] 1.5 Review the domain against `.stpr/template/src/WeightTracker.Domain/Workouts/Workout.cs` and `Common/Email.cs`: value objects are `record`s, action methods validate before mutating, and no `Id.New()` is introduced. Note any deviation in the change

## 2. Application

- [x] 2.1 Add `Application/Services/WorkoutAssignmentValidator` with `EnsureOwnerExists` (`UserNotFoundException`) and `EnsureCanTrain` (`InvalidWorkoutAssignmentException`), moving the checks out of `CreateWorkoutUseCase`. Add `test/WeightTracker.Unit/Application/Services/WorkoutAssignmentValidatorTests.cs` for existing/missing owner and USER/TRAINER/ADMIN/unknown trainer. Verify `dotnet build src/WeightTracker.Application` reaches only the expected call-site errors
- [x] 2.2 Update `CreateWorkoutUseCase` to take `WorkoutName` and `WorkoutDescription?` and to use the validator, with no other behavior change. Update `CreateWorkoutUseCaseTests` so existing cases still pass and the created workout carries name and description. Verify the project builds
- [x] 2.3 Add `UpdateWorkoutUseCase` following the order of checks in `design.md`, using `WorkoutEditAuthorizer` with operation `"PUT /workouts/{id}"`. Verify the project builds
- [x] 2.4 Add `test/WeightTracker.Unit/Application/Workouts/UpdateWorkoutUseCaseTests.cs`, mirroring `UpdateWorkoutExerciseUseCaseTests`. Cover: not found; owner, trainer and admin success; owner of a prescribed workout 403; unrelated caller 403; non-admin with a changed `userId`, a changed `trainerId` or a dropped `trainerId` 403; non-admin with an unchanged assignment succeeds; locked non-admin 409; locked admin succeeds; admin reassign owner, assign trainer, remove trainer; admin to self 400; unknown owner 404; trainer who cannot train 400; trainer equal to owner 400. Every rejection asserts no save and no commit. Verify the file compiles
- [x] 2.5 Fix the remaining Application call sites and their tests that build a `Workout`. Verify `dotnet build src/WeightTracker.Application` succeeds
- [x] 2.6 Review `UpdateWorkoutUseCase` against `UpdateWorkoutExerciseUseCase`: the edit rule is not restated inline, inputs are validated before any mutation, and `SaveChangesAsync` is called once

## 3. Persistence and migration

- [x] 3.1 Add `src/WeightTracker.Migrations/Migrations/2026102718xx_AddNameAndDescriptionToWorkouts.cs` (non-round minutes), shaped like `202610182247_AddStatusToWorkouts.cs`: `Name` `varchar(256)` not null with backfill default `Workout` that is then dropped, `Description` `varchar(1024)` nullable, and a `Down()` that deletes both. Verify `dotnet build src/WeightTracker.Migrations` succeeds
- [x] 3.2 Add `Name`/`Description` to `WorkoutEntity`, and configure them in `WeightTrackerDbContext` with explicit column names and max lengths taken from the value object constants (`Name` required). Verify the configuration matches the migration column for column
- [x] 3.3 Map both fields in `WorkoutMapper` in both directions, with value object construction inside the existing `DomainException` catch. Add `Name`/`Description` to the scalar copy in `WorkoutRepository.Save`. Update `test/WeightTracker.Unit/Infra/Mappers/WorkoutMapperTests.cs` for a round-trip with and without description, and for a blank stored name raising `PersistenceMappingException`. Verify the infra project builds
- [x] 3.4 Give the seeded workouts in `DatabaseSeeder` deterministic names, one with a description and one without, and expose them in `SeedData`. Verify `dotnet build src/WeightTracker.Infra` succeeds
- [x] 3.5 Update `test/WeightTracker.Integration/Repositories/WorkoutRepositoryTests.cs` call sites, and add a case that saves an updated name, description, date and reassigned owner/trainer, then reloads them. Verify it compiles

## 4. API

- [x] 4.1 Add `[Required] string? Name` and `string? Description` to `CreateWorkoutRequest`, with doc comments stating the rules. Build the value objects inside the `Create` action's `try`. Verify `dotnet build src/WeightTracker.Api` reaches only the expected errors
- [x] 4.2 Add `Name` and `Description` to `WorkoutResponse` and `WorkoutDetailResponse` (after `Id`). Verify the API project builds
- [x] 4.3 Add `UpdateWorkoutRequest` (`[Required]` `Name`, `WorkoutDate`, `UserId`; nullable `Description`, `TrainerId`), with doc comments explaining full replacement and the admin-only reassignment. Verify it compiles
- [x] 4.4 Add `[HttpPut("{id:guid}")] Update` to `WorkoutsController`, returning `WorkoutDetailResponse` with `ProducesResponseType` 200/400/401/403/404/409, and a catch that delegates to `ExceptionMapper.Map`. Register `UpdateWorkoutUseCase` and `WorkoutAssignmentValidator` in `Program.cs`. Verify `dotnet build WeightTracker.slnx` succeeds
- [x] 4.5 Review the controller action and DTOs against the `UpdateExercise` and `ChangeStatus` actions and `CreateWorkoutRequest`: only structural validation in DTOs, `CurrentUser` passed to the use case, and no new custom exception without a mapping

## 5. Integration tests

- [x] 5.1 Update `WorkoutCreateEndpointTests` (and every other integration test that posts a workout) to send `name`. Add cases: name stored trimmed; missing, blank and 257-char name 400; 256-char name 201; no description and blank description return `null`; 1024-char description 201; 1025-char description 400. Verify they compile
- [x] 5.2 Extend `WorkoutGetByIdEndpointTests` and `WorkoutResponseShapeTests` to assert `name` and `description` on `GET /workouts/{id}` and `GET /workouts`, including a `null` description. Verify they compile
- [x] 5.3 Add `test/WeightTracker.Integration/Workouts/WorkoutUpdateEndpointTests.cs`, replicating the structure of `WorkoutStatusChangeEndpointTests` and using `AuthenticatedClientFor`. Cover:
  - 401 with no token and with an invalid token;
  - 404 for a missing workout;
  - 400 for a missing `name`, `workoutDate` or `userId`, a blank or too long name, and a too long description;
  - 200 for the owner of a self-planned workout, the prescribing trainer, and an admin, each followed by a `GET` that shows the new values with status and exercises unchanged;
  - 200 when the description is cleared;
  - 403 for the owner of a prescribed workout, an unrelated user, and a trainer changing `userId` or dropping `trainerId`;
  - 409 for a trainer on a `STARTED` workout, and 200 for an admin on a `FINISHED` one;
  - admin reassignment: new owner 200, after which the previous owner's `GET /workouts/{id}` is 403 and their list excludes the workout; assign and remove trainer 200; unknown owner 404; admin to self 400; USER as trainer 400; trainer equal to owner 400;
  - every rejection followed by a `GET` that shows the workout unchanged.

  Verify it compiles
- [x] 5.4 Run `dotnet build WeightTracker.slnx` and `dotnet test WeightTracker.slnx` with Docker running. Verify all unit and integration tests pass

## 6. Template parity and review

- [x] 6.1 Copy `src/WeightTracker.Domain/Workouts/Workout.cs` and `test/WeightTracker.Unit/Domain/Workouts/WorkoutTests.cs` over their `.stpr/template/` counterparts. Run the drift check from `AGENTS.md` and verify it prints no `DRIFT:` line
- [x] 6.2 Walk the `AGENTS.md` quality checklist for this change: workout edit rule enforced in `UpdateWorkoutUseCase`, migration `Down()` reverses it, EF config matches the migration, the mapper round-trips, registrations are in `Program.cs`, both 401 cases exist for `PUT /workouts/{id}`, and every new test has labelled Arrange/Act/Assert with a single Act. Record any deviation in the change

## Workflow follow-up

- Archive the change once review is satisfied, so `workout-details` becomes a main spec and the `workout-creation`, `workout-retrieval` and `workout-editing` deltas are merged.
- Update the frontend and backoffice to send `name` on `POST /workouts` and to use `PUT /workouts/{id}` (separate changes).
