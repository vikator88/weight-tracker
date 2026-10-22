# Tasks

## 1. Core domain: `Session` alias

- [x] 1.1 Add `export type Session = Pick<AuthResponse, "accessToken" | "refreshToken">;` to `core/src/domain/auth/auth.ts` and re-export it by name from `core/src/domain/index.ts`; verify with `pnpm --filter @weight-tracker/core typecheck`

## 2. Core application: session use cases

- [x] 2.1 Change `SignupUseCase` to `(authRepository, keyValueStore, role)`: export the `ACCESS_TOKEN_KEY` (`auth.accessToken`) and `REFRESH_TOKEN_KEY` (`auth.refreshToken`) constants, store both tokens after a successful signup only, and catch a rejected `set` as `fail(UNEXPECTED)` without rethrowing (see `design.md`); verify with the core typecheck
- [x] 2.2 Extend `core/test/application/use-cases/auth/signup-use-case.test.ts`: add an in-test `FakeKeyValueStore` to every existing case, and add cases for both tokens stored on success (`expiresIn` not stored), nothing stored on failure, and `UNEXPECTED` when `set` rejects; AAA with one Act each; verify with `pnpm --filter @weight-tracker/core test`
- [x] 2.3 Create `core/src/application/use-cases/auth/get-session-use-case.ts` (`UseCaseInterface<void, Session | null>`, `keyValueStore` in the constructor, keys imported from `signup-use-case.ts`, `ok(null)` when either key is missing, `fail(UNEXPECTED)` on a rejected `get`) and re-export it from `use-cases/auth/index.ts`; verify with the core typecheck
- [x] 2.4 Add `core/test/application/use-cases/auth/get-session-use-case.test.ts` covering both keys present, neither present, only the access token present, and a rejected read; AAA with one Act each; verify with `pnpm --filter @weight-tracker/core test`
- [x] 2.5 Add `getSessionUseCase: UseCaseInterface<void, Session | null>` to `core/src/application/dependencies.ts`; verify the core typecheck passes (both fronts are expected to fail until groups 3 and 7)
- [x] 2.6 Review core against `design.md`, the `add-signup-usecase` skeleton references and `STPR_INVARIANTS.md`: kebab-case files, default exports (named for the alias file), no `throw`, no hand-written type, no edit under `core/src/contract/generated`; verify by reading `git diff -- core/`

## 3. Backoffice infrastructure: adapter and wiring

- [x] 3.1 Create `backoffice/src/infrastructure/local-storage-key-value-store.ts` (`implements KeyValueStoreInterface`, `localStorage` touched only inside the async methods) and delete `backoffice/src/infrastructure/in-memory-key-value-store.ts`; verify `grep -r in-memory-key-value-store backoffice/src backoffice/test` finds nothing
- [x] 3.2 Add `backoffice/test/infrastructure/local-storage-key-value-store.test.ts` (set then get, missing key is `null`, remove; `localStorage.clear()` in Arrange), AAA; verify with `pnpm --filter backoffice test`
- [x] 3.3 In `backoffice/src/infrastructure/container.ts`, register `keyValueStore` as `LocalStorageKeyValueStore`, pass `c.resolve("keyValueStore")` to `SignupUseCase` before the role (`2 // TRAINER`), and register `getSessionUseCase`; add a do-nothing `getSessionUseCase` entry to `backoffice/test/support/render-with-providers.tsx`; verify with `pnpm --filter backoffice typecheck` and that `test/infrastructure/container.test.ts` still passes

## 4. Backoffice translations

- [x] 4.1 Create `backoffice/src/i18n/{es,en}/auth.json` with every `signup.*` key and its copy from the source brief, add `home.signedIn` to both `common.json`, and register `auth` in `backoffice/src/i18n/resources.ts` for both languages; verify `test/i18n/translation-parity.test.ts` passes

## 5. Backoffice signup screen

