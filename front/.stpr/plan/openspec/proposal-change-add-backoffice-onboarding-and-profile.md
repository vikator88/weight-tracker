# Change Proposal: Add Backoffice Onboarding And Profile

Source brief for `/opsx:propose`. Artifacts go to `openspec/changes/add-backoffice-onboarding-and-profile/`.

## Change

Wire `GetMyProfileUseCase` and `UpdateMyProfileUseCase` to the backoffice UI: a blocking onboarding on the home
screen (first step: name and surname), the real user in the header's user menu, and an Edit Profile screen reached
from that menu.

## Objective

The core use cases for `GET /users/me` and `PUT /users/me` exist, but the UI still shows a mock user. A new account
has an empty name and surname, so the backoffice cannot greet or identify it. This change makes the user complete
their name before using the backoffice, shows who is signed in, and lets them change their name later. The
onboarding is built as a step flow so later changes can add steps without reworking it.

## Capabilities

- **Signed-in user loading.** Once the session is confirmed, the backoffice reads the profile with
  `getMyProfileUseCase` once and shares it with every screen inside `AppShell`. Saving the profile (onboarding or
  Edit Profile) replaces the shared value with the profile the API returns, so the header updates at once.
- **Onboarding status.** In this version the user is in onboarding when `name` or `surname` is empty after trimming.
  This check lives in one place (see Held State) so a later change can switch it to a field of `GET /users/me`
  without touching its callers.
- **Onboarding on the home screen** (`/`):
  - While onboarding is pending, the home content is replaced by a skeleton of mock sections (see Notes), and an
    onboarding card sits on top of it as a modal dialog.
  - The dialog cannot be dismissed: no close button, `Escape` and backdrop clicks do nothing. The header and drawer
    behind it cannot be used.
  - The card shows the step in its title ("Paso 1 de 1 · Dinos cómo te llamas"), the step's fields in its body, and
    the actions at the bottom: **Atrás** on every step but the first, **Siguiente** on every step but the last, and
    **Finalizar** on the last one. With one step only **Finalizar** shows.
  - Step 1, "Dinos cómo te llamas": Nombre and Apellidos, both required (see Forms).
  - **Finalizar** validates the step, then calls `updateMyProfileUseCase` with the trimmed values. On success the
    shared profile is replaced, the onboarding is no longer pending, the dialog closes, and the regular home content
    shows. On failure the dialog stays open with the error.
  - While onboarding is pending, any other `AppShell` route redirects to `/`, so a typed URL cannot skip it.
- **User menu with real data.** The menu shows `name surname`, the translated role, and initials from the first
  letter of the name and of the surname. While the profile is loading, failed to load, or onboarding is pending, the
  name, role and avatar render as skeletons, never as placeholder text.
- **Mi perfil** in the user menu navigates to `/profile`. Preferencias stays a placeholder.
- **Edit Profile screen** (`/profile`): a form with Nombre and Apellidos prefilled from the shared profile, with the
  same validation as onboarding, and a **Guardar** button. On success it shows a success message, stays on the screen
  and updates the shared profile. On failure it shows the error and keeps the values typed.

## Fronts And Roles

- backoffice -> lands here: yes -> roles: `TRAINER`, `ADMIN` -> both go through the same onboarding, see their own
  name and role in the user menu, and edit their own profile. No branch differs by role, except the role label.
- app -> lands here: only through core -> roles: `USER` -> no UI change. `UpdateMyProfileUseCase` now rejects names
  outside 3–100 characters for the app too, and the app's future profile forms reuse the same rules.

A user in onboarding must NOT see the regular home content, real-looking placeholder names in the user menu, or any
screen other than `/`. No user can see or edit another user's profile: both operations act on the session's user.

## Classification

Backoffice:

- `CurrentUserProvider` (loads the profile, exposes it with its load state, `reload` and `replace`) -> component
  (provider) -> `backoffice/src/presentation/common/components/current-user-provider.tsx`
- `CurrentUserContext` -> context -> `backoffice/src/presentation/common/context/current-user-context.ts`
- `useCurrentUser` (reworked: reads the context; returns `profile`, `loading`, `errorCode`, `onboardingPending`,
  `reload`, `replace`) -> state hook -> `backoffice/src/presentation/common/hooks/use-current-user.ts`
- `AppShell` (wraps its content in `CurrentUserProvider`; redirects non-`/` routes to `/` while onboarding is
  pending) -> component -> `backoffice/src/presentation/common/components/app-shell.tsx`
- `UserMenu` (real name, role, initials, skeletons; Mi perfil links to `/profile`) -> component ->
  `backoffice/src/presentation/common/components/user-menu.tsx`
- `HomeView` (skeleton plus onboarding while pending; regular content otherwise) -> view ->
  `backoffice/src/presentation/common/views/home-view.tsx`
