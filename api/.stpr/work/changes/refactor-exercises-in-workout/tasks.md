# Tasks: Refactor Exercises In Workout

Do the tasks in order. Each phase ends with `dotnet build WeightTracker.slnx`, where warnings are errors, except while the
build is expected to be red (see below). Run the affected tests as soon as they compile again.

References: `plan.md` in this folder, `.stpr/template/STPR_INVARIANTS.md`, `.stpr/template/README.md`
and `AGENTS.md`.

The build is expected to be **red from task 9**. `src` is green again after task 28, and the test
projects after task 36. `Workout` is retyped before its callers are adapted, and the compiler errors are the list of call sites to fix. Adapt each one as specified.
Do not reintroduce `Exercise` into the Workout aggregate to make the build compile.

---

## Phase 0 — Prerequisites

1. Start Docker. Check that `dotnet --version` reports 10.0.x.
2. Run `dotnet build WeightTracker.slnx` and `dotnet test WeightTracker.slnx` on the untouched tree.
   **Record the passing unit and integration counts.** They are the baseline.
3. Run the `.stpr/template` parity check from `AGENTS.md`, and write down any drift that already exists.

## Phase 1 — Domain types

4. Add `src/WeightTracker.Domain/Workouts/Effort.cs` with the explicit values `LOW = 1`, `MIDLOW = 2`,
   `MID = 3`, `MIDHIGH = 4` and `HIGH = 5`. Copy the shape of `BodyParts`.
5. Add these exceptions to `src/WeightTracker.Domain/Exceptions/`. Each derives from `DomainException`.
   - `InvalidEffortException(int value)`: copy the shape of `InvalidBodyPartException`. The message is
     `'{value}' is not a known effort`.
   - `ExerciseAlreadyInWorkoutException(Id exerciseId, Id workoutId)`, with the message
     `Exercise '{exerciseId}' is already part of workout '{workoutId}'`.
6. Retype `ExerciseNotInWorkoutException` to `(Id exerciseId, Id workoutId)`, with the message
   `Exercise '{exerciseId}' is not part of workout '{workoutId}'`.
7. Add `src/WeightTracker.Domain/Workouts/WorkoutExercise.cs`, a `sealed class` that extends `Entity`.
   Follow plan §3:
   - Private fields `_sets: List<SetPrescription>` and `_effort`.
   - Public `ExerciseId { get; }` and `Effort { get; }`.
   - `Sets => _sets.ToList()`, typed as `IReadOnlyList<SetPrescription>`.
   - The private constructor sets `Id = Id.New()`. Use it only from:
     - `public static Create(Id exerciseId, Effort effort, IEnumerable<SetPrescription> sets)`, which
       sets `IsNew = true`;
     - `public static Rehydrate(Id id, Id exerciseId, Effort effort, IEnumerable<SetPrescription> sets)`,
       which sets `Id = id` and `IsNew = false`.
   - Both factories throw `InvalidEffortException((int)effort)` when `Enum.IsDefined(effort)` is false.
     Both copy `sets` into a new list.
8. Add `test/WeightTracker.Unit/Domain/Workouts/WorkoutExerciseTests.cs` for the `WorkoutExerciseTests`
   cases in plan §12. Copy the Arrange/Act/Assert structure from `WorkoutTests`.

## Phase 2 — Workout aggregate

