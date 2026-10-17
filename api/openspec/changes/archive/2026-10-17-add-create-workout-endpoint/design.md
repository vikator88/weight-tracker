# Design

## Context

See `proposal.md` for the motivation and `specs/workout-creation/spec.md` for the behaviour. The current state that shapes the approach:

- `Workout.Create(Id userId, Id? trainerId, DateTime workoutDate)` exists and is covered by `WorkoutTests`. It generates the id with `Id.New()`, truncates the date with `.Date`, sets `IsNew = true` and throws `TrainerCannotBeWorkoutOwnerException` (already mapped to `400`) when `trainerId == userId`.
- `IWorkoutRepository.Save` inserts a new aggregate when `IsNew` is set. `workout_date` is a `date` column, and `WorkoutMapper` converts it with `DateOnly.FromDateTime`, so the time is never stored.
- `IUserRepository.GetById` returns the `User` with its `Role`. `IUserRepository.IsTrainerOrAdmin` exists and has integration tests, but no use case calls it yet. It answers "exists and may train" in one query.
- `WorkoutsController` resolves `(callerId, callerRole)` with `TryReadCaller` and passes them explicitly to each use case. The Application layer never reads claims.
- `WorkoutResponse.From(workout, exercises)` already projects a workout. A new workout has no entries, so an empty dictionary is enough.
- The seed has exactly one `USER`, one `TRAINER` and one `ADMIN`. That is enough to reach every branch over HTTP.

## Goals / Non-Goals

**Goals:**
- Wire `POST /workouts` through `Api → Application → Domain`, following `WorkoutsController` and `CreateSuperSeriesUseCase`.
- Keep the owner/trainer assignment rule in one Application method, so it is unit-testable branch by branch.

**Non-Goals:**
- No change to `Workout.Create` or any other domain code.
- No new repository methods, no migration and no seed change.
- Reusing `WorkoutEditAuthorizer` is out: it decides edits on an existing workout and does not apply here.

## Layer mapping

| Layer | Change |
| --- | --- |
| Api | `WorkoutsController.Create` (`[HttpPost]`, `201` + `WorkoutResponse`). `Dtos/CreateWorkoutRequest`. `ExceptionMapper` maps `InvalidWorkoutAssignmentException` to `400`. `Program.cs` registers `CreateWorkoutUseCase`. |
| Application | `UseCases/Workouts/CreateWorkoutUseCase`, `Exceptions/InvalidWorkoutAssignmentException`. |
| Domain | None. Uses `Workout.Create`, `Id`, `Role`. |
| Infra | None. |

Cross-area dependency: the use case depends on `IWorkoutRepository`, `IUserRepository` and `IUnitOfWork`. The Workouts area reads Users by id only (owner lookup and the trainer check), which is the same cross-aggregate-by-id pattern `WorkoutRepository` already uses.

## Decisions

### D1. Request shape and structural validation
`CreateWorkoutRequest { Guid? UserId; Guid? TrainerId; [Required] DateOnly? WorkoutDate }`. Only `WorkoutDate` is structurally required. Whether `UserId` is required depends on the caller's role, which is a business rule, so it lives in Application (invariants §1 and §4). `DateOnly` makes the API reject a date-time or garbage with `400` through model binding. The controller converts the date with `ToDateTime(TimeOnly.MinValue)`, as `WorkoutMapper` already does.
- Alternative: `DateTime`. It accepts a time the client may assume is stored, so the date-only contract would not show in the OpenAPI document.

### D2. One use case, role switch inside it
`CreateWorkoutUseCase.Execute(callerId, callerRole, Id? userId, Id? trainerId, DateTime workoutDate, ct)` returns the created `Workout`. It resolves `(ownerId, prescriberId)` in a private method that switches on `callerRole`, then checks against stored users, then calls `Workout.Create`, `Save` and `SaveChangesAsync`, matching the order in `CreateSuperSeriesUseCase`.
- Alternative: a shared `WorkoutCreateAuthorizer` service like `WorkoutEditAuthorizer`. Only one use case creates workouts, so a service would add DI wiring with no reuse. Extract it if a second creation path appears.

