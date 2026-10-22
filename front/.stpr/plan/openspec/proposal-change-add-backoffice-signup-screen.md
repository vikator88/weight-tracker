# Change Proposal: Add Backoffice Signup Screen

Source brief for `/opsx:propose`. Artifacts go to `openspec/changes/add-backoffice-signup-screen/`.

## Change

Add a signup screen to the backoffice that stores the returned tokens, and make the home screen an authorized
screen that sends visitors without stored tokens to signup.

## Objective

`SignupUseCase` is wired in the backoffice, but no screen calls it and the tokens it returns are thrown away. A
trainer has no way to create an account, and no screen tells a signed-in visitor from an anonymous one.

This change adds the signup UI, stores the tokens returned on signup, and guards the home screen. The result can be
checked end to end: signing up lands on home, and opening home directly without stored tokens sends the visitor to
signup.

## Capabilities

- A trainer signs up from `/signup` with email and password.
- On success, the tokens are stored through `KeyValueStoreInterface` (backed by `localStorage`) and the trainer
  lands on `/`.
- `/` is authorized. With no stored tokens, the visitor is redirected to `/signup` and never sees the home content.
- Stored tokens survive a page reload.

## Fronts And Roles

- backoffice -> lands here: yes -> roles: `TRAINER` -> signs up from `/signup` and then sees the authorized home.
  `ADMIN` has no signup: the container registers `SignupUseCase` with role `TRAINER` only. The home screen looks
  the same for every signed-in role, since it holds no role-specific content yet.
- app -> lands here: wiring only -> roles: `USER` -> no screen. Its `container.ts` passes `keyValueStore` to the new
  `SignupUseCase` constructor and registers `GetSessionUseCase`, so `Dependencies` still compiles. The app keeps its
  in-memory store. Its SQLite adapter belongs to the login slice.

An anonymous visitor must NOT see any home content, not even briefly while tokens are being read.

## Classification

- `Session` alias (`Pick<AuthResponse, "accessToken" | "refreshToken">`) -> entity type -> `core/src/domain/auth/auth.ts`
- `SignupUseCase` (now also stores the tokens) -> use case -> `core/src/application/use-cases/auth/signup-use-case.ts`
- `GetSessionUseCase` (reads the stored tokens) -> use case -> `core/src/application/use-cases/auth/get-session-use-case.ts`
- `Dependencies` key `getSessionUseCase` -> composition contract -> `core/src/application/dependencies.ts`
- `LocalStorageKeyValueStore` -> platform adapter -> `backoffice/src/infrastructure/local-storage-key-value-store.ts`
- Backoffice registrations (`keyValueStore`, `signupUseCase`, `getSessionUseCase`) -> composition root -> `backoffice/src/infrastructure/container.ts`
- App registrations (`signupUseCase`, `getSessionUseCase`) -> composition root -> `app/src/infrastructure/container.ts`
- `/signup` route -> route -> `backoffice/src/app/signup/page.tsx`
- `SignupView` -> view -> `backoffice/src/presentation/auth/views/signup-view.tsx`
- `SignupForm` -> component -> `backoffice/src/presentation/auth/components/signup-form.tsx`
- `RequireSession` (reads the session, then renders its children or redirects) -> component -> `backoffice/src/presentation/auth/components/require-session.tsx`
- `HomeView` (wrapped in `RequireSession`) -> view -> `backoffice/src/presentation/common/views/home-view.tsx`
- `auth` translations -> i18n -> `backoffice/src/i18n/{es,en}/auth.json`, registered in `backoffice/src/i18n/resources.ts`
- Barrels re-export `GetSessionUseCase` and `Session`

`InMemoryKeyValueStore` is removed from the backoffice, which no longer uses it. The app keeps its copy.

## Canonical Template Mapping

- `Session` alias -> `core/src/domain/auth/auth.ts` (skeleton-backed) -> extend it, the way `SignupCredentials` is
  derived from a generated type.
- `SignupUseCase` / `GetSessionUseCase` -> `core/src/application/use-cases/auth/signup-use-case.ts`
  (skeleton-backed) -> same area and shape. `LoginUseCase` ("calls a repository and a platform port") is `pending`.
  This change establishes that pattern in `SignupUseCase`.