- `HomeSkeleton` (mock sections) -> component -> `backoffice/src/presentation/common/components/home-skeleton.tsx`
- `LoadError` (error message with a retry button) -> component -> `backoffice/src/presentation/common/components/load-error.tsx`
- `OnboardingDialog` (step title, step body, Atrás / Siguiente / Finalizar) -> component ->
  `backoffice/src/presentation/onboarding/components/onboarding-dialog.tsx`
- `NameStep` (step 1 fields) -> component -> `backoffice/src/presentation/onboarding/components/name-step.tsx`
- `useOnboarding` (step list, current index, values, validation, back, next, finish) -> state hook ->
  `backoffice/src/presentation/onboarding/hooks/use-onboarding.ts`
- `ProfileNameFields` (Nombre and Apellidos fields; maps each `ProfileNameViolation` to `profile:validation.<violation>`) ->
  component -> `backoffice/src/presentation/profile/components/profile-name-fields.tsx`
- `ProfileForm` -> component -> `backoffice/src/presentation/profile/components/profile-form.tsx`
- `useUpdateProfile` (calls `updateMyProfileUseCase`, holds `saving`, `errorCode`, `saved`, then calls `replace`) ->
  state hook -> `backoffice/src/presentation/profile/hooks/use-update-profile.ts`
- `ProfileView` (inside `AppShell`) -> view -> `backoffice/src/presentation/profile/views/profile-view.tsx`
- `/profile` route -> route -> `backoffice/src/app/profile/page.tsx`
- `onboarding.json` and `profile.json` namespaces, `common.json` additions -> i18n -> `backoffice/src/i18n/{es,en}/`

Core:

- `PROFILE_NAME_MIN_LENGTH` (3), `PROFILE_NAME_MAX_LENGTH` (100), `ProfileNameViolation`
  (`"required" | "minLength" | "maxLength"`) and `validateProfileName(value): ProfileNameViolation | null` (trims, then
  checks) -> application validation rules -> `core/src/application/validation/profile-name-rules.ts`
- `validateUpdateProfileRequest(request)` (returns the violation per field, or none) -> same file
- `UpdateMyProfileUseCase` (trims both values, validates them, and returns `VALIDATION` without calling the repository
  when either is invalid; otherwise sends the trimmed values) -> use case -> `core/src/application/use-cases/users/update-my-profile-use-case.ts`
- Barrel -> `core/src/application/validation/index.ts`, re-exported from `core/src/application/index.ts`

The rules are pure functions with no dependency, so both fronts import them from `@weight-tracker/core/application`.
`Dependencies` and both `container.ts` are unchanged.

## Canonical Template Mapping

- `CurrentUserProvider` and its context -> `backoffice/src/presentation/common/context/container-context.ts` and
  `container-provider.tsx` -> the only provider and context pair in this front. The load-on-mount with an `active`
  guard follows `backoffice/src/presentation/auth/components/require-session.tsx`.
- `useCurrentUser` -> its current version, reworked in place; callers keep importing it.
- `useUpdateProfile` -> `backoffice/src/presentation/common/hooks/use-logout.ts` -> the closest state hook wrapping a
  use case with an in-flight flag and an `errorCode`.
- `ProfileForm`, `ProfileNameFields`, `NameStep` and `validateProfileName` -> `backoffice/src/presentation/auth/components/signup-form.tsx`
  and `login-form.tsx` -> the closest forms: trimmed values, per-field translated errors, disabled submit while
  pending, `errors:<code>` alert on failure.
- `ProfileView` and the `/profile` route -> `backoffice/src/presentation/common/views/home-view.tsx` and
  `backoffice/src/app/page.tsx` -> a view inside `AppShell` and a route that only re-exports it.
- `UserMenu`, `AppShell`, `HomeView` -> their current versions, extended in place.
- `profile-name-rules.ts` -> no reference exists: **this change establishes application-layer validation rules** in
  core (pure functions, no port, no `Result`). Its test follows the structure of the use case tests in
  `core/test/application/use-cases/users/`.
- `UpdateMyProfileUseCase` guard -> its current version, extended; the early `VALIDATION` failure follows the
  `DomainError` shape the HTTP error mapper produces.
- `OnboardingDialog` and `HomeSkeleton` -> no reference exists: **this change establishes the multi-step dialog and
  the skeleton pattern** in this front, with MUI `Dialog` (`disableEscapeKeyDown`, `onClose` ignoring backdrop
  clicks), `DialogTitle`, `DialogContent`, `DialogActions` and `Skeleton`. `LoadError` is the first error-state
  component in `presentation/common/components` (invariants: the first view that needs one creates it there).
- Tests -> `backoffice/test/presentation/common/components/user-menu.test.tsx`,
  `backoffice/test/presentation/common/views/home-view.test.tsx`, the signup form test, and
  `backoffice/e2e/login.spec.ts` and `signup.spec.ts`.

