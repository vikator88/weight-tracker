# Tasks

## 0. Prerequisite

- [x] 0.1 Archive `add-backoffice-navigation-drawer` first (its main specs `backoffice-navigation` and the drawer version of `backoffice-logout` must exist); verify `openspec validate add-backoffice-header --strict` reports no "Archive would refuse" note

## 1. Core language preference

- [x] 1.1 Create `core/src/application/use-cases/preferences/` with `get-preferred-language-use-case.ts` (exports `PREFERRED_LANGUAGE_KEY = "preferences.language"`; `UseCaseInterface<void, string | null>`; `UNEXPECTED` if the store throws), `set-preferred-language-use-case.ts` (`UseCaseInterface<string, void>`; `UNEXPECTED` if the store throws) and an `index.ts` barrel; re-export it from `use-cases/index.ts`; add `getPreferredLanguageUseCase` and `setPreferredLanguageUseCase` to `Dependencies`; verify with `pnpm --filter @weight-tracker/core typecheck`
- [x] 1.2 Add `core/test/application/use-cases/preferences/get-preferred-language-use-case.test.ts` (stored value, none, `UNEXPECTED`) and `set-preferred-language-use-case.test.ts` (stored under `preferences.language`, `UNEXPECTED`), following `get-session-use-case.test.ts`; AAA with one Act each; verify with `pnpm --filter @weight-tracker/core test`
- [x] 1.3 Review core against `get-session-use-case.ts`, `logout-use-case.ts` and their tests (same shape, no throw, no API call, no supported-language check); verify `LogoutUseCase` is unchanged so the preference survives logout

## 2. Wiring in both fronts

- [x] 2.1 Register both use cases with `(c) => new …(c.resolve("keyValueStore"))` in `backoffice/src/infrastructure/container.ts` and `app/src/infrastructure/container.ts`, and add placeholders (`get…` → `ok(null)`, `set…` → `ok(undefined)`) to both `test/support/render-with-providers.tsx`; verify `pnpm typecheck` passes for `backoffice` and `app` (apart from the pre-existing app form errors) and `pnpm --filter app test` still passes

## 3. Backoffice dependency and copy

- [x] 3.1 Add `@mui/icons-material` matching `@mui/material` 9.5.x with `pnpm --filter backoffice add`; verify `pnpm install` resolves a 9.x version and `pnpm --filter backoffice build` succeeds
- [x] 3.2 In `backoffice/src/i18n/{es,en}/common.json` add `menu.appName` and every `header.*` key from design.md, move `menu.settings.profile`, `menu.settings.preferences` and `menu.logout` to `header.user.*`, and remove `menu.settings` and `menu.logoAlt`; verify `test/i18n/translation-parity.test.ts` passes

## 4. Startup language

- [x] 4.1 Change `AppProviders`: subscribe to `languageChanged` to keep `document.documentElement.lang` in sync (unsubscribe on cleanup), then apply the stored language when `getPreferredLanguageUseCase` returns a supported code, otherwise `detectLanguage()`; verify with the backoffice typecheck and lint
- [x] 4.2 Add `backoffice/test/presentation/common/components/app-providers.test.tsx`, with `@/infrastructure/container` mocked to a `Container` of fakes and `navigator.languages` stubbed: stored `en` wins over a Spanish browser; no stored value gives the browser's `en`; stored `fr` gives the browser language; `<html lang>` follows; AAA with one Act each; verify with `pnpm --filter backoffice test`

## 5. Header

- [x] 5.1 Create `use-current-user.ts` (mock `{ name: "Usuario de prueba", role: "TRAINER" }`) and `use-alerts.ts` (the three `header.alerts.mock.*` keys) in `backoffice/src/presentation/common/hooks`; verify with the backoffice typecheck
- [x] 5.2 Create `header-search.tsx` (uncontrolled `TextField`, 50% width, placeholder, `aria-label`, start `SearchOutlined`) after checking the MUI 9 `TextField` slot props; and `app-header.tsx` (sticky `AppBar`, `Toolbar`, search, spacer, then `LanguageMenu`, `AlertsMenu`, `UserMenu`); verify with the backoffice typecheck and lint
- [x] 5.3 Create `language-menu.tsx` (current code, menu with Español / English, current one selected; on pick `i18n.changeLanguage` then `setPreferredLanguageUseCase`, result ignored); verify with the backoffice typecheck and lint
- [x] 5.4 Create `alerts-menu.tsx` (bell with `Badge` of the alert count, menu with title and the mock alerts; a click closes it); verify with the backoffice typecheck and lint
- [x] 5.5 Create `user-menu.tsx` (name, role, `Avatar` with initials, toggle; menu Mi perfil, Preferencias, `Divider`, Cerrar sesión via `useLogout`, `router.replace("/login")` on success, `Alert` with `errors:<code>` on failure, entry disabled while in flight); verify with the backoffice typecheck and lint
- [x] 5.6 Add `app-header.test.tsx`, `language-menu.test.tsx`, `alerts-menu.test.tsx` and `user-menu.test.tsx` under `backoffice/test/presentation/common/components/` with the cases in design.md (language test resets to `es` at the end); AAA with one Act each; verify with `pnpm --filter backoffice test`

## 6. Layout and drawer

- [x] 6.1 Change `AppShell` to a flex row of `NavigationDrawer` and a flex column holding `AppHeader` and the `main` box; verify with the backoffice typecheck
- [x] 6.2 Change `NavigationDrawer`: a `Toolbar` row with the decorative logo and `menu.appName` first, Inicio as a regular item with `HomeOutlined`, an icon per item from the brief, no settings section, and no logout, `useLogout`, `useRouter` or `Alert`; verify with the backoffice typecheck and lint
- [x] 6.3 Update `navigation-drawer.test.tsx` (app-name row first and not a button, Inicio first link, an icon per item, Configuración / Mi perfil / Preferencias / Cerrar sesión absent, logout cases removed) and `home-view.test.tsx` (the "Buscar" search box present with a session and absent without one, "Cerrar sesión" not shown outside the closed user menu); AAA with one Act each; verify with `pnpm --filter backoffice test`

## 7. E2E

- [x] 7.1 In `backoffice/e2e/login.spec.ts` make both logout flows open "Menú de usuario" before choosing "Cerrar sesión", and add a flow: log in, choose English, reload, see the "Type for search..." placeholder and `html[lang="en"]`; verify with `pnpm test:e2e`

## 8. Review and verification

- [x] 8.1 Review the backoffice against the drawer and logout references and the quality checklist in `front/AGENTS.md` (client components only, no `src/infrastructure` import outside `AppProviders`, no layout file under `src/app`, every string in `es` and `en`, mocks confined to `use-current-user.ts` and `use-alerts.ts`); verify each checklist item holds
- [x] 8.2 Check `.stpr/template/` has not drifted with the loop in `front/AGENTS.md`; verify it prints no `DRIFT:` line
- [x] 8.3 Run `pnpm typecheck && pnpm lint && pnpm test && pnpm test:e2e` from `front/`; verify it passes

## Workflow follow-up

- Archive after `add-backoffice-navigation-drawer` (`/opsx:archive`).
- Record the language-startup refinement in `STPR_INVARIANTS.md` as a separate template change.
- Replace `useCurrentUser` and `useAlerts` with real data once the API exposes them (an `api/` change first).
