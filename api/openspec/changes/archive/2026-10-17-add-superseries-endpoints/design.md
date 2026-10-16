# Design

## Context

See `proposal.md` for the motivation. Current state that shapes the approach:

- `Workout.CreateSuperSeries(IEnumerable<Id>)` and `Workout.BreakSuperSeries(SuperSeriesId)` exist and are unit tested. `CreateSuperSeries` matches members on the catalog `ExerciseId`. Since `WorkoutExercise.Id` exists and is persisted, the entry ids are already available.
- `WorkoutRepository.Save` reconciles children by `WorkoutExercise.Id`, including `SuperSeriesId`. No migration is needed.
- `WorkoutsController` exposes only `GET /workouts`. It resolves the caller with `TryReadCaller` and passes `(callerId, callerRole)` explicitly to the use case (`GetAllWorkoutsUseCase`). The Application layer never reads claims.
- `GET /workouts` returns no entry id and no superseries information, so a client cannot build a create request today.
- `UnauthorizedOperationException` → `403`, `SuperSeriesNotFoundException` → `404`, `ExerciseAlreadyInSuperSeriesException` → `409`, and the other superseries exceptions → `400` are already mapped.
- Seed: three workouts. Only `TrainedWorkout` has two entries, and their set totals differ (5 vs 3). There are four catalog exercises. `CurlBiceps` must stay unreferenced (the delete-exercise test relies on it), and an exercise may appear only once per workout, so a seeded workout has at most three entries.

## Goals / Non-Goals

**Goals:**
- Wire both operations through `Api → Application → Domain` following `WorkoutsController` and the mutating use case shape of `UpdateExerciseUseCase`.
- Implement the workout edit rule once, so the future add/remove/reorder-exercise endpoints reuse it unchanged.

**Non-Goals:**
- No change to `RemoveExercise`/`AddExercise` signatures (they keep using the catalog `ExerciseId`).
- No new repository methods: `GetById` and `Save` are enough.
- No second trainer in the seed. The "another trainer on a prescribed workout" branch is covered by unit tests (see Risks).

## Layer mapping

| Layer | Change |
| --- | --- |
| Api | `WorkoutsController`: `CreateSuperSeries` (`POST {workoutId:guid}/superseries`), `BreakSuperSeries` (`DELETE {workoutId:guid}/superseries/{superSeriesId:int}`). `Dtos/CreateSuperSeriesRequest`, `Dtos/SuperSeriesResponse`. `WorkoutExerciseResponse` gains `Id`, `SuperSeriesId`. `ExceptionMapper` maps `WorkoutNotFoundException` and `WorkoutExerciseNotInWorkoutException` to `404`. `Program.cs` registrations. |
| Application | `UseCases/Workouts/CreateSuperSeriesUseCase`, `UseCases/Workouts/BreakSuperSeriesUseCase`, `Services/WorkoutEditAuthorizer`, `Exceptions/WorkoutNotFoundException`. |
| Domain | `Workout.CreateSuperSeries` matches on `WorkoutExercise.Id`. New `WorkoutExerciseNotInWorkoutException`. `ExerciseAlreadyInSuperSeriesException` / `DuplicateExerciseInSuperSeriesException` carry the entry id. |
| Infra | `SeedData` + `DatabaseSeeder`: four new workouts (below). No mapper, repository or migration change. |

Cross-layer dependencies: the use cases depend on `IWorkoutRepository`, `IUnitOfWork` and `WorkoutEditAuthorizer`. The Workout area does not depend on any other domain area. `Role` comes from `Domain/Users`, as it already does in `GetAllWorkoutsUseCase`.

## Decisions

### D1. Entry ids in the request; aggregate matches on `WorkoutExercise.Id`
Chosen by the user. `CreateSuperSeries(IEnumerable<Id> workoutExerciseIds)` looks entries up by `item.Id`. An unknown id throws the new `WorkoutExerciseNotInWorkoutException(workoutExerciseId, workoutId)` → `404`. `ExerciseNotInWorkoutException` is not reused because its message and `RemoveExercise` refer to catalog exercises.
- Alternative: keep catalog ids. It needs no domain change, but contradicts the requested contract.
- The two superseries exception classes keep their names to avoid churn. Their parameter becomes `workoutExerciseId` and the message reads "Workout exercise '…'".

### D2. The edit rule is one Application service: `WorkoutEditAuthorizer`
`Application/Services/WorkoutEditAuthorizer.EnsureCanEdit(Workout workout, Id callerId, Role callerRole, string operation)` throws `UnauthorizedOperationException(operation)` unless:
`callerRole == ADMIN` **or** (`workout.TrainerId is not null` ? `workout.TrainerId == callerId` : `workout.UserId == callerId`).
It is synchronous, has no dependencies, and is registered scoped in `Program.cs`.
- Why Application: the invariants place authorization and permissions in Application. It is a service because the rule is shared by several use cases (the invariant "reusable in Application → ApplicationService").
- Alternative: a `Workout.CanBeEditedBy(...)` domain method. Rejected because it pulls a permission rule into the domain, against the invariants.
- Alternative: inline checks per use case. Rejected because the rule would diverge across future edit endpoints.

