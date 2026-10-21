# Proposal

## Why

A workout today is always editable and always visible to its athlete. A trainer cannot prepare a workout without the athlete seeing it half-built, and nothing stops a workout from changing once the athlete has started it. A status lifecycle (Draft → Published → Started → Finished) fixes both: it controls what the athlete sees and when editing stops.

## What Changes

- New `WorkoutStatus` with four values: `DRAFT`, `PUBLISHED`, `STARTED`, `FINISHED`. Every workout has exactly one status. New workouts start as `DRAFT`. The migration sets every existing workout to `DRAFT`.
- New endpoint `PUT /workouts/{workoutId}/status` that moves a workout to another status.
- The business transitions are `DRAFT → PUBLISHED`, `PUBLISHED → DRAFT`, `PUBLISHED → STARTED` and `STARTED → FINISHED`. Any other transition is rejected with `409 Conflict`, except for admins.
- Who may make which transition (the terms are defined under Terminology):
  - **Admin**: any status to any status, ignoring the business transitions.
  - **Editor who is not the athlete** (the prescribing trainer): `DRAFT ↔ PUBLISHED` only.
  - **Athlete who is not the editor** (`UserId` of a prescribed workout): forward only, `PUBLISHED → STARTED → FINISHED`.
  - **Athlete who is also the editor** (`UserId` of a workout with no trainer): every business transition.
  - A transition that is valid in the business graph but not allowed for the caller's role is rejected with `403 Forbidden`.
- **Edit lock**: no caller except an admin may edit the content (exercises, sets, superseries) of a `STARTED` or `FINISHED` workout. Those calls are rejected with `409 Conflict`. `PUBLISHED` workouts stay editable.
- **Draft visibility**: a `DRAFT` prescribed workout is hidden from its athlete. `GET /workouts` leaves it out of the list, and `GET /workouts/{id}` rejects it with `403 Forbidden`. An athlete who is also the editor always sees their own drafts. Admins and the prescribing trainer see drafts as before.
- Workout responses (`POST /workouts`, `GET /workouts`, `GET /workouts/{id}`) gain a `status` field, written as the enum name in the same way as `effort`. This adds a field and does not break existing clients.

### Terminology

The input document uses "owner" for whoever may edit the workout. The existing specs use "owner" for `UserId`. To keep the existing specs valid, this change uses:
- **Editor**: the caller allowed by the existing workout edit rule. That is the prescribing trainer (`TrainerId`) when there is one, and `UserId` otherwise.
- **Athlete**: the workout's `UserId` (called the "owner" in the existing specs).

### Assumptions recorded

- The third status is named `STARTED`. The input also calls it "In progress".
- When an athlete reads a hidden draft by id, the response is `403 Forbidden`, as for any other out-of-scope workout. It is not `404`.
- If an admin sets a workout to the status it already has, the call succeeds and changes nothing. If any other caller does the same, the response is `409 Conflict`, because that is not a business transition.
- The development and test seed sets each prescribed workout to a status, so the existing scoping tests keep their meaning.
- The template mapping in the input names `UpdateWorkoutExercisesUseCase.cs`, which does not exist. The reference used is `UpdateWorkoutExerciseUseCase.cs`.

## Capabilities

### New Capabilities

- `workout-status`: the status lifecycle, the transition graph, who may make which transition, and the `PUT /workouts/{workoutId}/status` endpoint.
  - Template references: Domain `WeightTracker.Domain/Workouts/Workout.cs` (aggregate) and `WeightTracker.Domain/Workouts/Effort.cs` (enum); Application `WeightTracker.Application/UseCases/Workouts/UpdateWorkoutExerciseUseCase.cs`; Infra `WeightTracker.Infra/Entities/WorkoutEntity.cs` and `WeightTracker.Infra/Mappers/WorkoutMapper.cs`; Migration `WeightTracker.Migrations/Migrations/AddEffortToWorkoutExercises_202610131437.cs` (a mandatory column backfilled through a temporary default) and `AddSuperSeriesIdToWorkoutExercises_202610161647.cs`; API `WeightTracker.Api/Endpoints/Workouts/WorkoutsController.cs`.

### Modified Capabilities

- `workout-editing`: adds the edit lock for `STARTED` and `FINISHED` workouts, which admins bypass. Template reference: `WeightTracker.Application/Services/WorkoutEditAuthorizer.cs`.
- `workout-retrieval`: the response carries `status`, and `DRAFT` prescribed workouts are hidden from their athlete by id and in the list. Template references: `WeightTracker.Application/UseCases/Workouts/GetWorkoutByIdUseCase.cs` and `GetAllWorkoutsUseCase.cs`.
- `workout-creation`: new workouts start as `DRAFT`, and the creation response carries `status`. Template reference: `WeightTracker.Application/UseCases/Workouts/CreateWorkoutUseCase.cs`.

## Module Classification

- **Workouts**: a mutable aggregate in `WeightTracker.Domain/Workouts`. It is not a catalog, and it is not read-only. Status is mutable business state owned by the aggregate.

## Business Identifiers

- None introduced. `WorkoutStatus` is state, not an identifier. Existing identifiers (`Id`, `UserId`, `TrainerId`) stay immutable.

## Conflicts With Template Or Invariants

- None blocking.
- The invariants define the workout edit rule (admin always; the prescribing trainer if any, otherwise the owner). A status change is **not** a content edit, so it follows its own role rule: for example, the athlete of a prescribed workout may start it but may not edit it. The status endpoint therefore does not use the edit rule. Content edits keep it, and the edit lock is added on top of it.
- One tension: the lock on `STARTED` and `FINISHED` workouts depends on the caller's role, because admins bypass it, and the aggregate does not know about roles. `design.md` explains how the aggregate still guards the transition semantics while the lock check that depends on the role stays in the single edit gate (`WorkoutEditAuthorizer`).

## Acceptance Criteria

- Every scenario in the delta specs is covered by tests. Each protected route keeps or gains the `401` cases for a missing token and for an invalid token.
- `dotnet test WeightTracker.slnx` passes with Docker running.
- The `.stpr/template` parity check reports no drift.

## Impact

- **Domain**: new `WorkoutStatus` enum, a `Status` field on `Workout`, transition methods, and new domain exceptions.
- **Application**: new `ChangeWorkoutStatusUseCase`. `WorkoutEditAuthorizer` gains the edit lock, and both read use cases apply the draft visibility rule.
- **Infra**: a `Status` column on `WorkoutEntity`, a mapper round-trip, an EF configuration update and seed statuses.
- **Migrations**: a new FluentMigrator migration that adds `workouts.status`.
- **API**: a new route, a request DTO, `status` added to the response DTOs, and new `ExceptionMapper` entries.
- **Tests**: unit tests (domain, use cases, mapper) and integration tests (the new route matrix, the edit lock on each edit route, and draft visibility).
