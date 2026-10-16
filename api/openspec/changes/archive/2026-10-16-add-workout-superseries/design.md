# Design

## Context

See `proposal.md` for motivation and `specs/workout-superseries/spec.md` for the requirements.

- `Workout` (aggregate root) owns `WorkoutExercise` entities. `Exercises` is a copied snapshot; mutation goes through the root (`AddExercise`, `RemoveExercise`), and `Rehydrate` validates stored data (duplicate exercise check).
- `WorkoutExercise.Sets` is a list of `SetPrescription(count, target)`. "Number of sets" is therefore `Sets.Sum(s => s.count)`.
- `WorkoutExercise` today has `ExerciseId`, `Effort` and `Sets`, all read-only after creation. The `Workouts` reference is a **mutable** aggregate.
- Persistence: `WorkoutMapper` maps the aggregate across `workouts`, `workout_exercises`, `workout_exercise_sets`. The closest migration precedent for adding a column to `workout_exercises` is `AddEffortToWorkoutExercises_202610131437`.
- The EF model must mirror the migrations exactly (column names included).

Canonical references (from `.stpr/template/README.md`): `Workouts` (template-backed) for the aggregate and `WorkoutTests`; `WorkoutMapper` and the effort migration (skeleton-backed) for infra.

## Goals / Non-Goals

**Goals:**
- Keep every superseries rule inside the `Workout` aggregate so no caller can build an invalid group.
- Add the column with no data loss for existing rows and a reversible `Down()`.

**Non-Goals:**
- No Application, API, DTO or authorization work (Admin/Trainer is a later use-case rule).
- No new table, no repository contract change, no domain events.
- No reordering of exercises so that superseries members become adjacent.

## Decisions

### Layer mapping

| Layer | Change |
| --- | --- |
| Api | Only `ExceptionMapper` entries for the new exceptions. No controller, DTO or OpenAPI change. |
| Application | None. |
| Domain | `SuperSeriesId` value object, `WorkoutExercise.SuperSeriesId`, `Workout.CreateSuperSeries` / `BreakSuperSeries`, `RemoveExercise` and `Rehydrate` adjustments, new exceptions. |
| Infrastructure | `WorkoutExerciseEntity.SuperSeriesId` (`int?`), EF column `super_series_id`, `WorkoutMapper` both directions. |
| Migrations | One new FluentMigrator migration adding the nullable column. |

Cross-module dependencies: none outside the Workouts slice. `Infra` depends on `Domain` as today; `Domain` takes no new reference. `ExceptionMapper` (Api) references the new Domain exceptions, which is the existing direction.

### 1. `SuperSeriesId` is a positive-integer record in `Domain/Workouts`

A `record` (invariant: value objects are records) wrapping an `int`, with `From(int)` that rejects values below 1 via a new `InvalidSuperSeriesIdException`, and `Value`. It lives in `Domain/Workouts`, not `Common`, because only `Workout` uses it. This is the deviation the proposal documents: `Id` (Guid) cannot express a per-workout integer label.

Alternative: reuse `int?` directly. Rejected, since the plan requires the value object and the invariants ask that domain-meaningful primitives be value objects.

### 2. `WorkoutExercise` carries `SuperSeriesId?`, changed only by the root

`WorkoutExercise` is immutable today (get-only properties). Superseries membership must change after creation, so it gets a `SuperSeriesId? SuperSeriesId { get; private set; }` with `internal` methods `JoinSuperSeries(SuperSeriesId)` and `LeaveSuperSeries()`. `internal` keeps callers from changing it without going through `Workout`, which enforces the rules. `Create` leaves it null; `Rehydrate` gains an optional trailing `SuperSeriesId? superSeriesId = null` parameter so existing callers and tests keep compiling.

Alternative: replace the entry with a new instance on every change. Rejected, because the entry `Id` must stay stable (the row is updated, not deleted and reinserted).

Also add `TotalSets => _sets.Sum(set => set.count)` so the same-count rule reads from one place.

### 3. Aggregate API

```csharp
public SuperSeriesId CreateSuperSeries(IEnumerable<Id> exerciseIds)
public void BreakSuperSeries(SuperSeriesId superSeriesId)
```

Members are identified by `ExerciseId`, matching `RemoveExercise`. `ExerciseId` is unique within a workout (enforced by `AddExercise`, `Rehydrate` and a unique index), so it identifies an entry unambiguously. Passing `WorkoutExercise` objects would let a caller pass a stale snapshot, because `Exercises` returns copies.

`CreateSuperSeries` validates before mutating anything (invariant: validate inputs before changing state), in this order:
1. fewer than two ids → `SuperSeriesRequiresTwoExercisesException`
2. the same id given twice → `DuplicateExerciseInSuperSeriesException` (never silently de-duplicated)
3. an id not in the workout → existing `ExerciseNotInWorkoutException`
4. a member already grouped → `ExerciseAlreadyInSuperSeriesException`
5. differing `TotalSets` → `SuperSeriesSetCountMismatchException`

Only then is the next id computed (`max(existing) + 1`, or 1) and applied to every member. The method returns the new `SuperSeriesId` so the future use case can report it.

`BreakSuperSeries` throws `SuperSeriesNotFoundException` when no entry carries the id; otherwise it calls `LeaveSuperSeries()` on each member.

