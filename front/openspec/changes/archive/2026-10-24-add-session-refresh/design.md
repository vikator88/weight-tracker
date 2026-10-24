# Design

## Context

See proposal.md (Why) for motivation and `specs/` for the required behaviour. Current state that shapes the approach:

- `FetchHttpClient` adds the access token through `getAccessToken`, which each `container.ts` resolves from
  `getSessionUseCase`. It must stay unchanged (invariants: no interceptor refreshes tokens).
- `Container` factories are lazy singletons, so one `RefreshSessionUseCase` instance exists per front. That is what
  makes an in-flight promise held in the instance a front-wide single flight.
- `mapHttpError` already maps `400 → VALIDATION`, `401 → UNAUTHORIZED`, anything else `→ UNEXPECTED`, no response
  `→ NETWORK`.
- Backoffice: `RequireSession` reads storage once on mount; there is no session state. `CurrentUserProvider` (from
  `add-backoffice-onboarding-and-profile`) holds the profile and already has `clear` and a generation guard that
  drops answers arriving after a `clear`. `UserMenu` already calls `clear()` and `router.replace("/login")` after
  logout. Login and signup views call `router.replace("/")` right after the use case succeeds.
- App: `SessionProvider` holds `status | errorCode` and `refresh()` (re-reads storage). Login, signup and home views
  call `session.refresh()`. The root layout guards with `Stack.Protected`.

## Goals / Non-Goals

**Goals:**

- One core use case owns refreshing, rotation-safe single flight and token clean-up.
- One presentation entry point per front (`runWithSession`) owns the "refresh once, retry once, end on failure"
  policy, so a screen cannot get it wrong.
- The backoffice gets a session state that every screen and the guard share.

**Non-Goals:**

- No proactive refresh, no cross-tab coordination, no new copy, no change to `FetchHttpClient`.
- The app gets no protected call in this change.
- `UserMenu` keeps its logout flow as it is (its `clear()` and `replace("/login")` become redundant but stay, to keep
  the change scoped).

## Decisions

### Core

- **Domain alias.** `core/src/domain/auth/auth.ts` gains `export type RefreshRequest = components["schemas"]["RefreshRequest"];`.
- **Port.** `AuthRepositoryInterface.refresh(request: RefreshRequest): Promise<Result<AuthResponse, DomainError>>`.
- **Repository.** `AuthRepository.refresh` mirrors `login`: `POST /auth/refresh` with `body: request`, `ok(response)`,
  `fail(mapHttpError(error))`. No new error mapping.
- **Use case.** `RefreshSessionUseCase implements UseCaseInterface<void, Session>`, constructor
  `(authRepository, keyValueStore)`, keys `ACCESS_TOKEN_KEY` / `REFRESH_TOKEN_KEY` imported from
  `signup-use-case.ts` like its siblings.
  - `execute()`: if `this.inFlight` is set, return it; otherwise set `this.inFlight = this.refresh()` and clear it in
    `finally` once it settles. Every waiter gets the same `Result` object.
  - `refresh()`: read the refresh token (`UNEXPECTED` on a read failure). None → clear tokens, fail
    `UNAUTHORIZED`, no request. Call `authRepository.refresh({ refreshToken })`. On `UNAUTHORIZED` or `VALIDATION`
    → clear tokens, fail `UNAUTHORIZED`. Any other failure → return it, tokens untouched. On success store access
    then refresh token (`UNEXPECTED` on a write failure) and return `ok({ accessToken, refreshToken })`.
  - Clearing removes the refresh token first, then the access token, as `LogoutUseCase` does (an access token alone
    already reads as no session). A removal failure returns `UNEXPECTED`.
  - Alternative rejected: single flight in presentation. Both fronts would duplicate it, and forgetting it is a
    correctness bug under rotation (brief, Notes).
  - Alternative rejected: reusing `LogoutUseCase` from inside the use case. Use cases do not compose other use cases
    in this codebase; the two removals are inlined, as in logout.
- **`Dependencies`** gains `refreshSessionUseCase: UseCaseInterface<void, Session>`; export the class from the auth
  use cases `index.ts`.

### Backoffice

- **Registration.** `container.ts`: `refreshSessionUseCase: (c) => new RefreshSessionUseCase(c.resolve("authRepository"), c.resolve("keyValueStore"))`.
- **Session state (new pattern).** Files, mirroring `current-user-context.ts` / `current-user-provider.tsx` /
  `use-current-user.ts`:
  - `presentation/auth/context/session-context.ts`: `SessionStatus = "restoring" | "signedIn" | "signedOut"`,
    `SessionContextValue { status; runWithSession; end; onEnd; reload }`, context default `null`.
  - `presentation/auth/components/session-provider.tsx` (`"use client"`): reads `getSessionUseCase` on mount
    (`ok(non-null)` → `signedIn`, anything else → `signedOut`). `reload(): Promise<void>` re-reads and sets the
    status. `end(): void` sets `signedOut` (tokens were already removed by the refresh or logout use case) and
    calls every `onEnd` listener synchronously. `onEnd(listener): () => void` subscribes and returns the
    unsubscribe.
  - `runWithSession<T>(call: () => Promise<Result<T, DomainError>>): Promise<Result<T, DomainError>>`: run `call`;
    not `UNAUTHORIZED` → return it. Else `refreshSessionUseCase.execute()`; refresh ok → return `await call()`
    (never retried again); refresh fails `UNAUTHORIZED` → `end()` and return the refresh failure; other failure →
    return it (session stays signed in).
  - `presentation/auth/hooks/use-session.ts`: throws outside the provider, like `useCurrentUser`.
  - Held state: `status`. Changed by mount read, `reload` (login, signup), `end` (failed refresh, logout). Resets on
    page reload.
