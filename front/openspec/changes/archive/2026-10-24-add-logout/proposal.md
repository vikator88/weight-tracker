# Proposal

Source brief: `.stpr/plan/openspec/proposal-change-add-logout.md`.

## Why

Login and signup store a session, but nothing removes it. While developing, the only way back to login is to clear
storage by hand (`localStorage` in the backoffice, wiping the app data in the app). This change adds a `LogoutUseCase`
to `core` and a quick logout button on the home view of both fronts. The button is a development convenience; its
placement is provisional and moves to a proper place in a later change.

## What Changes

- Core: add `LogoutUseCase`, which removes both stored tokens and makes no API call. `Dependencies` gains
  `logoutUseCase` (`UseCaseInterface<void, void>`). Barrels re-export it.
- Backoffice: `HomeView` gains a logout button. On success it calls `router.replace("/login")`; on failure it stays on
  home and shows the error. Register `logoutUseCase`.
- App: `HomeView` gains a logout button. On success it calls `session.refresh()`, and the gate shows login, with no
  navigation call; on failure it stays on home and shows the error. Register `logoutUseCase`.
- New `home.logout` key in each front's `common` namespace, in `es` and `en`.
- No API change, no snapshot refresh, no new error code.

## Capabilities

### New Capabilities

- `account-logout`: the core logout operation. It removes the stored tokens without calling the API and returns a
  storage failure as `UNEXPECTED`.
- `backoffice-logout`: the backoffice logout button on home: what follows a success and a failure.
- `app-logout`: the app logout button on home: what follows a success and a failure.

### Modified Capabilities

None. `auth-session`, `backoffice-access-guard` and `app-access-gate` already define that a missing token means no
session and what each front shows then; logout only produces that state.

## Fronts And Roles

- **backoffice**: `TRAINER` and `ADMIN` see the same button on home and log out the same way. Home is already hidden
  from a visitor without a session, so an anonymous visitor never sees the button.
- **app**: `USER` logs out from home. An athlete without a session never sees home, so never sees the button.

No role difference in either front.

## Canonical References

- `LogoutUseCase` and its test: modelled on `GetSessionUseCase` (`get-session-use-case.ts` and its test), the nearest
  shape: only `KeyValueStoreInterface`, no input, no API call, `UNEXPECTED` on a storage failure. It reuses
  `ACCESS_TOKEN_KEY` / `REFRESH_TOKEN_KEY` from `signup-use-case.ts`. `.stpr/template/README.md` has no row for it;
  it follows the `LoginUseCase` use-case row.
- Both `container.ts`: extended in place.
- Backoffice button: the submit handling of the backoffice `login-view.tsx` (call the use case, `router.replace` on
  success), with an MUI `Button`. Test: the existing backoffice `home-view.test.tsx`.
- App button: the submit handling of the app `login-view.tsx` (call the use case, `session.refresh()` on success, no
  navigation call), with a Paper `Button`. Test: the existing app `home-view.test.tsx`.
- Neither front is a UI reference for the other. `.stpr/template/` and its README are not updated.

## Constraints And Conflicts

- **Logout is local.** `core/openapi/v1.json` has no logout or revocation endpoint (only `/auth/signup`,
  `/auth/login`, `/auth/refresh`). The refresh token stays valid on the API until it expires. Revoking it needs an API
  change first (out of scope).
- **App gate (section 4.3).** The app view does not navigate to deny access. It calls `session.refresh()`, and the gate
  moves the athlete to `(auth)`.
- **Temporary placement.** The button lives inline in each `HomeView`, with no shared component. No conflict with the
  template, the invariants or a stop case.

## API Contract

None used. No snapshot refresh, no API change.

## Acceptance Criteria

- Clicking logout on home removes both stored tokens.
- Backoffice: after a successful logout the visitor is on `/login`, and opening `/` again redirects to `/login`.
- App: after a successful logout the login screen is shown and no home content is rendered.
- If removing the tokens fails, home stays and shows "Ha ocurrido un error inesperado.".
- The button is disabled while logout is in flight.
- `home.logout` exists in `es` ("Cerrar sesión") and `en` ("Log out").
- `pnpm typecheck && pnpm lint && pnpm test` passes from `front/`, with the API mocked in every tier.

## Impact

- `core`: `application/use-cases/auth` (new `logout-use-case.ts`, barrel), `application/dependencies.ts`, tests.
- `backoffice`: `src/presentation/common/views/home-view.tsx`, `src/i18n/{es,en}/common.json`,
  `src/infrastructure/container.ts`, `test/support/render-with-providers.tsx`, `test/presentation/common/views`.
- `app`: `src/presentation/common/views/home-view.tsx`, `src/i18n/{es,en}/common.json`,
  `src/infrastructure/container.ts`, `test/support/render-with-providers.tsx`, `test/presentation/common/views`.
- No new dependency.
