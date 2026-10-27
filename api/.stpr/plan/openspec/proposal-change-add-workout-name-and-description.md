## Change

Add name and description to workout, plus `PUT /workouts/{id}` to update its properties

## Objective

A workout is only identified today by its date, which is not enough for the frontend to list or recognise them.
We want every workout to carry a mandatory name and an optional description, set on creation, returned on every
read, and editable afterwards. We also need a single endpoint for the frontend to update the workout's own
properties (name, description, date) and, for admins only, its assignment (user and trainer).

## Capabilities

- Create a workout with a mandatory `name` and an optional `description` (`POST /workouts`)
- Return `name` and `description` in every workout read (`GET /workouts`, `GET /workouts/{id}`, and any endpoint that returns a `WorkoutResponse`)
- Update a workout's `name`, `description` and `workoutDate` through `PUT /workouts/{id}`
- Reassign a workout's `userId` and `trainerId` through the same `PUT /workouts/{id}`, `ADMIN` only
- Store name and description in DB

## Module Classification

- Workouts -> mutable aggregate -> WeightTracker.Domain/Workouts

Workouts is a mutable aggregate, not a catalog. Name, description, date and assignment all change after creation.

## Canonical Template Mapping

- Domain: Workout -> WeightTracker.Domain/Workouts/Workout.cs -> the aggregate we are modifying; new `Create`/`Rehydrate` parameters and a new action method to update details and another to reassign
- Domain: WorkoutName / WorkoutDescription -> WeightTracker.Domain/Common/Email.cs -> nearest string value object with validation and its own `DomainException`
- Domain: exceptions -> WeightTracker.Domain/Exceptions -> new `InvalidWorkoutNameException` / `InvalidWorkoutDescriptionException`, inheriting `DomainException` so `ExceptionMapper` maps them to 400 without a new entry
- Application: Create -> WeightTracker.Application/UseCases/Workouts/CreateWorkoutUseCase.cs -> extended with name and description
- Application: Update -> WeightTracker.Application/UseCases/Workouts/UpdateWorkoutExerciseUseCase.cs -> nearest use case that loads, authorizes with `WorkoutEditAuthorizer`, mutates and saves a workout
- Application: assignment rules -> WeightTracker.Application/UseCases/Workouts/CreateWorkoutUseCase.cs -> admin reassignment reuses the same owner/trainer validations (owner exists, trainer is TRAINER or ADMIN, trainer is not the owner); extract them to share, do not duplicate
- Application: edit rule -> WeightTracker.Application/Services/WorkoutEditAuthorizer.cs -> the edit rule enforced by the new use case
- Infrastructure: Workout Entity -> WeightTracker.Infra/Entities/WorkoutEntity.cs -> include the new fields
- Infrastructure: EF configuration -> WeightTracker.Infra/Persistence/WeightTrackerDbContext.cs -> column names and max lengths matching the migration
- Infrastructure: Workout Mapper -> WeightTracker.Infra/Mappers/WorkoutMapper.cs -> include the new fields, round-trip
- Migration: Name and Description columns -> WeightTracker.Migrations/Migrations/202610182247_AddStatusToWorkouts.cs -> migration that adds a mandatory column to `Workouts` with a backfill default that is then dropped
- API: Controller -> WeightTracker.Api/Endpoints/Workouts/WorkoutsController.cs -> new `PUT /workouts/{id:guid}` action
- API: DTOs -> WeightTracker.Api/Endpoints/Workouts/Dtos/CreateWorkoutRequest.cs, WorkoutResponse.cs -> add fields; new `UpdateWorkoutRequest.cs` mirroring `CreateWorkoutRequest`
- Seed: WeightTracker.Infra/Seed/DatabaseSeeder.cs, SeedData.cs -> seeded workouts get deterministic names (some with description, some without)
- Tests: unit -> test/WeightTracker.Unit/Domain/Workouts/WorkoutTests.cs and the closest existing use case tests in test/WeightTracker.Unit/Application/Workouts
- Tests: integration -> the closest existing workouts endpoint tests in test/WeightTracker.Integration/Workouts (e.g. the `PUT /workouts/{id}/status` tests) for the new route's matrix

## State And Mutability

- New `Name` column on `Workouts`: `varchar(256)`, not nullable. Existing rows are backfilled with `'Workout'` through a default that the migration drops afterwards (same shape as `AddStatusToWorkouts`)
- New `Description` column on `Workouts`: `varchar(1024)`, nullable. Existing rows stay `NULL`
- `Down()` drops both columns
- `Name`, `Description`, `WorkoutDate`, `UserId` and `TrainerId` are mutable after creation through `PUT /workouts/{id}`
- Status is not changed by `PUT /workouts/{id}`; it keeps its own endpoint `PUT /workouts/{id}/status`

