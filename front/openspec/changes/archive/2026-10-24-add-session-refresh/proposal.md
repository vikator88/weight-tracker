# Proposal

Source brief: `.stpr/plan/openspec/proposal-change-add-session-refresh.md`.

## Why

The access token lives 15 minutes (`expiresIn: 900`). Both fronts store the refresh token at login and signup but
never use it, so after 15 minutes every protected call fails with `UNAUTHORIZED`. The backoffice then shows
"Tu sesión no es válida" with skeletons and no way forward; during onboarding the user cannot even reach logout. The
invariants already require reactive refresh through a `RefreshSession` use case (sections 3 Application and
Presentation); now that the fronts call a protected endpoint (`/users/me`), it is needed.

## What Changes

- **Core: refresh the session.** A `RefreshSessionUseCase` reads the stored refresh token, calls
  `POST /auth/refresh`, and stores the new access and refresh tokens. The API rotates refresh tokens, so a token is
  usable once.
- **Core: one refresh at a time.** While a refresh is in flight, further calls wait for it and receive the same
  result instead of resending the already revoked token.
- **Core: ending a session that cannot be renewed.** A refresh rejected by the API (`401` or `400`), or no stored
  refresh token, removes both stored tokens and fails with `UNAUTHORIZED`. `NETWORK` and `UNEXPECTED` keep the
  tokens and are returned as they are.
- **Both fronts: reactive retry.** The session hook exposes `runWithSession(call)`: it runs the call, and on
  `UNAUTHORIZED` refreshes once and retries the call once. A refresh that fails with `UNAUTHORIZED` ends the session.
  Any other failure is returned unchanged.
- **Backoffice: session state (new pattern).** A `SessionProvider` / `useSession` (`restoring | signedIn |
  signedOut`, `runWithSession`, `end`, `reload`) mounted in `AppProviders` above `CurrentUserProvider`.
  `RequireSession` reads it instead of reading storage itself, so ending the session from any screen sends the user
  to `/login`. `CurrentUserProvider` clears the profile when the session ends. Login, signup and logout tell the
  session state after they write or clear tokens.
- **Backoffice: protected calls go through `runWithSession`**: `getMyProfileUseCase` in `CurrentUserProvider` and
  `updateMyProfileUseCase` in `useUpdateProfile`. Login, signup, logout and the refresh itself never do.
- **App:** the existing `SessionProvider` gains `runWithSession`; a failed refresh sets `signedOut` and the existing
  `Stack.Protected` guard moves the athlete to `(auth)`. The app has no protected call yet.
- **App (internal rename):** `SessionState.refresh` (re-reads storage, no API call) becomes `reload`, to avoid
  confusion with the token refresh. Callers are updated. Not user-visible.

## Capabilities

### New Capabilities

- `session-refresh`: renewing an expired access token with the stored refresh token, a single refresh in flight,
  retrying the failed call once, and ending the session when the refresh token is rejected. Lands in core and both
  fronts; the behaviour is the same for every role (`TRAINER`, `ADMIN`, `USER`). After the session ends, no role sees
  a signed-in screen or the previous user's profile. Canonical reference: `login-use-case.ts` / `logout-use-case.ts`
  and `AuthRepository.login` for core; the app's `session-provider.tsx`, extended in place; for the backoffice
  session state, **pending: this change establishes it**, following `container-provider.tsx` and
  `current-user-provider.tsx` for structure.

### Modified Capabilities

- `backoffice-access-guard`: the guard also follows the session ending while a guarded screen is shown (sends to
  `/login`, shows no signed-in content). Backoffice only; same for `TRAINER` and `ADMIN`. Reference:
  `require-session.tsx`, reworked in place.
- `app-access-gate`: the gate also follows the session from signed in to signed out without navigation. App only,
  role `USER`. Reference: the existing root layout guard, unchanged.

## Fronts And Roles

- **backoffice** (`TRAINER`, `ADMIN`, identical): an expired access token is renewed without the user noticing; a
  rejected refresh token lands the user on `/login` with the profile cleared.
- **app** (`USER`): no visible change until a protected call exists; a rejected refresh flips the guard to `(auth)`.
- No role sees anything new. No role may see a signed-in screen or the previous user's profile after the session ends.

## API Contract

- `POST /auth/refresh` is in `core/openapi/v1.json`: `RefreshRequest` (`refreshToken`) → `AuthResponse`
  (`accessToken`, `refreshToken`, `expiresIn`); `400` for a malformed body, `401` for a missing, unknown, expired or
  revoked token. The error mapper already maps `400` to `VALIDATION` and `401` to `UNAUTHORIZED`.
- No API change and no snapshot refresh.

## Errors And Copy

No new error code and no new copy. `NETWORK` during a refresh shows each screen's existing `errors:NETWORK` message.
No message on login after the session ends in this version.

## Constraints And Conflicts

- No conflict with the invariants, the template or a stop case. The refresh is a use case, not an HTTP interceptor;
  `FetchHttpClient` is unchanged; the app does not navigate on a failed refresh (section 4.3); the backoffice keeps
  `router.replace` in `RequireSession` as today.
- The backoffice session state is a new pattern this change establishes; no other deviation.
- **Depends on** `add-backoffice-onboarding-and-profile` (still under `openspec/changes/`; its code is merged).
  Archive it before this change: `CurrentUserProvider` and `useUpdateProfile` come from it.
- Out of scope: proactive refresh from `expiresIn`, a refresh inside `FetchHttpClient`, a "session expired" message,
  refresh across browser tabs. Login, signup and logout behave as today for the user.

## Acceptance Criteria

- `pnpm typecheck && pnpm lint && pnpm test && pnpm test:e2e` passes from `front/`.
- Core, backoffice component, app component and backoffice E2E tests listed in the brief's verification tiers exist
  and pass, with the API mocked.
- `.stpr/template/` is unchanged and still byte-identical to its counterparts.

## Impact

- `core`: `RefreshRequest` alias, `AuthRepositoryInterface.refresh`, `AuthRepository.refresh`,
  `RefreshSessionUseCase`, `Dependencies.refreshSessionUseCase`, tests.
- `backoffice`: `container.ts`, new `presentation/auth/{context,components,hooks}` session files, `AppProviders`,
  `RequireSession`, `CurrentUserProvider`, `useUpdateProfile`, `useLogout`, login and signup views, test support,
  component tests, new E2E `session-refresh.spec.ts`.
- `app`: `container.ts`, `session-context.ts`, `session-provider.tsx`, callers of `refresh` (login, signup, home
  views), test support, session provider test.
- No dependency, no API, no snapshot change.
