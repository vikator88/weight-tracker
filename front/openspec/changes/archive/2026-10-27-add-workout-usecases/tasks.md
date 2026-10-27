# Tasks

## 1. Contract snapshot

- [x] 1.1 Run the API from `main` (Debug API, port 5199), then `pnpm --filter @weight-tracker/core openapi:refresh` and `pnpm --filter @weight-tracker/core openapi:generate`; verify the diff of `core/openapi/v1.json` and `core/src/contract/generated` shows `name`/`description` on workouts, `PUT /workouts/{id}` and its request schema, and the exercise changes only (stop and report anything else)
- [x] 1.2 Confirm nothing under `core/src/contract/generated` was edited by hand and note the generated schema names; verify `pnpm --filter @weight-tracker/core typecheck` passes

## 2. Core domain and contracts

- [x] 2.1 Create `core/src/domain/workouts/workout.ts` and `core/src/domain/exercises/exercise.ts` with aliases over the generated schemas (names from 1.2) and re-export them from `core/src/domain/index.ts`; verify with the core typecheck
- [x] 2.2 Create `workout-repository-interface.ts` (`create`, `update`, `addExercise`) and `exercise-repository-interface.ts` (`getAll`) in `core/src/application/ports` and re-export them from `ports/index.ts`; verify with the core typecheck

## 3. Core repositories

- [x] 3.1 Create `workout-repository.ts` in `core/src/infrastructure/repositories` following `user-repository.ts` (`POST /workouts`, `PUT /workouts/{id}`, `POST /workouts/{workoutId}/exercises`, body unchanged, failures through `mapHttpError`) and re-export it; verify with the core typecheck
- [x] 3.2 Create `exercise-repository.ts` (`GET /exercises`, no body) and re-export it; verify with the core typecheck
- [x] 3.3 Add `core/test/infrastructure/repositories/workout-repository.test.ts` following `user-repository.test.ts`: method, path and body per operation, success returns the response, `400` → `VALIDATION`, `401` → `UNAUTHORIZED`, `403` → `FORBIDDEN`, `404` → `NOT_FOUND`, `409` → `CONFLICT` (add-exercise), `500` → `UNEXPECTED`, rejected request → `NETWORK`; AAA with one Act each; verify with `pnpm --filter @weight-tracker/core test`
- [x] 3.4 Add `core/test/infrastructure/repositories/exercise-repository.test.ts`: request shape, success returns the list (including empty), `500` → `UNEXPECTED`, rejected request → `NETWORK`; verify with the core tests

## 4. Core use cases

- [x] 4.1 Create `create-workout-use-case.ts`, `update-workout-use-case.ts` and `add-exercise-to-workout-use-case.ts` plus `index.ts` in `core/src/application/use-cases/workouts`, and `get-exercises-use-case.ts` plus `index.ts` in `use-cases/exercises`, each depending only on its repository interface and returning the repository `Result`; re-export both folders from `use-cases/index.ts`; verify with the core typecheck
- [x] 4.2 Add the six keys to `core/src/application/dependencies.ts`; verify with the core typecheck
- [x] 4.3 Add one core test per use case with a fake repository: input forwarded unchanged (omitted `description`, `userId` and `trainerId` stay omitted; path id split from the body), success returned, failure returned unchanged, repository called once; AAA with one Act each; verify with the core tests
- [x] 4.4 Review core against `user.ts`, `user-repository*.ts`, `get-my-profile-use-case.ts`, `update-my-profile-use-case.ts` and their tests (same shape, aliases only, no throw, no validation); verify the only difference is the documented one

## 5. Wiring in both fronts

- [x] 5.1 Register the six keys in `backoffice/src/infrastructure/container.ts` and `app/src/infrastructure/container.ts`, add their placeholders to both `test/support/render-with-providers.tsx`, and add the four use case keys to both `test/infrastructure/container.test.ts`; verify `pnpm typecheck` and both container tests pass
- [x] 5.2 Confirm `errors` copy in `backoffice` and `app` has `FORBIDDEN`, `NOT_FOUND` and `CONFLICT` in `es` and `en`; add only what is missing; verify the translation parity tests pass
- [x] 5.3 Review both fronts' wiring against the existing user registrations (same order and style, no presentation change); verify the quality checklist in `front/AGENTS.md` holds

## 6. Verification

- [x] 6.1 Check `.stpr/template/` has not drifted with the loop in `front/AGENTS.md`; verify it prints no `DRIFT:` line
- [x] 6.2 Run `pnpm typecheck && pnpm lint && pnpm test` from `front/`; verify it passes (no E2E flow is planned)

## Workflow follow-up

- Commit the snapshot refresh (1.1–1.2) as its own step, separate from the hand-written code.
- Archive the change after review (`/opsx:archive`).