## Business Rules

- `Name` is mandatory. It is trimmed; empty or whitespace-only is rejected; max 256 chars after trimming
- `Description` is optional. It is trimmed; empty or whitespace-only is stored as `null`; max 1024 chars after trimming
- Invalid name or description is rejected by the domain (400) before any aggregate state changes
- Both rules live in value objects (`WorkoutName`, `WorkoutDescription`); the API only does structural validation
- `PUT /workouts/{id}` is a full replacement of the editable properties: `name`, `description` (null clears it), `workoutDate`, `userId`, `trainerId` (null means no trainer). The frontend sends back what it read from `GET /workouts/{id}` with its changes
- Changing name, description or date is an **edit** and follows the workout edit rule from `STPR_INVARIANTS.md` through `WorkoutEditAuthorizer`, including the lock: a `STARTED` or `FINISHED` workout cannot be updated except by an `ADMIN` (409)
- Reassignment (a `userId` or `trainerId` different from the current one) follows the same validations as creation: owner must exist (404), trainer must be a TRAINER or ADMIN (400), trainer cannot be the owner (400)
- The aggregate exposes the updates as action methods (e.g. `UpdateDetails(name, description, date)` and `Reassign(userId, trainerId)`); validation of all inputs happens before any mutation, and the use case does not set properties directly
- The whole request is applied atomically: if any part is rejected, nothing is saved

## Roles And Visibility

- `ADMIN`: can update every property of any workout, including `userId` and `trainerId`, in any status
- Prescribing trainer (`TrainerId`): can update name, description and date of the workouts they prescribe, while not locked
- Owner (`UserId`) of a workout without trainer: can update name, description and date, while not locked
- Owner of a prescribed workout: cannot update it (403), consistent with the edit rule
- Any non-admin sending a `userId` or `trainerId` different from the current values gets 403. Sending the current values unchanged is allowed
- Anyone else: 403. The permission check runs before the lock check, so a caller without permission never learns the status
- A prescribed `DRAFT` hidden from its athlete stays hidden: the athlete gets the same response as for any other workout they cannot edit (403, as the existing edit endpoints already do)
- After an admin reassignment, read visibility follows the new `UserId`/`TrainerId` with the existing `IsHiddenFrom` rule; no new visibility logic

## Business Identifiers And Mutability

- `Workout.Id` is unchanged and immutable
- `Name` is not a unique business identifier; two workouts may share a name
- `UserId` and `TrainerId` are references to users, not business identifiers of the workout; they are mutable only by `ADMIN` as described above

If a future change wants any listed identifier to become mutable, exact persistence semantics must be defined before coding starts.

## Out Of Scope

- Changing status through `PUT /workouts/{id}`
- Changing exercises, sets or superseries through `PUT /workouts/{id}`
- `PATCH` semantics or partial updates
- Name uniqueness, search or filtering by name
- Translations of name or description
- Frontend and backoffice changes

## Notes

- No new identifiers; no `Id.New()` usage is introduced by this change
- `PUT /workouts/{id}` returns `200` with the updated `WorkoutResponse`
- Structural errors (missing body, missing `name`, missing `workoutDate`, missing `userId`) return 400 from model binding/validation
- Register the new use case in `Program.cs`
- Migration file: `YYYYMMDDHHmm_AddNameAndDescriptionToWorkouts.cs` with a non-round timestamp later than `202610271200`
- Verification layers:
  - Unit: `WorkoutName` and `WorkoutDescription` (blank, trim, boundaries 256/257 and 1024/1025, null description); `Workout.Create`/`Rehydrate` with the new fields; `UpdateDetails` and `Reassign` including no mutation on invalid input; update use case for every role branch, the lock, and the non-admin reassignment rejection; mapper round-trip
  - Integration: `POST /workouts` stores and returns name and description, rejects missing/too long name and too long description; `GET` endpoints return the new fields; `PUT /workouts/{id}` matrix: 200 for admin/trainer/owner-without-trainer, admin reassignment of user and trainer, 401 no token, 401 invalid token, 403 owner of prescribed workout, 403 unrelated user, 403 non-admin changing `userId`/`trainerId`, 404 missing workout, 404 missing new owner, 400 invalid trainer, 400 invalid name/description, 409 locked for non-admin, 200 locked for admin
  - Full `dotnet test WeightTracker.slnx` with Docker running
- No deviations from the mapped template references are allowed
