# Tasks

## 1. Core

- [x] 1.1 Add the `RefreshRequest` alias in `core/src/domain/auth/auth.ts` and `refresh` to `AuthRepositoryInterface`; add a `refresh` method to the fake repositories in `signup-use-case.test.ts` and `login-use-case.test.ts`; verify `pnpm --filter @weight-tracker/core typecheck` passes
- [x] 1.2 Implement `AuthRepository.refresh` modelled on `login`, and extend `auth-repository.test.ts` with request shape (`POST /auth/refresh`, body), success, `400`, `401`, `500` and network cases; verify the core repository tests pass
- [x] 1.3 Implement `RefreshSessionUseCase` (single in-flight promise, token clean-up as in `logout-use-case.ts`) and export it from the auth use cases `index.ts`; add `refreshSessionUseCase` to `Dependencies`
- [x] 1.4 Write `core/test/application/use-cases/auth/refresh-session-use-case.test.ts` from `login-use-case.test.ts`: stores both new tokens; sends the stored refresh token; no stored token → `UNAUTHORIZED`, no request, tokens removed; `401` and `400` → tokens removed and `UNAUTHORIZED`; `NETWORK` and `UNEXPECTED` → tokens kept, failure unchanged; storage failure → `UNEXPECTED`; two concurrent calls → one request, same result; a call after settling → a new request. Verify `pnpm --filter @weight-tracker/core test` passes
- [x] 1.5 Review core against `login-use-case.ts`, `logout-use-case.ts`, `AuthRepository.login` and their tests (structure, naming, Arrange / Act / Assert); verify no drift with the `.stpr/template/` drift check from `AGENTS.md`

## 2. Backoffice

- [x] 2.1 Register `refreshSessionUseCase` in `backoffice/src/infrastructure/container.ts`; verify `test/infrastructure/container.test.ts` resolves the new key
- [x] 2.2 Update `test/support/render-with-providers.tsx`: fake `refreshSessionUseCase`, `refresh` on the fake `authRepository`, default `getSessionUseCase` → `ok(null)`, mount `SessionProvider` between `ContainerProvider` and `CurrentUserProvider` (the exception documented in design.md); this task lands together with 2.3
- [x] 2.3 Add `presentation/auth/context/session-context.ts`, `presentation/auth/components/session-provider.tsx` and `presentation/auth/hooks/use-session.ts` (`status`, `runWithSession`, `end`, `reload`) and mount `SessionProvider` in `AppProviders` above `CurrentUserProvider`; verify `app-providers.test.tsx` still passes
- [x] 2.4 Write `test/presentation/auth/components/session-provider.test.tsx`: restoring, signed in, signed out, read failure → signed out; `reload` re-reads; `runWithSession` retries once after a successful refresh, ends the session on `UNAUTHORIZED` refresh, keeps it on `NETWORK`, and does not refresh a non-`UNAUTHORIZED` failure; verify it passes
- [x] 2.5 Read the `useRouter` guide in `backoffice/node_modules/next/dist/docs/`, then rework `RequireSession` to read `useSession` (`signedOut` → `router.replace("/login")`); verify the existing home-view guard tests still pass
- [x] 2.6 Make `CurrentUserProvider` call `getMyProfileUseCase` through `runWithSession` and `clear()` when the session becomes `signedOut`; add to `home-view.test.tsx`: profile read fails with `UNAUTHORIZED`, refresh succeeds → profile shown and read ran twice; refresh fails with `UNAUTHORIZED` → `replace("/login")` and no profile name shown; verify it passes
- [x] 2.7 Make `useUpdateProfile` call `updateMyProfileUseCase` through `runWithSession`; add to `profile-view.test.tsx`: save fails with `UNAUTHORIZED`, refresh succeeds → save retried and confirmed; verify it passes
- [x] 2.8 Make login and signup views `await reload()` before `router.replace("/")`, and `useLogout` call `end()` on success; verify `login-view.test.tsx`, `signup-view.test.tsx` and `user-menu.test.tsx` still pass, with a login `UNAUTHORIZED` still showing its message and no refresh
- [x] 2.9 Add `backoffice/e2e/session-refresh.spec.ts` modelled on `login.spec.ts`: (a) `GET /users/me` answers `401` once, `/auth/refresh` returns new tokens, home shows the profile; (b) `/auth/refresh` answers `401` and the user lands on `/login`; verify `pnpm test:e2e` passes
- [x] 2.10 Review the backoffice against `container-provider.tsx`, `current-user-provider.tsx`, the previous `require-session.tsx` and the closest tests (structure, naming, Arrange / Act / Assert, no new strings, presentation imports no `@weight-tracker/core/infrastructure`)

## 3. App

- [x] 3.1 Register `refreshSessionUseCase` in `app/src/infrastructure/container.ts`; verify `test/infrastructure/container.test.ts` resolves the new key
- [x] 3.2 Rename `SessionState.refresh` to `reload` in `session-context.ts` and `session-provider.tsx`, and update the login, signup and home views and the existing `session-provider.test.tsx`; verify the app tests pass
- [x] 3.3 Add `runWithSession` to the app `SessionProvider` (failed `UNAUTHORIZED` refresh → `signedOut`); add `refreshSessionUseCase` and `authRepository.refresh` fakes to `test/support/render-with-providers.tsx`
- [x] 3.4 Extend `test/presentation/auth/context/session-provider.test.tsx`: `runWithSession` retries once after a refresh; failed refresh sets `signedOut`; a `NETWORK` refresh keeps `signedIn`; non-`UNAUTHORIZED` failures are not refreshed; verify `pnpm --filter app test` passes
- [x] 3.5 Review the app against its previous `session-provider.tsx` and its test (no navigation added, no Expo API touched, Arrange / Act / Assert)

## 4. Integration

- [x] 4.1 Run the `.stpr/template/` drift check from `AGENTS.md` and verify it prints nothing
- [x] 4.2 Run `pnpm typecheck && pnpm lint && pnpm test && pnpm test:e2e` from `front/` and verify all pass
- [x] 4.3 With the API running, log in to the backoffice, let the access token expire (or replace it in local storage with an expired one) and verify `/users/me` is retried after `POST /auth/refresh` succeeds despite the expired bearer header (design.md, Risks)

## Workflow follow-up

- Archive `add-backoffice-onboarding-and-profile` before archiving this change.
- Archive this change after review.
