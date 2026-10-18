# Design

## Context

`GetAllWorkoutsUseCase` already scopes by role and joins catalog exercises; `IWorkoutRepository.GetById` exists and the edit use cases already throw `WorkoutNotFoundException` / `UnauthorizedOperationException`. `WorkoutsController` inherits `AuthorizedController`, so `CurrentUser` is available. `Create` currently returns `StatusCode(201, ...)` because no read route existed. See proposal.md for motivation and the assumptions.

## Goals / Non-Goals

**Goals:**
- Add a read-only use case and route following the skeleton references named in the proposal.
- Return no catalog data, so the read needs no exercise repository call.

**Non-Goals:**
- Changing `GET /workouts` or `WorkoutResponse`.
- Domain, persistence, migration or seed changes.

## Decisions

- **Layers.** Api: `GetById` action + `WorkoutDetailResponse`. Application: `GetWorkoutByIdUseCase`. Domain/Infrastructure: untouched, reuse `IWorkoutRepository.GetById`.
- **Use case returns the `Workout` aggregate.** The scope rule is an in-use-case check on the loaded aggregate (ADMIN always; TRAINER if `UserId` or `TrainerId` equals caller; USER if `UserId` equals caller), mirroring the three branches of `GetAllWorkoutsUseCase`. Missing -> `WorkoutNotFoundException`; out of scope -> `UnauthorizedOperationException`. Both already have `ExceptionMapper` mappings; no new exception.
  - *Alternative:* new repository queries per role. Rejected: one id lookup plus a check is simpler and avoids three methods.
  - *Alternative:* 404 for out-of-scope to hide existence. Rejected: AGENTS.md asks for 403 on ownership failures, and ids are unguessable GUIDs.
- **Separate detail DTO.** `WorkoutDetailResponse` / `WorkoutExerciseDetailResponse` omit `Name` and `BodyPart`; `SetResponse` is reused. Reusing `WorkoutResponse` would force an exercise lookup and leak catalog data the proposal excludes.
- **Location header.** `Create` uses `CreatedAtAction(nameof(GetById), new { id }, body)`, keeping the body and `201`. The action name is fixed by `nameof`, so a route rename cannot break it silently.
- **Registration.** Register the use case in `Program.cs`.
- **Route.** `{id:guid}`, like `ExercisesController.GetById`; a malformed id fails routing with `404`.

## Risks / Trade-offs

- [Two response shapes for workouts] -> Documented in OpenAPI via `ProducesResponseType`; list endpoint can move to the detail shape in a later change.
- [403 reveals that an id exists] -> Accepted; ids are random GUIDs.
- [`.stpr/template/src` drift after touching `WorkoutsController`] -> Run the drift check from AGENTS.md and refresh copies if needed.
