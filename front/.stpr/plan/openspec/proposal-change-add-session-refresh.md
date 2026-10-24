# Change Proposal: Add Session Refresh

Source brief for `/opsx:propose`. Artifacts go to `openspec/changes/add-session-refresh/`.

## Change

Renew the access token with the stored refresh token when the API answers `401`, retry the failed call once, and
end the session when the refresh token is no longer valid. Both fronts.

## Objective

The access token lives 15 minutes (`expiresIn: 900`). The fronts store the refresh token at login and signup but
never use it, so after 15 minutes every protected call fails with `UNAUTHORIZED`. The backoffice then shows "Tu
sesión no es válida" with skeletons and no way forward; during onboarding the user cannot even reach logout. The
invariants already require reactive refresh through a `RefreshSession` use case; this change implements it now that
the fronts call protected endpoints (`/users/me`).

## Capabilities

- **Refresh the session (core).** `RefreshSessionUseCase` reads the stored refresh token, calls `POST /auth/refresh`,
  and stores the new access and refresh tokens. The API rotates refresh tokens: the presented one is revoked, so it
  can be used only once.
- **One refresh at a time.** While a refresh is in flight, further calls to `RefreshSessionUseCase` wait for it and
  get the same result instead of sending the (already revoked) token again. Without this, two calls that fail with
  `401` together would end the session.
- **Ending a session that cannot be renewed.** When the API rejects the refresh token (`401` or `400`), or there is
  no stored refresh token, the use case removes both stored tokens and fails with `UNAUTHORIZED`. A `NETWORK` or
  `UNEXPECTED` failure keeps the tokens (the user may be offline) and is returned as is.
- **Reactive retry (presentation, both fronts).** A session hook exposes `runWithSession(call)`. It runs the call;
  when the result fails with `UNAUTHORIZED`, it calls `RefreshSessionUseCase` once and, on success, runs the call
  again once and returns that result. When the refresh fails with `UNAUTHORIZED`, the session ends: the session state
  becomes signed out and the front sends the user to login (see Screens). Any other failure is returned unchanged.
- **Every protected call goes through it.** In the backoffice: `getMyProfileUseCase` in `CurrentUserProvider` and
  `updateMyProfileUseCase` in `useUpdateProfile`. In the app there is no protected call yet; the hook is ready for
  the first one. Login, signup, logout and the refresh itself never go through it.
- **Backoffice session state.** The backoffice has no session state today: `RequireSession` reads the session once.
  This change adds a backoffice `SessionProvider` and `useSession` (status `restoring | signedIn | signedOut`) and
  makes `RequireSession` read from it, so ending the session from any screen sends the user to login. Logout uses
  the same state.

## Fronts And Roles

- backoffice -> lands here: yes -> roles: `TRAINER`, `ADMIN` -> the same for both. A user whose token expired keeps
  working without noticing; a user whose refresh token is no longer valid lands on login.
- app -> lands here: yes -> roles: `USER` -> the existing `SessionProvider` gains `runWithSession`; ending the
  session flips the existing `Stack.Protected` guard, which moves the athlete to `(auth)`. No visible change until
  a protected call exists.

No role sees anything new. After the session ends, a user must NOT see any signed-in screen or the previous user's
profile.

## Classification

Core:

- `refresh(request: RefreshRequest)` -> repository contract method -> `core/src/application/ports/auth-repository-interface.ts`
- `refresh` -> repository implementation -> `core/src/infrastructure/repositories/auth-repository.ts`
- `RefreshRequest` -> domain alias over the generated schema -> `core/src/domain/auth/`
- `RefreshSessionUseCase` (`UseCaseInterface<void, Session>`; single in-flight refresh) -> use case ->
  `core/src/application/use-cases/auth/refresh-session-use-case.ts`
- `refreshSessionUseCase` key -> `Dependencies` -> `core/src/application/dependencies.ts`

Backoffice:

