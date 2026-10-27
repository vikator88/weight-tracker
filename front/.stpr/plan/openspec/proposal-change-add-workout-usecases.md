# Change Proposal: Add Workout And Exercise Catalog Use Cases

Source brief for `/opsx:propose`. Artifacts go to `openspec/changes/add-workout-usecases/`.

## Change

Bring the API's workout creation, workout update, exercise catalog listing and "add exercise to a workout" operations
into `core`: refresh the contract snapshot, add the `workouts` and `exercises` domain aliases, a `WorkoutRepository`, an
`ExerciseRepository`, and four use cases: `CreateWorkoutUseCase`, `UpdateWorkoutUseCase`, `GetExercisesUseCase` and
`AddExerciseToWorkoutUseCase`. Both fronts register them. No screen is added.

## Objective

The API now carries a mandatory `name` and an optional `description` on every workout, a `PUT /workouts/{id}` to edit
them, and a read-only exercise catalog (`GET /exercises` is public). The front snapshot predates all of it: `core` has
no workouts or exercises area, and `CreateWorkoutRequest` in the snapshot still lacks `name` and `description`.

After this change `core` offers these four operations through use cases that return `Result` and never throw, so later
changes can build the workout screens without touching `core`.

## Capabilities

- Create a workout **without exercises**: `workoutDate` and `name` required, `description` optional. `userId` and
  `trainerId` are sent when the caller's role requires them (see Fronts And Roles).
- Update a workout: `name`, `description` and `workoutDate` can be edited by any role the API allows; `userId` and
  `trainerId` can be changed only by an `ADMIN`.
- List the exercise catalog (public endpoint, no token needed).
- Add an exercise to a workout: `exerciseId`, `effort` and a list of set prescriptions.
- Every API failure comes back as a `DomainError` code, with the API's validation detail kept in the message.
- All four use cases are available from each front's container, ready for presentation to use.

## Fronts And Roles

- backoffice -> lands here: wiring only -> roles: `ADMIN`, `TRAINER` -> the container registers the repositories and
  the four use cases. No screen, hook or copy changes.
- app -> lands here: wiring only -> roles: `USER` -> same registrations. No screen, hook or copy changes.

Who must send what is decided by the API, which the front mirrors in the data it passes, not in checks:

| Caller role | `userId` on create         | `trainerId` on create | `userId` / `trainerId` on update |
| ----------- | -------------------------- | --------------------- | -------------------------------- |
| `USER`      | omitted (own workout)      | omitted               | only unchanged values allowed    |
| `TRAINER`   | **required** (the athlete) | omitted (own id)      | only unchanged values allowed    |
| `ADMIN`     | **required**               | **required**          | may change both                  |

The use cases add no role check and forward the input unchanged; a missing or forbidden value comes back from the API as
`VALIDATION` or `FORBIDDEN`. A future form is what decides which fields each role sees, and it must hide the ones the
role cannot use. `GetExercisesUseCase` is callable without a session.

## Classification

Contract:

- Snapshot refresh + regeneration -> `core/openapi/v1.json` and `core/src/contract/generated/api.ts` -> its own step,
  reviewed on its own (see API Contract).

Core:

- `Workout`, `WorkoutDetail`, `CreateWorkoutInput`, `UpdateWorkoutInput`, `AddExerciseToWorkoutInput`, `SetPrescription`
  (aliases of the generated schemas) -> entity types -> `core/src/domain/workouts/workout.ts`
- `Exercise` (alias of `ExerciseResponse`) -> entity type -> `core/src/domain/exercises/exercise.ts`
- `WorkoutRepositoryInterface` (`create`, `update`, `addExercise`) -> repository contract -> `core/src/application/ports/workout-repository-interface.ts`
- `ExerciseRepositoryInterface` (`getAll`) -> repository contract -> `core/src/application/ports/exercise-repository-interface.ts`
- `WorkoutRepository`, `ExerciseRepository` -> repository implementations -> `core/src/infrastructure/repositories/`
- `CreateWorkoutUseCase`, `UpdateWorkoutUseCase`, `AddExerciseToWorkoutUseCase` -> use cases -> `core/src/application/use-cases/workouts/`
- `GetExercisesUseCase` -> use case -> `core/src/application/use-cases/exercises/get-exercises-use-case.ts`
- `Dependencies` keys `workoutRepository`, `exerciseRepository`, `createWorkoutUseCase`, `updateWorkoutUseCase`,
  `addExerciseToWorkoutUseCase`, `getExercisesUseCase` -> composition contract -> `core/src/application/dependencies.ts`
