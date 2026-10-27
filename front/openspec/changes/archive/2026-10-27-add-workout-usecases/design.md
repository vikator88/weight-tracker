# Design

Refines the brief `.stpr/plan/openspec/proposal-change-add-workout-usecases.md`. Architecture comes from `.stpr/template/STPR_INVARIANTS.md`; this only maps the change.

## Context

The users slice (`user.ts`, `user-repository*.ts`, `get-my-profile-use-case.ts`, `update-my-profile-use-case.ts`) is the nearest skeleton-backed reference. Both fronts' `container.ts` already pass `getAccessToken` to `FetchHttpClient`, and the mapper already maps 400/401/403/404/409, so no front infrastructure or error work is needed. The snapshot is stale and is refreshed first.

## Decisions

- **Contract first.** Run the API from `main` (Debug API, port 5199), then `openapi:refresh` and `openapi:generate`, as a commit of its own. Expected diff: `name`/`description` on workout schemas, `PUT /workouts/{id}` with its request schema, exercise schema changes, removal of exercise `POST`/`PUT`/`DELETE`. Out-of-scope workout routes stay unused. Unexpected diff, or no `PUT`, stops the change. Aliases follow the generated names.
- **Domain aliases.** `core/src/domain/workouts/workout.ts` (workout, workout detail, create/update/add-exercise inputs, set prescription) and `core/src/domain/exercises/exercise.ts`, aliases over `components["schemas"][...]` only, re-exported from the domain barrel.
- **Repositories.** `WorkoutRepositoryInterface` (`create`, `update`, `addExercise`) and `ExerciseRepositoryInterface` (`getAll`) in `ports/`; implementations in `infrastructure/repositories/` following `UserRepository`: `httpClient.request`, `ok(response)`, `fail(mapHttpError(error))`. Paths: `POST /workouts`, `PUT /workouts/{id}`, `POST /workouts/{workoutId}/exercises`, `GET /exercises`.
- **Use cases** in `use-cases/workouts/` and `use-cases/exercises/`, each depending only on its repository interface. `UpdateWorkoutUseCase` and `AddExerciseToWorkoutUseCase` take one input object holding the path id plus the body (`{ workoutId, ...body }` / `{ id, ...body }`); the use case splits it and calls the repository with the id and the body.
- **No validation in the use cases.** Unlike `UpdateMyProfileUseCase`, which trims and checks shared length rules, these forward input unchanged, as the brief decides: name/description limits and role requirements belong to the API, and a later form owns structural checks. This is the one deliberate difference from that reference.
- **No role branching** in `core`; the role table in the brief documents future screens only.
- **Public catalog.** `GetExercisesUseCase` does not read the session; the shared client attaches a token only when one exists.

## Dependencies keys and wiring

New keys in `Dependencies`: `workoutRepository`, `exerciseRepository`, `createWorkoutUseCase`, `updateWorkoutUseCase`, `addExerciseToWorkoutUseCase`, `getExercisesUseCase`. Registered in `backoffice/src/infrastructure/container.ts` and `app/src/infrastructure/container.ts` in the same style as the user registrations; placeholders added to both `test/support/render-with-providers.tsx`; the four use case keys added to both `container.test.ts`.

## State, errors and copy

- Held state: none; the catalog is not cached.
- Error codes: `VALIDATION`, `UNAUTHORIZED`, `FORBIDDEN`, `NOT_FOUND`, `CONFLICT`, `NETWORK`, `UNEXPECTED`, all already mapped and translated in `es` and `en`. No new code, no new copy. Verify `errors` copy has `FORBIDDEN`, `NOT_FOUND` and `CONFLICT` in both languages when implementing; adding a missing key is then in scope.
- `backoffice` and `app` stay independent; no UI, hook, route or translation changes.

## Risks

- API drift at refresh time (handled by the stop rule above).
- The `PUT` body may require `userId`/`trainerId` even from non-admins (sending current values is allowed). The input type simply follows the generated schema; callers pass current values.
- Generated numeric fields use `integer | string` unions; aliases keep them as generated.

## Tests

Core unit only, API mocked at `HttpClientInterface` and repository fakes, AAA with one Act per test. No component or E2E tests; existing suites must keep passing.
