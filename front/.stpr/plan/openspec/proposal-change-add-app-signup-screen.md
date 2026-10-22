# Change Proposal: Add App Signup Screen

Source brief for `/opsx:propose`. Artifacts go to `openspec/changes/add-app-signup-screen/`.

## Change

Add a signup screen to the app that stores the returned tokens, add the app's session state hook, gate the routes by
session in the root layout, and reorganize `app/src/app` into the route groups of `STPR_INVARIANTS.md` section 4.3
("Routing").

## Objective

`SignupUseCase` and `GetSessionUseCase` are wired in the app, but no screen calls them. An athlete has no way to
create an account, and nothing tells a signed-in athlete from an anonymous one. The app's router is also flat
(`_layout.tsx` and `index.tsx`), with no place for a signed-out flow next to the signed-in one.

This change adds the signup UI, stores the tokens returned on signup, adds the session state hook that the root
layout's auth gate reads, and moves the routes into the `(auth)` / `(app)` groups. The result can be checked from a
running app: signing up lands on home, and opening the app without stored tokens shows signup.

## Capabilities

- An athlete signs up from `/signup` with email and password.
- On success, the tokens are stored through `KeyValueStoreInterface`, the session state becomes signed in, and the
  auth gate shows `/`. No code navigates.
- `/` is reachable only with a session. Without stored tokens, the gate shows `/signup` and the home content is never
  rendered.
- A signed-in athlete cannot reach `/signup`.
- The native splash stays up while the session is restored, so no screen flashes before the gate decides.

## Fronts And Roles

- app -> lands here: yes -> roles: `USER` -> signs up from `/signup` and then sees the signed-in home. The container
  already registers `SignupUseCase` with role `USER` (`1`) and `GetSessionUseCase`, so this change does not touch
  `container.ts`. The home screen holds no role-specific content.
- backoffice -> lands here: no -> roles: none -> nothing changes. Its signup slice is already in place.

An anonymous athlete must NOT see any home content, not even briefly while tokens are being read.

## Classification

- Root layout (`RootLayout` + local `RootNavigator`, splash hold, `Stack.Protected` gate) -> route (layout) ->
  `app/src/app/_layout.tsx`
- Not-found route -> route -> `app/src/app/+not-found.tsx`
- `(auth)` navigator -> route (layout) -> `app/src/app/(auth)/_layout.tsx` (`AuthLayout`)
- `/signup` route -> route -> `app/src/app/(auth)/signup.tsx`
- `(app)` navigator -> route (layout) -> `app/src/app/(app)/_layout.tsx` (`AppLayout`)
- `/` route (moved from `app/src/app/index.tsx`) -> route -> `app/src/app/(app)/index.tsx`
- Session state hook -> state hook -> `app/src/presentation/auth/context/session-context.ts`,
  `app/src/presentation/auth/context/session-provider.tsx` (`SessionProvider`),
  `app/src/presentation/auth/hooks/use-session.ts` (`useSession`)
- `SessionProvider` composition -> component -> `app/src/presentation/common/components/app-providers.tsx`
- `SignupView` -> view -> `app/src/presentation/auth/views/signup-view.tsx`
- `SignupForm` -> component -> `app/src/presentation/auth/components/signup-form.tsx`
- `HomeView` (unchanged content, now reached only through the gate) -> view ->
  `app/src/presentation/common/views/home-view.tsx`
- `NotFoundView` -> view -> `app/src/presentation/common/views/not-found-view.tsx`
- `auth` translations -> i18n -> `app/src/i18n/{es,en}/auth.json`, registered in `app/src/i18n/resources.ts`
- Test providers -> test support -> `app/test/support/render-with-providers.tsx` gains a session option

No core piece, no adapter and no container registration is added. `InMemoryKeyValueStore` stays as the app store.

## Target Router Structure

Before:

```text
app/src/app/
  _layout.tsx        AppProviders + Stack + StatusBar
  index.tsx          HomeView
```

After:

```text
app/src/app/
  _layout.tsx        RootLayout: AppProviders + RootNavigator + StatusBar
                     RootNavigator: splash hold, Stack with
                       <Stack.Protected guard={signedIn}>  (app)
                       <Stack.Protected guard={!signedIn}> (auth)
  +not-found.tsx     NotFoundView
  (auth)/
    _layout.tsx      AuthLayout: Stack of the signed-out screens
    signup.tsx       SignupView     -> /signup
  (app)/
    _layout.tsx      AppLayout: Stack, the home first
    index.tsx        HomeView       -> /
```