- Use case tests -> `core/test/application/use-cases/auth/signup-use-case.test.ts` (skeleton-backed).
- `LocalStorageKeyValueStore` -> `pending` (login) -> this change establishes it. Model its shape on
  `backoffice/src/infrastructure/in-memory-key-value-store.ts`.
- Both `container.ts` -> skeleton-backed -> extend them and keep their structure.
- Signup route, view and form -> `pending` (login route, view and form) -> this change establishes the backoffice
  pattern. Use `backoffice/src/app/page.tsx` and `home-view.tsx` for route and view structure. The app is not a
  reference.
- `RequireSession` -> no reference exists -> this change establishes it.
- View tests -> `pending` (login) -> follow `backoffice/test/presentation/common/views/home-view.test.tsx` and
  `backoffice/test/support/render-with-providers.tsx`.
- E2E -> `pending` (login) -> follow `backoffice/e2e/home.spec.ts`.

`.stpr/template/` is not updated. The login slice may adopt or revise the patterns set here.

## Screens And Navigation

- backoffice -> `/signup` -> `SignupView` -> typed URL, or redirected from `/` without a session -> success:
  `router.replace("/")`; failure: stays on the screen with an error message and the entered email kept.
- backoffice -> `/` -> `HomeView` inside `RequireSession` -> typed URL, or redirected after signup -> while reading
  the session: a progress indicator and no home content; no session: `router.replace("/signup")`; session: home
  content (title and a short signed-in message).

Navigation is client-side only (`next/navigation`), since the backoffice is a static export.

## API Contract

- `POST /auth/signup` -> in `core/openapi/v1.json`: yes -> request `SignupRequest`, response `AuthResponse`.

No snapshot refresh. No API change. `GetSessionUseCase` only reads local storage and makes no API call.

## Held State

- No context provider is added. `SessionProvider` / `useSession` stay with the login slice.
- `localStorage` keys `auth.accessToken` and `auth.refreshToken`, through `KeyValueStoreInterface` -> written by
  `SignupUseCase` on success only -> nothing removes them in this change (no logout yet). They survive reloads.
- `RequireSession` local component state (`checking` / `authorized`) -> set from `GetSessionUseCase` on mount ->
  reset on unmount.
- `SignupForm` local component state (field values, field errors, submitting, API error) -> reset on screen exit.

## Forms And Structural Validation

- Signup form -> `email` -> required, email format.
- Signup form -> `password` -> required. Password strength is the API's rule and is not checked here.

The snapshot declares no maximum lengths for `SignupRequest`, so none are checked. The submit button is disabled
while a request is in flight.

## Errors And Copy

- Weak password -> `VALIDATION` -> `auth:signup.errors.VALIDATION`
- Malformed email that passes the format check -> `UNAUTHORIZED` -> `auth:signup.errors.UNAUTHORIZED`
- Email already registered -> `CONFLICT` -> `auth:signup.errors.CONFLICT`
- No response -> `NETWORK` -> `errors:NETWORK`
- Any other code -> `errors:<code>`

The generic texts for `VALIDATION`, `UNAUTHORIZED` and `CONFLICT` are about sessions and conflicts in general and
would mislead on signup, so this screen overrides those three. No new error code. No change to the HTTP error mapper.

New `auth` namespace:

- `signup.title` -> "Crear cuenta" -> "Create account"
- `signup.email` -> "Email" -> "Email"
- `signup.password` -> "Contraseña" -> "Password"
- `signup.submit` -> "Crear cuenta" -> "Create account"
- `signup.validation.emailRequired` -> "Introduce tu email." -> "Enter your email."
- `signup.validation.emailFormat` -> "Introduce un email válido." -> "Enter a valid email."
- `signup.validation.passwordRequired` -> "Introduce una contraseña." -> "Enter a password."
- `signup.errors.VALIDATION` -> "La contraseña no cumple los requisitos de seguridad." -> "The password does not meet the security requirements."
- `signup.errors.UNAUTHORIZED` -> "El email no es válido." -> "The email is not valid."
- `signup.errors.CONFLICT` -> "Ya existe una cuenta con este email." -> "An account with this email already exists."

