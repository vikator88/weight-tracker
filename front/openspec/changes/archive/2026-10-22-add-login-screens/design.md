# Design

## Context

See `proposal.md` (Why) and the delta specs (behaviour). This design builds on the in-flight
`add-backoffice-signup-screen` and `add-app-signup-screen` changes and does not reopen their decisions.

Current state, from the sources:

- Core `Auth` area: `auth.ts` aliases `SignupRequest`, `AuthResponse`, `Role`, `SignupCredentials`, `Session`.
  `AuthRepositoryInterface` has `signup`. `SignupUseCase(authRepository, keyValueStore, role)` stores both tokens
  under `ACCESS_TOKEN_KEY` / `REFRESH_TOKEN_KEY`, which it exports. `GetSessionUseCase` imports them from it.
  `Dependencies` has `authRepository`, `signupUseCase` and `getSessionUseCase`.
- Backoffice: `/signup` (`SignupView` + `SignupForm`, MUI). `HomeView` sits inside `RequireSession`, which calls
  `router.replace("/signup")` when there is no session. `LocalStorageKeyValueStore` is registered.
- App: `_layout.tsx` gates `(app)` / `(auth)` with `Stack.Protected` on `useSession().status`. `(auth)` holds only
  `signup.tsx`, and `AuthLayout` renders a bare `Stack` with no `Stack.Screen` declarations. `SignupView` calls
  `session.refresh()` on success. The container registers `InMemoryKeyValueStore`.
- `expo-router` resolves to 57.0.29. `expo-sqlite` is not installed.
- `POST /auth/login` is in the snapshot (`LoginRequest` = `{ email, password }`, response `AuthResponse`).

The invariants (section 3 "Presentation" and section 4.3 "Routing" / "Presentation") fix the patterns used here. This
design applies them and does not restate them.

## Goals / Non-Goals

**Goals:**

- Establish `LoginUseCase` as the reference for "calls a repository and a platform port".
- Establish `SqliteKeyValueStore` as the app's session storage.
- Give both fronts a login screen built from their own signup screen, with no new pattern.

**Non-Goals:**

- `signOut` / logout, refresh-token rotation, a backoffice `SessionProvider`.
- Moving the token keys out of `signup-use-case.ts`. Login imports them from there, as `GetSessionUseCase` does.
- A shared credentials form, and a router-level test of the app gate.

## Decisions

### File map

| Front      | Building block            | File                                                                                             |
| ---------- | ------------------------- | ------------------------------------------------------------------------------------------------ |
| core       | Entity type               | `core/src/domain/auth/auth.ts` (`LoginRequest`)                                                  |
| core       | Repository contract       | `core/src/application/ports/auth-repository-interface.ts` (`login`)                              |
| core       | Use case                  | `core/src/application/use-cases/auth/login-use-case.ts` (new), `auth/index.ts` barrel            |
| core       | Composition contract      | `core/src/application/dependencies.ts` (`loginUseCase`)                                          |
| core       | Repository implementation | `core/src/infrastructure/repositories/auth-repository.ts` (`login`)                              |
| backoffice | Composition root          | `backoffice/src/infrastructure/container.ts`                                                     |
| backoffice | Route                     | `backoffice/src/app/login/page.tsx` (new)                                                        |
| backoffice | View / component          | `backoffice/src/presentation/auth/views/login-view.tsx`, `.../components/login-form.tsx` (new)   |
| backoffice | Component (changed)       | `backoffice/src/presentation/auth/components/require-session.tsx` (target `/login`)              |
| backoffice | View (changed)            | `backoffice/src/presentation/auth/views/signup-view.tsx` (link to `/login`)                      |
| app        | Platform adapter          | `app/src/infrastructure/sqlite-key-value-store.ts` (new); `in-memory-key-value-store.ts` removed |
| app        | Composition root          | `app/src/infrastructure/container.ts`                                                            |
| app        | Layout (changed)          | `app/src/app/(auth)/_layout.tsx` (declares `login`, then `signup`)                               |
| app        | Route                     | `app/src/app/(auth)/login.tsx` (new)                                                             |
| app        | View / component          | `app/src/presentation/auth/views/login-view.tsx`, `.../components/login-form.tsx` (new)          |
| app        | Component (changed)       | `app/src/presentation/auth/components/signup-form.tsx` (link to `/login`)                        |
| both       | Translations              | `<front>/src/i18n/{es,en}/auth.json`                                                             |

### Core

- `LoginRequest = components["schemas"]["LoginRequest"]`, next to `SignupRequest`. No credentials alias is needed: the
  request carries no role, so the use case input is `LoginRequest` itself.