- Barrels re-export the new types, ports, repositories and use cases

Backoffice and app:

- Registrations of the six keys -> composition root -> `backoffice/src/infrastructure/container.ts` and `app/src/infrastructure/container.ts`
- Placeholders for the six keys -> test helper -> `backoffice/test/support/render-with-providers.tsx` and `app/test/support/render-with-providers.tsx`

Exact alias names follow the generated schema names after the refresh; the ones above are placeholders for
`CreateWorkoutRequest`, `UpdateWorkoutRequest`, `AddOrUpdateExerciseRequest`, `SetPrescriptionRequest` and the
workout/exercise responses.

## Canonical Template Mapping

- Domain aliases -> `core/src/domain/users/user.ts` and `core/src/domain/auth/auth.ts` (skeleton-backed) -> aliases over
  `components["schemas"][...]` only, one folder per domain area, re-exported from the domain barrel. No hand-written type.
- `WorkoutRepository` / `ExerciseRepository` and their interfaces -> `user-repository-interface.ts` and
  `user-repository.ts` (skeleton-backed) -> call `HttpClientInterface`, return the response as the domain type, failures
  through `mapHttpError`. `getAll` has no body; `create`, `update` and `addExercise` send the request unchanged.
- `GetExercisesUseCase` -> `core/src/application/use-cases/users/get-my-profile-use-case.ts` (skeleton-backed) -> no-input
  `UseCaseInterface<void, …>` delegating to a repository.
- `CreateWorkoutUseCase`, `UpdateWorkoutUseCase`, `AddExerciseToWorkoutUseCase` -> `update-my-profile-use-case.ts`
  (skeleton-backed) -> input forwarded unchanged, repository `Result` returned as is. `UpdateWorkoutUseCase` and
  `AddExerciseToWorkoutUseCase` take the path id (`workoutId`) plus the body as one input object.
- Core tests -> the tests of those same files plus `user-repository.test.ts` (skeleton-backed).
- Both `container.ts` and both `render-with-providers.tsx` -> extended in place, structure kept.

The users slice is the nearest reference because it is the latest area added the same way. `.stpr/template/` and its
README are not updated here.

## Screens And Navigation

None. No route, view or component changes.

## API Contract

- `POST /workouts` -> in `core/openapi/v1.json`: **yes, stale** -> request `CreateWorkoutRequest` (after refresh: `name`
  required, `description`, `workoutDate` required, `userId`, `trainerId`), response `WorkoutResponse`; failures `400`,
  `401`, `403`, `404`.
- `PUT /workouts/{id}` -> **no, yet** -> request `UpdateWorkoutRequest`, response `WorkoutResponse`; failures `400`,
  `401`, `403`, `404`.
- `GET /exercises` -> **yes** -> no body, response `ExerciseResponse[]`; no security requirement (public).
- `POST /workouts/{workoutId}/exercises` -> **yes** -> request `AddOrUpdateExerciseRequest` (`exerciseId`, `effort`,
  `sets` with at least one `SetPrescriptionRequest`), response a workout exercise; failures `400`, `401`, `403`, `404`,
  `409`.

The missing and changed parts come from the already merged API change `add-workout-name-and-description`
(`api/openspec/changes/archive/2026-10-27-add-workout-name-and-description/`), and the catalog from
`make-exercises-read-only-catalog` and `add-exercise-translations-and-muscle`. This change refreshes the snapshot: run
the API (Debug API, port 5199) from `main`, then `pnpm --filter @weight-tracker/core openapi:refresh` and
`openapi:generate`. This is its own commit-sized step, before any hand-written code.

Review the diff of `core/openapi/v1.json` and the generated types. Expected: `name` and `description` on the workout
schemas, the new `PUT /workouts/{id}` with `UpdateWorkoutRequest`, `ExerciseResponse` changes from the exercise
translation/muscle change, and the removal of `POST`/`PUT`/`DELETE /exercises`. Out-of-scope operations (`PUT`/`DELETE`
exercises of a workout, superseries, status) stay in the snapshot untouched and unused. Anything unexpected means the API
drifted, and the change stops there. If generated names differ from those above, the aliases follow the generated names.

## Held State

None. No use case stores anything, and no provider or hook is added. In particular, the exercise catalog is not cached.

