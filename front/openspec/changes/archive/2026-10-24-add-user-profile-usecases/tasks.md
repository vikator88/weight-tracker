# Tasks

## 1. Contract snapshot

- [x] 1.1 Run the API from the `add-users-me-endpoints` code (Debug API, port 5199), then `pnpm --filter @weight-tracker/core openapi:refresh` and `pnpm --filter @weight-tracker/core openapi:generate`; verify `git diff core/openapi/v1.json core/src/contract/generated` adds only `GET`/`PUT /users/me` and the `UserProfileResponse` and `UpdateProfileRequest` schemas (stop and report if anything else changed)
- [x] 1.2 Confirm nothing under `core/src/contract/generated` was edited by hand and the generated schema names; verify `pnpm --filter @weight-tracker/core typecheck` passes

## 2. Core users area

- [x] 2.1 Create `core/src/domain/users/user.ts` with `UserProfile` and `UpdateProfileRequest` aliases over the generated schemas (names as generated in 1.2) and re-export them from `core/src/domain/index.ts`; verify with the core typecheck
- [x] 2.2 Create `core/src/application/ports/user-repository-interface.ts` (`getMe`, `updateMe`) and re-export it from `ports/index.ts`; verify with the core typecheck
- [x] 2.3 Create `core/src/infrastructure/repositories/user-repository.ts` following `auth-repository.ts` (`GET /users/me` with no body, `PUT /users/me` with the body unchanged, failures through `mapHttpError`) and re-export it from `repositories/index.ts`; verify with the core typecheck
- [x] 2.4 Add `core/test/infrastructure/repositories/user-repository.test.ts` following `auth-repository.test.ts`: request shape for both methods, success returns the profile, `400` → `VALIDATION` with the detail kept, `401` → `UNAUTHORIZED`, `500` → `UNEXPECTED`, rejected request → `NETWORK`; AAA with one Act each; verify with `pnpm --filter @weight-tracker/core test`
- [x] 2.5 Create `core/src/application/use-cases/users/get-my-profile-use-case.ts`, `update-my-profile-use-case.ts` and `index.ts`, depending only on `UserRepositoryInterface`, and re-export the folder from `use-cases/index.ts`; add `userRepository`, `getMyProfileUseCase` and `updateMyProfileUseCase` to `Dependencies`; verify with the core typecheck
- [x] 2.6 Add `core/test/application/use-cases/users/get-my-profile-use-case.test.ts` and `update-my-profile-use-case.test.ts` with a fake repository: success value returned, input forwarded unchanged, repository called once, failure returned unchanged; AAA with one Act each; verify with `pnpm --filter @weight-tracker/core test`
- [x] 2.7 Review core against `auth.ts`, `auth-repository.ts`, `login-use-case.ts`, `get-session-use-case.ts` and their tests (same shape, aliases only, no throw, no validation, no key-value store); verify no difference remains other than paths, names and the missing storage step

## 3. Wiring in both fronts

- [x] 3.1 Register `userRepository`, `getMyProfileUseCase` and `updateMyProfileUseCase` in `backoffice/src/infrastructure/container.ts` and `app/src/infrastructure/container.ts`, add their placeholders to both `test/support/render-with-providers.tsx`, and add the two use case keys to both `test/infrastructure/container.test.ts`; verify `pnpm typecheck` and both fronts' `container.test.ts` pass
- [x] 3.2 Review both fronts' wiring against the existing auth registrations (same order and style, no presentation change); verify the quality checklist in `front/AGENTS.md` holds

## 4. Verification

- [x] 4.1 Check `.stpr/template/` has not drifted with the loop in `front/AGENTS.md`; verify it prints no `DRIFT:` line
- [x] 4.2 Run `pnpm typecheck && pnpm lint && pnpm test` from `front/`; verify it passes

## Workflow follow-up

- Commit the snapshot refresh (1.1–1.2) as its own step, separate from the hand-written code.
- Next change: wire `getAccessToken` into each front's `FetchHttpClient`, then replace `useCurrentUser`'s placeholder with `GetMyProfileUseCase`.
- Archive the change after review (`/opsx:archive`).