9. Rework `src/WeightTracker.Domain/Workouts/Workout.cs`:
   - Remove `using WeightTracker.Domain.Exercises;`.
   - `_exercises` becomes `List<WorkoutExercise>`. `Exercises` becomes
     `IReadOnlyList<WorkoutExercise> => _exercises.ToList()`.
   - `Rehydrate(Id id, Id userId, Id? trainerId, DateTime workoutDate, IEnumerable<WorkoutExercise> exercises)`
     copies the list. When two entries share an `ExerciseId`, it throws
     `ExerciseAlreadyInWorkoutException(duplicateExerciseId, id)`.
   - `AddExercise(Id exerciseId, Effort effort, IEnumerable<SetPrescription> sets)`:
     - When `_exercises` already holds that `ExerciseId`, throw `ExerciseAlreadyInWorkoutException`
       before changing anything.
     - Otherwise append `WorkoutExercise.Create(exerciseId, effort, sets)`.
   - `RemoveExercise(Id exerciseId)`: throw `ExerciseNotInWorkoutException(exerciseId, Id)` when the
     exercise is absent. Otherwise remove the matching entry.
   - Update the XML docs, including the `Exercises` "snapshot" doc.
   - Do not call `AddDomainEvent`.
10. In `src/WeightTracker.Domain/Common/Entity.cs`, reword the `<remarks>`. The equality rationale must
    no longer cite `Workout` dictionary keys. Equality code is unchanged.
11. Rewrite `test/WeightTracker.Unit/Domain/Workouts/WorkoutTests.cs` for the `WorkoutTests` cases in
    plan §12. Delete `AddExercise_ShouldMergeIntoAnEquivalentExerciseInstance` and
    `RemoveExercise_ShouldAcceptAnEquivalentExerciseInstance`. Keep the `Create` / `Rehydrate` /
    trainer cases.
12. **Review (domain):** check plan §13 "Domain". `grep` `src/WeightTracker.Domain/Workouts` for
    `Exercises;` (the namespace import). It must find nothing.

## Phase 3 — Migration

13. Add `src/WeightTracker.Migrations/Migrations/AddEffortToWorkoutExercises_202610131437.cs`, with
    `[Migration(202610131437)]`:
    - `Up()`:
      1. `Alter.Table("workout_exercises").AddColumn("effort").AsInt32().NotNullable().WithDefaultValue(3)`,
         with a comment saying `3` is `Effort.MID` and is used only to backfill existing rows;
      2. `Delete.DefaultConstraint().OnTable("workout_exercises").OnColumn("effort")`.
    - `Down()`: `Delete.Column("effort").FromTable("workout_exercises")`.
14. Extend `test/WeightTracker.Integration/Migrations/MigrationsTests.cs`:
    - `AddEffort_ShouldBackfillExistingRowsWithMid`:
      1. Migrate up to `202610080944`.
      2. Insert one user, one exercise, one workout and one `workout_exercises` row with SQL.
      3. Migrate up to the latest version.
      4. Assert that `effort = 3`.
    - `AddEffort_Down_ShouldRemoveTheColumn`: migrate to the latest version, then down to `202610080944`,
      then assert that the column no longer exists.
15. **Review (migration):** the class name follows `ActionObject_YYYYMMDDHHmm`. `Down()` reverses `Up()`.
    No EF migration was generated.

## Phase 4 — Persistence

16. `src/WeightTracker.Infra/Entities/WorkoutExerciseEntity.cs`: add `public int Effort { get; set; }`,
    with an XML doc saying it holds the `Effort` enum value. Update the `<remarks>`: the exercise row
    is no longer loaded by the Workout repository.
17. `WeightTrackerDbContext`: add `entity.Property(workoutExercise => workoutExercise.Effort).HasColumnName("effort");`
    to the `WorkoutExerciseEntity` block. Keep the unique index and the FK with no navigation.
18. `src/WeightTracker.Infra/Mappers/WorkoutMapper.cs`:
    - `MapToEntity`: iterate over `workout.Exercises` with a position counter. Set
      `Id = workoutExercise.Id.Value`, `ExerciseId = workoutExercise.ExerciseId.Value` and
      `Effort = (int)workoutExercise.Effort`. Map the sets from `workoutExercise.Sets` as today, including
      `Id.New()` for set rows.
    - `MapToDomain(WorkoutEntity entity)`: remove the `exercises` parameter, the unknown-exercise branch
      and the `Domain.Exercises` using. Order the rows by `Position`.
    - Build each entry with `WorkoutExercise.Rehydrate(Id.From(row.Id), Id.From(row.ExerciseId), MapEffort(row.Effort), sets)`
      inside the existing `try` / `catch (DomainException)`, so that failures are wrapped in
      `PersistenceMappingException`.
    - Add a private `MapEffort(int)`, copying `ExerciseMapper.MapBodyPart`: an undefined value throws
      `PersistenceMappingException`.
