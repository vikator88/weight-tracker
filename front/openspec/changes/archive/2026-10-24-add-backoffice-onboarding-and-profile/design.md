# Design

## Context

See `proposal.md` for the motivation and `specs/` for the required behaviour.

- Core already has `getMyProfileUseCase` and `updateMyProfileUseCase` in `Dependencies`, registered in both
  `container.ts`. `UpdateMyProfileUseCase` forwards its request unchanged.
- In the backoffice, every signed-in page renders its own `AppShell` (`RequireSession` → drawer, header, `main`).
  Next remounts a page's tree on navigation, so state held inside `AppShell` does not survive going from `/` to
  `/profile`. Only `AppProviders`, rendered by the root layout, survives navigation.
- `useCurrentUser` returns a constant mock; `UserMenu` is its only caller.
- The contract declares no length limits on `UpdateProfileRequest`. The API trims and accepts 1–100 characters.

## Goals / Non-Goals

**Goals:**

- One profile read per signed-in visit, shared by every signed-in screen.
- One place that decides "onboarding pending", replaceable by an API field later.
- An onboarding dialog whose step list can grow without changing the dialog.
- Name rules defined once in core and enforced by the use case, for both fronts.

**Non-Goals:**

- Session refresh on `UNAUTHORIZED` (no session hook exists in the backoffice yet; a failed load just shows the error).
- Any app UI.
- Updating `STPR_INVARIANTS.md` or `.stpr/template/`.

## Decisions

### 1. Name rules live in core's application layer (recorded deviation)

`core/src/application/validation/profile-name-rules.ts`:

```ts
export const PROFILE_NAME_MIN_LENGTH = 3;
export const PROFILE_NAME_MAX_LENGTH = 100;
export type ProfileNameViolation = "required" | "minLength" | "maxLength";
export function validateProfileName(value: string): ProfileNameViolation | null;
export function validateUpdateProfileRequest(request: UpdateProfileRequest): {
  name: ProfileNameViolation | null;
  surname: ProfileNameViolation | null;
};
```

All functions trim before measuring. Exported through `core/src/application/validation/index.ts` and
`core/src/application/index.ts`, so presentation imports them from `@weight-tracker/core/application` (already an
allowed import; `infrastructure` stays forbidden).

`UpdateMyProfileUseCase.execute` trims both values, calls `validateUpdateProfileRequest`, and when either field has a
violation returns `fail(new DomainError("VALIDATION", "<field>: <violation>"))` without calling the repository.
Otherwise it calls `updateMe` with the trimmed request.

- **Deviation 1 — business rule in the front.** The minimum of 3 is not an API rule. Decided by the product owner;
  the API may still receive shorter names from other clients. The maximum mirrors `PersonName.MaxLength` (100).
- **Deviation 2 — validation outside presentation.** The invariants place structural validation in presentation.
  Here the rules are shared by both fronts and enforced by the use case; presentation only maps a
  `ProfileNameViolation` to `profile:validation.<violation>` with `{ min, max }` interpolated from the constants.
- **Alternatives:** per-front presentation validators (duplicated limits, drift risk between fronts); an API change
  adding the minimum (rejected by the product owner for this version). Changing the invariants to allow shared
  validation rules is a separate template change.
- The `account-profile` sentence "It SHALL NOT validate, trim…" is replaced (see its delta).

### 2. The shared profile lives in `AppProviders`, loaded on demand by `AppShell`

The brief placed `CurrentUserProvider` inside `AppShell`. Because each page renders its own `AppShell`, that would
reload the profile on every navigation and break the "loaded once" requirement. Instead:

- `CurrentUserProvider` (`presentation/common/components/current-user-provider.tsx`) is mounted in `AppProviders`,
  inside `ContainerProvider`. It starts idle and does not call the API by itself.
- Its value (`CurrentUserContext` in `presentation/common/context/current-user-context.ts`):
  `{ profile, status: "idle" | "loading" | "loaded" | "failed", errorCode, load, reload, replace, clear }`.
  - `load()` reads the profile only when `status` is `idle` (no-op otherwise).
  - `reload()` reads it again (used by Reintentar).
  - `replace(profile)` sets the profile returned by an update.
  - `clear()` returns to `idle` with no profile.
  - Reads use the `active` guard pattern from `RequireSession` so a late answer after `clear()` is ignored.
- `AppShell` renders an inner `SignedInContent` inside `RequireSession`; it calls `load()` on mount. So the read
  happens only after the session is confirmed, once per visit.
- `UserMenu` calls `clear()` after a successful logout, before `router.replace("/login")`. A page reload discards it
  anyway.
- `useCurrentUser` (reworked in place) reads the context and adds the derived
  `onboardingPending = status === "loaded" && (profile.name.trim() === "" || profile.surname.trim() === "")`. This is
  the only place that rule exists.
- **Alternative:** a `layout.tsx` for a signed-in route group holding `AppShell`. Rejected for this change: it moves
  routes and `RequireSession` and touches the login, signup and drawer specs. Revisit if more shared signed-in state
  appears.

### 3. Onboarding gating

- `HomeView` decides what `/` shows from `useCurrentUser`:
  - `idle`/`loading` → `HomeSkeleton`;
  - `failed` → `HomeSkeleton` + `LoadError` (`errors:<code>`, Reintentar → `reload`);
  - `onboardingPending` → `HomeSkeleton` + `OnboardingDialog`;
  - otherwise → the current home content.
