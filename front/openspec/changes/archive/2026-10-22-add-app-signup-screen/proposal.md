# Proposal

Source brief: `.stpr/plan/openspec/proposal-change-add-app-signup-screen.md`.

## Why

`SignupUseCase` and `GetSessionUseCase` are wired in the app, but no screen calls them. An athlete cannot create an
account, nothing tells a signed-in athlete from an anonymous one, and the router is flat (`_layout.tsx` and
`index.tsx`), with no place for a signed-out flow. This change adds the signup screen and the session state the root
layout's auth gate reads, and moves the routes into the groups that `STPR_INVARIANTS.md` section 4.3 ("Routing")
now requires. It can be checked from a running app: signing up lands on home, and opening the app with no stored
tokens shows signup.

## What Changes

- App: reorganize `src/app` into `(auth)` and `(app)` route groups. `index.tsx` moves to `(app)/index.tsx`; add
  `(auth)/_layout.tsx`, `(auth)/signup.tsx`, `(app)/_layout.tsx` and `+not-found.tsx`.
- App: the root layout becomes the auth gate. `RootLayout` renders `AppProviders` around a local `RootNavigator`,
  which keeps the native splash up while the session is restored and renders a `Stack` with
  `Stack.Protected guard={signedIn}` around `(app)` and `guard={!signedIn}` around `(auth)`.
- App: add the session state hook (`SessionContext`, `SessionProvider`, `useSession`) in `presentation/auth`,
  composed in `app-providers.tsx`. It reads `GetSessionUseCase` on mount and on `refresh()`. It is brought forward
  from the login slice so the gate can guard now.
- App: add `SignupView` and `SignupForm`, with structural validation and translated errors, in a new `auth`
  namespace. A successful signup calls `refresh()`; no code navigates.
- App: `HomeView` gains a signed-in message. Add `NotFoundView` with a link to `/`.
- No core change, no `container.ts` change, no adapter. `InMemoryKeyValueStore` stays.

## Capabilities

### New Capabilities

- `app-signup`: the app signup screen: its form, structural validation, error messages and what follows a success.
- `app-access-gate`: which app screens a session unlocks, what an athlete without one sees, and what shows while the
  session is restored.

### Modified Capabilities

None. `account-signup` behaviour is unchanged. The app's in-memory session lifetime is already specified by
`auth-session` in the in-flight `add-backoffice-signup-screen` change.

## Fronts And Roles

- **app**: `USER` signs up from `/signup` and then sees `/`. The container already registers `SignupUseCase` with role
  `USER` (`1`), so no role can be picked. The home screen holds no role-specific content. An athlete without a session
  must not see any home content, not even while the session is restored; a signed-in athlete must not reach
  `/signup`.
- **backoffice**: no change. Its signup slice is already in place.

## Canonical References

- Route groups, layouts and the auth gate: no code reference exists. **This change establishes them**, as section
  4.3 ("Routing") of `.stpr/template/STPR_INVARIANTS.md` fixes them, using `app/src/app/_layout.tsx` and
  `app/src/app/index.tsx` for file structure.
- Session state hook: follows the `ContainerContext` trio (`presentation/common/context/container-context.ts`,
  `container-provider.tsx`, `hooks/use-container.ts`). As a state hook it is `pending` (login); **this change
  establishes it in the app**.
- `container.ts`: skeleton-backed, unchanged.
- Signup route, view and form: `pending` (login). **This change establishes the app pattern**, using
  `app/src/app/index.tsx` and `presentation/common/views/home-view.tsx` for structure. The backoffice is not a
  reference.
- View and provider tests: `pending` (login). They follow `app/test/presentation/common/views/home-view.test.tsx` and
  `app/test/support/render-with-providers.tsx`.
- E2E: no app tier exists. App E2E is a known gap, accepted for now.

`.stpr/template/` and its README are not updated here.

## Constraints And Conflicts

- **Session hook scope.** Section 4.3 lets the root `Stack` stay unguarded until the session state hook exists. This
  change adds that hook now instead of waiting for login, so the gate applies from the start. Sign-in and sign-out
  are left to login.
- **Gate placement.** Section 4.3 makes `src/app/_layout.tsx` the only place that decides access and forbids
  `Redirect` / `router.replace` for access control. Unlike the backoffice, there is no `RequireSession` component.
- **Pinned Expo Router.** `expo-router` `~57.0.29` has `Stack.Protected` with `guard` only. `redirectTo` (SDK 58+) is
  not used.
- **Not authorization.** The gate is client-side navigation. The API remains the authority on every protected call.
- **No business rule.** Password strength stays with the API. The form checks only presence and email format.
- No other conflict with the template, the invariants or a stop case.

## API Contract

- `POST /auth/signup` is in `core/openapi/v1.json`: request `SignupRequest`, response `AuthResponse`. No snapshot
  refresh, no API change. `GetSessionUseCase` calls no endpoint.

## Acceptance Criteria

- Opening the app with no stored tokens shows `/signup`; the home heading is never rendered.
- Signing up with a valid email and a password the API accepts stores both tokens and shows `/` with the home heading
  and the signed-in message, without any navigation call in the view.
- With a session, `/signup` cannot be reached.
- An empty or malformed email, or an empty password, shows a field error and sends no request. Values are trimmed.
- `VALIDATION`, `UNAUTHORIZED`, `CONFLICT` and `NETWORK` from signup each show their translated message, keep the
  entered email, and leave the session signed out.
- An unknown path shows the not-found screen with a link to `/`.
- Every new string exists in `es` and `en`.
- App component tests pass with the API mocked. `pnpm typecheck && pnpm lint && pnpm test` passes from `front/`.

## Impact

- `app`: `src/app` (restructured), `src/presentation/auth` (new), `src/presentation/common` (`app-providers.tsx`,
  `home-view.tsx`, new `not-found-view.tsx`), `src/i18n` (new `auth` namespace, `common` keys, `resources.ts`),
  `test/` (new tests, `render-with-providers.tsx` mounts `SessionProvider`).
- `core`, `backoffice`: none.
- No new dependency. No API change.