19. `src/WeightTracker.Infra/Repositories/WorkoutRepository.cs`:
    - Remove `LoadReferencedExercises`, and call `WorkoutMapper.MapToDomain(entity)`.
    - In `Reconcile`, key by `WorkoutExerciseEntity.Id` instead of `ExerciseId`. For a surviving row,
      refresh `Position` and `Effort` and replace its `Sets`, as today. Remove dropped rows from the
      navigation and add new ones.
    - Rewrite the `<remarks>` in plan §6 terms, so they explain why EF orders a delete and re-insert of
      the same `(workout_id, exercise_id)`.
    - Leave `ExistsByExerciseId` unchanged.
20. Add `Task<IEnumerable<Exercise>> GetAllByIds(IEnumerable<Id> exerciseIds, CancellationToken cancellationToken);`
    to `IExerciseRepository`, with an XML doc: "Unknown ids are ignored; empty input returns empty."
    Implement it in `ExerciseRepository`:
    - Return `[]` for empty input without querying.
    - Otherwise run one `AsNoTracking` `Where(ids.Contains(...))` query and map with
      `ExerciseMapper.MapToDomain`.

## Phase 5 — Seed

21. Add `OwnedPressBancaEntryId` (`…0031`), `TrainedSentadillaEntryId` (`…0032`),
    `TrainedPlanchaEntryId` (`…0033`) and `TrainerOwnPressBancaEntryId` (`…0034`) to
    `src/WeightTracker.Infra/Seed/SeedData.cs`, each with a one-line XML doc.
22. Rewrite `DatabaseSeeder.SeedWorkouts`:
    - Drop the local `Exercise.Rehydrate` instances.
    - Pass a list of `WorkoutExercise.Rehydrate(Id.From(<entry id>), Id.From(<exercise id>), <effort>, [<sets>])`
      to `Workout.Rehydrate`, exactly as in plan §10, in that order.
    - Remove the `Domain.Exercises` using if nothing else in the file needs it.

23. Add the four ids to the id list in `test/WeightTracker.Unit/Infra/Seed/SeedDataTests.cs`.
24. **Review (seed):** check plan §13 "Seed". Make sure no existing seeded id, credential or set
    prescription changed.

## Phase 6 — Application and API

25. Add `src/WeightTracker.Application/UseCases/Workouts/WorkoutListDTO.cs`:
    `public sealed record WorkoutListDTO(IReadOnlyList<Workout> Workouts, IReadOnlyDictionary<Id, Exercise> Exercises);`
26. `GetAllWorkoutsUseCase`:
    - Inject `IExerciseRepository`.
    - `Execute` keeps the role switch, then collects
      `workouts.SelectMany(w => w.Exercises).Select(e => e.ExerciseId).Distinct()`.
    - It calls `GetAllByIds` once and returns `new WorkoutListDTO(workouts.ToList(), exercises.ToDictionary(e => e.Id))`.
    - Do not commit. Registration in `Program.cs` is unchanged because the constructor is resolved by DI.
      Confirm that `IExerciseRepository` is registered.
27. `src/WeightTracker.Api/Endpoints/Workouts/Dtos/WorkoutResponse.cs`:
    - `WorkoutResponse.From(Workout workout, IReadOnlyDictionary<Id, Exercise> exercises)`
    - `WorkoutExerciseResponse(Guid ExerciseId, string Name, string BodyPart, string Effort, IReadOnlyList<SetResponse> Sets)`
      with `From(WorkoutExercise entry, Exercise exercise)`, where `Effort = entry.Effort.ToString()`.
    - Keep the exercise order from `workout.Exercises`.
