# Design

## Context

See proposal.md (Why, Terminology) for the motivation and the editor/athlete vocabulary. Requirements are in the delta specs under `specs/`.

Current state that shapes the approach:
- `Workout` (`WeightTracker.Domain/Workouts/Workout.cs`) has no lifecycle state. `Create` and `Rehydrate` build it from `UserId`, `TrainerId`, the date and the exercises.
- `WorkoutEditAuthorizer` (`WeightTracker.Application/Services`) is the single gate every editing use case calls (`EnsureCanEdit`). It knows the caller's role and the workout.
- Read visibility is computed separately in `GetWorkoutByIdUseCase` (in memory) and in `GetAllWorkoutsUseCase` (through repository queries chosen by role).
- `Effort` is stored as an `int` column and mapped through `MapEffort` in `WorkoutMapper`, which rejects unknown values with `PersistenceMappingException`. The API exposes it as the enum name through `JsonStringEnumConverter` on requests and `ToString()` on responses.
- The seed builds workouts through `Workout.Rehydrate`.

## Goals / Non-Goals

**Goals:**
- The aggregate owns the status and the business transition graph.
- Role rules for transitions, the edit lock and draft visibility each live in exactly one place in the Application layer.
- Existing integration tests keep their meaning, with seed adjustments only where the new draft rule would hide a seeded workout.

**Non-Goals:**
- No status history, timestamps or audit of who changed the status.
- No status filter query parameter on `GET /workouts`.
- No change to who may edit or read a workout beyond the draft rule and the lock.

## Decisions

### D1. `WorkoutStatus` enum modelled on `Effort`
`WeightTracker.Domain/Workouts/WorkoutStatus.cs` contains `DRAFT = 1, PUBLISHED = 2, STARTED = 3, FINISHED = 4`. Explicit values make the persisted `int` stable. Upper case names match `Effort`, and these names are the API wire values.
- *Alternative*: store the status as a string column. Rejected so persistence stays the same as `effort`.

### D2. The aggregate guards the transition graph
`Workout` gains `Status` (with a private setter) and two action methods:
- `ChangeStatus(WorkoutStatus target)`: validates that `target` is defined (`InvalidWorkoutStatusException`, `400`). It then checks that `(Status, target)` is one of the four business transitions, and throws `InvalidWorkoutStatusTransitionException` (`409`) otherwise. A same-status request is not in the graph, so it throws too.
- `OverrideStatus(WorkoutStatus target)`: validates that `target` is defined, then sets it, skipping the graph. Only the admin path calls it.

`Create` always sets `DRAFT`. `Rehydrate` gains a `WorkoutStatus status` parameter and rejects undefined values.
- *Alternative*: one `ChangeStatus(target, bool force)`. Rejected because a boolean flag hides the admin semantics, while two named methods keep the intent clear at the call site.

### D3. Role rules for status changes live in a new `WorkoutStatusAuthorizer`
`WeightTracker.Application/Services/WorkoutStatusAuthorizer.cs` sits next to `WorkoutEditAuthorizer` and follows its shape. Given `(workout, callerId, callerRole, target)` it returns whether the caller may *attempt* that transition:
- ADMIN → always (the use case then calls `OverrideStatus`).
- Prescribing trainer → only when `{Status, target}` is `{DRAFT, PUBLISHED}` in either direction.
- Athlete of a prescribed workout → only `PUBLISHED → STARTED` or `STARTED → FINISHED`.
- Athlete of a workout with no trainer → any target. The aggregate then enforces the graph.
- Anyone else → no.

A refusal throws `UnauthorizedOperationException` (`403`). This keeps "valid transition but wrong role" (`403`) apart from "not a transition at all" (`409`). To do that without copying the graph, the aggregate exposes `Workout.CanTransitionTo(target)`, which `ChangeStatus` also uses. A move that is outside the lifecycle is not judged by the authorizer for a related caller, so it reaches the aggregate and gets `409`. The athlete of a prescribed `DRAFT` is refused whatever the target, since that workout is hidden from them (`IsHiddenFrom`). Callers unrelated to the workout are refused for every target.

### D4. `ChangeWorkoutStatusUseCase` follows `UpdateWorkoutExerciseUseCase`
`Execute(callerId, callerRole, workoutId, target, ct)`:
1. Load the workout, or throw `WorkoutNotFoundException` (`404`).
2. Call `WorkoutStatusAuthorizer.EnsureCanChange(...)` (`403`).
3. If the caller is an admin, call `workout.OverrideStatus(target)`. Otherwise call `workout.ChangeStatus(target)`.
4. `Save` and `SaveChangesAsync`, then return the workout.

It does **not** go through `WorkoutEditAuthorizer`, because a status change is not a content edit: the athlete of a prescribed workout may not edit it but may start it. The workout edit rule in the invariants is about editing content. This use case does not edit content, so it enforces the status rule instead, and the proposal's Conflicts section records this.

