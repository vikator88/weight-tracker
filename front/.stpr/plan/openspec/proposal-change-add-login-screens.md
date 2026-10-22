# Change Proposal: Add Login Screens

Source brief for `/opsx:propose`. Artifacts go to `openspec/changes/add-login-screens/`.

## Change

Add a `LoginUseCase` to `core` and a login screen to both fronts. Login becomes the screen a visitor without a session
lands on, and login and signup link to each other.

## Objective

Signup stores a session, but there is no way to start one with an existing account. Today both fronts send a visitor
without a session to signup: the backoffice through `RequireSession`, the app through its `Stack.Protected` gate.

After this change both fronts behave the same way. Opening the front without a session lands on login. Login and signup
link to each other. Either one stores the tokens and lands on home. The session survives a reload in the backoffice
and an app restart in the app.

## Capabilities

- Log in with email and password through a use case that returns `Result`, never throws, and stores both tokens on
  success.
- Backoffice: `/login` screen. `/` sends visitors without a session to `/login` instead of `/signup`.
- App: `/login` screen in `(auth)`, the first screen the gate shows to an athlete without a session.
- Login links to signup, and signup links back to login, in both fronts.
- The app stores tokens in SQLite, so the session survives an app restart.

## Fronts And Roles

- backoffice -> lands here: yes -> roles: `TRAINER`, `ADMIN` -> both log in from `/login` and see the same home.
  Only `TRAINER` can sign up, unchanged.
- app -> lands here: yes -> roles: `USER` -> logs in from `/login`, or moves to `/signup`, then sees home.

`LoginRequest` has no role, so the front does not filter by role on login. If a `USER` logs in to the backoffice, or a
trainer logs in to the app, that is for the API to reject. Nothing here checks it (see Out Of Scope).

An anonymous visitor must NOT see any home content in either front, not even while the session is being read. A
signed-in athlete must not reach `/login` in the app: the gate already hides the whole `(auth)` group.

## Classification

Core:

- `LoginRequest` alias -> entity type -> `core/src/domain/auth/auth.ts`
- `AuthRepositoryInterface.login` -> repository contract -> `core/src/application/ports/auth-repository-interface.ts`
- `AuthRepository.login` -> repository implementation -> `core/src/infrastructure/repositories/auth-repository.ts`
- `LoginUseCase` -> use case -> `core/src/application/use-cases/auth/login-use-case.ts`
- `Dependencies` key `loginUseCase` -> composition contract -> `core/src/application/dependencies.ts`
- Barrels re-export `LoginUseCase` and `LoginRequest`

Backoffice:

- Registration of `loginUseCase` -> composition root -> `backoffice/src/infrastructure/container.ts`
- `/login` route -> route -> `backoffice/src/app/login/page.tsx`
- `LoginView` -> view -> `backoffice/src/presentation/auth/views/login-view.tsx`
- `LoginForm` -> component -> `backoffice/src/presentation/auth/components/login-form.tsx`
- `RequireSession` (now redirects to `/login`) -> component -> `backoffice/src/presentation/auth/components/require-session.tsx`
- `SignupView` (gains the link to login) -> view -> `backoffice/src/presentation/auth/views/signup-view.tsx`
- `auth.login.*` and link keys -> i18n -> `backoffice/src/i18n/{es,en}/auth.json`

App:

- `SqliteKeyValueStore` -> platform adapter -> `app/src/infrastructure/sqlite-key-value-store.ts`
- Registrations of `keyValueStore` (now SQLite) and `loginUseCase` -> composition root -> `app/src/infrastructure/container.ts`
- `/login` route -> route -> `app/src/app/(auth)/login.tsx`
- `AuthLayout` (declares `login` before `signup`) -> layout -> `app/src/app/(auth)/_layout.tsx`
- `LoginView` -> view -> `app/src/presentation/auth/views/login-view.tsx`
- `LoginForm` -> component -> `app/src/presentation/auth/components/login-form.tsx`
- `SignupForm` (gains the link to login) -> component -> `app/src/presentation/auth/components/signup-form.tsx`
- `auth.login.*` and link keys -> i18n -> `app/src/i18n/{es,en}/auth.json`

`InMemoryKeyValueStore` is removed from the app, which no longer uses it. The root layout, the session state hook
(`SessionProvider` / `useSession`) and `HomeView` do not change in the app.

## Canonical Template Mapping

- `LoginRequest` alias -> `core/src/domain/auth/auth.ts` (skeleton-backed) -> extend it next to `SignupRequest`.
- `AuthRepositoryInterface.login` / `AuthRepository.login` -> the `signup` method in the same files (skeleton-backed)
  -> same operation shape: `POST`, body passed through, failures through `mapHttpError`.
- `LoginUseCase` -> `core/src/application/use-cases/auth/signup-use-case.ts` (skeleton-backed) -> the same flow
  without the role: call the repository, store both tokens on success, return `UNEXPECTED` if storing fails. It
  reuses `ACCESS_TOKEN_KEY` / `REFRESH_TOKEN_KEY` from that file, as `GetSessionUseCase` already does. The
  `LoginUseCase` row in `.stpr/template/README.md` is `pending`. This change establishes it.
