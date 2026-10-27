# Design

## Context

See proposal.md for the motivation and specs for the behavior. What exists today:

- `Workout` (`Domain/Workouts/Workout.cs`) holds `UserId`, `TrainerId`, `WorkoutDate`, `Status` and exercises. `UserId` is documented as immutable, and nothing in the aggregate changes `UserId`, `TrainerId` or `WorkoutDate` after creation. `Create` rejects a trainer equal to the owner with `TrainerCannotBeWorkoutOwnerException`.
- `CreateWorkoutUseCase` resolves owner and trainer by role, then checks inline that the owner exists (`UserNotFoundException`, 404) and that an admin-assigned trainer is a TRAINER or ADMIN (`InvalidWorkoutAssignmentException`, 400).
- `WorkoutEditAuthorizer.EnsureCanEdit` enforces the edit rule and the lock (403, then 409). Edit use cases like `UpdateWorkoutExerciseUseCase` load the workout, authorize, mutate and save.
- `WorkoutRepository.Save` copies the scalar columns (`UserId`, `TrainerId`, `WorkoutDate`, `Status`) onto the tracked entity for existing workouts, so new scalars must be added there explicitly.
- `ExceptionMapper` maps any unlisted `DomainException` to 400.
- The controller builds domain values from the request (for example `set.ToDomain()`) and passes them to the use case.

## Goals / Non-Goals

**Goals:**
- Model name and description as value objects, so their rules live in the domain once and are shared by create, update and rehydrate.
- Give the aggregate explicit action methods for the new mutations instead of property setters.
- Share the owner and trainer checks between creation and reassignment.

**Non-Goals:**
- Changing `WorkoutEditAuthorizer` or `WorkoutStatusAuthorizer` logic. The new use case only calls the edit authorizer.
- Changing the shape of `GET /workouts` scoping or `IsHiddenFrom`.

## Decisions

### Layer mapping

| Layer | Change |
| --- | --- |
| Domain | `WorkoutName` and `WorkoutDescription` records in `Domain/Workouts`. `InvalidWorkoutNameException` and `InvalidWorkoutDescriptionException` (both `DomainException`) in `Domain/Exceptions`. `Workout`: `Name` and `Description` properties, new `Create`/`Rehydrate` parameters, plus `UpdateDetails` and `Reassign` action methods. |
| Application | `CreateWorkoutUseCase` takes name and description. New `UpdateWorkoutUseCase`. New `WorkoutAssignmentValidator` service shared by both use cases. |
| Infrastructure | Migration `AddNameAndDescriptionToWorkouts`. `WorkoutEntity.Name`/`Description`, EF column config with max lengths, `WorkoutMapper` both directions, `WorkoutRepository.Save` scalar copy, seed. |
| Api | `CreateWorkoutRequest.Name`/`Description`, new `UpdateWorkoutRequest`, `Name`/`Description` on `WorkoutResponse` and `WorkoutDetailResponse`, `PUT /workouts/{id:guid}` action, DI registration in `Program.cs`. |

Cross-module dependencies: the Workout aggregate keeps referencing users by `Id` only. `UpdateWorkoutUseCase` depends on `IWorkoutRepository`, `IUserRepository` (through the validator), `WorkoutEditAuthorizer` and `IUnitOfWork`.

### Value objects modeled after `Email`

`WorkoutName.From(string?)` trims and throws `InvalidWorkoutNameException` when the result is blank or longer than 256. `WorkoutDescription.From(string?)` trims and returns `null` when the input is null or blank, otherwise it validates the 1024 limit and throws `InvalidWorkoutDescriptionException` when it is exceeded. The aggregate stores `WorkoutName` and `WorkoutDescription?`.

Alternative: plain strings validated in the aggregate. Rejected because the checklist forbids domain-significant primitives left as ad hoc strings, and the same rule is needed in three places (`Create`, `UpdateDetails`, `Rehydrate`).

The value objects live in `Domain/Workouts` and not `Domain/Common`, because only workouts use them.

### Aggregate actions

- `UpdateDetails(WorkoutName name, WorkoutDescription? description, DateTime workoutDate)` replaces the three values and keeps only the date part, as the constructor does. Its inputs are already-valid value objects, so it cannot fail halfway.
- `Reassign(Id userId, Id? trainerId)` throws `TrainerCannotBeWorkoutOwnerException` before mutating when the trainer equals the owner, then sets both. The `UserId` doc comment changes from "immutable" to "changed only through `Reassign`".
- Neither method checks permissions or the lock. Those are Application rules (invariants: permissions live in Application), enforced before the call.

### `UpdateWorkoutUseCase` order of checks

