# Design

## Context

See proposal.md for motivation and scope, and the `account-logout`, `backoffice-logout` and `app-logout` specs for
the required behaviour.

Today the tokens live under `auth.accessToken` / `auth.refreshToken` through `KeyValueStoreInterface`
(`localStorage` in the backoffice, SQLite in the app). `KeyValueStoreInterface.remove` already exists in both adapters
and in both test fakes. Nothing calls it yet. The API snapshot has no logout endpoint.

Each front already has the pieces logout needs:

- backoffice: `HomeView` is wrapped in `RequireSession`, which reads the session once on mount and redirects to
  `/login` when there is none. It holds no shared session state.
- app: `SessionProvider` holds `status` and exposes `refresh()`. The root layout's `Stack.Protected` gate follows
  `status`.

## Goals / Non-Goals

**Goals:**

- One use case in `core`, used by both fronts.
- The smallest possible UI: one button inline in each `HomeView`, easy to delete or move later.

**Non-Goals:**

- A reusable logout component, an app bar, or a menu.
- Any change to `RequireSession`, `SessionProvider`, the gate, or the test helpers' structure.

## Decisions

### Core

- **`LogoutUseCase`** in `core/src/application/use-cases/auth/logout-use-case.ts`, implementing
  `UseCaseInterface<void, void>`. The constructor takes only `KeyValueStoreInterface`. `execute()` removes
  `REFRESH_TOKEN_KEY`, then `ACCESS_TOKEN_KEY`, inside one `try/catch`, and returns `ok(undefined)`. A throw from the
  store returns `fail(new DomainError("UNEXPECTED", "The session could not be cleared"))`. The keys are imported from
  `signup-use-case.ts`, as `GetSessionUseCase` and `LoginUseCase` already do.
  - _Why the refresh token first:_ if the second removal fails, the access token alone remains, and
    `GetSessionUseCase` already reads that as no session. Removing the long-lived token first leaves the safer half.
  - _No rollback:_ a partially cleared store already reads as signed out. Restoring a token would only re-create a
    session the user asked to end.
  - _Alternative rejected:_ a new `remove` method on a session repository. There is no such repository, and the other
    session use cases talk to the store directly.
- **`Dependencies`** gains `logoutUseCase: UseCaseInterface<void, void>`.
- **Barrel** `use-cases/auth/index.ts` (and the application barrel through it) re-exports `LogoutUseCase`.

### Backoffice

- **`container.ts`**: `logoutUseCase: (c) => new LogoutUseCase(c.resolve("keyValueStore"))`, next to
  `getSessionUseCase`.
- **`HomeView`**: inside `RequireSession`, below the existing text, an MUI `Button` (`variant="outlined"`) labelled
  `t("home.logout")`. Local state `loggingOut: boolean` and `errorCode: DomainErrorCode | null`, as in the backoffice
  `LoginView`. On click: set `loggingOut`, clear `errorCode`, call `logoutUseCase`. On success,
  `router.replace("/login")` (from `next/navigation`). On failure, store the code, clear `loggingOut`, and render
  `<Alert severity="error">{t(\`errors:${errorCode}\`)}</Alert>`. The button is `disabled={loggingOut}`.
  - `RequireSession` is not touched. After the redirect, the next visit to `/` re-reads the store and redirects again.

### App

- **`container.ts`**: `logoutUseCase: (c) => new LogoutUseCase(c.resolve("keyValueStore"))`.
- **`HomeView`**: below the existing text, a Paper `Button` (`mode="outlined"`) labelled `t("home.logout")`. Local
  state `loggingOut` and `errorCode`, as in the app `LoginView`. On press: call `logoutUseCase`. On success,
  `await session.refresh()` and nothing else: the gate hides `(app)` and shows `(auth)`, whose first screen is login.
  On failure, store the code, clear `loggingOut`, and render `<HelperText type="error">` with
  `t(\`errors:${errorCode}\`)`. The button is `disabled={loggingOut}`and`loading={loggingOut}`.
  - No `router` call, per section 4.3: views do not navigate to deny access.

### Held state

| State                     | Holder                        | Changed by                                                      | Resets              |
| ------------------------- | ----------------------------- | --------------------------------------------------------------- | ------------------- |
| Stored tokens             | `KeyValueStoreInterface`      | `LoginUseCase`, `SignupUseCase` (set); `LogoutUseCase` (remove) | on logout           |
| App `status`              | `SessionProvider` (unchanged) | `refresh()` after logout                                        | read again on mount |
| `loggingOut`, `errorCode` | each `HomeView`               | the logout handler                                              | on screen exit      |

### Errors and copy

- `UNEXPECTED` (store failure) -> `errors:UNEXPECTED` -> "Ha ocurrido un error inesperado." / "Something went wrong."
  (existing keys). No other code can come out of `LogoutUseCase`.
- New key `home.logout` in each front's `common.json`: `es` "Cerrar sesión", `en` "Log out".
- No new error code and no HTTP error mapper change.

### Tests

- **Test helpers.** Each `render-with-providers.tsx` gains one `logoutUseCase` placeholder in `unusedDependencies`.
  This is the established way to register a new key, not a change of pattern.
- **core** `core/test/application/use-cases/auth/logout-use-case.test.ts`, shaped like
  `get-session-use-case.test.ts`, with a fake store: both keys removed and `ok`; `UNEXPECTED` when `remove` throws.
- **backoffice** `home-view.test.tsx` gains: the button shows with a session; click calls the use case and
  `replace("/login")`; `UNEXPECTED` shows the message and does not call `replace`; the existing "no session" case also
  asserts the button is absent.
- **app** `home-view.test.tsx` gains: click calls the use case and `getSessionUseCase` runs a second time; `UNEXPECTED`
  shows the message. No navigation is mocked, so none can happen.
- No E2E: the button is temporary.

### App gate: `redirectTo` on the `(app)` guard

Found in manual testing: after logout the app showed `+not-found`, and it showed it again after a restart. The guard
flips, but the URL stays `/`, which only `(app)/index.tsx` serves. `+not-found` sits outside any guard, so it is
the only available match. "Go to home" links to `/` again, so the user stays on the same screen.

Fix: `<Stack.Protected guard={signedIn} redirectTo="/login">` in `src/app/_layout.tsx`. The prop exists since
SDK 58 (`ProtectedProps.redirectTo?: Href`). Access is still decided only by the root layout, by declaration: no
`Redirect` and no `router.replace`. Views and the session hook still do not navigate.

- _Alternatives rejected:_ `(auth)/index.tsx` (two route files for `/`); `Redirect` in a view or the provider
  (forbidden by section 4.3); moving `+not-found` into a guard (the invariants keep it at the root).
- This refines the invariant's "Expo Router moves the user to the first available screen". Updating
  `STPR_INVARIANTS.md` to name `redirectTo` is left to a separate template change.

## Risks / Trade-offs

- [The refresh token stays valid on the API after logout] → Accepted for a development button. Revocation needs an
  API endpoint first, planned in `api/`.
- [The button lives in `HomeView`, so moving it later touches home and its tests] → Kept inline and self-contained so
  the move is a cut and paste. The `LogoutUseCase` and its wiring stay.
- [Backoffice: another open tab keeps showing home until it navigates] → Accepted. `RequireSession` only checks on
  mount, as today.