The app is not a UI reference. `.stpr/template/` and its README are not updated.

## Screens And Navigation

- backoffice -> `/` -> `HomeView` inside `AppShell` -> after login, signup, or Inicio in the drawer:
  - profile loading -> `HomeSkeleton`, user menu as skeleton, no dialog.
  - load failed -> `HomeSkeleton` with `LoadError` on top; **Reintentar** calls `reload`.
  - onboarding pending -> `HomeSkeleton` with `OnboardingDialog` on top -> **Finalizar** success: dialog closes, regular
    home content shows, user menu shows the new name; failure: dialog stays with the error.
  - onboarding complete -> regular home content.
- backoffice -> `/profile` -> `ProfileView` inside `AppShell` -> user menu → Mi perfil -> **Guardar** success: stays,
  success message, header updated; failure: stays, error shown, values kept. While onboarding is pending:
  `router.replace("/")`.
- Header logout stays as it is, but cannot be reached while the onboarding dialog is open (see Notes).

## API Contract

- `GET /users/me` -> in `core/openapi/v1.json`: yes -> `UserProfile` (`UserProfileResponse`: `id`, `email`, `name`,
  `surname`, `role`).
- `PUT /users/me` -> in `core/openapi/v1.json`: yes -> `UpdateProfileRequest` (`name`, `surname`) -> `UserProfile`.

Both endpoints exist as needed. No API change and no snapshot refresh: the length rules are a front decision (see
Notes).

## Held State

- `CurrentUserProvider` -> `profile` (`UserProfile | null`), `loading`, `errorCode` -> loaded once on mount by
  `getMyProfileUseCase`; `reload` re-runs it; `replace` sets the profile returned by `updateMyProfileUseCase` -> reset
  when `AppShell` unmounts (logout navigates to `/login`) and on page reload. Not persisted.
- `onboardingPending` -> derived from `profile` (`name` or `surname` empty after trimming), never stored. It is
  computed only inside `useCurrentUser`.
- `useOnboarding` -> current step index and the values typed per step -> reset when the dialog unmounts.
- `useUpdateProfile` -> `saving`, `errorCode`, `saved` -> reset on screen exit; `saved` clears when a field changes.
- Form values in `ProfileForm` -> initialised from `profile` -> reset on screen exit.

## Forms And Structural Validation

Values are trimmed before validation and sent trimmed. Both forms call `validateProfileName` from core, and
`UpdateMyProfileUseCase` repeats the check before any request.

- Onboarding step 1 and Edit Profile -> `name` -> required, minimum length 3, maximum length 100.
- Onboarding step 1 and Edit Profile -> `surname` -> required, minimum length 3, maximum length 100.

The maximum (100) mirrors `PersonName.MaxLength` in the API domain. The minimum (3) is a front-only rule by decision:
the API still accepts any non-blank value. See Notes.

## Errors And Copy

- Loading the profile fails -> `NETWORK`, `UNEXPECTED`, `UNAUTHORIZED` -> `LoadError` with `errors:<code>` and
  Reintentar.
- Saving fails because the values break the rules (use case guard) or the API rejects them -> `VALIDATION` ->
  `errors:VALIDATION` above the actions. The form normally prevents this with per-field messages first.
- Saving fails for any other reason -> `NETWORK`, `UNAUTHORIZED`, `UNEXPECTED` -> `errors:<code>` above the actions.

No new error code: all of them are already mapped and translated.

New keys (Spanish as requested or proposed; English proposed):

- `onboarding:stepTitle` -> "Paso {{current}} de {{total}} · {{title}}" -> "Step {{current}} of {{total}} · {{title}}"
- `onboarding:steps.name.title` -> "Dinos cómo te llamas" -> "Tell us your name"
- `onboarding:actions.back` -> "Atrás" -> "Back"
- `onboarding:actions.next` -> "Siguiente" -> "Next"
- `onboarding:actions.finish` -> "Finalizar" -> "Finish"
- `profile:title` -> "Mi perfil" -> "My profile"
- `profile:fields.name` -> "Nombre" -> "Name"
- `profile:fields.surname` -> "Apellidos" -> "Surname"
- `profile:validation.required` -> "Este campo es obligatorio." -> "This field is required."
- `profile:validation.minLength` -> "Debe tener al menos {{min}} caracteres." -> "Must be at least {{min}} characters."
- `profile:validation.maxLength` -> "No puede superar los {{max}} caracteres." -> "Must be at most {{max}} characters."
- `profile:actions.save` -> "Guardar" -> "Save"
- `profile:saved` -> "Perfil actualizado." -> "Profile updated."
- `common:loadError.retry` -> "Reintentar" -> "Retry"
- `common:loading` -> "Cargando" -> "Loading" (accessible label for the skeletons)

