# Tasks

## 0. Prerequisite

- [x] 0.1 Confirm `add-logout` is archived (`openspec/specs/backoffice-logout/spec.md` exists); if not, archive it first; verify `openspec validate add-backoffice-navigation-drawer --strict` still passes

## 1. Backoffice copy and asset

- [x] 1.1 Add every `menu.*` key from design.md to `backoffice/src/i18n/es/common.json` and `en/common.json`, and remove `home.logout` from both; verify `test/i18n/translation-parity.test.ts` passes
- [x] 1.2 Add the placeholder `backoffice/public/logo.svg`; verify `pnpm --filter backoffice build` copies it to the export output as `logo.svg`

## 2. Logout hook

- [x] 2.1 Create `backoffice/src/presentation/common/hooks/use-logout.ts` (`useLogout` → `{ loggingOut, errorCode, logout }`, `logout()` returns `true` on success, stores the code and returns `false` on failure, never navigates); verify with `pnpm --filter backoffice typecheck`

## 3. Navigation drawer

- [x] 3.1 Read the Next 16 `next/link`, `usePathname` and `useRouter` guides in `backoffice/node_modules/next/dist/docs/` and the MUI 9 `Drawer` slot props, then create `backoffice/src/presentation/common/components/navigation-drawer.tsx` as in design.md: permanent 260px drawer whose paper is a `nav` named `menu.label`, logo + Inicio link selected on `/`, the five sections as `ListSubheader` + placeholder `ListItemButton`s from a module-level constant, a `Divider`, and the Cerrar sesión entry using `useLogout`, `router.replace("/login")` on success and an `Alert` with `errors:<code>` on failure; verify with the backoffice typecheck and lint
- [x] 3.2 Add `backoffice/test/presentation/common/components/navigation-drawer.test.tsx`, following `home-view.test.tsx`: items in order with the logo alt and the named `nav`; Inicio `href="/"` and selected; header and placeholder clicks call no use case and no `replace`; logout success → `replace("/login")`; disabled while pending; `UNEXPECTED` shows its message without navigating; AAA with one Act each; verify with `pnpm --filter backoffice test`

## 4. App shell and home

- [x] 4.1 Create `backoffice/src/presentation/common/components/app-shell.tsx` (`RequireSession` around a flex `Box` with `NavigationDrawer` and a `main` `Box` holding `children`); verify with the backoffice typecheck
- [x] 4.2 Change `HomeView` to wrap itself in `AppShell` instead of `RequireSession`, drop its `main` container and its logout button, state and imports; verify with the backoffice typecheck and lint
- [x] 4.3 Update `backoffice/test/presentation/common/views/home-view.test.tsx`: add `usePathname` to the `next/navigation` mock, remove the four logout cases, assert the "Menú principal" navigation is shown with a session and absent with no session, with a read failure and while reading; verify with `pnpm --filter backoffice test`

## 5. E2E

- [x] 5.1 Add a flow to `backoffice/e2e/login.spec.ts`: log in with `POST /auth/login` intercepted, click "Cerrar sesión" in the drawer, end on `/login`; open `/` and end on `/login` with no "Menú principal" navigation; verify with `pnpm test:e2e`

## 6. Review and verification

- [x] 6.1 Review the backoffice against `require-session.tsx`, the `add-logout` handling it replaces and `login-view.tsx` (naming, client components only, no server code, i18n in `es` and `en`, no layout file added under `src/app`); verify the quality checklist in `front/AGENTS.md` holds for the backoffice
- [x] 6.2 Check `.stpr/template/` has not drifted with the loop in `front/AGENTS.md`; verify it prints no `DRIFT:` line
- [x] 6.3 Run `pnpm typecheck && pnpm lint && pnpm test && pnpm test:e2e` from `front/`; verify it passes

## Workflow follow-up

- Replace `backoffice/public/logo.svg` with the final logo (same file name).
- Archive the change after review (`/opsx:archive`), after `add-logout`.
