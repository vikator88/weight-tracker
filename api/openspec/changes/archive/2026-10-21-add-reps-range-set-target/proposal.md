# Proposal

## Why

A set target can only be a fixed number of reps, a duration or max reps. Trainers often prescribe a rep range instead ("3 x 8-12"), where any count between the minimum and the maximum completes the set. Today that intent cannot be stored, so it ends up approximated as a fixed rep count.

## What Changes

- Add a fourth set target type, `RepsRange`, carrying a minimum and a maximum number of reps.
- Domain rule: the minimum is at least 1 and the maximum is strictly greater than the minimum. Equal or lower maximums are rejected.
- `POST /workouts/{workoutId}/exercises` and `PUT /workouts/{workoutId}/exercises/{exerciseId}` accept `targetType: "RepsRange"`, with `target` as the minimum and a new optional `maxTarget` as the maximum. `maxTarget` is required for `RepsRange` and ignored for every other type, the same way `MaxReps` already ignores `target`.
- Workout responses project a range as `{ "type": "RepsRange", "value": <min>, "maxValue": <max> }`. `maxValue` is omitted for every other type, so existing payloads are unchanged.
- Persistence: a new nullable column `target_max_value` on `workout_exercise_sets`, null for every type except `RepsRange`. Existing rows need no backfill.

No breaking changes: the request field and the response field are both additive and optional.

## Capabilities

### New Capabilities

None.

### Modified Capabilities

- `workout-exercises`: the "Set prescriptions are validated" requirement gains the `RepsRange` target type, its min/max rules, and how `maxTarget` is treated for the other types.
  Canonical template reference: `Workouts` aggregate (`WeightTracker.Domain/Workouts/SetTarget.cs`, `SetTargetTypes.cs`) for the value object; `WorkoutMapper` for the discriminated mapping; `CreateWorkoutsTable` migration shape, with `AddEffortToWorkoutExercises_202610131437.cs` as the closest add-column example; `WorkoutsController` / `AddOrUpdateExerciseRequest` for the request DTO.

## Module Classification

| Affected area | Classification | Read-only? |
| --- | --- | --- |
| `WeightTracker.Domain/Workouts` (`SetTarget`) | Value object inside the mutable `Workout` aggregate | No. Not a catalog: set prescriptions are replaced on every exercise update. |
| `WeightTracker.Infra/Mappers/WorkoutMapper` | Aggregate mapper for the mutable `Workout` aggregate | No. Writes and reads the set rows. |
| `WeightTracker.Api/Endpoints/Workouts` | REST controller and DTOs for the mutable aggregate | No. Request and response DTOs both change. |
| `WeightTracker.Migrations` | FluentMigrator migration | N/A. Schema only. |

## Business Identifiers

No business identifier is introduced or changed. Set rows keep their existing `Id.New()` identifiers and are still replaced wholesale on update.

## Conflicts With The Source Plan

Resolved while planning, recorded here so implementation does not reopen them:

- The source plan maps the new column to the `workout_exercises` table. Set targets live in `workout_exercise_sets` (`target_type`, `target_value`), one row per prescription, so the column goes there. The add-column example is still the reference for the migration's shape.
- The source plan names the request field `MaxReps`, which collides with the existing `MaxReps` target type. The user chose `maxTarget` for the request, `maxValue` for the response and `target_max_value` for the column.
- The source plan's type name "Range" becomes `RepsRange`, to state that it bounds reps and not seconds.

No conflict with `.stpr/template/STPR_INVARIANTS.md` or the template. No deviation from the mapped references is allowed.

## Out Of Scope

- Duration ranges (for example 30 to 45 seconds).
- Recording the reps an athlete actually performed against the range.
- Changing seed data. Seeded workouts keep their current targets.

## Acceptance Criteria

- An exercise added or updated with `3 x RepsRange`, `target 8`, `maxTarget 12` is stored and returned as 3 sets of 8 to 12 reps.
- `RepsRange` with a missing `maxTarget`, a `maxTarget` equal to or lower than `target`, or a `target` below 1 responds `400 Bad Request` and changes nothing.
- A `maxTarget` sent with `Reps`, `Duration` or `MaxReps` is ignored and not stored.
- Responses for non-range targets do not contain `maxValue`.
- A range round-trips through the mapper and the repository.
- The full `dotnet test WeightTracker.slnx` passes with Docker running.

## Impact

- Domain: `SetTarget.cs`, `SetTargetTypes.cs`.
- Infra: `WorkoutExerciseSetEntity`, `WeightTrackerDbContext` set configuration, `WorkoutMapper`.
- Migrations: one new migration adding `target_max_value` to `workout_exercise_sets`.
- Api: `AddOrUpdateExerciseRequest` (`SetPrescriptionRequest`), `WorkoutResponse` (`SetTargetResponse`).
- Tests: unit tests for `SetTarget` and `WorkoutMapper`; integration tests for the add and update endpoints, the response shape and the repository.
- No new endpoints, so the auth matrix is unchanged. `ExceptionMapper` already maps `InvalidSetPrescriptionException` to `400`.
