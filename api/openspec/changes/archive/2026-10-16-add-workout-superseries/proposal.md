# Proposal

## Why

A superseries is two or more exercises with the same number of sets, performed set by set: the first set of every exercise back to back without rest, then a rest, then the next set of every exercise. The `Workout` aggregate can only describe exercises one after another, so trainers cannot prescribe this common training technique. This change defines the business rules in the domain and the persistence that backs them, before any use case or endpoint is built on top.

## What Changes

- `Workout` aggregate gains a way to group existing exercises into a superseries and a way to break one. Breaking only ungroups; the exercises stay in the workout.
- New value object `SuperSeriesId` (integer) identifies a superseries **within one workout**.
- `WorkoutExercise` carries an optional `SuperSeriesId` (null = not in a superseries).
- New domain exceptions for the new rules (see Business Rules), each with an explicit `ExceptionMapper` entry. No endpoint raises them yet, but the invariants require every custom exception to be mapped.
- `workout_exercises` table gets a nullable integer column `super_series_id` via a FluentMigrator migration; `WorkoutExerciseEntity`, the EF configuration and `WorkoutMapper` round-trip it.
- Unit tests for the value object, the aggregate rules and the mapper round-trip. Integration suite re-run to prove the migration and mapping against real Postgres.

### Business Rules

- A superseries needs at least two exercises (assumption, implied by the definition above).
- Every exercise in a superseries must have the same total number of sets. Total sets are the sum of `count` over the exercise's `SetPrescription` list, because a prescription is `count x target` (assumption; the request says "same number of sets").
- An exercise can belong to at most one superseries.
- Only exercises already in the workout can be grouped.
- Breaking a superseries sets `SuperSeriesId` to null on its members and never deletes them. Breaking an unknown id is an error.
- A `SuperSeriesId` is unique only inside its workout; two workouts may both have superseries `1`.
- Assumption: the aggregate assigns the id (next integer after the highest one in use in that workout), so callers never choose it.
- Assumption: removing an exercise from a superseries that would be left with one member also ungroups that member, so no superseries with fewer than two exercises can exist. `Rehydrate` rejects stored data that violates the rules above.

### Roles And Visibility

Only Admin or Trainer may group or break superseries. That check is an Application-layer permission and Application/API are out of scope here, so this change adds no authorization code. The follow-up use cases must enforce it.

## Capabilities

### New Capabilities

- `workout-superseries`: grouping and ungrouping of exercises inside a workout, including the same-set-count rule, per-workout identity of a superseries, and its persistence round-trip.
  - Canonical template reference: `Workouts` (aggregate root with value objects, `template-backed`), with `WorkoutMapper` and the `AddEffortToWorkoutExercises_202610131437` migration as `skeleton-backed` references for the mapping and the added column, and `WorkoutTests` for the unit test shape.

### Modified Capabilities

None. `openspec/specs/` has no existing capabilities. Existing workout behavior (create, add/remove exercise, rehydrate duplicate check) is unchanged except for the ungroup-on-removal rule above.

## Business Identifiers And Mutability

- `SuperSeriesId`: **immutable** once assigned to a group. A member can leave (the group is broken, or it dissolves on removal) but an id never changes to another value. The id of a broken superseries may be reassigned later to a new group; no row outlives its group.
- Uniqueness scope: `(workout, SuperSeriesId)`. No table or unique constraint is added; the `workout_exercises` column has no cross-workout uniqueness.
- Existing identifiers (`Workout.Id`, `WorkoutExercise.Id`, `ExerciseId`) are unchanged and stay `Id` (Guid).

## Template Conflicts

- **Per-entity identifier type.** The invariants say not to create identifier-specific abstractions unless the plan explicitly requires specific format or semantics. This plan does: `SuperSeriesId` is an integer scoped to a workout, which `Id` (Guid) cannot express. It is a group label inside the aggregate, not an entity or aggregate identifier, so the rule "entities and aggregates Id must be Guids" does not apply. It lives in `Domain/Workouts`, not `Domain/Common`, because only this aggregate uses it. Needs a unit test as a new value object.
- **Generated identifier.** The invariants require generated identifiers to come from a plan-approved abstraction. This plan approves the aggregate as that abstraction (next integer in the workout), not `Id.New()`, and adds no preflight collision check because the aggregate sees all ids of its workout.
- **Exception mapping.** "Every custom exception must have an explicit mapping" while the API is out of scope. Resolved in design: add the mapping with the exceptions so none relies on the generic `DomainException` fallback.

## Impact

- Domain: `WeightTracker.Domain/Workouts` (`Workout`, `WorkoutExercise`, new `SuperSeriesId`), `WeightTracker.Domain/Exceptions` (new exceptions).
- Infra: `WorkoutExerciseEntity`, `WeightTrackerDbContext` configuration, `WorkoutMapper`.
- Migrations: one new migration following `ActionObject_YYYYMMDDHHmm`, with a `Down()` that drops the column. Existing rows keep `null`.
- API: none beyond the `ExceptionMapper` entries. No DTO, endpoint or OpenAPI change.
- Application: none. No use case, no repository contract change.
- Template parity: `.stpr/template/src` copies of `Workout.cs`, `WorkoutExercise.cs` and `WorkoutTests` must be refreshed to stay byte-identical with their `src/` counterparts.
- Verification: unit tests (value object, aggregate rules, mapper round-trip) and the full `dotnet test WeightTracker.slnx` with Docker running.

## Out Of Scope

- Application use cases, repository contract changes, controllers, DTOs and OpenAPI changes.
- Role enforcement (Admin or Trainer) and the `401`/`403` test matrix; they arrive with the first endpoint.
- Seed data containing superseries.
- Any ordering rule between superseries members and `Position`, beyond preserving the existing order.