New exceptions (all extend `DomainException`): `InvalidSuperSeriesIdException`, `SuperSeriesRequiresTwoExercisesException`, `DuplicateExerciseInSuperSeriesException`, `ExerciseAlreadyInSuperSeriesException`, `SuperSeriesSetCountMismatchException`, `SuperSeriesNotFoundException`.

### 4. Id assignment: next integer in the workout

`max + 1` over ids currently in use. No retry or collision pre-check is needed because the aggregate holds every id of its workout and the operation is a single in-memory step. Reuse after a break (break `1`, create again → `1`) is accepted: nothing outside the workout references the id.

Alternative: a per-workout counter column. Rejected, since it adds a column and a rule for no behavior the spec needs.

### 5. `RemoveExercise` dissolves a pair

After removing an entry that had a `SuperSeriesId`, if exactly one member remains with that id, call `LeaveSuperSeries()` on it. This keeps the "never fewer than two" invariant without a second public operation. It is the only change to existing behavior.

### 6. `Rehydrate` validates stored groups

After the existing duplicate-exercise check, group entries by `SuperSeriesId`; any group with fewer than two members → `SuperSeriesRequiresTwoExercisesException`, any group with differing `TotalSets` → `SuperSeriesSetCountMismatchException`. `MapToDomain` already wraps `DomainException` in `PersistenceMappingException`, so a corrupt row surfaces consistently.

### 7. Persistence

- Migration `AddSuperSeriesIdToWorkoutExercises_2610161XXX` (class name `ActionObject_YYYYMMDDHHmm`, `[Migration(<same timestamp>)]`, pick a non-round time after `202610131437`): `Alter.Table("workout_exercises").AddColumn("super_series_id").AsInt32().Nullable();` and `Down()` deleting the column. No default and no backfill: null is the correct value for existing rows.
- No index and no unique constraint. Uniqueness is per workout and only meaningful together with `workout_id`; the aggregate enforces it and a constraint could not express "same id, same workout, different exercises".
- `WorkoutExerciseEntity.SuperSeriesId` (`int?`), configured `.HasColumnName("super_series_id")`.
- `WorkoutMapper`: `MapToEntity` writes `entry.SuperSeriesId?.Value`; `MapToDomain` passes `item.SuperSeriesId is null ? null : SuperSeriesId.From(item.SuperSeriesId.Value)` to `WorkoutExercise.Rehydrate`.

### 8. Error mapping

Each new exception gets an explicit `ExceptionMapper` entry even though no endpoint can raise it yet, because the invariants require every custom exception to have a mapping (today an unlisted `DomainException` would silently fall into the generic 400 branch). Status codes follow the existing groups in `ExceptionMapper.cs`: `InvalidSuperSeriesIdException`, `SuperSeriesRequiresTwoExercisesException`, `DuplicateExerciseInSuperSeriesException` and `SuperSeriesSetCountMismatchException` join the 400 group (invalid input), `SuperSeriesNotFoundException` joins the 404 group with `ExerciseNotInWorkoutException`, and `ExerciseAlreadyInSuperSeriesException` joins the 409 group with `ExerciseAlreadyInWorkoutException`.

### 9. Tests

- Unit, `SuperSeriesIdTests` (new value object): valid, zero, negative, equality.
- Unit, `WorkoutTests`: one test per rule in the spec (create, ids per workout, mismatch, one exercise, duplicate, not in workout, already grouped, break, break leaves others, unknown break, removal dissolves a pair, removal from three, rehydrate invalid groups). Arrange/Act/Assert with exactly one Act.
- Unit, `WorkoutExerciseTests` and `WorkoutMapperTests`: `TotalSets`, round trip with and without a superseries.
- Integration: no new test file, because no endpoint changes. The full `dotnet test WeightTracker.slnx` with Docker proves the migration applies on a fresh Postgres and the EF model matches, and an existing workout test continues to pass against the new column.
- Template parity: refresh the `.stpr/template` copies of `Workout.cs`, `WorkoutExercise.cs` and `WorkoutTests.cs` (the mapper and migration are skeleton-backed and have no copy) and re-run the drift check.

## Risks / Trade-offs

- [Id reuse after a break could confuse a client holding an old id] → No client exists yet; the future API change should return the workout's current groups after each operation. Revisit if a stable id becomes a requirement.
- [`internal` setters on `WorkoutExercise` are a departure from its fully immutable shape] → Scoped to the Domain assembly and only called by `Workout`; covered by aggregate tests.
- [Dissolving a pair on removal is behavior the request did not state] → Recorded as an assumption in the proposal and spec; one-method change if the owner prefers throwing instead.
- [Role rule (Admin/Trainer) has no enforcement point yet] → Called out in the proposal's Out Of Scope; the first use case must enforce it and add the 401/403 matrix.
- [Corrupt stored groups make a workout unloadable] → Surfaces as `PersistenceMappingException`; there is no data today because the column is new and null.

## Migration Plan

1. Deploy the migration (adds a nullable column; online-safe, no rewrite on Postgres 11+).
2. Deploy the code. Existing rows load with no superseries.
3. Rollback: roll the code back first, then run `Down()`. Any superseries data is lost on `Down()`, which is acceptable because nothing writes it until the Application layer exists.

## Open Questions

- Should `Workout` expose a read model of its superseries (for example `SuperSeries : IReadOnlyList<IReadOnlyList<WorkoutExercise>>`) now, or leave that to the change that adds the endpoint? Deferrable; entries already expose `SuperSeriesId`.
