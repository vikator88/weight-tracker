# Proposal

## Why

The previous change (`2026-10-16-add-workout-superseries`) taught the `Workout` aggregate to group and ungroup its exercises into superseries and persisted that membership, but nothing outside the domain can use it yet. This change exposes the two operations through the API so clients can create and break superseries on an existing workout. It also sets the rule for who may edit a workout. That rule applies to every future workout-editing endpoint.

Source plan: `.stpr/plan/openspec/proposal-change-add-superseries-usecases-endpoints.md`.

## What Changes

- New endpoint `POST /workouts/{workoutId}/superseries` with body `CreateSuperseriesRequest { exerciseIds: Guid[] }`, where every id is the id of a **`WorkoutExercise` entry** of that workout. Returns `201 Created` with the new superseries id.
- New endpoint `DELETE /workouts/{workoutId}/superseries/{superSeriesId}`. Returns `204 No Content`. The exercises stay in the workout; they only leave the group.
- New use cases `CreateSuperSeriesUseCase` and `BreakSuperSeriesUseCase` in `WeightTracker.Application/UseCases/Workouts`. Both mutate state only through `Workout.CreateSuperSeries` / `Workout.BreakSuperSeries`.
- New **workout edit rule**, enforced by a shared Application service: `ADMIN` can always edit. If the workout has a prescribing trainer, only that trainer may edit it. If it has none, the owner may edit it. Anyone else gets `403`. The rule was also added to `.stpr/template/STPR_INVARIANTS.md` ("Business authorization rules"), `AGENTS.md` and `.stpr/review/CHECKLIST_REVISION.md`.
- **Domain change**: `Workout.CreateSuperSeries` now identifies members by `WorkoutExercise` id rather than catalog `ExerciseId`. The superseries exceptions refer to the entry id. A new domain exception covers an entry id that is not in the workout.
- `GET /workouts` response: every exercise entry now also exposes its own `id` (needed to call the create endpoint) and its `superSeriesId` (`null` when ungrouped). This is additive and does not break existing clients.
- New Application exception `WorkoutNotFoundException` mapped to `404`.
- Seed data gains workouts with and without superseries, for both prescribed and self-created workouts.

## Capabilities

### New Capabilities

- `workout-editing`: who may change an existing workout. It is the single authorization rule that every workout-editing operation enforces.
  Canonical reference: authorization by caller in `GetAllWorkoutsUseCase` (skeleton-backed, `src/WeightTracker.Application/UseCases/Workouts`), the `UnauthorizedOperationException` → `403` mapping in `ExceptionMapper`, and `WorkoutScopingTests` (template-backed) for covering every role branch.

### Modified Capabilities

- `workout-superseries`: members are identified by `WorkoutExercise` id. Adds the HTTP operations to create and break a superseries, and makes membership visible when reading workouts.
  Canonical references: `Workouts` aggregate and `WorkoutTests` (template-backed). `WorkoutsController` and `UpdateExerciseUseCase` for the mutating use case shape (skeleton-backed). `AuthEndpointsTests` (template-backed) and `ExerciseUpdateEndpointTests` (skeleton) for the endpoint test matrix. `DatabaseSeeder` for the seed.

## Identifiers and mutability

| Identifier | Owner | Mutable? |
| --- | --- | --- |
| `Workout.Id` | `Workout` aggregate | Immutable |
| `WorkoutExercise.Id` | entry of `Workout` | Immutable. This change makes it part of the public contract. |
| `SuperSeriesId` | `Workout` (unique inside it), assigned by the aggregate | Immutable while the superseries exists. Never supplied by the client on create. After a break, the value may be reused by a later superseries in the same workout. |
| Membership (`WorkoutExercise.SuperSeriesId`) | `Workout` | Mutable, only through `CreateSuperSeries`, `BreakSuperSeries` and `RemoveExercise` |

## Conflicts with the source plan, template or invariants

- **Endpoint folder.** The source plan places the endpoints in `WeightTracker.Api/Endpoints/Exercises`, but the routes are `/workouts/...`. The invariant "one folder in Endpoints per resource" puts them in `Endpoints/Workouts` (`WorkoutsController`). Resolved in favour of the invariant.
- **Roles.** The source plan says "only Admin or Trainers". The user later set the workout edit rule above, which also lets a regular user edit a workout they created themselves (no prescribing trainer) and stops a trainer from editing another trainer's prescribed workout. The edit rule supersedes the source plan.
- **Request ids vs aggregate.** The source plan's request carries `WorkoutExercise` ids, while the aggregate matched catalog `ExerciseId`. The user chose `WorkoutExercise` ids, so the aggregate method changes (domain scope beyond "wire existing methods").
- **Template parity.** `WorkoutScopingTests` and `WorkoutTests` are template-backed. Changing the seed and the aggregate means updating both and their `.stpr/template` copies, which must stay byte-identical.

## Out of scope

- Endpoints to add, remove or reorder workout exercises (the edit rule is defined now and those endpoints will reuse it).
- E2E tests.
- `GET /workouts/{id}` or any superseries read endpoint. Membership is read through `GET /workouts`.

## Acceptance criteria

- Both routes behave as specified in `specs/workout-superseries/spec.md` and enforce `specs/workout-editing/spec.md`.
- Each route carries its own `401` (no token), `401` (invalid token), `403`, `404` and `400` cases, plus success for admin, the prescribing trainer and the owner of a self-created workout.
- Unit tests cover the aggregate change, both use cases and the edit-rule service in every branch.
- `dotnet test WeightTracker.slnx` passes with Docker running, and the template parity check reports no drift.

## Impact

- **Api**: `WorkoutsController` (2 actions), `Dtos/CreateSuperSeriesRequest`, `Dtos/SuperSeriesResponse`, `WorkoutResponse` (entry `id`, `superSeriesId`), `ExceptionMapper` (2 new mappings), `Program.cs` registrations.
- **Application**: 2 use cases, the workout edit service, `WorkoutNotFoundException`.
- **Domain**: `Workout.CreateSuperSeries` signature semantics, superseries exception messages, new entry-not-found exception.
- **Infra**: `SeedData` and `DatabaseSeeder`. No migration: the schema already stores entry ids and superseries membership.
- **Tests**: unit (domain, application), integration (new endpoint tests, updated scoping/shape tests).