- `SignedInContent` in `AppShell` redirects with `router.replace("/")` when `onboardingPending` and
  `usePathname() !== "/"`, rendering nothing meanwhile. This covers `/profile` and every future signed-in route.
- The header and drawer stay rendered; the MUI `Dialog` is modal, so its backdrop and focus trap make them unusable.
  `disableEscapeKeyDown` plus an `onClose` that ignores `backdropClick` keep it open. No close button.

### 4. Step model

`presentation/onboarding/hooks/use-onboarding.ts` takes an ordered `OnboardingStep[]`:

```ts
interface OnboardingStep {
  id: string;
  titleKey: string; // onboarding:steps.<id>.title
  Body: ComponentType<OnboardingStepProps>; // renders the fields, receives values, errors, onChange
  validate: (
    values: OnboardingValues,
  ) => Partial<Record<keyof OnboardingValues, ProfileNameViolation>>;
}
```

The hook holds `index`, `values` (`{ name, surname }` for now) and per-field errors, and exposes `isFirst`, `isLast`,
`back`, `next` (validates the current step first) and `finish` (validates, calls `useUpdateProfile().save`, which
calls `replace` on success). `OnboardingDialog` receives the steps as a prop with the production list as default, so
tests can pass a two-step fake list. The production list is `[nameStep]`. `NameStep` renders `ProfileNameFields`.

### 5. Forms

- `ProfileNameFields` (`presentation/profile/components/`) renders the two MUI `TextField`s with `error` and
  `helperText`, as `signup-form.tsx` does, and maps violations to translated text. Shared by `NameStep` and
  `ProfileForm` (same front, same area of concern).
- `useUpdateProfile` (`presentation/profile/hooks/`) follows `use-logout.ts`: `saving`, `errorCode`, `saved`, and
  `save(request): Promise<boolean>`; on success it calls `replace(result.value)` and sets `saved`. `clearSaved()` is
  called by the form on field change.
- `ProfileForm` initialises its values from `profile` once; it validates on submit with `validateProfileName`, then
  calls `save`. Submit is disabled while `saving`. Errors show as `errors:<code>` in an `Alert` above the button;
  success as an `Alert severity="success"` with `profile:saved`.
- `ProfileView` = `AppShell` + `Container` + title + `ProfileForm`, rendered only when `profile` is loaded (skeleton
  otherwise). Route `src/app/profile/page.tsx` re-exports it, like `src/app/page.tsx`. Static export unchanged.

### 6. User menu

- Name: `${profile.name} ${profile.surname}`; role: `header.user.roles.<role>`; initials: first letter of name +
  first letter of surname, upper-cased. The old `initials(name)` helper is replaced.
- When `status !== "loaded"` or `onboardingPending`: `Skeleton` text for name and role and a circular `Skeleton`
  for the avatar, with `aria-label` `common:loading`. The toggle and menu still work.
- "Mi perfil" closes the menu and calls `router.push("/profile")`.
- The `CurrentUser` interface and the mock constant are removed.

### 7. Translations

New namespaces `onboarding` and `profile` in `backoffice/src/i18n/{es,en}/`, registered in `src/i18n/resources.ts`.
New `common` keys `loading` and `loadError.retry`. Keys and text are those listed in the brief. Error codes used:
`VALIDATION`, `UNAUTHORIZED`, `NETWORK`, `UNEXPECTED` — all already in `errors.json` for both languages. No new code
in the HTTP error mapper. The parity test covers the new files once they are in `resources`.

### 8. Tests

- **core:** `core/test/application/validation/profile-name-rules.test.ts`, structured like the use case tests;
  `update-my-profile-use-case.test.ts` gains trim and rejection cases (the fake repository records `updates`; an
  invalid request leaves it empty).
- **backoffice component:** `render-with-providers.tsx` must mount `CurrentUserProvider` (it mirrors `AppProviders`).
  Fake `getMyProfileUseCase` / `updateMyProfileUseCase` return chosen `Result`s. Files: `home-view.test.tsx`
  (extended), `user-menu.test.tsx` (extended), new `onboarding-dialog.test.tsx` and `profile-view.test.tsx`. Every
  test uses Arrange / Act / Assert with one Act.
- **backoffice E2E:** new `onboarding.spec.ts` and `profile.spec.ts`; `login.spec.ts` and `signup.spec.ts` add a
  `page.route` for `GET /users/me` answering a complete profile so the dialog does not block them.

## Risks / Trade-offs

- [Front-only minimum diverges from the API] → Documented as a deviation; the use case enforces it for both fronts.
  If the API adopts it later, the core rule remains as fast feedback.
- [User in onboarding cannot log out] → Accepted by the product owner. A "Cerrar sesión" action in the dialog
  reusing `useLogout` is the fallback if this becomes a problem.
- [Provider in `AppProviders` outlives the session] → `clear()` on logout and the `active` guard on late answers;
  reads only happen behind `RequireSession`.
- [Redirect flashes the target page] → `SignedInContent` renders nothing while redirecting.
- [Existing E2E flows break when home starts reading `/users/me`] → Updated in the same change.

## Migration Plan

No data migration. Deploy is the normal static export. Rollback is reverting the change; the API is untouched.
