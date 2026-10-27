# Tasks

## 1. Rules and atom inventory

- [x] 1.1 Amend `.stpr/template/STPR_INVARIANTS.md`: add `Container` to the layout primitives allowed at any level, let providers import the library's theme and cache providers, and allow a structural widget used by a single organism to be imported there; verify `pnpm exec prettier --check .stpr` passes
- [x] 1.2 Count, per front, the MUI / Paper widgets imported by components and views (`grep` the import lines) and fix the atom list as those used by two or more files; verify the list matches the design's expected sets or record the difference in the design

## 2. Backoffice

- [x] 2.1 Create `common/components/atoms` with one atom per widget in the list, each forwarding the widget's own props and using only theme tokens; verify `pnpm --filter backoffice typecheck`
- [x] 2.2 Create the `molecules` and `organisms` folders and move the molecules (`header-search`, `load-error`, `home-skeleton`, and `profile-name-fields` in `common`, since profile and onboarding share it) with their imports updated and widgets replaced by atoms; verify typecheck
- [x] 2.3 Move the organisms (`alerts-menu`, `language-menu`, `user-menu`, `app-header`, `navigation-drawer`, `app-shell`, `login-form`, `signup-form`, `profile-form`, `onboarding-dialog`, `name-step`) with imports updated and shared widgets replaced by atoms; keep single-use structural widgets as they are; verify typecheck
- [x] 2.4 Replace the widget imports in the four views (`home`, `login`, `signup`, `profile`) with atoms and layout primitives; verify typecheck
- [x] 2.5 Move the component tests to mirror the new paths and fix their relative support imports, changing no assertion; verify `pnpm --filter backoffice test`
- [x] 2.6 Review the backoffice: no widget import outside `atoms/` except layout primitives, provider theme imports and documented single-use organism widgets; no upward import; no file mixing levels

## 3. App

- [x] 3.1 Create `common/components/atoms` with `button`, `text`, `helper-text` and `text-input` atoms using `useTheme()` and `StyleSheet`; verify `pnpm --filter app typecheck`
- [x] 3.2 Move `login-form` and `signup-form` to `auth/components/organisms` using the atoms, and replace the widget imports in the `home` and `not-found` views; verify typecheck and `pnpm --filter app test`
- [x] 3.3 Review the app against the same checks as 2.6; `app/ios` and `app/android` stay untouched

## 4. Verification

- [x] 4.1 Check `.stpr/template/` has not drifted with the loop in `front/AGENTS.md`; verify it prints no `DRIFT:` line
- [x] 4.2 Run `pnpm typecheck && pnpm lint && pnpm test && pnpm test:e2e` from `front/`; verify it passes (the backoffice login, signup and profile flows exercise the migrated components)

## Workflow follow-up

- Commit one front per commit, atoms first inside each.
- Promoting the migrated files to `.stpr/template/` and switching the `pending` rows in its README is a separate step.
- Archive the change after review (`/opsx:archive`).
