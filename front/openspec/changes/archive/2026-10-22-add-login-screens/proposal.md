# Proposal

Source brief: `.stpr/plan/openspec/proposal-change-add-login-screens.md`.

## Why

Signup stores a session, but an existing account has no way to start one. Both fronts send a visitor without a session
to signup: the backoffice through `RequireSession`, the app through its `Stack.Protected` gate. The app also keeps its
tokens in memory, so even a signed-in athlete is signed out when the app restarts. This change adds login to `core`
and to both fronts, makes login the screen a visitor without a session lands on, and gives the app the SQLite store
the invariants assign to it.

## What Changes

- Core: add the `LoginRequest` alias, `login` on `AuthRepositoryInterface` / `AuthRepository`, and `LoginUseCase`,
  which stores both tokens on success, like `SignupUseCase`. `Dependencies` gains `loginUseCase`.
- Backoffice: add `/login` (route, `LoginView`, `LoginForm`). `RequireSession` now sends visitors without a session to
  `/login` instead of `/signup`. Login links to signup, and signup links back to login. Register `loginUseCase`.
- App: add `(auth)/login.tsx` (`LoginView`, `LoginForm`). `AuthLayout` puts `login` first, so the gate shows it to an
  athlete without a session. A successful login calls `session.refresh()`, and nothing navigates. Login and signup
  link to each other with `Link replace`. Register `loginUseCase`.
- App: replace `InMemoryKeyValueStore` with a new `SqliteKeyValueStore` (`expo-sqlite`, added with `expo install`).
  The session survives a restart.
- New `login.*`, `login.toSignup` and `signup.toLogin` keys in each front's `auth` namespace, in `es` and `en`.
- No API change, no snapshot refresh, no new error code.

## Capabilities

### New Capabilities

- `account-login`: the core login operation. It sends the credentials to `POST /auth/login`, stores the tokens on
  success, and returns every failure as a `DomainError` code.
- `backoffice-login`: the backoffice login screen: form, structural validation, error messages, what follows a
  success, and the link to signup.
- `app-login`: the app login screen: form, structural validation, error messages, what follows a success, and the link
  to signup.

### Modified Capabilities

These capabilities are introduced by the in-flight changes `add-backoffice-signup-screen` and `add-app-signup-screen`.
Both must be archived before this one.

- `backoffice-access-guard`: a visitor without a session (or whose session read fails) ends on `/login`, not `/signup`.
- `backoffice-signup`: the signup screen gains a link to `/login`.
- `app-access-gate`: the signed-out screen the gate shows first is login, not signup; both login and signup are hidden
  from a signed-in athlete.
- `app-signup`: an athlete without a session first sees login; signup gains a link to `/login`.
- `auth-session`: the app session survives a restart (SQLite) instead of lasting only for the running process.

## Fronts And Roles

- **backoffice**: `TRAINER` and `ADMIN` both log in from `/login` and see the same home. Only `TRAINER` can sign up,
  unchanged. An anonymous visitor must not see any home content, not even while the session is read.
- **app**: `USER` logs in from `/login`, or moves to `/signup`. An anonymous athlete must not see any signed-in content.
  A signed-in athlete must not reach `/login` or `/signup`.

`LoginRequest` has no role, so neither front filters by role on login. If a front should refuse a role it does not
serve, that is for the API to decide (out of scope).

## Canonical References

- `LoginRequest`, `AuthRepository.login`, `LoginUseCase` and their tests: skeleton-backed, modelled on `SignupRequest`,
  `AuthRepository.signup`, `SignupUseCase` and their tests. `LoginUseCase` is `pending` in `.stpr/template/README.md`;
  **this change establishes it**.
- Both `container.ts`: skeleton-backed, extended in place.
- Backoffice login route, view, form and tests: modelled on the backoffice signup ones (`src/app/signup/page.tsx`,
  `signup-view.tsx`, `signup-form.tsx`, `signup-view.test.tsx`, `e2e/signup.spec.ts`).
- App login route, view, form and test: modelled on the app signup ones (`(auth)/signup.tsx`, `signup-view.tsx`,
  `signup-form.tsx`, `signup-view.test.tsx`). Neither front is a UI reference for the other.
- `SqliteKeyValueStore`: `pending`; **this change establishes it**, shaped like `InMemoryKeyValueStore`, with its test
  shaped like `local-storage-key-value-store.test.ts`.
- `.stpr/template/` and its README are not updated here.

## Constraints And Conflicts

- **Gate placement (app).** Section 4.3 forbids `Redirect` / `router.replace` for access control. Login calls
  `session.refresh()` and the gate moves the athlete on. The login⇄signup links are in-flow navigation inside
  `(auth)`, which views may do.
- **Initial `(auth)` screen.** `AuthLayout` declares `login` before `signup`. In `expo-router` 57.0.29, declared
  screens come first, in declaration order, and the stack starts on the first one. So no `unstable_settings` export is
  needed, and the layout keeps to "a layout default-exports `<Name>Layout`" (see design.md).
- **Archive order.** The modified capabilities exist only in the in-flight signup changes. Those changes must be
  archived first, or the deltas here have nothing to modify.
- **Not authorization.** Both guards are client-side navigation. The API remains the authority on every protected call.
- **No business rule.** The forms check only presence and email format. The API decides whether credentials are valid.
- No other conflict with the template, the invariants or a stop case.

## API Contract

- `POST /auth/login`: in `core/openapi/v1.json`. Request `LoginRequest` (`email`, `password`), response
  `AuthResponse`. The API answers `400` for a missing field and `401` for a malformed or unknown email or a wrong
  password.
- `POST /auth/signup`: unchanged.

No snapshot refresh, no API change.

## Acceptance Criteria

- Backoffice: opening `/` without a session ends on `/login`, and the home heading is never rendered.
- App: launching without a session shows login, and no signed-in content is rendered.
- Logging in with valid credentials stores both tokens and shows home: in the backoffice through `router.replace("/")`,
  in the app through the gate, with no navigation call in the view.
- An empty or malformed email, or an empty password, shows a field error and sends no request.
- A `401` shows "Email o contraseña incorrectos.", keeps the entered email and stores nothing. `NETWORK` shows the
  generic network message.
- Login links to signup, and signup links back to login, in both fronts.
- App: after logging in and restarting, the athlete sees home.
- Every new string exists in `es` and `en`.
- `pnpm typecheck && pnpm lint && pnpm test && pnpm test:e2e` passes from `front/`, with the API mocked in every tier.

## Impact

- `core`: `domain/auth/auth.ts`, `application/ports/auth-repository-interface.ts`, `application/use-cases/auth`
  (new `login-use-case.ts`), `application/dependencies.ts`, `infrastructure/repositories/auth-repository.ts`, barrels,
  tests.
- `backoffice`: `src/app/login`, `src/presentation/auth` (`login-view.tsx`, `login-form.tsx`, `require-session.tsx`,
  `signup-view.tsx`), `src/i18n/{es,en}/auth.json`, `src/infrastructure/container.ts`, `test/`, `e2e/`.
- `app`: `src/app/(auth)` (`_layout.tsx`, new `login.tsx`), `src/presentation/auth` (`login-view.tsx`,
  `login-form.tsx`, `signup-form.tsx`), `src/i18n/{es,en}/auth.json`, `src/infrastructure` (new
  `sqlite-key-value-store.ts`, `container.ts`, `in-memory-key-value-store.ts` removed), `test/`.
- New dependency: `expo-sqlite` in `app`, bundled in Expo Go, so no development build is needed.