### D5. The edit lock goes in `WorkoutEditAuthorizer`
After the existing permission check, `EnsureCanEdit` throws `WorkoutLockedException` (`409`) when the caller is not an admin and `workout.IsLocked` is true (`Status` is `STARTED` or `FINISHED`). `IsLocked` is a domain query on the aggregate, so the meaning of "locked" lives in the domain. Every edit use case already calls `EnsureCanEdit`, so the lock reaches all five edit routes without touching them. The permission check runs first, so an unauthorized caller still gets `403` and does not learn the workout's status.
- *Alternative*: guard each aggregate edit method (`AddExercise`, ...) with the lock. Rejected because the admin bypass would force a role flag into the domain API, and the authorizer is already the single gate.
- `WorkoutLockedException` is an Application exception, because the rule depends on the caller's role.

### D6. Draft visibility is a domain query used by both read use cases
`Workout.IsHiddenFrom(Id callerId)` returns `Status == DRAFT && TrainerId is not null && UserId == callerId`. It does not depend on role, because it is about whether the caller is the athlete.
- `GetWorkoutByIdUseCase`: after the role check, `IsHiddenFrom` → `UnauthorizedOperationException` (`403`). Admins skip it, because an admin is never the athlete of a prescribed workout in practice and the spec gives admins full visibility.
- `GetAllWorkoutsUseCase`: filter the scoped list in memory with `!IsHiddenFrom(callerId)` before collecting exercise ids, for the TRAINER and USER branches. A TRAINER can be the athlete of a workout an admin prescribed for them, so the TRAINER branch needs the filter too.
- *Alternative*: new repository queries that exclude drafts in SQL. Rejected for now because the lists are small and caller-scoped, so filtering in memory keeps the repository surface unchanged. If the lists grow, the filter can move into a `GetAllBy...` query without changing the specs.

### D7. Persistence
- Migration `AddStatusToWorkouts_<yyyyMMddHHmm>` follows `AddEffortToWorkoutExercises_202610131437`. It adds `status` (`int`, not null) with default `1` (DRAFT) to backfill existing rows, then drops the default. `Down()` deletes the column.
- `WorkoutEntity.Status` is an `int`. The EF configuration in `WeightTrackerDbContext` maps `status` as required.
- `WorkoutMapper` writes `(int)workout.Status` and reads it through `MapStatus` (the same pattern as `MapEffort`, throwing `PersistenceMappingException` on unknown values). It passes the status to `Rehydrate`.

### D8. API
- `PUT /workouts/{workoutId:guid}/status` in `WorkoutsController`, with `ChangeWorkoutStatusRequest { [Required] [JsonConverter(JsonStringEnumConverter<WorkoutStatus>)] WorkoutStatus? Status }`, mirroring `AddOrUpdateExerciseRequest.Effort`. The action returns `200` with `WorkoutDetailResponse`, and every catch delegates to `ExceptionMapper.Map`.
- `WorkoutResponse` and `WorkoutDetailResponse` gain `string Status` (`workout.Status.ToString()`), placed after `TrainerId`.
- `ExceptionMapper`: `InvalidWorkoutStatusException` → `400`. `InvalidWorkoutStatusTransitionException` and `WorkoutLockedException` → `409`.
- Registration in `Program.cs`: `WorkoutStatusAuthorizer` and `ChangeWorkoutStatusUseCase`.

### D9. Seed
`Rehydrate` calls in `DatabaseSeeder` pass a status:
- `TrainedWorkoutId`, `PrescribedPairWorkoutId` and `PrescribedSuperSeriesWorkoutId` are `PUBLISHED`, so the user's existing read, list and flow tests keep their meaning.
- The workouts that have no trainer (`OwnedWorkoutId`, `TrainerOwnWorkoutId`, `SelfCreatedPairWorkoutId` and `SelfCreatedSuperSeriesWorkoutId`) stay `DRAFT`.
- Add `PrescribedDraftWorkoutId` (user + trainer, `DRAFT`) and `PrescribedStartedWorkoutId` (user + trainer, `STARTED`) in `SeedData`, so the visibility and lock tests have fixed fixtures. `FINISHED` and self-planned cases are set up in Arrange through the API (an admin calling `PUT .../status`). Tests never write rows directly.

## Risks / Trade-offs

- [Adding seeded workouts changes list counts in `WorkoutScopingTests`] → Update those assertions in the same task. They assert by id where possible.
- [Status changes bypass `WorkoutEditAuthorizer`, which could look like a breach of the workout edit rule in the invariants] → D4 records the reason, and the status rule is enforced by its own single authorizer.
- [In-memory draft filtering on lists] → It is acceptable at the current scale, and D6 notes how to move it into SQL.
- [A concurrent edit and status change could interleave] → Not handled, as there is no optimistic concurrency today. This is out of scope.

## Migration Plan

- Deploy the migration before or with the API. Existing workouts become `DRAFT`, which hides existing prescribed workouts from their athletes until their trainer publishes them. This is the intended behaviour according to the input ("Set existing workouts as Draft").
- Rollback: `dotnet run --project src/WeightTracker.Migrations -- "<conn>" --down` reverts the whole schema. To revert only this change, roll back to the previous migration version, which drops the `status` column.
