# Proposal

## Why

A workout is only identified by its date, which is not enough for the frontend to list, recognise or describe workouts. There is also no way to change a workout's own properties after creation: the date is fixed, and a workout assigned to the wrong user or trainer cannot be corrected.

## What Changes

- Every workout carries a mandatory `name` (trimmed, 1–256 chars) and an optional `description` (trimmed, up to 1024 chars, blank stored as `null`).
- **BREAKING**: `POST /workouts` requires `name` and accepts an optional `description`. A body without `name` is rejected with `400`.
- Every workout response (`POST /workouts`, `GET /workouts`, `GET /workouts/{id}`, `PUT /workouts/{id}/status` and the new `PUT`) returns `name` and `description`.
- New `PUT /workouts/{id}` replaces a workout's `name`, `description` and `workoutDate`, and its `userId` and `trainerId`. It returns `200` with the updated workout.
  - Changing name, description or date is an edit. It follows the existing workout edit rule, including the lock on `STARTED`/`FINISHED` workouts for non-admins.
  - Changing `userId` or `trainerId` is reserved to `ADMIN`. A non-admin sending values different from the current ones gets `403`; sending them unchanged is allowed.
  - Admin reassignment applies the same owner and trainer checks as creation.
- Existing workouts are migrated with the name `Workout` and no description.
- **Conflict with the current domain**: `Workout.UserId` is documented as immutable once the workout exists. This change makes `UserId` and `TrainerId` mutable through an admin-only aggregate action. The invariants do not forbid it, but the aggregate's doc comment and the template copy of `Workout.cs` must be updated together.

## Capabilities

### New Capabilities

- `workout-details`: updating an existing workout's own properties (name, description, date) and its assignment (owner, trainer) through `PUT /workouts/{id}`, with the role rules for each. Template reference: `WorkoutsController` + `UpdateWorkoutExerciseUseCase` (load, authorize with `WorkoutEditAuthorizer`, mutate, save); assignment checks reuse `CreateWorkoutUseCase`.

### Modified Capabilities

- `workout-creation`: `POST /workouts` requires `name`, accepts `description`, validates both, and returns them. Template reference: `CreateWorkoutUseCase`, `Workout.Create`, `Email` value object for validation.
- `workout-retrieval`: `GET /workouts/{id}` and `GET /workouts` return `name` and `description`. Template reference: `WorkoutsController`, `WorkoutDetailResponse`/`WorkoutResponse`.
- `workout-editing`: the edit rule and the lock also cover updating a workout's name, description and date. Template reference: `WorkoutEditAuthorizer`.

## Business Identifiers

- `Workout.Id` stays immutable.
- `name` is not unique and not an identifier.
- `UserId` and `TrainerId` are references to users, not business identifiers of the workout. They become mutable, by `ADMIN` only. The row keeps its `Id`, so reassignment updates the existing record in place and cannot create duplicates.

## Impact

- **Domain**: `Workout` (new fields, `Create`/`Rehydrate` signatures, two new action methods), new `WorkoutName` and `WorkoutDescription` value objects and their exceptions.
- **Application**: `CreateWorkoutUseCase`, new `UpdateWorkoutUseCase`, and owner/trainer assignment checks shared between the two.
- **Infrastructure**: `WorkoutEntity`, `WorkoutMapper`, `WeightTrackerDbContext`, `DatabaseSeeder`/`SeedData`, plus a new FluentMigrator migration adding `Name` and `Description` to `Workouts`.
- **API**: `CreateWorkoutRequest`, new `UpdateWorkoutRequest`, `WorkoutResponse`, `WorkoutDetailResponse`, `WorkoutsController`, `Program.cs` registration.
- **Template**: `.stpr/template/src/WeightTracker.Domain/Workouts/Workout.cs` and `.stpr/template/test/WeightTracker.Unit/Domain/Workouts/WorkoutTests.cs` must be updated to stay byte-identical.
- **Clients**: the frontend and backoffice must send `name` on creation. Their changes are out of scope.
