# Tasks

## 1. Core login operation

- [x] 1.1 Add `LoginRequest` to `core/src/domain/auth/auth.ts` as an alias of `components["schemas"]["LoginRequest"]` and re-export it from the domain barrel; verify with `pnpm --filter @weight-tracker/core typecheck`
- [x] 1.2 Add `login(request: LoginRequest)` to `AuthRepositoryInterface` and implement it in `AuthRepository` like `signup`, with path `/auth/login`; verify with the core typecheck
- [x] 1.3 Extend `core/test/infrastructure/repositories/auth-repository.test.ts` with a `login` block: `POST /auth/login` with the body unchanged, success, 400 → `VALIDATION`, 401 → `UNAUTHORIZED`, network → `NETWORK`, 500 → `UNEXPECTED`; AAA with one Act each; verify with `pnpm --filter @weight-tracker/core test`
- [x] 1.4 Create `core/src/application/use-cases/auth/login-use-case.ts` (`LoginUseCase(authRepository, keyValueStore)`, which reuses `ACCESS_TOKEN_KEY` / `REFRESH_TOKEN_KEY` from `signup-use-case.ts`, stores both tokens on success and returns `UNEXPECTED` if a write fails), export it from the use-case barrels, and add `loginUseCase: UseCaseInterface<LoginRequest, AuthResponse>` to `Dependencies`; verify with the core typecheck
- [x] 1.5 Add `core/test/application/use-cases/auth/login-use-case.test.ts`, following `signup-use-case.test.ts`: credentials forwarded unchanged with no role, both tokens stored on success, nothing stored on failure, repository failure returned unchanged, `UNEXPECTED` when the store rejects; AAA with one Act each; verify with `pnpm --filter @weight-tracker/core test`
- [x] 1.6 Review core against `signup-use-case.ts`, `auth-repository.ts` and their tests (same shape, no throw, no business rule); verify that no difference remains other than the path and the missing role

## 2. Backoffice wiring and translations

- [x] 2.1 Register `loginUseCase: (c) => new LoginUseCase(c.resolve("authRepository"), c.resolve("keyValueStore"))` in `backoffice/src/infrastructure/container.ts`, and add the `loginUseCase` and `authRepository.login` placeholders in `backoffice/test/support/render-with-providers.tsx`; verify with `pnpm --filter backoffice typecheck` and `test/infrastructure/container.test.ts`
- [x] 2.2 Add every `login.*` key, `login.toSignup` and `signup.toLogin` to `backoffice/src/i18n/{es,en}/auth.json` with the copy in `design.md`; verify `test/i18n/translation-parity.test.ts` passes

## 3. Backoffice login screen

- [x] 3.1 Read the Next 16 `next/link` guide in `backoffice/node_modules/next/dist/docs/`, then create `backoffice/src/presentation/auth/components/login-form.tsx` and `.../views/login-view.tsx` as copies of the signup ones: `loginUseCase`, override `UNAUTHORIZED` only, `router.replace("/")` on success, and a MUI `Link` (`component={NextLink}`) to `/signup` below the form; verify with the backoffice typecheck and lint
- [x] 3.2 Create `backoffice/src/app/login/page.tsx`, which default-exports `LoginView` and nothing else; verify `pnpm --filter backoffice build` still produces a static export with a `login` page
- [x] 3.3 Add `backoffice/test/presentation/auth/views/login-view.test.tsx`, following `signup-view.test.tsx`: empty form and malformed email send nothing; button disabled while pending; success → `replace("/")`; `UNAUTHORIZED` shows "Email o contraseña incorrectos." and keeps the email; `NETWORK` message; link `href` is `/signup`; AAA with one Act each; verify with `pnpm --filter backoffice test`
- [x] 3.4 Add the `/login` link below the form in `signup-view.tsx`, and assert its `href` in `signup-view.test.tsx`; verify with `pnpm --filter backoffice test`
- [x] 3.5 Change `RequireSession` to `router.replace("/login")` (and its doc comment), and update `home-view.test.tsx` to expect `/login` with the heading hidden, for both no session and a read failure; verify with `pnpm --filter backoffice test`

## 4. Backoffice E2E