- `refreshSessionUseCase` registration -> composition root -> `backoffice/src/infrastructure/container.ts`
- `SessionContext`, `SessionProvider`, `useSession` (`status`, `runWithSession`, `end`, `reload`) -> context, provider,
  state hook -> `backoffice/src/presentation/auth/{context,components,hooks}/`
- `SessionProvider` mounted in `AppProviders`, above `CurrentUserProvider`
- `RequireSession` (reads `useSession` instead of calling `getSessionUseCase`; redirects to `/login` on
  `signedOut`) -> component -> `backoffice/src/presentation/auth/components/require-session.tsx`
- `CurrentUserProvider`, `useUpdateProfile` -> wrap their calls in `runWithSession`; `CurrentUserProvider` clears
  the profile when the session ends
- `useLogout`, login and signup views -> tell the session state after they write or clear tokens (`end` / `reload`)

App:

- `refreshSessionUseCase` registration -> composition root -> `app/src/infrastructure/container.ts`
- `SessionContext`, `SessionProvider`, `useSession` -> add `runWithSession` -> `app/src/presentation/auth/`

Both test supports (`render-with-providers.tsx`) register a fake `refreshSessionUseCase` and mount the session
provider where they do not already.

## Canonical Template Mapping

- `RefreshSessionUseCase` and its test -> `core/src/application/use-cases/auth/login-use-case.ts` and its test ->
  same shape: repository call, then tokens stored through `KeyValueStoreInterface`, `UNEXPECTED` on a storage
  failure. Token removal follows `logout-use-case.ts`.
- `AuthRepository.refresh` and its tests -> `AuthRepository.login` -> same request and error mapping.
- App `runWithSession` -> `app/src/presentation/auth/context/session-provider.tsx`, extended in place.
- Backoffice `SessionProvider` / `useSession` -> no backoffice session state exists: **this change establishes it**,
  following the backoffice's `container-provider.tsx` and `current-user-provider.tsx` for structure. The app's
  provider is not a UI reference, but the session behaviour (statuses, `runWithSession`) is the same contract.
- `RequireSession` -> its current version, reworked in place.
- Tests -> the existing auth use case tests in `core/test/application/use-cases/auth/`, the backoffice
  `home-view.test.tsx` and `profile-view.test.tsx`, the app session provider test if one exists (otherwise the
  closest app component test), and `backoffice/e2e/login.spec.ts`.

`.stpr/template/` and its README are not updated.

## Screens And Navigation

- backoffice -> any signed-in route -> a protected call fails with `401` -> refresh succeeds -> the call is retried
  and the screen behaves as if nothing happened.
- backoffice -> any signed-in route -> refresh rejected -> session ends -> `RequireSession` sends the user to `/login`
  (`router.replace`, as today). The shared profile is cleared.
- app -> refresh rejected -> session state becomes `signedOut` -> the `Stack.Protected` guard moves the athlete to
  `(auth)`. No code navigates (invariants, section 4.3).
- No new route.

## API Contract

- `POST /auth/refresh` -> in `core/openapi/v1.json`: yes -> `RefreshRequest` (`refreshToken`) -> `AuthResponse`
  (`accessToken`, `refreshToken`, `expiresIn`). Answers `400` for a malformed body and `401` for a missing, unknown,
  expired or revoked token.
- The API rotates the refresh token on every successful refresh and rejects a replayed one.

No API change, no snapshot refresh.

## Held State

- Stored tokens -> `KeyValueStoreInterface` (`auth.accessToken`, `auth.refreshToken`) -> replaced by
  `RefreshSessionUseCase` on success; removed by it when the refresh token is rejected; removed by logout -> as today.
- In-flight refresh -> a promise inside `RefreshSessionUseCase` (one container instance per front) -> set when a
  refresh starts, cleared when it settles -> never persisted.