There is one top-level destination, so there is no `(tabs)` yet. The second destination adds `(app)/(tabs)/` and
moves the home to `(app)/(tabs)/index.tsx`. A later login screen goes in `(auth)`, and a later full-screen screen in
`(app)` as `<screen>.tsx`.

The organization comes from the reference project's Expo Router tree (`planea/app/src/app`) as generalized in section
4.3 ("Routing"). Its imperative `BootstrapGate`, its loading `index` route and its drawer are not taken.

## Canonical Template Mapping

- App routes and layouts -> no reference for groups exists -> follow section 4.3 ("Routing") of
  `.stpr/template/STPR_INVARIANTS.md`; use the current `app/src/app/_layout.tsx` and `app/src/app/index.tsx` for file
  structure.
- Session state hook -> follow the `ContainerContext` trio (`presentation/common/context/container-context.ts`,
  `container-provider.tsx`, `hooks/use-container.ts`).
- `container.ts` -> `app/src/infrastructure/container.ts` (skeleton-backed) -> unchanged. It already registers
  `signupUseCase` and `getSessionUseCase`.
- Signup route, view and form -> `pending` (login route, view and form) -> this change establishes the app pattern.
  Use `app/src/app/index.tsx` and `app/src/presentation/common/views/home-view.tsx` for route and view structure.
  The backoffice is not a reference.
- View tests -> `pending` (login) -> follow `app/test/presentation/common/views/home-view.test.tsx` and
  `app/test/support/render-with-providers.tsx`.
- E2E -> no app E2E tier exists -> app E2E is a known gap, accepted for now.

`.stpr/template/` is not updated. The login slice may adopt or revise the patterns set here.

## Screens And Navigation

- app -> `/signup` -> `SignupView` -> shown by the gate when there is no session -> success: `session.refresh()`, the
  guard flips and Expo Router shows `/`; failure: stays on the screen with an error message and the entered email
  kept.
- app -> `/` -> `HomeView` -> shown by the gate when there is a session -> title and a short signed-in message.
- app -> any unknown path -> `NotFoundView` -> a deep link to a route that does not exist -> a link to `/` (the gate
  then shows whichever screen the session allows).

No `Redirect` and no `router.replace` are used for access control. The only navigation call is the not-found link.

## API Contract

- `POST /auth/signup` -> in `core/openapi/v1.json`: yes -> request `SignupRequest`, response `AuthResponse`.

No snapshot refresh. No API change. `GetSessionUseCase` only reads local storage and makes no API call.

## Held State

- `SessionProvider` (app-wide, composed in `app-providers.tsx`) -> `status: "restoring" | "signedIn" | "signedOut"`
  and `refresh()` -> set from `GetSessionUseCase` on mount and on every `refresh()` -> a `fail` result sets
  `signedOut` and holds the `DomainError` code -> lives for the app process. It never navigates and never calls
  `SignupUseCase`.
- Keys `auth.accessToken` and `auth.refreshToken` in `InMemoryKeyValueStore`, through `KeyValueStoreInterface` ->
  written by `SignupUseCase` on success only -> lost when the app process ends; nothing removes them in this change
  (no logout yet).
- `SignupView` screen-local state (field values, field errors, submitting, last error code) -> `useState` in the view
  or `hooks/use-signup.ts` beside it -> reset on screen exit.

## Forms And Structural Validation

- Signup form -> `email` -> required, email format.
- Signup form -> `password` -> required. Password strength is the API's rule and is not checked here.

Values are trimmed before validation; no other normalization. The snapshot declares no maximum lengths for
`SignupRequest`, so none are checked. Fields are Paper `TextInput` with `error` set and a `HelperText type="error"`
holding a translated key. The submit `Button` shows `loading` and is disabled while the request is pending. The view
is wrapped in `KeyboardAvoidingView`. The email field uses the email keyboard with autocapitalization off; the
password field is secure text entry.

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

`common` namespace:

- `home.signedIn` -> "Has iniciado sesión." -> "You are signed in."
- `notFound.title` -> "Esta pantalla no existe." -> "This screen does not exist."
- `notFound.goHome` -> "Ir al inicio" -> "Go to home"

## Platform Capabilities And Dependencies

- Key-value storage -> `KeyValueStoreInterface` (existing) -> unchanged `LocalStorageKeyValueStore` ->
  unchanged `InMemoryKeyValueStore` -> development build: no.
