# Change Proposal: Add Logout

Source brief for `/opsx:propose`. Artifacts go to `openspec/changes/add-logout/`.

## Change

Add a `LogoutUseCase` to `core` and a temporary logout button on the home view of both fronts.

## Objective

Login and signup store a session, but nothing removes it. While developing, the only way to get back to login is to
clear storage by hand (`localStorage` in the backoffice, reinstalling or wiping the app data in the app).

This change adds a quick logout: one button on home that clears the stored tokens and returns to login. The button is
a development convenience. Its placement is provisional and will move to a proper place (menu, profile, settings) in
a later change.

## Capabilities

- Log out through a use case that removes both stored tokens, returns `Result`, and never throws.
- Backoffice: a logout button on `HomeView`. On success the visitor lands on `/login`.
- App: a logout button on `HomeView`. On success the session is re-read, the gate flips, and the athlete sees
  `(auth)`'s login.

## Fronts And Roles

- backoffice -> lands here: yes -> roles: `TRAINER`, `ADMIN` -> both see the same button on home and log out the same way.
- app -> lands here: yes -> roles: `USER` -> logs out from home.

No role difference. Home is already hidden from visitors without a session, so the button is never shown to them.

## Classification

Core:

- `LogoutUseCase` -> use case -> `core/src/application/use-cases/auth/logout-use-case.ts`
- `Dependencies` key `logoutUseCase` (`UseCaseInterface<void, void>`) -> composition contract -> `core/src/application/dependencies.ts`
- Barrels re-export `LogoutUseCase`

Backoffice:

- Registration of `logoutUseCase` -> composition root -> `backoffice/src/infrastructure/container.ts`
- `HomeView` (gains the button) -> view -> `backoffice/src/presentation/common/views/home-view.tsx`
- `home.logout` key -> i18n -> `backoffice/src/i18n/{es,en}/common.json`

App:

- Registration of `logoutUseCase` -> composition root -> `app/src/infrastructure/container.ts`
- `HomeView` (gains the button) -> view -> `app/src/presentation/common/views/home-view.tsx`
- `home.logout` key -> i18n -> `app/src/i18n/{es,en}/common.json`

No new component or state hook. The button lives inline in each `HomeView`, since it is temporary.

## Canonical Template Mapping

- `LogoutUseCase` -> `core/src/application/use-cases/auth/get-session-use-case.ts` -> the nearest shape: depends only
  on `KeyValueStoreInterface`, takes no input, makes no API call, wraps store access in `try/catch` and returns
  `UNEXPECTED` on failure. Reuses `ACCESS_TOKEN_KEY` / `REFRESH_TOKEN_KEY` from `signup-use-case.ts`.
- Core test -> `core/test/application/use-cases/auth/get-session-use-case.test.ts`.
- Both `container.ts` -> extend them and keep their structure.
- Backoffice button -> the submit handling in `backoffice/src/presentation/auth/views/login-view.tsx` (call the use
  case, `router.replace` on success) -> MUI `Button`, same front.
- App button -> the submit handling in `app/src/presentation/auth/views/login-view.tsx` (call the use case,
  `session.refresh()` on success, no navigation call) -> Paper `Button`, same front.
- Component tests -> the existing `home-view.test.tsx` of each front.

No UI reference crosses fronts. `.stpr/template/` and its README are not updated.

## Screens And Navigation

Backoffice:

- `/` -> `HomeView` -> logout button -> success: `router.replace("/login")`; failure: stays on home and shows the error.

App:

- `/` (in `(app)`) -> `HomeView` -> logout button -> success: `session.refresh()`, the guard flips, Expo Router shows
  `(auth)` with login first, with no navigation call; failure: stays on home and shows the error.

The button is disabled while the use case runs.

## API Contract

None. The snapshot has no logout or token-revocation endpoint (`/auth/signup`, `/auth/login`, `/auth/refresh` only),
so logout is local: the tokens are removed from the device and nothing is sent to the API. No snapshot refresh.

## Held State

- Tokens under `auth.accessToken` / `auth.refreshToken`, through `KeyValueStoreInterface` -> now also removed by
  `LogoutUseCase` -> backoffice `localStorage`, app SQLite.
- App `SessionProvider` (existing) -> `status` -> re-read by `refresh()` after logout -> unchanged.
- Backoffice `RequireSession` -> unchanged; the next visit to `/` re-reads the store and redirects.
- `HomeView` local state (in-flight flag, error code) -> reset on screen exit.

## Forms And Structural Validation

None.

## Errors And Copy

- The store fails to remove a token -> `UNEXPECTED` -> `errors:UNEXPECTED` on home.

No new error code. No change to the HTTP error mapper.

New key in the `common` namespace of both fronts:

- `home.logout` -> "Cerrar sesión" -> "Log out"

## Platform Capabilities And Dependencies

None. `KeyValueStoreInterface.remove` already exists in both adapters.

## Out Of Scope

- A final placement for logout (app bar, menu, profile, settings). This button is temporary.
- Revoking the refresh token on the API. It needs an API change first.
- Confirmation dialog, token refresh, expiry handling.
- A `SessionProvider` in the backoffice.
- Updating `.stpr/template/` or its README.
- Must not change: the API, the snapshot, the HTTP error mapper, `LoginUseCase`, `SignupUseCase`,
  `GetSessionUseCase`, `RequireSession`, the app's root layout and session state hook.

## Notes

- **Temporary button.** Keep it minimal and inline in each `HomeView`. Do not extract a shared component for it.
- **Removal.** The use case removes both keys. If removing either fails it returns `UNEXPECTED`; it does not try to
  restore what was already removed (a half-cleared store reads as no session in `GetSessionUseCase` anyway).
- **Next 16.** Read `backoffice/node_modules/next/dist/docs/` for `useRouter` from `next/navigation` before writing.
- **Verification tiers:**
  - core unit: `logout-use-case.test.ts`: both keys are removed and `ok` is returned; `UNEXPECTED` when the store throws.
  - backoffice component: `home-view.test.tsx`: the button is shown with a session; clicking it calls the use case
    and goes to `/login`; on `UNEXPECTED` it stays and shows the message.
  - app component: `home-view.test.tsx`: clicking the button calls the use case and re-reads the session
    (`getSessionUseCase` called again) with no navigation call; on `UNEXPECTED` it shows the message.
  - Both `render-with-providers.tsx` gain a `logoutUseCase` placeholder.
  - Backoffice E2E: not required for this temporary button.
  - App E2E: known gap, accepted.
- Done means `pnpm typecheck && pnpm lint && pnpm test` passes from `front/`.
- No conflict with a stop case. No deviation from the mapped references is allowed.
