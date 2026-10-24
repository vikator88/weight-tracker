# Tasks

## 1. Core name rules and use case guard

- [x] 1.1 Create `core/src/application/validation/profile-name-rules.ts` (`PROFILE_NAME_MIN_LENGTH`, `PROFILE_NAME_MAX_LENGTH`, `ProfileNameViolation`, `validateProfileName`, `validateUpdateProfileRequest`, trimming before measuring) and `validation/index.ts`, re-exported from `core/src/application/index.ts`; verify with `pnpm --filter @weight-tracker/core typecheck`
- [x] 1.2 Add `core/test/application/validation/profile-name-rules.test.ts` following the structure of `core/test/application/use-cases/users/update-my-profile-use-case.test.ts`: `""` and `"   "` → `required`, 2 → `minLength`, 3 and 100 → valid, 101 → `maxLength`, `"  ab  "` → `minLength`, per-field result of `validateUpdateProfileRequest`; AAA with one Act each; verify with `pnpm --filter @weight-tracker/core test`
- [x] 1.3 Extend `UpdateMyProfileUseCase` to trim both values, fail with `VALIDATION` without calling the repository when either breaks the rules, and send the trimmed request otherwise; update its doc comment; verify with the core typecheck
- [x] 1.4 Extend `update-my-profile-use-case.test.ts`: trimmed values reach the repository, an invalid name and an invalid surname each fail with `VALIDATION` and leave `updates` empty; existing cases still pass; verify with `pnpm --filter @weight-tracker/core test`
- [x] 1.5 Review core against its references (use case tests, existing use case shape) and the recorded deviations in `design.md`; confirm `Dependencies`, repositories and both `container.ts` are unchanged (`git diff --stat core app`)

## 2. Backoffice shared profile

- [x] 2.1 Read the `usePathname`, `useRouter` and `next/link` guides in `backoffice/node_modules/next/dist/docs/` and note anything that changes the design's use of them
- [x] 2.2 Create `presentation/common/context/current-user-context.ts` and `presentation/common/components/current-user-provider.tsx` (`status`, `profile`, `errorCode`, `load` no-op unless idle, `reload`, `replace`, `clear`, `active` guard), following `container-context.ts` / `container-provider.tsx` and `require-session.tsx`; mount it in `AppProviders` inside `ContainerProvider`; verify with `pnpm --filter backoffice typecheck`
- [x] 2.3 Rework `presentation/common/hooks/use-current-user.ts` to read the context and derive `onboardingPending`; remove the `CurrentUser` mock; verify with the backoffice typecheck
- [x] 2.4 Mount `CurrentUserProvider` in `backoffice/test/support/render-with-providers.tsx` with fake `getMyProfileUseCase` / `updateMyProfileUseCase` results the tests can choose; verify existing backoffice tests still pass (`pnpm --filter backoffice test`)
- [x] 2.5 In `AppShell`, add `SignedInContent` inside `RequireSession` that calls `load()` on mount and redirects to `/` with `router.replace` (rendering nothing) while `onboardingPending` on another path; verify with the backoffice typecheck

## 3. Backoffice common components and user menu

- [x] 3.1 Add `common` keys `loading` and `loadError.retry` in `src/i18n/es/common.json` and `en/common.json`; verify the translation parity test passes
- [x] 3.2 Create `presentation/common/components/home-skeleton.tsx` (three stat cards, a wide block, five list rows, all MUI `Skeleton`, labelled `common:loading`) and `load-error.tsx` (`errors:<code>` `Alert` + Reintentar button calling a prop); verify with the backoffice typecheck
- [x] 3.3 Update `UserMenu`: `name surname`, translated role, initials from name and surname, skeletons when not loaded or onboarding pending, Mi perfil → `router.push("/profile")`, `clear()` after successful logout before `router.replace("/login")`; verify with the backoffice typecheck
- [x] 3.4 Extend `backoffice/test/presentation/common/components/user-menu.test.tsx`: "Ana García", "Entrenador", "AG"; `ADMIN` → "Administrador"; skeletons and no name text while loading, on failure and in onboarding; Mi perfil pushes `/profile`; Preferencias does nothing; logout still replaces to `/login`; AAA with one Act each; verify with `pnpm --filter backoffice test`
- [x] 3.5 Update any `app-header.test.tsx` expectations that relied on "Usuario de prueba"; verify with `pnpm --filter backoffice test`

## 4. Backoffice profile area

