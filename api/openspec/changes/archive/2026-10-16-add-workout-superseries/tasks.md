# Tasks

## 1. SuperSeriesId value object

- [x] 1.1 Add `InvalidSuperSeriesIdException` (extends `DomainException`) in `Domain/Exceptions` and the `SuperSeriesId` record in `Domain/Workouts` (positive integer, `From(int)`, `Value`); verify `dotnet build WeightTracker.slnx` passes
- [x] 1.2 Add `SuperSeriesIdTests` in `test/WeightTracker.Unit/Domain/Workouts` (valid value, zero, negative, equality by value) with Arrange/Act/Assert phases; verify `dotnet test test/WeightTracker.Unit --filter SuperSeriesIdTests` passes

## 2. WorkoutExercise membership

- [x] 2.1 Add `SuperSeriesId?` (private set), `TotalSets`, internal `JoinSuperSeries` / `LeaveSuperSeries`, and an optional trailing `superSeriesId` parameter on `Rehydrate`; `Create` leaves it null; verify existing `WorkoutExerciseTests` still pass unchanged
- [x] 2.2 Add `WorkoutExerciseTests` for `TotalSets` (sum of prescription counts, including a mix of prescriptions) and for `Create` / `Rehydrate` membership; verify the new tests pass

## 3. Workout aggregate rules

- [x] 3.1 Add the exceptions `SuperSeriesRequiresTwoExercisesException`, `DuplicateExerciseInSuperSeriesException`, `ExerciseAlreadyInSuperSeriesException`, `SuperSeriesSetCountMismatchException`, `SuperSeriesNotFoundException` in `Domain/Exceptions`; verify the solution builds
- [x] 3.2 Implement `Workout.CreateSuperSeries(IEnumerable<Id>)` with validation in the order given in design.md before any state change, next-id assignment (`max + 1`, starting at 1) and the returned `SuperSeriesId`; verify with the unit tests of 3.5
- [x] 3.3 Implement `Workout.BreakSuperSeries(SuperSeriesId)` and the ungroup-on-removal rule in `RemoveExercise`; verify with the unit tests of 3.5
- [x] 3.4 Validate stored superseries in `Workout.Rehydrate` (group of one, differing totals); verify with the unit tests of 3.5
- [x] 3.5 Add `WorkoutTests` mirroring the existing `WorkoutTests` shape, one test per scenario in `specs/workout-superseries/spec.md` (group, ids per workout, mismatch, mixed prescriptions with equal totals, single exercise, duplicate id, exercise not in workout, already grouped, break, break leaves other group, unknown break, nothing mutated after a rejected create, removal dissolves a pair, removal from three, rehydrate group of one, rehydrate mismatch); verify `dotnet test test/WeightTracker.Unit --filter WorkoutTests` passes with exactly one `// Act` per test
- [x] 3.6 Review the `Workout` / `WorkoutExercise` changes against the `Workouts` template reference and `STPR_INVARIANTS.md` (private constructors, `IsNew` untouched, no domain events, value object is a record, no framework reference in Domain); fix any deviation before closing the group

## 4. Persistence

- [x] 4.1 Add `AddSuperSeriesIdToWorkoutExercises_YYYYMMDDHHmm` in `WeightTracker.Migrations/Migrations` (timestamp after `202610131437`, not a round time) adding nullable int `super_series_id`, with a `Down()` that drops it; verify the migrator runs up and with `--down` against the local Postgres, then up again
- [x] 4.2 Add `SuperSeriesId` (`int?`) to `WorkoutExerciseEntity` and `.HasColumnName("super_series_id")` in `WeightTrackerDbContext`; verify the column name matches the migration and the solution builds with warnings as errors
- [x] 4.3 Map `SuperSeriesId` in both directions in `WorkoutMapper`; verify with the tests of 4.4
- [x] 4.4 Extend `WorkoutMapperTests` with a round trip with a superseries, a round trip without one, and a stored group of one that raises `PersistenceMappingException`; verify `dotnet test test/WeightTracker.Unit --filter WorkoutMapperTests` passes
- [x] 4.5 Review the migration, entity, EF configuration and mapper against `AddEffortToWorkoutExercises_202610131437`, `WorkoutMapper` and the EF checklist items in `AGENTS.md` before closing the group

## 5. Error mapping

- [x] 5.1 Add explicit `ExceptionMapper` entries for all six new exceptions, following the groups in design.md section 8; verify with a unit or mapper-level test that each exception maps to its expected status, using the existing `ExceptionMapper` tests as the shape if there are any, and otherwise state in the PR that no mapper test exists to extend

## 6. Template parity and full verification

- [x] 6.1 Copy the changed `Workout.cs`, `WorkoutExercise.cs` and `WorkoutTests.cs` into `.stpr/template/src` and `.stpr/template/test`, and add `SuperSeriesId.cs` to `.stpr/template/src/WeightTracker.Domain/Workouts` only if the template should show it; verify the drift check in `AGENTS.md` prints no `DRIFT:` line
- [x] 6.2 Run `docker compose -f ../infra/docker-compose.yml up -d` if needed, then the full `dotnet test WeightTracker.slnx` with Docker running; verify unit and integration suites are green with no test skipped
- [x] 6.3 Review the whole change against `.stpr/review/CHECKLIST_REVISION.md` and the quality checklist in `AGENTS.md` (no Application/API files touched beyond `ExceptionMapper`, no EF Core migration generated, `ValueGeneratedNever` untouched), and confirm the capability spec scenarios each have a covering test

## Workflow follow-up

- Archive the change after review; the first Application/API change that uses superseries must enforce the Admin/Trainer rule and add the per-route `401`/`403` matrix.