- Core tests -> `signup-use-case.test.ts` and `auth-repository.test.ts` (skeleton-backed).
- Both `container.ts` -> skeleton-backed -> extend them and keep their structure.
- Backoffice login route, view and form -> `backoffice/src/app/signup/page.tsx`, `signup-view.tsx`,
  `signup-form.tsx` -> the same screen with other copy, use case and error overrides.
- Backoffice tests -> `backoffice/test/presentation/auth/views/signup-view.test.tsx`, `e2e/signup.spec.ts`.
- App login route, view and form -> `app/src/app/(auth)/signup.tsx`, `app/src/presentation/auth/views/signup-view.tsx`,
  `signup-form.tsx` (from `add-app-signup-screen`) -> the same screen: `KeyboardAvoidingView`, Paper `TextInput` +
  `HelperText`, trimmed values, and `session.refresh()` on success with no navigation call.
- App login view test -> `app/test/presentation/auth/views/signup-view.test.tsx`.
- `SqliteKeyValueStore` -> `pending` -> this change establishes it. Model its shape on
  `app/src/infrastructure/in-memory-key-value-store.ts` and its test on
  `backoffice/test/infrastructure/local-storage-key-value-store.test.ts` (a port test, not UI).

The backoffice and the app each copy their own signup screen. Neither is a UI reference for the other.
`.stpr/template/` and its README are not updated here. Promoting the login slice to the template is its own step, once
this change is verified.

## Screens And Navigation

Backoffice (client-side navigation only, `next/navigation` / `next/link`; static export):

- `/login` -> `LoginView` -> typed URL, redirected from `/` without a session, or the link on signup -> success:
  `router.replace("/")`; failure: stays, shows the error, keeps the email. A link goes to `/signup`.
- `/signup` -> `SignupView` -> the link on login, or typed URL -> unchanged, plus a link to `/login`.
- `/` -> `HomeView` inside `RequireSession` -> no session: `router.replace("/login")` (was `/signup`).

App (Expo Router, gate unchanged):

- `/login` -> `LoginView` -> the first screen of `(auth)`, shown by the gate when there is no session, or the link on
  signup -> success: `session.refresh()`, the guard flips, and Expo Router shows `(app)`, with no navigation call;
  failure: stays, shows the error, keeps the email. A link goes to `/signup`.
- `/signup` -> `SignupView` -> the link on login -> unchanged, plus a link back to `/login`.

The links between login and signup are typed `Link`s with `replace`, inside the forms, so moving between them several
times does not pile up screens. This is in-flow navigation inside `(auth)`, not access control, so section 4.3 allows
it.

## API Contract

- `POST /auth/login` -> in `core/openapi/v1.json`: yes -> request `LoginRequest` (`email`, `password`), response
  `AuthResponse`.
- `POST /auth/signup` -> yes -> unchanged.

No snapshot refresh. No API change.

## Held State

- Tokens under `auth.accessToken` / `auth.refreshToken`, through `KeyValueStoreInterface` -> written by
  `LoginUseCase` or `SignupUseCase` on success only -> nothing removes them yet (no logout). Backoffice:
  `localStorage`. App: SQLite, survives restarts.
- App `SessionProvider` (existing) -> `status` -> restored on mount, and re-read by `refresh()` after login or signup ->
  unchanged by this change.
- Backoffice `RequireSession` local state -> unchanged.
- `LoginForm` / `SignupForm` local state (fields, field errors, submitting, API error) -> reset on screen exit.

## Forms And Structural Validation

- Login form -> `email` -> required, email format.
- Login form -> `password` -> required.

Each front's login form trims values exactly as its own signup form does. The snapshot declares no maximum lengths.
Submit is disabled while a request is in flight.

## Errors And Copy

Login, from the API's `ExceptionMapper`:

- Missing field (model validation, blocked by the form first) -> `VALIDATION` (400) -> `errors:VALIDATION`
- Malformed or unknown email, or wrong password -> `UNAUTHORIZED` (401) -> `auth:login.errors.UNAUTHORIZED`. The
  API makes the three cases indistinguishable on purpose, and so does the screen.
- No response -> `NETWORK` -> `errors:NETWORK`
- Any other code -> `errors:<code>`

No new error code. No change to the HTTP error mapper.

New keys in the `auth` namespace of both fronts:

- `login.title` -> "Iniciar sesión" -> "Log in"
- `login.email` -> "Email" -> "Email"
- `login.password` -> "Contraseña" -> "Password"
- `login.submit` -> "Entrar" -> "Log in"
- `login.validation.emailRequired` -> "Introduce tu email." -> "Enter your email."
- `login.validation.emailFormat` -> "Introduce un email válido." -> "Enter a valid email."
- `login.validation.passwordRequired` -> "Introduce tu contraseña." -> "Enter your password."
- `login.errors.UNAUTHORIZED` -> "Email o contraseña incorrectos." -> "Incorrect email or password."
- `login.toSignup` -> "¿No tienes cuenta? Crea una" -> "No account? Create one"
- `signup.toLogin` -> "¿Ya tienes cuenta? Inicia sesión" -> "Already have an account? Log in"

