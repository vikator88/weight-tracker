# Proposal

Source brief: `.stpr/plan/openspec/proposal-change-add-workout-usecases.md`.

## Why

The API now gives every workout a mandatory `name` and an optional `description`, a `PUT /workouts/{id}` to edit it, and a
read-only exercise catalog with a public `GET /exercises`. `core` has no workouts or exercises area, and the contract
snapshot predates the API change: `CreateWorkoutRequest` lacks `name` and `description`, `PUT /workouts/{id}` is missing,
and the removed exercise write routes are still listed. The fronts cannot build workout screens until `core` offers these
operations.

## What Changes

- Refresh and regenerate the contract snapshot (`openapi:refresh`, `openapi:generate`) as a step of its own.
- Add the `workouts` and `exercises` domain aliases over the generated schemas.
- Add `WorkoutRepository` and `ExerciseRepository` (contracts and implementations).
- Add four use cases returning `Result`, never throwing: `CreateWorkoutUseCase` (no exercises), `UpdateWorkoutUseCase`,
  `AddExerciseToWorkoutUseCase` and `GetExercisesUseCase`.
- Register the repositories and use cases in both fronts' `container.ts` and test placeholders.
- No screen, hook, provider, port, package, copy key or error code. The HTTP error mapper already maps 400, 401, 403,
  404 and 409; the fronts already send the bearer token through `getAccessToken`.
- Role rules (who must send `userId`/`trainerId`, who may reassign) stay in the API. The use cases add no role check and
  forward the input unchanged; the API's refusal comes back as `VALIDATION` or `FORBIDDEN`.

Not breaking for the fronts: nothing consumes the workout routes yet.

## Capabilities

### New Capabilities

- `workout-management`: creating a workout without exercises, updating its name, description, date and (admin only, by the API) its user and trainer, and adding an exercise with its set prescriptions. Canonical references (skeleton-backed): `user-repository-interface.ts`, `user-repository.ts` and `update-my-profile-use-case.ts` for repository and use cases; `user.ts` for aliases. Lands in `core`, wired in `backoffice` (`ADMIN`, `TRAINER`) and `app` (`USER`).
- `exercise-catalog`: listing the exercise catalog through the public endpoint. Canonical references (skeleton-backed): `user-repository.ts` and `get-my-profile-use-case.ts` for a no-input read. Lands in `core`, wired in both fronts.

### Modified Capabilities

None.

## Impact

- API endpoints: `POST /workouts` (in snapshot, stale), `PUT /workouts/{id}` (not in snapshot; added by the merged API change `add-workout-name-and-description`), `POST /workouts/{workoutId}/exercises` (in snapshot), `GET /exercises` (in snapshot, public).
- Code: `core/openapi/v1.json`, `core/src/contract/generated`, `core/src/domain`, `core/src/application` (ports, use cases, `dependencies.ts`), `core/src/infrastructure/repositories`, both `container.ts`, both `test/support/render-with-providers.tsx`, both `container.test.ts`, and core tests.
- Out of scope: any screen or role-based form logic; updating/deleting a workout's exercise; superseries; workout status; reading or deleting workouts; catalog writes; caching the catalog; updating `.stpr/template/`.
- Stop case: if the refreshed snapshot lacks `PUT /workouts/{id}` or `name`/`description`, stop; that is an API problem.