- `AuthRepositoryInterface.login(request: LoginRequest): Promise<Result<AuthResponse, DomainError>>`.
  `AuthRepository.login` is `signup` with path `/auth/login`: one `httpClient.request<AuthResponse>` in `try`, and
  `mapHttpError` in `catch`.
- `LoginUseCase implements UseCaseInterface<LoginRequest, AuthResponse>`, constructed as
  `new LoginUseCase(authRepository, keyValueStore)`. Its `execute` body is `SignupUseCase`'s without the role spread:
  return a failed repository result as is; on success write both tokens; if a write throws, return
  `fail(new DomainError("UNEXPECTED", "The session could not be stored"))`.
  - _Alternative considered:_ a shared `storeSession` helper for both use cases. Rejected: two six-line bodies do
    not justify a new abstraction, and it would change `SignupUseCase`, which is out of scope.
- `Dependencies.loginUseCase: UseCaseInterface<LoginRequest, AuthResponse>`. Both containers register
  `loginUseCase: (c) => new LoginUseCase(c.resolve("authRepository"), c.resolve("keyValueStore"))`. No role is passed.

### Backoffice

- `LoginView` / `LoginForm` are copies of `SignupView` / `SignupForm` with the `login.*` keys and `loginUseCase`. The
  error overrides are `["UNAUTHORIZED"]` only. A login `VALIDATION` means a missing field, which the shared copy
  already describes. Success: `router.replace("/")`.
- The links are a MUI `Link` with `component={NextLink}` and `href="/signup"` or `"/login"`, placed below the form in
  each view. Read `backoffice/node_modules/next/dist/docs/` for `next/link` in Next 16 first.
- `RequireSession` changes only its redirect target, and its doc comment says "login".

### App

- **`AuthLayout`** renders `<Stack screenOptions={{ headerShown: false }}>` with `<Stack.Screen name="login" />` then
  `<Stack.Screen name="signup" />`. In `expo-router` 57.0.29, `getSortedChildren` (`build/useScreens.js`) puts
  declared screens first, in declaration order, and the stack's initial route is the first screen. So when
  `Stack.Protected` falls back to `(auth)`, login is shown.
  - _Alternative considered:_ `export const unstable_settings = { initialRouteName: "login" }`. Rejected: not needed
    with explicit declarations, and it would add a second export to a layout. `(auth)/index.tsx` is not possible: it
    would share `/` with `(app)/index.tsx`.
- **`LoginView` / `LoginForm`** are copies of the app's `SignupView` / `SignupForm` (`KeyboardAvoidingView`, Paper
  `TextInput` + `HelperText`, trimmed values, `loading` + `disabled` button), using `login.*` and `loginUseCase`. On
  success: `await session.refresh()`, and nothing navigates. Error overrides: `["UNAUTHORIZED"]`.
- **Links.** `<Link href="/signup" replace asChild>` around a Paper `Button mode="text"` at the bottom of `LoginForm`,
  and the same with `/login` at the bottom of `SignupForm`. The hrefs are typed literals (`typedRoutes`). `replace`
  keeps the `(auth)` stack at one screen.
- **`SqliteKeyValueStore implements KeyValueStoreInterface`:**
  - The database `weight-tracker.db` is opened lazily with `SQLite.openDatabaseAsync`. The open promise is kept in a
    private field, so it is opened once. Right after opening, `execAsync` runs
    `CREATE TABLE IF NOT EXISTS key_value (key TEXT PRIMARY KEY NOT NULL, value TEXT NOT NULL)`. Nothing runs at
    import time or in the constructor, so `container.test.ts` and the view tests never touch SQLite.
  - `get` uses `getFirstAsync<{ value: string }>("SELECT value FROM key_value WHERE key = ?", key)` and returns
    `row?.value ?? null`. `set` uses `runAsync("INSERT OR REPLACE INTO key_value (key, value) VALUES (?, ?)", ...)`.
    `remove` uses `runAsync("DELETE FROM key_value WHERE key = ?", key)`.
  - Errors are not caught here: the port may reject, and the use cases already turn that into `UNEXPECTED`.
  - Read the SDK 57 `expo-sqlite` docs before writing it, and install with `pnpm exec expo install expo-sqlite`.
    `expo-sqlite` is in Expo Go, so no development build is needed and no config plugin is required.
  - _Alternative considered:_ `expo-secure-store` (encrypted). Rejected: the invariants fix `expo-sqlite` and record
    that SQLite is not encrypted as an accepted decision.
- The `SessionProvider`, the root layout and `HomeView` do not change.

### State