## Platform Capabilities And Dependencies

None. `Skeleton` and `Dialog` are in `@mui/material`; the icons already come from `@mui/icons-material`.

## Out Of Scope

- Onboarding steps beyond step 1, and an onboarding status field from `GET /users/me`.
- Editing email, role, password or avatar; Preferencias.
- Onboarding or profile screens in the app.
- Real content for the home sections; the skeleton is a placeholder.
- Adding the minimum length to the API, or declaring the limits in the snapshot. No snapshot refresh.
- Must not change: the API, `GetMyProfileUseCase`, the repositories, `Dependencies`, `RequireSession`, login and
  signup screens, the app UI.

## Notes

- **Allowed deviation: name length rules in core.** Two invariants are relaxed on purpose, by the product owner's
  decision, and the design must record both:
  1. _Business rules belong to the API._ The minimum of 3 characters exists only in the front. The API may still
     receive shorter values from other clients; the fronts guarantee 3–100 for their own users.
  2. _Structural validation lives in presentation._ The rules live once in `core/src/application/validation/` so
     both fronts share them, and `UpdateMyProfileUseCase` enforces them. Presentation only maps violations to
     translation keys.
     This is not a stop case for this change. Updating `STPR_INVARIANTS.md` to allow shared validation rules in core is a
     separate template change. If the API later adopts the minimum, the core rule stays as fast feedback.
- **Logout during onboarding.** The dialog blocks the header, so a user cannot log out until they finish. This
  version accepts that, as requested ("just remain and complete the onboarding"). If it is not acceptable, add a
  secondary "Cerrar sesión" action inside the dialog, reusing `useLogout`.
- **Step model.** `useOnboarding` holds an ordered list of steps; each step has a title key, a body component and a
  validator. Atrás / Siguiente move between steps without calling the API; Finalizar validates the last step and sends
  the collected values. A later step that needs another endpoint extends Finalizar.
- **Role label.** `role` is a `string` in the contract. `UserMenu` shows `header.user.roles.<role>`; the keys exist for
  `TRAINER` and `ADMIN`. The `CurrentUser` interface and its mock (`"Usuario de prueba"`) are removed.
- **Home skeleton.** Mock sections only: a row of three stat cards, a wide chart block, and a list of five rows, all
  `Skeleton`. It is shown while loading, on load failure, and during onboarding.
- **Next 16.** Read the `app` routing, `useRouter`, `usePathname` and `next/link` guides in
  `backoffice/node_modules/next/dist/docs/` before adding `/profile`. The export stays static.
- **Verification tiers:**
  - core unit: `profile-name-rules.test.ts` (empty and whitespace-only → `required`; 2 characters → `minLength`; 3 →
    valid; 100 → valid; 101 → `maxLength`; surrounding spaces trimmed before counting, so `"  ab  "` → `minLength`).
  - core unit: `update-my-profile-use-case.test.ts` extended (invalid name or surname → `VALIDATION` and the
    repository is not called; valid values are sent trimmed).
  - backoffice component: `home-view.test.tsx` (loading skeleton; load failure with retry calling the use case again;
    empty name shows the dialog over the skeleton and hides the regular content; empty surname alone also does;
    complete profile shows the regular content and no dialog; Finalizar success closes the dialog and the menu shows
    the name; Finalizar failure keeps the dialog with the error; Escape and backdrop do not close it).
  - backoffice component: `onboarding-dialog.test.tsx` (title "Paso 1 de 1 · Dinos cómo te llamas"; only Finalizar,
    no Atrás and no Siguiente; required, minimum and maximum messages; trimmed values sent; Finalizar disabled while
    saving). Add a two-step fake list in the test to cover Atrás and Siguiente.
  - backoffice component: `user-menu.test.tsx` (name, surname, role and initials from the profile; skeletons and no
    name text while loading, on failure and in onboarding; Mi perfil navigates to `/profile`).
  - backoffice component: `profile-view.test.tsx` (prefilled fields; validation; Guardar success shows the message
    and updates the header; `VALIDATION` and `NETWORK` failures show their messages; redirect to `/` in onboarding).
  - backoffice E2E: new `onboarding.spec.ts` (sign in with an empty-name profile, complete the step, the header
    shows the name) and `profile.spec.ts` (open Mi perfil, change the name, the header updates). Existing login and
    signup flows intercept `GET /users/me` with a complete profile so they are not blocked by the dialog.
  - Translation parity covers the new namespaces and keys.
- Done means `pnpm typecheck && pnpm lint && pnpm test && pnpm test:e2e` passes from `front/`.
- No other deviation is allowed beyond the name-rules deviation above and the patterns this change establishes
  (dialog, skeleton, load error, core validation rules), recorded in the design.