- Splash hold -> `SplashScreen.preventAutoHideAsync()` / `SplashScreen.hide()` from `expo-router` -> development
  build: no.

No new package. `Stack` and `Stack.Protected` come from `expo-router`.

## Out Of Scope

- Login, logout, token refresh and expiry handling. The session hook only checks that both tokens are present. It
  does not check that they are valid, and the home screen makes no authorized API call.
- Persisting tokens across app restarts. `SqliteKeyValueStore` belongs to the login slice.
- The reactive refresh-and-retry on `UNAUTHORIZED`.
- `Stack.Protected`'s `redirectTo` (SDK 58+, not in the pinned `expo-router`).
- A password confirmation field, or showing password rules (those are API rules).
- `(tabs)`, a drawer, or any secondary screen in `(app)`.
- Any core change, any `container.ts` change, any backoffice change.
- Updating `.stpr/template/`, `.stpr/template/README.md` or `.stpr/template/STPR_INVARIANTS.md`.
- Must not change: the API, the snapshot, the HTTP error mapper, `AuthRepository`, `SignupUseCase`,
  `GetSessionUseCase`.

## Notes

- **Router rule.** This change follows `.stpr/template/STPR_INVARIANTS.md` section 4.3 ("Routing" and
  "Presentation"). Bringing the session state hook forward from the login slice is what lets the root `Stack` be
  guarded now instead of staying unguarded under the transition rule.
- **Why the gate is in the root layout.** Section 4.3 makes `src/app/_layout.tsx` the only place that decides access,
  with `Stack.Protected`, and forbids `Redirect` / `router.replace` for access control. Signup therefore only
  refreshes the session; the guard does the rest.
- **No loading route.** While `status` is `restoring`, the native splash stays up; `RootNavigator` calls
  `SplashScreen.hide()` once it is `signedIn` or `signedOut`. `/` is `(app)/index.tsx` only, so the current
  `app/src/app/index.tsx` is moved, not duplicated.
- **Session in the app.** `SignupUseCase` already writes both tokens through the injected store and returns `fail`
  with `UNEXPECTED` if writing fails. The app's store is in memory, so a session lasts for the app process only. This
  is enough to check the flow and is replaced by the SQLite adapter in the login slice, which extends `SessionProvider`
  with sign-in and sign-out.
- **No business rule.** "No tokens, no home" is a client-side navigation guard, not authorization. The API remains
  the authority on every protected call.
- **Expo docs.** `app/package.json` pins `expo` `~57`. Read `https://docs.expo.dev/versions/v57.0.0/` for
  `expo-router` route groups, `Stack.Protected`, `SplashScreen` and `+not-found` before writing navigation code.
- **Verification tiers:**
  - app component: `test/presentation/auth/views/signup-view.test.tsx`: structural errors block submit and the use
    case is not called; values are trimmed; success calls the session's `refresh` and makes no `router` call; each of
    `VALIDATION`, `UNAUTHORIZED`, `CONFLICT` and `NETWORK` shows its message; the email is kept after a failure.
  - app component: `test/presentation/auth/context/session-provider.test.tsx`: `restoring` then `signedIn` with
    stored tokens; `signedOut` with none; `signedOut` on a `fail` result; `refresh()` re-reads the session.
  - app component: `home-view.test.tsx` still passes (heading and signed-in message).
  - app component: `test/presentation/common/views/not-found-view.test.tsx`: shows the message and the link to `/`.
  - The use cases are replaced through `render-with-providers.tsx`, which already registers `signupUseCase` and
    `getSessionUseCase` fakes and gains a session option for views.
  - app translation parity test passes with the new `auth` namespace.
  - core and backoffice: no new tests; their suites still pass.
  - The root layout's gate is not tested at the component tier (`expo-router/testing-library` is not adopted yet; the
    login slice decides). App E2E: none, a known gap accepted for now. The flows a backoffice E2E would cover (sign
    up and land on `/`; open the app with no session and see `/signup`; a `409` shows the conflict message) are
    covered at the component tier only, through the session and view tests above.
- Done means `pnpm typecheck && pnpm lint && pnpm test` passes from `front/`. `pnpm test:e2e` is not required, since
  no backoffice flow changes.
- No conflict with a stop case. No deviation from a skeleton reference is allowed. Where a reference is `pending`,
  this change establishes it as described above.