- [x] 4.1 Add `backoffice/e2e/login.spec.ts`, with `POST /auth/login` intercepted by `page.route`: (1) log in → `/` with the home heading; (2) `/` with empty storage → `/login`, no home heading; (3) `401` → invalid-credentials message; (4) link to `/signup` and back to `/login`. Remove the "no session" flow from `signup.spec.ts`; verify with `pnpm test:e2e`
- [x] 4.2 Review the backoffice against its signup route, view, form and tests (structure, naming, i18n, no server code); verify the quality checklist in `front/AGENTS.md` holds for the backoffice

## 5. App storage

- [x] 5.1 Read the SDK 57 `expo-sqlite` docs, then run `pnpm exec expo install expo-sqlite` from `app/`; verify `app/package.json` lists it and `pnpm dlx expo-doctor` reports no incompatible version
- [x] 5.2 Create `app/src/infrastructure/sqlite-key-value-store.ts` as in `design.md` (lazy, single open; `CREATE TABLE IF NOT EXISTS key_value`; `get` / `set` with `INSERT OR REPLACE` / `remove`; nothing at import or construction time); verify with `pnpm --filter app typecheck`
- [x] 5.3 Add `app/test/infrastructure/sqlite-key-value-store.test.ts`, with `expo-sqlite` mocked by a `Map`-backed fake database: missing key → `null`, set then get, overwrite, remove, and the database opened once across calls; AAA with one Act each; verify with `pnpm --filter app test`
- [x] 5.4 Register `SqliteKeyValueStore` as `keyValueStore` and `loginUseCase` in `app/src/infrastructure/container.ts`, delete `app/src/infrastructure/in-memory-key-value-store.ts`, and add the `loginUseCase` and `authRepository.login` placeholders in `app/test/support/render-with-providers.tsx`; verify with the app typecheck and `test/infrastructure/container.test.ts`

## 6. App login screen

- [x] 6.1 Add every `login.*` key, `login.toSignup` and `signup.toLogin` to `app/src/i18n/{es,en}/auth.json`; verify `app/test/i18n/translation-parity.test.ts` passes
- [x] 6.2 Create `app/src/presentation/auth/components/login-form.tsx` and `.../views/login-view.tsx` as copies of the app's signup ones: `loginUseCase`, override `UNAUTHORIZED` only, `await session.refresh()` on success with no `router` call, and a `<Link href="/signup" replace asChild>` around a Paper text `Button` at the bottom of the form; verify with the app typecheck and lint
- [x] 6.3 Create `app/src/app/(auth)/login.tsx` (the two-statement route), and declare `<Stack.Screen name="login" />` then `<Stack.Screen name="signup" />` in `app/src/app/(auth)/_layout.tsx`; verify with the app typecheck (typed routes include `/login`)
- [x] 6.4 Add `app/test/presentation/auth/views/login-view.test.tsx`, following the app's `signup-view.test.tsx`, with `expo-router`'s `Link` mocked as in `not-found-view.test.tsx`: field errors send nothing, values trimmed, pending state, success re-runs `getSessionUseCase` with no router call, `UNAUTHORIZED` (email kept) and `NETWORK` messages, link to `/signup`; AAA with one Act each; verify with `pnpm --filter app test`
- [x] 6.5 Add the `<Link href="/login" replace asChild>` to `app/src/presentation/auth/components/signup-form.tsx`, mock `Link` in `signup-view.test.tsx`, and add a case for the `/login` link; verify with `pnpm --filter app test`
- [x] 6.6 Check by hand on a running app (`pnpm exec expo start`, API mocked or running): a launch with no session shows login; login ⇄ signup does not stack screens; a successful login shows home; after a restart home is still shown. Verify each behaviour is observed
- [x] 6.7 Review the app against its signup route, view, form and tests and section 4.3 of the invariants (no access navigation, typed hrefs, `HelperText`, no colour literal); verify the quality checklist in `front/AGENTS.md` holds for the app

## 7. Integration

- [x] 7.1 Run the template drift check from `front/AGENTS.md`; verify it prints no `DRIFT` line
- [x] 7.2 Run `pnpm typecheck && pnpm lint && pnpm test && pnpm test:e2e` from `front/`; verify everything passes

## Workflow follow-up

- Archive `add-backoffice-signup-screen` and `add-app-signup-screen` before archiving this change, so the MODIFIED deltas have a target.
- Once verified, promote the login slice to `.stpr/template/` and switch its rows in `.stpr/template/README.md` to `template-backed`, as its own step.