### D3. Check order and error codes
1. Role-dependent body checks, with no I/O → `InvalidWorkoutAssignmentException` (`400`):
   - `USER`: a `userId` other than the caller, or any `trainerId`.
   - `TRAINER`: `userId` missing or equal to the caller, or any `trainerId`.
   - `ADMIN`: `userId` missing or equal to the caller. `trainerId == userId` is left to the domain (`TrainerCannotBeWorkoutOwnerException`, `400`), so that rule is not stated twice.
2. Owner lookup through `IUserRepository.GetById` (skipped for `USER`, whose owner is the authenticated caller) → `UserNotFoundException` (`404`).
3. `TRAINER` only: the owner's `Role` must be `USER` → `UnauthorizedOperationException("POST /workouts")` (`403`).
4. `ADMIN` with `trainerId`: `IUserRepository.IsTrainerOrAdmin(trainerId)` must be true → `InvalidWorkoutAssignmentException` (`400`).
5. `Workout.Create(ownerId, prescriberId, date)`.

Running the cheap `400` checks first means a malformed request never touches the database, and it matches the spec's "404 after the role-dependent 400 checks".
- Why `400`, not `404`, for an unknown `trainerId`: `IsTrainerOrAdmin` cannot tell "missing" from "not a trainer", and both mean the admin named someone who cannot prescribe. The user chose `400` for this.
- Why `403` only for a trainer → non-`USER` owner: the request is well-formed, but the caller lacks permission over that user. Every other misuse is a malformed assignment (`400`), by user decision.

### D4. One new exception with distinct messages
`InvalidWorkoutAssignmentException : ApplicationLayerException`, built with a message that names the broken rule (for example "Trainers must specify the user the workout is for"). One type keeps `ExceptionMapper` to one line and gives clients readable `ProblemDetails`.
- Alternative: one exception per rule. That means more types and more mapper entries but no extra behaviour, because they all map to `400`.

### D5. Response
`201 Created` with `WorkoutResponse.From(workout, new Dictionary<Id, Exercise>())`, and no `Location`, because no single-workout read route exists. This mirrors `CreateSuperSeries`. Returning the full workout gives the client the id it needs for later exercise and superseries calls.

## Testing approach

- **Unit** (`test/WeightTracker.Unit/Application/Workouts/CreateWorkoutUseCaseTests.cs`), mirroring `CreateSuperSeriesUseCaseTests` (strict mocks, a `AUseCase()` factory, AAA). One test per branch of D3, plus success per role. Each rejection verifies that neither `Save` nor `SaveChangesAsync` ran.
- **Integration** (`test/WeightTracker.Integration/Workouts/WorkoutCreateEndpointTests.cs`), mirroring `SuperSeriesCreateEndpointTests` and `AuthEndpointsTests`. The tests authenticate only through `AuthenticatedClientFor`, use seeded users only, and assert persistence through `GET /workouts`, including that the owner sees the workout and, for a trainer-created workout, that the trainer does as well.
- No domain test changes: `WorkoutTests` already covers `Create`.

## Risks / Trade-offs

- [`IsTrainerOrAdmin` is the first production caller of that method] → its integration tests in `UserRepositoryTests` already cover the true, false and unknown-id cases.
- [Collapsing several rules into one `400` type hides which rule failed from code that switches on type] → the message names the rule, and no caller needs to branch on type today.
- [A second `TRAINER`-role owner case can't be seeded without new seed data] → the `403` branch is reachable with the seeded `ADMIN` as owner. The "another trainer" variant is covered by unit tests.

## Migration Plan

No schema or data change. Deploying adds the route, and rolling back removes it. Workouts created in the meantime remain valid rows.
