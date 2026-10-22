# Proposal

Source brief: `.stpr/plan/openspec/proposal-change-add-backoffice-signup-screen.md`.

## Why

`SignupUseCase` is wired in the backoffice, but no screen calls it and the tokens it returns are discarded. A trainer
cannot create an account, and nothing tells a signed-in visitor from an anonymous one. This change adds the signup
screen, keeps the tokens, and guards the home screen, so the flow can be checked end to end: signing up lands on
home, and opening home directly without stored tokens goes to signup.

## What Changes

- **BREAKING (core contract)**: `SignupUseCase` now also receives `KeyValueStoreInterface` and stores the access
  and refresh tokens on success. Its constructor becomes `(authRepository, keyValueStore, role)`. This reverses the
  `account-signup` requirement that signup stores nothing.
- Add `GetSessionUseCase` to `core`: reads the stored tokens and returns a `Session` or `null`. It makes no API call.
- Add the `Session` alias (`Pick<AuthResponse, "accessToken" | "refreshToken">`) to the `Auth` domain area.
- Add the `getSessionUseCase` key to `Dependencies` and register it in both fronts.
- Backoffice: add `LocalStorageKeyValueStore` and register it as `keyValueStore`, replacing `InMemoryKeyValueStore`,
  which is deleted from the backoffice.
- Backoffice: add the `/signup` route, `SignupView` and `SignupForm`, with structural validation and translated
  errors, in a new `auth` translation namespace.
- Backoffice: add a `RequireSession` component. `HomeView` (`/`) is wrapped in it, so visitors without stored tokens
  are redirected to `/signup` and never see home content.
- App: wiring only. Its container passes its in-memory `keyValueStore` to `SignupUseCase` and registers
  `GetSessionUseCase`. No screen changes.

## Capabilities

### New Capabilities

- `auth-session`: reading the session the front has stored after authenticating, and how long it survives in each
  front.
- `backoffice-signup`: the backoffice signup screen: its form, structural validation, error messages and where it
  leads.
- `backoffice-access-guard`: which backoffice screens need a stored session, and what an anonymous visitor sees
  instead.

### Modified Capabilities

- `account-signup`: a successful signup now stores the access and refresh tokens, and a storage failure is returned
  as `UNEXPECTED`. The role and error requirements do not change.

## Fronts And Roles

- **backoffice**: `TRAINER` signs up from `/signup` and lands on `/`. `ADMIN` has no signup, because the container
  registers `SignupUseCase` with role `TRAINER` only. The home screen is the same for every signed-in role, because
  it holds no role-specific content yet. An anonymous visitor must not see any home content, not even while the
  session is being read.
- **app**: wiring only. Athletes (`USER`) get no screen. A signup from the app would now store its tokens in the
  app's in-memory store, which nothing reads yet.

## Canonical References

`.stpr/template/` is still empty. Each piece takes the reference below:

- `Session` alias, `SignupUseCase`, `GetSessionUseCase` and their tests: skeleton-backed from `add-signup-usecase`
  (`core/src/domain/auth/auth.ts`, `core/src/application/use-cases/auth/signup-use-case.ts`,
  `core/test/application/use-cases/auth/signup-use-case.test.ts`). The "use case calls a repository and a platform
  port" shape is `pending` (`LoginUseCase`), so **this change establishes it** in `SignupUseCase`.
- `LocalStorageKeyValueStore`: `pending` (login). **This change establishes it**, keeping the shape of
  `backoffice/src/infrastructure/in-memory-key-value-store.ts`.
- `Dependencies` and both `container.ts`: skeleton-backed. Extend them and keep their structure.
- Signup route, view and form: `pending` (login route, view and form). **This change establishes the backoffice
  pattern**, using `backoffice/src/app/page.tsx` and `home-view.tsx` for structure. The app is not a reference.
- `RequireSession`: no reference exists. **This change establishes it.**
- View tests: `pending` (login). They follow `backoffice/test/presentation/common/views/home-view.test.tsx` and
  `backoffice/test/support/render-with-providers.tsx`.
- E2E: `pending` (login). It follows `backoffice/e2e/home.spec.ts`.

`.stpr/template/` and its README are not updated here. The login slice may adopt or revise these patterns.

## Constraints And Conflicts

- **Reverses an accepted requirement.** `account-signup` currently says signup stores no tokens and leaves storage
  to login. Storing them here follows the invariants: session handling is a use case, tokens go through
  `KeyValueStoreInterface`, and presentation cannot reach adapters. The delta spec records the change.
- **Guard placement.** The invariants allow a layout to render only the root providers, so the guard is a
  presentation component wrapping the view, not a route-group layout.
- **Static export.** The guard is client-side navigation only. There is no middleware, and the invariants forbid it.
  It is not authorization: the API remains the authority on every protected call.
- **No hand-written type.** `Session` is derived from the generated `AuthResponse`.
- **No business rule.** Password strength stays with the API. The form checks only that fields are present and the
  email format.
- No other conflict with the template, the invariants or a stop case.

## API Contract

- `POST /auth/signup` is in `core/openapi/v1.json`: request `SignupRequest`, response `AuthResponse`. No snapshot
  refresh, no API change. `GetSessionUseCase` calls no endpoint.

## Acceptance Criteria

- Signing up with a valid email and a password the API accepts stores both tokens in `localStorage` and lands on
  `/`, which shows the home heading.
- Opening `/` with no stored tokens ends on `/signup`, and the home heading is never rendered.
- Reloading `/` after signing up stays on `/`.
- An empty or malformed email, or an empty password, shows a field error and sends no request.
- `VALIDATION`, `UNAUTHORIZED`, `CONFLICT` and `NETWORK` from signup each show their translated message on the
  signup screen, and no tokens are stored.
- Every new string exists in `es` and `en`.
- Core unit, backoffice unit and component tests, and the backoffice E2E flows in `design.md` pass with the API
  mocked. `pnpm typecheck && pnpm lint && pnpm test && pnpm test:e2e` passes from `front/`.

## Impact

- `core`: `domain/auth/auth.ts`, `use-cases/auth/signup-use-case.ts`, new `get-session-use-case.ts`, barrels,
  `dependencies.ts`, and tests.
- `backoffice`: `src/infrastructure` (new adapter, container, `InMemoryKeyValueStore` removed), `src/app/signup`,
  `src/presentation/auth`, `home-view.tsx`, `src/i18n` (new `auth` namespace, a `common` key, `resources.ts`),
  `test/`, `e2e/`.
- `app`: `src/infrastructure/container.ts` and `test/support/render-with-providers.tsx` only.
- No new dependency. No API change.