- **Mounting.** `AppProviders`: `ContainerProvider > SessionProvider > CurrentUserProvider`.
- **`RequireSession`.** Reads `useSession().status`: `restoring` → progress indicator; `signedOut` →
  `router.replace("/login")` in an effect and the progress indicator; `signedIn` → children. No storage read of its
  own. Read the Next 16 `useRouter` guide in `backoffice/node_modules/next/dist/docs/` before editing.
- **`CurrentUserProvider`.** Wraps `getMyProfileUseCase` in `runWithSession`, and subscribes `clear` with
  `useEffect(() => onEnd(clear), [onEnd, clear])`. `clear` therefore runs inside `end()`, before the read that
  triggered it settles, so the existing generation guard drops that read; the next sign-in starts again from `idle`,
  so a different user never sees the previous profile.
  - Alternative rejected: an effect calling `clear()` when `status === "signedOut"` (found during apply). It fails
    `react-hooks/set-state-in-effect`. Also rejected: remounting `CurrentUserProvider` with a key, which remounts
    the whole signed-in subtree, and disabling the rule.
- **`useUpdateProfile`.** Wraps `updateMyProfileUseCase` in `runWithSession`.
- **Login and signup views.** After the use case succeeds: `await reload()`, then `router.replace("/")`. Without
  the `await`, `RequireSession` on `/` would still read the stale `signedOut` and bounce back to `/login`.
- **`useLogout`.** On success calls `end()`.
- **Errors and copy.** No new code. `UNAUTHORIZED` after an ended session is never shown (the user is on login);
  `NETWORK` is shown by the existing `LoadError` / profile form messages. No `es` / `en` change.

### App

- **Registration.** Same `refreshSessionUseCase` entry in `app/src/infrastructure/container.ts`.
- **`SessionState`.** `refresh` is renamed `reload` (same behaviour); `runWithSession` is added with the same
  contract as the backoffice. Ending sets the snapshot to `{ status: "signedOut", errorCode: null }`. No `end` is
  exposed: nothing in the app clears tokens outside a use case that is followed by `reload` today.
- **Callers.** Login, signup and home views call `session.reload()`.
- **Navigation.** None. `signedOut` flips `Stack.Protected` (invariants, section 4.3).

### Test support (documented exception)

- Both `test/support/render-with-providers.tsx` add `refreshSessionUseCase` (unused default `undefined as never`)
  and `refresh` on the fake `authRepository`.
- The backoffice helper mounts `SessionProvider` between `ContainerProvider` and `CurrentUserProvider`. Because the
  provider now reads the session in every rendered test, the backoffice default `getSessionUseCase` changes from
  `undefined as never` to `ok(null)`, as the app's helper already does. This is the only change to a test helper's
  defaults.
- Core fakes implementing `AuthRepositoryInterface` in the signup and login use case tests gain a `refresh` method so
  they keep type-checking.
- E2E: `backoffice/e2e/session-refresh.spec.ts` logs in through intercepted `/auth/login` (as `login.spec.ts`),
  registers `mockUsersMe`, then a later `page.route` on `/users/me` that answers `401` once and `route.fallback()`s
  afterwards, and intercepts `/auth/refresh` (with its CORS preflight). `e2e/support/users-me.ts` is unchanged.

## Risks / Trade-offs

- [The refresh request carries the expired access token in `Authorization`, because `FetchHttpClient` attaches it to
  every call] → The API's refresh endpoint is anonymous and must ignore an invalid bearer; E2E mocks cannot catch
  this. Verify once manually against the running API during apply; if it rejects, it is an API fix, not a front
  workaround.
- [A storage failure while clearing tokens after a rejected refresh returns `UNEXPECTED`, so the front stays signed
  in with a token that cannot be renewed] → Rare; the next protected call tries again and the user can log out.
- [Two browser tabs share storage; the second tab refreshes with an already rotated token] → Out of scope; that tab
  ends its session (proposal, Out of scope).
- [`UserMenu` and `RequireSession` both call `router.replace("/login")` on logout] → Idempotent; accepted to keep
  the change scoped.

## Migration Plan

Archive `add-backoffice-onboarding-and-profile` before archiving this change. No data migration; existing stored
tokens keep working. Rollback is a revert.