1. Load the workout, or throw `WorkoutNotFoundException` (404).
2. Compute `assignmentChanged = userId != workout.UserId || trainerId != workout.TrainerId`.
3. If the caller is not an admin and `assignmentChanged` is true, throw `UnauthorizedOperationException` (403).
4. Call `WorkoutEditAuthorizer.EnsureCanEdit(..., "PUT /workouts/{id}")` (403, then 409 for non-admins).
5. If `assignmentChanged` is true (only admins get this far with a change), run `WorkoutAssignmentValidator`:
   - when `userId` changed: the new owner must not be the caller (`InvalidWorkoutAssignmentException`, 400) and must exist (`UserNotFoundException`, 404);
   - when `trainerId` changed to a value: the new trainer must be a TRAINER or ADMIN (`InvalidWorkoutAssignmentException`, 400).

   Then call `workout.Reassign(userId, trainerId)`.
6. Call `workout.UpdateDetails(name, description, date)`.
7. Save, then `SaveChangesAsync`. Any earlier throw leaves nothing persisted.

Step 3 runs before step 4 so that a non-admin who tries to reassign always gets 403, never 409. The two checks are not merged into `WorkoutEditAuthorizer`, because reassignment is not part of the edit rule and the authorizer is shared by every edit use case.

Only changed values are validated, so an admin can update the name of a workout whose existing trainer would no longer pass the role check.

### Shared assignment checks

`WorkoutAssignmentValidator` (`Application/Services`, registered scoped) exposes:
- `EnsureOwnerExists(Id ownerId, CancellationToken)`
- `EnsureCanTrain(Id trainerId, CancellationToken)`

Both wrap the existing `IUserRepository` calls. `CreateWorkoutUseCase` switches to it with no behavior change. Its role-based `ResolveAssignment`, and the trainer-to-USER-owner 403, stay in the create use case because they are creation-only. The "admin cannot own" check is a one-line comparison, so the update use case does it inline instead of sharing it.

Alternative: duplicating the two repository checks in the update use case. Rejected by the "no duplicated code" checklist item.

### API contract

`UpdateWorkoutRequest` mirrors `CreateWorkoutRequest`. `[Required]` applies to `Name`, `WorkoutDate` and `UserId`; `Description` and `TrainerId` are nullable. Length rules are not repeated as attributes: the value objects are the single source, and they still return 400. The action returns `WorkoutDetailResponse`, matching `GET /workouts/{id}` and `PUT /workouts/{id}/status`. The route uses `{id:guid}`, as `GetById` does.

`CreateWorkoutRequest.Name` is `[Required]`. The controller builds `WorkoutName`/`WorkoutDescription` inside the existing `try`, so validation errors go through `ExceptionMapper` like every other domain exception.

### Persistence

The migration is `AddNameAndDescriptionToWorkouts_2026102718xx` (non-round minutes). Its shape follows `AddStatusToWorkouts`:
- `Name`: `AsString(256).NotNullable().WithDefaultValue("Workout")`, then the default constraint is deleted.
- `Description`: `AsString(1024).Nullable()`.

`Down()` deletes both columns. EF config gets `HasColumnName("Name").HasMaxLength(256).IsRequired()` and `HasColumnName("Description").HasMaxLength(1024)`. `WorkoutMapper.MapToDomain` builds the value objects inside its existing `DomainException` catch. Reassignment updates the same row by `Id` through the existing tracked-entity path, so it cannot create a duplicate.

### Template parity

`.stpr/template/src/WeightTracker.Domain/Workouts/Workout.cs` and `.stpr/template/test/WeightTracker.Unit/Domain/Workouts/WorkoutTests.cs` are curated copies. They are recopied from `src`/`test` after the change, and the drift check must pass. No other touched file has a template copy.

## Risks / Trade-offs

- [Breaking `POST /workouts` for current clients that do not send `name`] → The frontend and backoffice must add the field. Called out in the proposal; not handled here.
- [Full-replacement `PUT`: a client that omits `trainerId` on a prescribed workout gets 403 as a non-admin, or silently removes the trainer as an admin] → Documented in the spec and in the request DTO's doc comments. Clients send back what they read from `GET /workouts/{id}`.
- [Existing workouts all named `Workout`] → Acceptable for development data. Users can rename them through the new endpoint.
- [`Create`/`Rehydrate` signature change touches about 60 test call sites] → Mechanical update. Tests that do not care about the name use a shared test default instead of a new literal per test.

## Migration Plan

Apply the migration with the migrator or on API startup in Development. Rollback with `--down` drops both columns. Since `Down()` only removes the new columns, earlier migrations are unaffected.
