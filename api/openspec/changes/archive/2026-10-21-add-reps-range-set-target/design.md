# Design

## Context

See `proposal.md` (Why, Conflicts With The Source Plan) and `specs/workout-exercises/spec.md` for the behavior.

`SetTarget` is a closed record hierarchy (`Reps`, `Duration`, `MaxReps`) inside the `Workout` aggregate. Each prescription is one row in `workout_exercise_sets` with a `target_type` discriminator (`SetTargetTypes`) and a nullable `target_value`. Four switches translate the hierarchy, and every one of them must learn the new case:

- `SetPrescriptionRequest.ToDomain()` (API request to domain)
- `SetTargetResponse.From()` (domain to API response)
- `WorkoutMapper.MapTargetToColumns()` / `MapTargetToDomain()` (domain to and from rows)

`.stpr/template/src/WeightTracker.Domain/Workouts/SetTarget.cs` is a curated copy, so it must stay byte-identical to the source after the change.

## Goals / Non-Goals

**Goals:**
- Add the range as one more case of the existing hierarchy, with no new abstraction around it.
- Keep the min/max rule in the domain constructor so every entry point (API, seed, rehydration) is guarded.

**Non-Goals:**
- Reworking `SetTarget` into a generic `{ min, max }` shape for every type.
- Any change to authorization, use cases or endpoints. `AddWorkoutExerciseUseCase` and `UpdateWorkoutExerciseUseCase` already pass `SetPrescription`s through unchanged.

## Layer Mapping

| Layer | Change |
| --- | --- |
| Domain | `SetTarget.RepsRange(int min, int max)`; `SetTargetTypes.RepsRange = "RepsRange"`. |
| Application | None. No new use case, repository method or registration. |
| Infrastructure | `WorkoutExerciseSetEntity.TargetMaxValue` (`int?`); `WeightTrackerDbContext` maps it to `target_max_value`; `WorkoutMapper` writes and reads it. New FluentMigrator migration. |
| Api | `SetPrescriptionRequest.MaxTarget` (`int?`) and its `AllowedValues` list; `SetTargetResponse.MaxValue` (`int?`, omitted when null). |

No cross-domain-area dependency is added.

## Decisions

**1. A new `RepsRange` record, not a flag on `Reps`.**
The hierarchy is already discriminated by type, and every switch is exhaustive by convention. A separate record keeps `Reps` meaning "exactly N" and puts the min/max rule on its own constructor. Alternative: `Reps(int value, int? max)`. Rejected because it makes every `Reps` consumer handle an optional max and blurs the existing meaning.

**2. Validation lives in the `RepsRange` constructor and throws `InvalidSetPrescriptionException`.**
`min < 1` and `max <= min` both throw, with messages that mirror `Reps` (for example `max reps must be greater than min reps, got 8-8`). `ExceptionMapper` already maps that exception to `400`, so no mapping changes. The request DTO stays structural: like `Reps(Target ?? 0)` today, it passes `MaxTarget ?? 0`, so a missing max reaches the domain as `0` and fails the `max > min` rule instead of needing a DTO-level check.

**3. A second nullable column, `target_max_value`, instead of encoding both bounds in `target_value`.**
`target_value` keeps the minimum, so a range reads like reps in the existing column and existing rows are untouched. The new column is null for every non-range type. Alternative: a separate table, or a string `"8-12"`. Rejected: both are heavier and lose integer typing.

**4. Mapper symmetry.**
`MapTargetToColumns` returns a third element, `TargetMaxValue`, null for every case except `RepsRange`. `MapTargetToDomain` takes the max value as well and calls `RequireValue` for both bounds of a range, so a corrupt row fails as `PersistenceMappingException` like the other cases. For non-range types the stored max is ignored on read; writes never set it.

**5. Response field `maxValue`, omitted when null.**
Same `JsonIgnore(WhenWritingNull)` treatment as `Value`, so `Reps`, `Duration` and `MaxReps` payloads are byte-for-byte unchanged.

**6. Migration shape.**
Mirrors `AddEffortToWorkoutExercises_202610131437.cs`: `Alter.Table("workout_exercise_sets").AddColumn("target_max_value").AsInt32().Nullable()`, with `Down()` deleting the column. Nullable, so no default and no backfill step. Naming follows `AddTargetMaxValueToWorkoutExerciseSets_<yyyyMMddHHmm>`.

## Risks / Trade-offs

- [A new switch case is forgotten in one of the four translations] → `MapTargetToColumns` throws on unknown types and `SetTargetResponse.From` falls through to `MaxReps`. The response-shape integration test and the mapper round-trip test cover the new case explicitly so a miss fails a test.
- [Rolling back the migration drops stored ranges] → `Down()` removes the column, and remaining `RepsRange` rows would then fail rehydration. Roll back the code before the schema, which is the normal order.
- [`SetTarget.cs` template copy drifts] → the task list updates `.stpr/template/src/.../SetTarget.cs` and runs the drift check.

## Migration Plan

1. Deploy the migration (additive, nullable column; safe on existing data).
2. Deploy the API.
3. Rollback: revert the API first, then run the migrator with `--down` only if the column must go.
