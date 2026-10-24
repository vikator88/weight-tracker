# Proposal

Source brief: `.stpr/plan/openspec/proposal-change-add-backoffice-onboarding-and-profile.md`.

## Why

`GetMyProfileUseCase` and `UpdateMyProfileUseCase` exist in core, but the backoffice still shows a mock user
("Usuario de prueba"). A new account has an empty name and surname, so the backoffice cannot identify who is signed
in. This change makes the user complete their name before using the backoffice, shows the real user in the header,
and lets them change their name later. The onboarding is built as a step flow so later changes can add steps.

## What Changes

- **Signed-in user in the backoffice.** After the session is confirmed, `AppShell` loads the profile once with
  `getMyProfileUseCase` and shares it with every screen. Saving the profile replaces the shared value with the profile
  the API answers, so the header updates at once. A failed load shows the error with a retry button.
- **Onboarding on `/`.** The user is in onboarding when `name` or `surname` is empty after trimming. The check lives
  in one place so a later change can read it from `GET /users/me`. While pending, the home content is replaced by a
  skeleton of placeholder sections, with a modal card on top that cannot be dismissed. The card title shows the step
  ("Paso 1 de 1 · Dinos cómo te llamas"), the body shows the step's fields, and the bottom shows Atrás (not on the
  first step), Siguiente (not on the last step) and Finalizar (last step only). Step 1 asks for Nombre and Apellidos.
  Finalizar saves them with `updateMyProfileUseCase`. While pending, every other route redirects to `/`.
- **User menu with the real user.** It shows `name surname`, the translated role and initials from the name and the
  surname. While the profile is loading, failed to load or onboarding is pending, they show as skeletons.
  **BREAKING** for the `backoffice-header` spec: the placeholder user is removed.
- **Mi perfil** in the user menu navigates to a new `/profile` screen. Preferencias stays a placeholder.
- **Edit Profile (`/profile`).** Nombre and Apellidos prefilled from the shared profile, the same validation as
  onboarding, and Guardar. Success shows a message and updates the header; failure shows the error and keeps the
  values.
- **Name rules in core.** A new `core/src/application/validation/profile-name-rules.ts` holds the limits (3 to 100
  characters after trimming) and a pure validator both fronts import. `UpdateMyProfileUseCase` trims both values,
  checks them, and fails with `VALIDATION` without calling the API when one is invalid.
  **BREAKING** for the `account-profile` spec: updating no longer sends the values unchanged.

## Capabilities

### New Capabilities

- `backoffice-current-user`: loading the signed-in user's profile once per signed-in session in the backoffice,
  sharing it across screens, replacing it after a save, the onboarding-pending status, and the load error state.
- `backoffice-onboarding`: the blocking, multi-step onboarding dialog on `/`, its first step (name and surname), the
  home skeleton behind it, and the redirect to `/` while it is pending.
- `backoffice-profile`: the Edit Profile screen at `/profile` and its form.

### Modified Capabilities

- `backoffice-header`: the user menu shows the real user from the shared profile, with skeletons instead of the
  placeholder; Mi perfil navigates to `/profile`.
- `account-profile`: updating trims name and surname and rejects values outside 3–100 characters with `VALIDATION`
  before any request; the shared name rules are exposed to both fronts.

## Fronts And Roles

- **backoffice** (`TRAINER`, `ADMIN`): every capability lands here. Both roles go through the same onboarding, see
  their own name and role, and edit their own profile. Only the role label differs. A user in onboarding must not see
  the regular home content, any placeholder name in the user menu, or any screen other than `/`. Nobody can see or
  edit another user's profile: both operations act on the session's user.
- **app** (`USER`): no UI change. It is affected only through core: its name updates now go through the same
  3–100 check.

## Canonical References

- `CurrentUserProvider` and context -> `backoffice/src/presentation/common/context/container-context.ts` and
  `container-provider.tsx`; load-on-mount with an `active` guard from `backoffice/src/presentation/auth/components/require-session.tsx`.
- `useCurrentUser` -> its current version, reworked in place.
- `useUpdateProfile` -> `backoffice/src/presentation/common/hooks/use-logout.ts`.
- `ProfileForm`, `ProfileNameFields`, `NameStep` -> `backoffice/src/presentation/auth/components/signup-form.tsx` and
  `login-form.tsx`.
- `ProfileView` and `/profile` route -> `backoffice/src/presentation/common/views/home-view.tsx` and
  `backoffice/src/app/page.tsx`.
- `UserMenu`, `AppShell`, `HomeView`, `UpdateMyProfileUseCase` -> their current versions, extended in place.
- `OnboardingDialog`, `HomeSkeleton`, `LoadError` -> **pending: this change establishes** the multi-step dialog, the
  skeleton and the first error-state component in the backoffice.
- `profile-name-rules.ts` -> **pending: this change establishes** validation rules in core's application layer.
- The app is not a UI reference. `.stpr/template/` is not updated.

## Conflicts With The Invariants

Two rules are relaxed on purpose, by the product owner's decision. The design records both. Neither is treated as a
stop case for this change:

1. **Business rules belong to the API** (and the project rule "reject any business rule in the front"). The minimum
   of 3 characters exists only in the front. The API still accepts any non-blank value up to 100 characters from
   other clients; the fronts guarantee 3–100 for their own users. The maximum (100) mirrors the API's
   `PersonName.MaxLength`.
2. **Structural validation lives in presentation.** The rules live once in `core/src/application/validation/` so
   both fronts share them, and `UpdateMyProfileUseCase` enforces them. Presentation only maps violations to
   translation keys.

Updating `STPR_INVARIANTS.md` to allow shared validation rules in core is a separate template change.

## Impact

- **API contract:** `GET /users/me` and `PUT /users/me`, both in `core/openapi/v1.json`. No API change, no snapshot
  refresh, no new domain type.
- **core:** new `application/validation/` (rules and barrel, re-exported from `application/index.ts`);
  `UpdateMyProfileUseCase` and its test. `Dependencies`, repositories and both `container.ts` are unchanged.
- **backoffice:** `AppShell`, `UserMenu`, `HomeView`, `useCurrentUser` reworked; new current-user provider and
  context, `HomeSkeleton`, `LoadError`, `onboarding/` and `profile/` areas, `/profile` route; new `onboarding` and
  `profile` translation namespaces and `common` keys in `es` and `en`; component tests and two new E2E flows; the
  existing login and signup E2E flows intercept `GET /users/me`.
- **app:** no code change.
- **Error codes:** `VALIDATION`, `UNAUTHORIZED`, `NETWORK`, `UNEXPECTED`, all already mapped and translated. No new code.
- **Dependencies:** none.
- **Accepted limitation:** a user in onboarding cannot reach logout until they finish, as requested.
- **Out of scope:** more onboarding steps, an onboarding field from the API, editing email, role, password or avatar,
  Preferencias, app screens, real home content, adding the minimum to the API.