28. `WorkoutsController.GetAll`:
    `var result = await _getAllWorkouts.Execute(...); return Ok(result.Workouts.Select(w => WorkoutResponse.From(w, result.Exercises)).ToList());`
    Leave `[ProducesResponseType]` unchanged.
29. `ExceptionMapper`: add `ExerciseAlreadyInWorkoutException` to the `409` group and
    `InvalidEffortException` to the `400` group, explicitly. Do not rely on the `DomainException`
    fallback.
30. **Review (application + API):** check plan §13 "Application" and "API".

## Phase 7 — Tests

31. `test/WeightTracker.Unit/Infra/Mappers/WorkoutMapperTests.cs`: adapt the fixtures to
    `WorkoutExercise`. Add the mapper cases in plan §12. Delete
    `MapToDomain_ShouldRejectAWorkoutReferencingAnUnknownExercise`.
32. `test/WeightTracker.Unit/Application/Workouts/GetAllWorkoutsUseCaseTests.cs`: add an
    `IExerciseRepository` mock. Keep the four role cases and assert on `result.Workouts`. Add:
    - `ShouldLoadEachReferencedExerciseOnce`: `Verify` that `GetAllByIds` is called once with the
      distinct ids.
    - `ShouldReturnTheReferencedExercisesById`
33. `test/WeightTracker.Integration/Common/ExceptionMapperTests.cs`: add the two new exceptions and the
    retyped `ExerciseNotInWorkoutException`.
34. `test/WeightTracker.Integration/Repositories/WorkoutRepositoryTests.cs`: adapt the existing cases to
    `AddExercise(Id, Effort, sets)` / `RemoveExercise(Id)`. Add:
    - `Save_ShouldPersistTheEffortOfEachExercise`
    - `Save_ShouldKeepTheRowIdOfASurvivingExercise`
    - `Save_ShouldAllowRemovingAndReaddingTheSameExerciseInOneSave`. If this fails, stop (plan §6).
35. `test/WeightTracker.Integration/Repositories/ExerciseRepositoryTests.cs`: add the three
    `GetAllByIds` cases from plan §12.
36. `test/WeightTracker.Integration/Workouts/WorkoutResponseShapeTests.cs`:
    - Extend `Workout_ShouldNestItsExercisesAndSets` to assert `effort == "HIGH"`.
    - Add `Exercise_ShouldCarryItsEffort`, a theory over the four seeded entries in plan §10.
    - Add `WorkoutWithSeveralExercises_ShouldKeepTheirOrder`: Sentadilla, then Plancha.
37. Run `AuthEndpointsTests` (`Workouts_ShouldRejectAnonymousCallers`,
    `Workouts_ShouldRejectAGarbageToken`) and `WorkoutScopingTests` **unchanged**. Both must pass.
38. **Review (tests):** plan §12 is complete. Every new test follows Arrange/Act/Assert with exactly one
    Act. No test depends on another test's writes.

## Phase 8 — Template sync

39. Copy these files byte for byte into their `.stpr/template/` counterparts:
    - `Workout.cs`
    - `ExerciseNotInWorkoutException.cs`
    - `IExerciseRepository.cs`
    - `WorkoutTests.cs`

    Add `.stpr/template/src/WeightTracker.Domain/Workouts/WorkoutExercise.cs` and `Effort.cs` as new copies.
40. In `.stpr/template/README.md`, change the `Workouts` row's "Why" to
    `Create / Rehydrate / IsNew, inner entity (WorkoutExercise), value objects, multi-table aggregate`.
    No other change.
41. In the Arrange/Act/Assert example in `.stpr/template/STPR_INVARIANTS.md` §4, change the Act line to
    `var act = () => workout.RemoveExercise(Id.New());`. No rule text changes.
42. **Review (template):** run the parity check. Only drift that was already recorded in task 3 may remain.

## Phase 9 — Verification and final review

