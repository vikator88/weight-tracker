# Tasks

## 1. Domain: `RepsRange` set target

- [x] 1.1 Add `SetTargetTypes.RepsRange = "RepsRange"` in `WeightTracker.Domain/Workouts/SetTargetTypes.cs`; verify `dotnet build WeightTracker.slnx` passes.
- [x] 1.2 Add `SetTarget.RepsRange(int min, int max)` in `SetTarget.cs`, following the `Reps` record shape (lowercase `min`/`max` properties, XML doc, `ToString()` as `"8-12 reps."`). Throw `InvalidSetPrescriptionException` when `min < 1` or `max <= min`; verify it builds.
- [x] 1.3 Extend `test/WeightTracker.Unit/Domain/Workouts/SetTargetTests.cs` mirroring the existing `Reps` cases: valid range keeps both bounds, `min` 0 throws, `max == min` throws, `max < min` throws, `ToString()`. Arrange / Act / Assert phases. Verify the unit suite passes.
- [x] 1.4 Copy the changed `SetTarget.cs` to `.stpr/template/src/WeightTracker.Domain/Workouts/SetTarget.cs` and verify the drift check in `AGENTS.md` prints nothing.
- [x] 1.5 Review: compare `RepsRange` against the existing `SetTarget` cases and the `Workouts` reference in `.stpr/template/` (naming, validation in constructor, exception type); record any deviation or confirm none.

## 2. Persistence: column, EF model and mapper

- [x] 2.1 Add migration `AddTargetMaxValueToWorkoutExerciseSets_<yyyyMMddHHmm>.cs` in `WeightTracker.Migrations/Migrations`, mirroring `AddEffortToWorkoutExercises_202610131437.cs`: nullable `int` column `target_max_value` on `workout_exercise_sets`, `Down()` deletes it. Verify it builds and the migrator runs up and `--down` against the compose database.
- [x] 2.2 Add `int? TargetMaxValue` to `WorkoutExerciseSetEntity` (doc: null except for `RepsRange`) and update the `TargetType` doc comment; map it to `target_max_value` in `WeightTrackerDbContext`. Verify the column name matches the migration.
- [x] 2.3 In `WorkoutMapper`, return `TargetMaxValue` from `MapTargetToColumns` (null for every case but `RepsRange`), write it in `MapToEntity`, and handle `SetTargetTypes.RepsRange` in `MapTargetToDomain` with `RequireValue` on both bounds. Verify it builds.
- [x] 2.4 Extend `test/WeightTracker.Unit/Infra/Mappers/WorkoutMapperTests.cs` following its existing target cases: `RepsRange` round-trips, non-range targets write a null max, and a `RepsRange` row with a null max throws `PersistenceMappingException`. Verify the unit suite passes.
- [x] 2.5 Extend `test/WeightTracker.Integration/Repositories/WorkoutRepositoryTests.cs` with a save-and-load of a workout holding a `RepsRange` set, following its existing set-target test. Verify with `dotnet test` (Docker running).
- [x] 2.6 Review: compare the migration against the `CreateWorkoutsTable` / add-column references and the mapper against `WorkoutMapper`'s existing discriminated mapping; confirm EF config mirrors the migration exactly.

## 3. API: request and response

- [x] 3.1 In `AddOrUpdateExerciseRequest.cs`, add `int? MaxTarget` to `SetPrescriptionRequest` (doc: maximum reps for `RepsRange`, ignored otherwise), add `SetTargetTypes.RepsRange` to `AllowedValues` and the `TargetType` doc, and map it in `ToDomain()` as `new SetTarget.RepsRange(Target ?? 0, MaxTarget ?? 0)`. Verify it builds.
- [x] 3.2 In `WorkoutResponse.cs`, add `int? MaxValue` to `SetTargetResponse` with `JsonIgnore(WhenWritingNull)` and project `SetTarget.RepsRange` as `{ type: "RepsRange", value: min, maxValue: max }`. Verify it builds.
- [x] 3.3 Extend `test/WeightTracker.Integration/Workouts/WorkoutExerciseAddEndpointTests.cs`, mirroring its set-validation cases: `3 x RepsRange 8-12` returns the range in the response and persists it; `maxTarget` equal to, lower than, or missing, and `target` 0, each respond `400` and leave the workout unchanged; `Reps` with a `maxTarget` returns no `maxValue`. Authenticate with `AuthenticatedClientFor`. Verify with `dotnet test`.
- [x] 3.4 Extend `test/WeightTracker.Integration/Workouts/WorkoutExerciseUpdateEndpointTests.cs` with an update to a valid range and one invalid range (`400`, unchanged), following its existing cases. Verify with `dotnet test`.
- [x] 3.5 Extend `test/WeightTracker.Integration/Workouts/WorkoutResponseShapeTests.cs` to assert that non-range targets omit `maxValue`, next to `MaxRepsTarget_ShouldBeSerializedWithoutAValue`. Verify with `dotnet test`.
- [x] 3.6 Review: compare the DTO and response changes against `WorkoutsController` / existing DTO patterns (structural validation only, domain owns value rules) and confirm `ExceptionMapper` needs no change.

## 4. Final verification

- [x] 4.1 Run `dotnet build WeightTracker.slnx` (warnings are errors) and the full `dotnet test WeightTracker.slnx` with Docker running; all tests pass.
- [x] 4.2 Run the template drift check from `AGENTS.md`; it prints nothing.
- [x] 4.3 Walk the `AGENTS.md` quality checklist and confirm each applicable item.

## Workflow follow-up

- Sync the delta spec into `openspec/specs/workout-exercises/spec.md` and archive the change after review.