### D3. Use case order: validate input → load → 404 → 403 → domain → save → commit
Both use cases follow `UpdateExerciseUseCase`: one `Execute`, `Save`, then a single `SaveChangesAsync`.
- `CreateSuperSeriesUseCase.Execute(Id callerId, Role callerRole, Id workoutId, IReadOnlyList<Id> workoutExerciseIds, CancellationToken)` → `SuperSeriesId`.
- `BreakSuperSeriesUseCase.Execute(Id callerId, Role callerRole, Id workoutId, int superSeriesId, CancellationToken)`. It builds `SuperSeriesId.From` first, so a non-positive id is a `400` before any I/O.
- A missing workout yields `404` before the permission check. Without a workout there is nothing to authorize against. This matches the `workout-editing` spec.

### D4. HTTP contract
- The controller reuses `TryReadCaller` (missing or unusable claims → `401`). Route and body `Guid`s become `Id` with `Id.From` in the controller, as `ExercisesController` does. `Guid.Empty` → `InvalidIdException` → `400`.
- `CreateSuperSeriesRequest(IReadOnlyList<Guid> ExerciseIds)` has `[Required]` only. The list size is a domain rule (`400` from the domain). The JSON name is `exerciseIds`, as requested.
- Create returns `201` with `SuperSeriesResponse(int Id)` (`{ "id": 1 }`), built by `SuperSeriesResponse.From(SuperSeriesId)`. There is no `Location` header because no route reads a single superseries or workout. `CreatedAtAction` would need one.
- Break returns `204`.
- `[ProducesResponseType]`: create declares 201/400/401/403/404/409; break declares 204/400/401/403/404.
- `WorkoutExerciseResponse` adds `Id` (entry id) and `SuperSeriesId` (`int?`, serialized as `null` when ungrouped, not omitted, so clients can rely on the field).

### D5. Seed data
Four new workouts, all owned by the seeded user, with fixed ids in `SeedData` (workouts `…0024`–`…0027`, entries from `…0035`):

| Workout | Trainer | Entries | Superseries | Used for |
| --- | --- | --- | --- | --- |
| `PrescribedPairWorkoutId` | Trainer | Press Banca 3×10 reps, Sentadilla 3×8 reps | none | create: trainer 201, admin 201, owner 403 |
| `PrescribedSuperSeriesWorkoutId` | Trainer | Press Banca 3×10, Sentadilla 3×8 (SS 1), Plancha 4×30 s | 1 | break: trainer 204, admin 204, owner 403. Create with a grouped entry: 409 |
| `SelfCreatedPairWorkoutId` | none | Sentadilla 4×6, Plancha 4×45 s | none | create: owner 201, trainer 403 |
| `SelfCreatedSuperSeriesWorkoutId` | none | Press Banca 4×8, Plancha 4×30 s (SS 1) | 1 | break: owner 204, trainer 403 |

Other cases reuse the existing seed. Set-total mismatch uses `TrainedWorkout` (5 vs 3, trainer, `400`). An entry from another workout gives `404`. Unknown superseries `2` gives `404`. Seeding goes through `Workout.Rehydrate`, which validates the stored superseries.

## Risks / Trade-offs

- [New seed workouts change what `GET /workouts` returns] → Update `WorkoutScopingTests` expected sets (user: 6, trainer: `TrainedWorkout`, `TrainerOwnWorkout` and the two prescribed ones, admin: 7) and keep the exclusion assertions: the trainer must not see either self-created workout. Update the `.stpr/template` copy too. Grep other suites for counts tied to the seed.
- [`exerciseIds` carries entry ids while the response also has `exerciseId` (catalog id)] → Clients may send the wrong id. That fails cleanly with `404`, and a spec scenario plus the OpenAPI summary on the action state it. Renaming the field to `workoutExerciseIds` would remove the ambiguity. The field name was kept as requested.
- [`404` before `403` lets any authenticated caller probe whether a workout id exists] → Accepted. Ids are v7 GUIDs, and the existing API already distinguishes the two.
- [No second trainer in the seed] → The "another trainer on a prescribed workout" branch is covered in `WorkoutEditAuthorizerTests` and in a use case unit test, not over HTTP. Integration covers the other trainer branch (trainer on a self-created workout it does not own).
- [Template-backed files change (`Workout.cs`, `WorkoutTests.cs`, `WorkoutScopingTests.cs`)] → Copy them to `.stpr/template` and run the parity check.

## Migration Plan

No schema change. Deploying is additive for clients: `GET /workouts` adds two fields, and two routes are new. Rollback is a code revert. Seed changes affect only Development and the test database.