- Backoffice session state -> `SessionProvider` (`restoring | signedIn | signedOut`) -> read on mount from
  `getSessionUseCase`; set to `signedOut` by `end` (failed refresh, logout); re-read by `reload` (login, signup) ->
  resets on page reload.
- App session state -> existing `SessionProvider` -> also set to `signedOut` when a refresh fails.
- Backoffice shared profile -> `CurrentUserProvider` -> cleared when the session becomes `signedOut`.

## Forms And Structural Validation

None.

## Errors And Copy

- Access token expired, refresh succeeds -> no error shown.
- Refresh token rejected -> `UNAUTHORIZED` -> the user lands on login. No message on the login screen in this
  version.
- No connection while refreshing -> `NETWORK` -> the screen shows its usual `errors:NETWORK` message; tokens kept.

No new error code and no new copy.

## Platform Capabilities And Dependencies

None. Key-value storage already exists in both fronts.

## Out Of Scope

- Proactive refresh before expiry (timers based on `expiresIn`). The invariants require reactive refresh.
- An HTTP interceptor that refreshes inside `FetchHttpClient` (forbidden by the invariants).
- A "your session expired" message on login.
- Refresh across browser tabs (a second tab with the old refresh token will be rejected after the first refreshes;
  it ends its session and the user logs in again).
- Must not change: the API, the snapshot, `FetchHttpClient`, login, signup and logout behaviour as seen by the user.

## Notes

- **Depends on** `add-backoffice-onboarding-and-profile` (uses `CurrentUserProvider` and `useUpdateProfile`) and on
  the access-token wiring in both `container.ts` (`getAccessToken` reads the session through `getSessionUseCase`).
  Archive that change first.
- **Why single-flight lives in the use case.** Both fronts need it, and the rotation rule makes it a correctness
  issue, not an optimisation. Keeping it in core means presentation cannot forget it. It holds no UI state.
- **`refresh` naming in the app.** The app's `SessionState.refresh` re-reads the stored session; it does not call
  the API. Rename it to `reload` in this change to avoid confusion with the token refresh, and update its callers.
- **Retry once only.** `runWithSession` never retries a retried call, and never refreshes for a failure other than
  `UNAUTHORIZED`, so a `401` caused by something else cannot loop.
- **Next 16.** Read the `useRouter` guide in `backoffice/node_modules/next/dist/docs/` before touching
  `RequireSession`.
- **Expo.** No Expo API is touched; `SessionProvider` stays plain React.
- **Verification tiers:**
  - core unit: `refresh-session-use-case.test.ts` (stores both new tokens; sends the stored refresh token; no stored
    refresh token → `UNAUTHORIZED` and no request; `401` and `400` → tokens removed and `UNAUTHORIZED`; `NETWORK` →
    tokens kept and `NETWORK`; storage failure → `UNEXPECTED`; two concurrent calls → one request, same result).
    `auth-repository.test.ts` gains `refresh` (request shape, success, `400`, `401`, `500`, network).
  - backoffice component: a session test (restoring, signed in, signed out; `end` sends `RequireSession` to
    `/login`); `home-view.test.tsx` (profile read fails with `UNAUTHORIZED`, refresh succeeds, the profile shows and
    the read ran twice; refresh fails, the user is sent to `/login` and no profile shows); `profile-view.test.tsx`
    (save retried after refresh). Existing logout, login and signup tests still pass.
  - app component: session provider (`runWithSession` retries once after refresh; failed refresh sets `signedOut`;
    non-`UNAUTHORIZED` failures are not refreshed).
  - backoffice E2E: in a new `session-refresh.spec.ts`, `GET /users/me` answers `401` once, `POST /auth/refresh` is
    intercepted with new tokens, and home shows the profile; a second flow answers `401` to the refresh and lands on
    `/login`.
- Done means `pnpm typecheck && pnpm lint && pnpm test && pnpm test:e2e` passes from `front/`.
- No conflict with a stop case. The backoffice session state is a new pattern this change establishes; no other
  deviation is allowed.