43. Run `dotnet build WeightTracker.slnx` and `dotnet test WeightTracker.slnx` with Docker running.
    Everything must pass. The count must be at least the baseline from task 2, minus the 3 deleted tests
    (tasks 11 and 31), plus the new ones.
44. Grep `src` and `test` for `Guid.NewGuid` and `Guid.CreateVersion7`. Nothing new is allowed. Also grep
    `src/WeightTracker.Domain/Workouts` and `src/WeightTracker.Infra/Mappers/WorkoutMapper.cs` for
    `Exercise ` usages that cross the aggregate boundary.
45. Start the API in Development, then call `GET /workouts` in `/scalar` as `admin@weighttracker.test`.
    `effort` must appear, and the `name` / `bodyPart` values must be unchanged.
46. Go through the `AGENTS.md` quality checklist and every item in plan §13. Optionally, run
    `.stpr/review/CHECKLIST_REVISION.md`.

---

## Completion status

Tasks 1–44 and 46 are done. The full `dotnet test WeightTracker.slnx` passes with Docker running:
286 unit and 142 integration tests, against a baseline of 270 and 125.

Deviations from the task text:

- **Task 7:** `WorkoutExercise` copies the newer `Exercise.cs` lifecycle shape. Its private constructor
  receives the id, and `Create` passes `Id.New()`. `Id.New()` is still called only in `Create` (plan §11).
- **Task 14:** `DatabaseMigrator` gains `MigrateUp(connectionString, targetVersion)` and an optional
  `targetVersion` on `MigrateDown` (default `0`, so existing callers are unchanged). The version-targeted
  migration tests need them. An extra test, `AddEffort_ShouldLeaveNoDefaultForNewRows`, covers the
  dropped default from plan §9.
- **Task 39:** `.stpr/template/src/WeightTracker.Domain/Common/Entity.cs` was synced too. Task 10
  changed its remark, and the file is a template copy.
- **Task 31:** the rewritten `WorkoutMapperTests` fixture uses `Id.New()` instead of the
  `Guid.CreateVersion7()` it had before.
- **Compile-only:** `EntityEqualityTests` passes `[]` instead of `new()` to `Workout.Rehydrate`.
- **Extra coverage:** `AddExercise_ShouldRejectAnUndefinedEffort` and
  `Create_ShouldTellApartTheSameSetsInADifferentOrder`.
- **Task 45 (manual Scalar check):** not done. Starting the API applies the migration to the local
  development database. The same HTTP path is covered by `WorkoutResponseShapeTests` through
  `WebApplicationFactory`.

Parity: only the drift on `test/WeightTracker.Integration/Auth/AuthEndpointsTests.cs` remains. It was
recorded in task 3 and was already there before this change.

The remove-and-re-add test (`Save_ShouldAllowRemovingAndReaddingTheSameExerciseInOneSave`) passes:
EF orders the delete before the insert, as plan §6 expects.

### Final review

- **Fixed:** `WorkoutExerciseTests.Sets_ShouldNotExposeTheInternalCollectionForMutation` was really
  testing that `Create` copies its input. It is renamed to `Create_ShouldCopyTheGivenSets`. A real
  `Sets` exposure test was added for the plan §12 case "`Sets` cannot be used to mutate the entity".
- **Checked and unchanged:**
  - `WorkoutsController` delegates its catch to `ExceptionMapper.Map`.
  - Both new exceptions have explicit mappings.
  - The state plan §6 declares immutable has no setter or mutator (`ExerciseId`, `Effort`, and `Sets`,
    which is copied in and out).
  - Only aggregate roots have repositories.
  - `Effort` is guarded in the domain, not in a use case.
  - Uniqueness of an exercise per workout is enforced in `AddExercise` and `Rehydrate` before
    persistence, and backed by `uq_workout_exercises_workout_exercise`.
  - Auth responses and credential storage are untouched.
- **Remaining gap:** task 45, the manual Scalar check, as stated above.