| Holder                                      | Holds                                             | Changed by                                      | Resets                           |
| ------------------------------------------- | ------------------------------------------------- | ----------------------------------------------- | -------------------------------- |
| Key-value storage (`localStorage` / SQLite) | `auth.accessToken`, `auth.refreshToken`           | `LoginUseCase`, `SignupUseCase` on success      | never in this change (no logout) |
| App `SessionProvider` (unchanged)           | `status`, `errorCode`                             | its mount, and `refresh()` from login or signup | app restart, re-read from SQLite |
| Backoffice `RequireSession` (unchanged)     | authorized flag                                   | session read on mount                           | unmount                          |
| `LoginForm` / `LoginView` local state       | fields, field errors, submitting, last error code | the user, and the use case result               | screen exit                      |

### Errors and translations

| Code           | Source               | Login screen shows (`es` / `en`)                                                                     |
| -------------- | -------------------- | ---------------------------------------------------------------------------------------------------- |
| `UNAUTHORIZED` | 401                  | `auth:login.errors.UNAUTHORIZED`: "Email o contraseña incorrectos." / "Incorrect email or password." |
| `VALIDATION`   | 400                  | `errors:VALIDATION` (existing)                                                                       |
| `NETWORK`      | no response          | `errors:NETWORK`: "No hay conexión con el servidor." (existing)                                      |
| any other      | mapper               | `errors:<code>` (existing)                                                                           |
| `UNEXPECTED`   | storage write failed | `errors:UNEXPECTED` (existing)                                                                       |

New keys in each front's `auth.json`: `login.title`, `login.email`, `login.password`, `login.submit`,
`login.validation.{emailRequired,emailFormat,passwordRequired}`, `login.errors.UNAUTHORIZED`, `login.toSignup`,
`signup.toLogin`. The texts are those listed in the brief. Each front keeps its own file, and no translation is shared
between fronts. No new mapper code.

### Tests

- **Core.** `core/test/application/use-cases/auth/login-use-case.test.ts` follows `signup-use-case.test.ts`:
  credentials pass through unchanged; both tokens are stored on success; nothing is stored on failure; `UNEXPECTED`
  when the store rejects; a repository failure comes back unchanged. `auth-repository.test.ts` gains a `login` block:
  `POST /auth/login` with the body, success, 400 → `VALIDATION`, 401 → `UNAUTHORIZED`, network → `NETWORK`,
  500 → `UNEXPECTED`.
- **Backoffice.** `login-view.test.tsx` follows `signup-view.test.tsx`: empty form and malformed email block submit;
  pending disables the button; success → `replace("/")`; `UNAUTHORIZED` (email kept) and `NETWORK` messages; the link
  `href` is `/signup`. `signup-view.test.tsx` asserts the `/login` link. `home-view.test.tsx` expects `/login`.
  `e2e/login.spec.ts` covers the four flows in the brief, and flow (2) is removed from `signup.spec.ts`.
- **App.** `login-view.test.tsx` follows the app's `signup-view.test.tsx`: field errors, trimmed values, pending state,
  success → `getSessionUseCase` runs again and no `expo-router` call is made, `UNAUTHORIZED` (email kept) and
  `NETWORK`. `expo-router`'s `Link` is mocked in the file to render its child, as `not-found-view.test.tsx` does. The
  test asserts the link text, and the mock records the `href`. `signup-view.test.tsx` gets the same `Link` mock and a
  case for the `/login` link. `test/infrastructure/sqlite-key-value-store.test.ts` mocks `expo-sqlite` with a fake
  database backed by a `Map` and checks get/set/remove, a missing key → `null`, an overwrite, and that the database is
  opened only once.
- **Test helpers.** Both `render-with-providers.tsx` gain `loginUseCase: () => ({ execute: async () => undefined as never })`,
  and the `authRepository` placeholder gains `login`. These are additive keys, not a pattern change.
- Every test uses `// Arrange` / `// Act` / `// Assert`, with one Act.

## Risks / Trade-offs

- [The `(auth)` initial screen depends on declaration order, an Expo Router internal] → Verified against 57.0.29.
  The app gate is checked by hand on a running app, and a task re-checks it after any `expo-router` upgrade.
- [Adding `Link` to the app's `SignupForm` makes its existing test need a router] → `Link` is mocked in that file, as
  in `not-found-view.test.tsx`.
- [A user who logs in to the wrong front (an athlete in the backoffice) gets a session there] → Accepted. The API is
  the authority on every protected call, and no protected call exists yet. A role check belongs to the API.
- [SQLite data is not encrypted on the device] → An accepted decision in the invariants.
- [Two forms with the same fields drift apart] → Accepted for now. Merging them is a later refactor.
- [Archive order] → `add-backoffice-signup-screen` and `add-app-signup-screen` must be archived before this change,
  or its MODIFIED deltas have no target.

## Migration Plan

No API or data migration. The in-memory store never persisted anything, so no stored session needs to move. Rollback
is a revert of the change. After a rollback, the app is back to the in-memory store, and an existing SQLite file is
ignored.