## Forms And Structural Validation

None here. A future form would mirror the API's declared limits: `name` required, 1–256 characters after trimming;
`description` optional, up to 1024; `workoutDate` required, `YYYY-MM-DD`; `sets` at least one. Those limits are owned by
the API and the snapshot may not declare them, in which case the form change decides, not this one. `core` adds no
validation.

## Errors And Copy

- Missing or invalid token -> `UNAUTHORIZED` (401) -> already mapped and translated.
- Role not allowed (e.g. a non-admin changing `userId`/`trainerId`, or editing a locked workout) -> `FORBIDDEN` (403) ->
  already mapped, if the mapper has it.
- Blank/missing `name`, missing `userId`/`trainerId` for the role, invalid set prescription -> `VALIDATION` (400) -> already mapped.
- Unknown workout, user, trainer or exercise -> `NOT_FOUND` (404) -> already mapped, if the mapper has it.
- Exercise already in the workout -> `CONFLICT` (409) -> already mapped, if the mapper has it.
- No response -> `NETWORK`; any other status -> `UNEXPECTED` -> already mapped.

The codes must be checked against `core/src/infrastructure/http` while implementing. If `FORBIDDEN`, `NOT_FOUND` or
`CONFLICT` is not mapped, adding it (mapper branch, `DomainErrorCode`, test branch, `errors` copy in `es` and `en`) is
part of this change and is the only reason to touch the mapper. No other new copy keys.

## Platform Capabilities And Dependencies

None. No new port and no new package.

## Out Of Scope

- Any screen, hook or provider, and any role-based form logic.
- Updating or deleting an exercise **of a workout** (`PUT`/`DELETE /workouts/{workoutId}/exercises/{exerciseId}`).
- Superseries operations, and workout status changes.
- Reading workouts (`GET /workouts`, `GET /workouts/{id}`) and deleting workouts.
- Creating, updating or deleting catalog exercises. The API no longer offers them.
- Caching the exercise catalog.
- Sending the access token from the fronts (see Notes), reactive refresh on `401`.
- Updating `.stpr/template/` or its README.
- Must not change: the API, the auth and users use cases, the stored tokens.

## Notes

- **Token wiring.** The `FetchHttpClient` sends the bearer token only when `getAccessToken` is provided. If the fronts
  still construct it with `baseUrl` only, the three workout use cases answer `401` from a front as wired today;
  `GetExercisesUseCase` works because the endpoint is public. Check `container.ts` first: the "auto refresh token" work
  (#24) may already have wired it. If it has not, this stays out of scope, as in the profile change, and the design
  records it as a prerequisite for the first screen.
- **Create without exercises.** `CreateWorkoutRequest` has no exercises field; exercises are added afterwards with
  `AddExerciseToWorkoutUseCase`.
- **Role rules live in the API.** `core` must not branch on the role, because that would be a business rule in the
  front. The role table above is documentation for the future screens.
- **No stop case**, provided the refresh shows `PUT /workouts/{id}` and `name`/`description`. If it does not, stop: that
  is an API problem.
- **Verification tiers:**
  - core unit, one test per use case (`create-workout-use-case.test.ts`, `update-workout-use-case.test.ts`,
    `add-exercise-to-workout-use-case.test.ts`, `get-exercises-use-case.test.ts`): input forwarded unchanged (including an
    omitted `description` and omitted `userId`/`trainerId`); success returned; repository failure returned unchanged.
  - core unit, `workout-repository.test.ts` and `exercise-repository.test.ts`, following `user-repository.test.ts`: the
    method, path and body sent (`POST /workouts`, `PUT /workouts/{id}`, `POST /workouts/{workoutId}/exercises`,
    `GET /exercises`); success returns the response; every mapped status per operation (`400`, `401`, `403`, `404`,
    `409` where the contract lists it), `500` → `UNEXPECTED`, network failure → `NETWORK`.
  - core unit, HTTP error mapper: a branch test for each code added, if any.
  - front tiers: no component or E2E test, since nothing visible changes. Existing suites must still pass with the new
    keys registered and the new placeholders in place.
  - container tests: `backoffice/test/infrastructure/container.test.ts` and `app/test/infrastructure/container.test.ts`
    gain the six new keys wherever they list or resolve registrations.
- Done means `pnpm typecheck && pnpm lint && pnpm test` passes from `front/`. No E2E flow is planned.
- No deviation from a skeleton reference is allowed.