## Platform Capabilities And Dependencies

- Key-value storage -> `KeyValueStoreInterface` (existing) -> `LocalStorageKeyValueStore` (unchanged) ->
  `SqliteKeyValueStore` (new) -> development build: no, `expo-sqlite` is bundled in Expo Go.
- `expo-sqlite` -> `app` -> the invariants name it as the app's session storage. Add it with
  `pnpm exec expo install expo-sqlite`, never `pnpm add`.

## Out Of Scope

- Logout, token refresh, expiry handling, `RefreshSessionUseCase`.
- A `SessionProvider` in the backoffice. It keeps `RequireSession`.
- Backoffice: redirecting a visitor who already has a session away from `/login` or `/signup`.
- Rejecting, in the front, a login by a role the front does not serve. If that is needed, the API should decide it.
- Forgot password, remember me, showing the password.
- Sharing one form between login and signup (see Notes).
- Updating `.stpr/template/` or `.stpr/template/README.md`.
- Must not change: the API, the snapshot, the HTTP error mapper, `SignupUseCase`, `GetSessionUseCase`, the app's root
  layout and session state hook.

## Notes

- **Builds on in-flight changes.** `add-backoffice-signup-screen` and `add-app-signup-screen` are still under
  `openspec/changes/`, not archived. This change builds on both and does not reopen their decisions.
- **Login first in `(auth)`.** With no session, the gate shows the first available screen of `(auth)`. `AuthLayout`
  declares `<Stack.Screen name="login" />` before `signup`. Check in the SDK 57 Expo Router docs that this order
  decides the initial screen. If it does not, the layout also exports `unstable_settings = { initialRouteName: "login" }`.
  That is the only allowed deviation from "a layout default-exports `<Name>Layout`", and the design must record it.
  `(auth)/index.tsx` is not an option: it would share the URL `/` with `(app)/index.tsx`.
- **Why the app gets SQLite now.** With the in-memory store, an app login lasts only until the app is closed. The
  invariants and both signup proposals leave the SQLite adapter to the login slice. The adapter opens the database and
  creates its single key/value table on first use, inside its async methods, never at import time. Read the SDK 57
  `expo-sqlite` docs before writing it.
- **Separate forms.** `LoginForm` duplicates `SignupForm`'s two fields on purpose, so each screen keeps its own copy
  and error overrides and signup does not change. Merging them can be a later refactor.
- **Next 16.** Read `backoffice/node_modules/next/dist/docs/` for `next/link` before adding the links.
- **Verification tiers:**
  - core unit: `login-use-case.test.ts`: credentials pass through, both tokens are stored on success, nothing is
    stored on failure, `UNEXPECTED` when the store fails.
  - core unit: `auth-repository.test.ts` adds `login`: the request (method, path, body), success, 400 -> `VALIDATION`,
    401 -> `UNAUTHORIZED`, network failure -> `NETWORK`.
  - backoffice component: `login-view.test.tsx`: structural errors block submit and the use case is not called;
    success goes to `/`; `UNAUTHORIZED` and `NETWORK` show their messages; the link points to `/signup`.
  - backoffice component: `signup-view.test.tsx` checks the link to `/login`. `home-view.test.tsx`: no session goes to
    `/login`, and the home heading is not rendered.
  - backoffice E2E: `e2e/login.spec.ts`, with `POST /auth/login` intercepted: (1) log in and land on `/` with the home
    heading; (2) open `/` with empty storage and end on `/login` without the home heading; (3) a `401` shows the
    invalid-credentials message; (4) the link goes to `/signup` and back. In `signup.spec.ts`, flow (2) moves here.
  - app unit: `test/infrastructure/sqlite-key-value-store.test.ts`: get, set, remove, and missing key -> `null`,
    against a mocked `expo-sqlite`.
  - app component: `login-view.test.tsx`: structural errors block submit and the use case is not called; success
    re-reads the session (`getSessionUseCase` called again) and no navigation call is made; `UNAUTHORIZED` and
    `NETWORK` show their messages and keep the email; the link targets `/signup`. `signup-view.test.tsx` checks the
    link to `/login`.
  - The gate itself (login shown first without a session) is not unit-tested, since the root layout has no component
    test today. It is checked by hand on a running app.
  - Both `render-with-providers.tsx` gain a `loginUseCase` placeholder, and the app's `authRepository` placeholder
    gains `login`.
  - App E2E: known gap, accepted.
- Done means `pnpm typecheck && pnpm lint && pnpm test && pnpm test:e2e` passes from `front/`.
- No conflict with a stop case. Apart from the conditional `unstable_settings` above, no deviation from a skeleton
  reference is allowed. Where a reference is `pending`, this change establishes it as described above.