`common` namespace: `home.signedIn` -> "Has iniciado sesión." -> "You are signed in."

## Platform Capabilities And Dependencies

- Key-value storage -> `KeyValueStoreInterface` (existing) -> `LocalStorageKeyValueStore` (new) -> unchanged
  in-memory store -> development build: no.

No new package.

## Out Of Scope

- Login, logout, token refresh and expiry handling. `RequireSession` only checks that both tokens are present. It
  does not check that they are valid, and the home screen makes no authorized API call.
- `SessionProvider` / `useSession`, and the reactive refresh-and-retry.
- Redirecting a visitor who already has a session away from `/signup`.
- A password confirmation field, or showing password rules (those are API rules).
- Any app screen, and the app's SQLite store.
- Role-specific home content, and any `ADMIN` flow.
- Updating `.stpr/template/` or `.stpr/template/README.md`.
- Must not change: the API, the snapshot, the HTTP error mapper, `AuthRepository`.

## Notes

- **Why `SignupUseCase` stores the tokens.** The invariants make session handling a use case job and say tokens are
  stored through `KeyValueStoreInterface`. Presentation may not reach platform adapters. So the use case gets the
  store as a constructor argument, `new SignupUseCase(authRepository, keyValueStore, role)`, and writes both tokens
  only when the repository succeeds. If writing fails, it returns `fail` with `UNEXPECTED` and does not throw. This
  reverses the previous change's "the use case does not store tokens", which deferred storage to login.
- **Why `GetSessionUseCase` exists.** `RequireSession` needs to read the tokens, and presentation may only reach
  storage through a use case. It returns `ok(session)` when both keys exist and `ok(null)` otherwise. `Session` is
  derived from `AuthResponse`, not hand-written. `expiresIn` is not stored: it is relative to the response and
  nothing uses it yet.
- **Why a guard component and not a layout.** The invariants allow a layout to render only the root providers. The
  guard therefore lives in presentation and wraps the view that needs it.
- **`localStorage` and the static export.** `next build` prerenders client components, so `localStorage` is only
  touched inside the adapter's async methods, which are only called from effects or event handlers. Never call
  them during render. Read `backoffice/node_modules/next/dist/docs/` for `useRouter` / `next/navigation` in
  Next 16 before writing navigation code.
- **No business rule.** "No tokens, no home" is a client-side navigation guard, not authorization. The API remains
  the authority on every protected call.
- **Verification tiers:**
  - core unit: `signup-use-case.test.ts`: stores both tokens on success, stores nothing on failure, returns
    `UNEXPECTED` when the store fails, still sends the configured role.
  - core unit: `get-session-use-case.test.ts`: session when both keys exist, `null` when either is missing.
  - backoffice unit: `test/infrastructure/local-storage-key-value-store.test.ts`: get, set and remove against
    jsdom's `localStorage`.
  - backoffice component: `test/presentation/auth/views/signup-view.test.tsx`: structural errors block submit
    and the use case is not called; success navigates to `/`; each of `VALIDATION`, `UNAUTHORIZED`, `CONFLICT` and
    `NETWORK` shows its message.
  - backoffice component: `home-view.test.tsx` updated: no session redirects to `/signup` and the home heading is
    not rendered; with a session the heading is shown.
  - backoffice component: `render-with-providers.tsx` gains a `getSessionUseCase` fake. The app's test support
    gets the matching registration.
  - backoffice E2E: `e2e/signup.spec.ts`, with `POST /auth/signup` intercepted by `page.route`: (1) sign up and
    land on `/` seeing the home heading; (2) open `/` with empty storage and end on `/signup` with no home heading;
    (3) a `409` response shows the conflict message. `home.spec.ts` is replaced by these flows.
- Done means `pnpm typecheck && pnpm lint && pnpm test && pnpm test:e2e` passes from `front/`.
- No conflict with a stop case. No deviation from a skeleton reference is allowed. Where a reference is `pending`,
  this change establishes it as described above.