- [x] 4.1 Add `profile` namespace in `src/i18n/es/profile.json` and `en/profile.json` with the keys from the brief and register it in `src/i18n/resources.ts`; verify the parity test passes
- [x] 4.2 Create `presentation/profile/components/profile-name-fields.tsx` (two `TextField`s with `error`/`helperText`, violations mapped to `profile:validation.<violation>` with `{ min, max }` from core) following `signup-form.tsx`; verify with the backoffice typecheck
- [x] 4.3 Create `presentation/profile/hooks/use-update-profile.ts` following `use-logout.ts` (`saving`, `errorCode`, `saved`, `save`, `clearSaved`; `replace` on success); verify with the backoffice typecheck
- [x] 4.4 Create `presentation/profile/components/profile-form.tsx` and `presentation/profile/views/profile-view.tsx` (inside `AppShell`, skeleton until loaded), and the route `src/app/profile/page.tsx` re-exporting the view like `src/app/page.tsx`; verify with the backoffice typecheck and `pnpm --filter backoffice build` (static export still succeeds)
- [x] 4.5 Add `backoffice/test/presentation/profile/views/profile-view.test.tsx` following `home-view.test.tsx`: prefilled fields; "Al" → min-length message and no update; " Lucía " → update with "Lucía", "Perfil actualizado." and header "Lucía García"; typing clears the message; `VALIDATION` and `NETWORK` messages with values kept; Guardar disabled while saving; redirect to `/` in onboarding; AAA with one Act each; verify with `pnpm --filter backoffice test`

## 5. Backoffice onboarding area and home

- [x] 5.1 Add `onboarding` namespace in `src/i18n/es/onboarding.json` and `en/onboarding.json` with the keys from the brief and register it in `src/i18n/resources.ts`; verify the parity test passes
- [x] 5.2 Create `presentation/onboarding/hooks/use-onboarding.ts` (step list, index, values, errors, `isFirst`, `isLast`, `back`, `next`, `finish` through `useUpdateProfile`) and `presentation/onboarding/components/name-step.tsx` (renders `ProfileNameFields`); verify with the backoffice typecheck
- [x] 5.3 Create `presentation/onboarding/components/onboarding-dialog.tsx` (MUI `Dialog` with `disableEscapeKeyDown`, `onClose` ignoring backdrop clicks, title `onboarding:stepTitle`, Atrás / Siguiente / Finalizar by position, `errors:<code>` alert, Finalizar disabled while saving; `steps` prop defaulting to `[nameStep]`); verify with the backoffice typecheck
- [x] 5.4 Add `backoffice/test/presentation/onboarding/components/onboarding-dialog.test.tsx`: title "Paso 1 de 1 · Dinos cómo te llamas"; only Finalizar shown; required, min and max messages with no update; trimmed values sent; Finalizar disabled while saving; `NETWORK` failure keeps the dialog and values; Escape and backdrop do not close it; a two-step fake list covers Siguiente (blocked when invalid), Atrás keeping values, and "Paso 2 de 2"; AAA with one Act each; verify with `pnpm --filter backoffice test`
- [x] 5.5 Update `HomeView`: skeleton while idle/loading, skeleton + `LoadError` on failure, skeleton + `OnboardingDialog` while pending, current content otherwise; verify with the backoffice typecheck
- [x] 5.6 Extend `backoffice/test/presentation/common/views/home-view.test.tsx`: loading skeleton with no content; `NETWORK` failure shows the message and Reintentar, retry reads again and shows content; empty name and whitespace-only surname each show the dialog and hide the content; complete profile shows content and no dialog; Finalizar success closes the dialog and the header shows the name; one profile read per render; existing session cases still pass; AAA with one Act each; verify with `pnpm --filter backoffice test`
- [x] 5.7 Review the backoffice against its mapped references (`container-provider.tsx`, `require-session.tsx`, `use-logout.ts`, `signup-form.tsx`, `home-view.tsx`, `page.tsx`) and the patterns this change establishes (dialog, skeleton, load error); confirm presentation imports no `@weight-tracker/core/infrastructure` and every string is in `es` and `en`

## 6. Backoffice E2E

- [x] 6.1 In `backoffice/e2e/login.spec.ts` and `signup.spec.ts`, intercept `GET /users/me` with a complete profile so existing flows are not blocked; verify with `pnpm test:e2e`
- [x] 6.2 Add `backoffice/e2e/onboarding.spec.ts`: sign in, `GET /users/me` answers an empty name, fill Nombre and Apellidos, Finalizar (`PUT /users/me` intercepted), dialog closes and the header shows the name; verify with `pnpm test:e2e`
- [x] 6.3 Add `backoffice/e2e/profile.spec.ts`: signed in with a complete profile, open the user menu → Mi perfil, change Nombre, Guardar, "Perfil actualizado." and the header updates; verify with `pnpm test:e2e`

## 7. Integration checks

- [x] 7.1 Run the `.stpr/template/` drift check from `AGENTS.md` and confirm no `DRIFT:` lines
- [x] 7.2 Run `pnpm typecheck && pnpm lint && pnpm test && pnpm test:e2e` from `front/` and confirm every command passes

## Workflow follow-up

- Leave the `<!-- BEGIN:nextjs-agent-rules -->` block in `backoffice/AGENTS.md` if `next dev` rewrites it, and commit it.
- Plan the template change that allows shared validation rules in core in `STPR_INVARIANTS.md`.
- Archive the change after review.