- [x] 5.1 Before writing navigation code, read the Next 16 docs in `backoffice/node_modules/next/dist/docs/` for `useRouter` / `next/navigation` and static exports; note any deprecation that affects `design.md` and stop to update the plan if one does
- [x] 5.2 Create `backoffice/src/presentation/auth/components/signup-form.tsx` (`"use client"`, MUI `TextField`s with labels, `noValidate`, structural checks from `design.md`, `submitting` disables the button, optional `errorMessage` in an `Alert`, values sent as typed); verify with `pnpm --filter backoffice typecheck`
- [x] 5.3 Create `backoffice/src/presentation/auth/views/signup-view.tsx` (`"use client"`, resolves `signupUseCase`, maps the failure code to `auth:signup.errors.<code>` for `VALIDATION` / `UNAUTHORIZED` / `CONFLICT` and `errors:<code>` otherwise, `router.replace("/")` on success, no `try/catch`) and `backoffice/src/app/signup/page.tsx` that only re-exports the view; verify with `pnpm --filter backoffice typecheck`
- [x] 5.4 Add `backoffice/test/presentation/auth/views/signup-view.test.tsx` with `next/navigation` mocked in the file and `signupUseCase` faked through `renderWithProviders`: empty form and malformed email show field errors and do not call `execute`; a short password is still sent; typed values reach `execute`; success calls `replace("/")`; `VALIDATION`, `UNAUTHORIZED`, `CONFLICT`, `NETWORK` each show their Spanish message and keep the email; the button is disabled while pending; AAA with one Act each; verify with `pnpm --filter backoffice test`

## 6. Backoffice access guard and home

- [x] 6.1 Create `backoffice/src/presentation/auth/components/require-session.tsx` (`"use client"`, `checking` / `authorized`, `CircularProgress` while checking, `router.replace("/signup")` on no session or failure, result ignored after unmount) and wrap `HomeView`'s content in it, adding the `home.signedIn` line; verify with `pnpm --filter backoffice typecheck`
- [x] 6.2 Rewrite `backoffice/test/presentation/common/views/home-view.test.tsx` with `next/navigation` mocked and `getSessionUseCase` faked: a session shows the heading and "Has iniciado sesión."; no session and a failed read each call `replace("/signup")` with no heading; a pending read shows the progress indicator and no heading; AAA; verify with `pnpm --filter backoffice test`

## 7. App wiring

- [x] 7.1 In `app/src/infrastructure/container.ts`, pass `c.resolve("keyValueStore")` to `SignupUseCase` before the role (`1 // USER`) and register `getSessionUseCase`; add a do-nothing `getSessionUseCase` entry to `app/test/support/render-with-providers.tsx`; verify with `pnpm --filter app typecheck` and `pnpm --filter app test`
- [x] 7.2 Review the app: the role is still `1`, and `git diff --stat -- app/` shows only `src/infrastructure/container.ts` and `test/support/render-with-providers.tsx`

## 8. Backoffice E2E and review

- [x] 8.1 Replace `backoffice/e2e/home.spec.ts` with `backoffice/e2e/signup.spec.ts`, intercepting `http://localhost:5199/auth/signup` with `page.route` (fulfilling the `OPTIONS` preflight and sending `Access-Control-Allow-Origin`): (1) sign up, land on `/` with the heading, reload and stay; (2) open `/` with fresh storage and end on `/signup` with no heading; (3) a `409` shows "Ya existe una cuenta con este email."; AAA; verify with `pnpm test:e2e` from `front/`
- [x] 8.2 Review the backoffice against `design.md`, the mapped references and `STPR_INVARIANTS.md`: `src/app` holds routes only; every view is `"use client"`; presentation never imports `@weight-tracker/core/infrastructure` or `src/infrastructure`; `localStorage` is used only in the adapter; no hard-coded string; the role is `2`; MUI only; still `output: "export"`; verify by reading `git diff -- backoffice/` (the Next agent-rules block in `backoffice/AGENTS.md` may reappear and is committed as is)

## 9. Integration checks

- [x] 9.1 Run the template drift check from `AGENTS.md` and verify it prints nothing (`.stpr/template/` is untouched)
- [x] 9.2 Run `pnpm typecheck && pnpm lint && pnpm test && pnpm test:e2e` from `front/` and verify it passes

## Workflow follow-up

- Archive the change after review, which syncs the `account-signup` delta and creates the `auth-session`,
  `backoffice-signup` and `backoffice-access-guard` main specs.
