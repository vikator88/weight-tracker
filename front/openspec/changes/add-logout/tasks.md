# Tasks

## 1. Core logout operation

- [x] 1.1 Create `core/src/application/use-cases/auth/logout-use-case.ts` (`LogoutUseCase(keyValueStore)`, `UseCaseInterface<void, void>`, which removes `REFRESH_TOKEN_KEY` then `ACCESS_TOKEN_KEY` imported from `signup-use-case.ts` and returns `UNEXPECTED` if a removal throws), export it from the use-case barrels, and add `logoutUseCase: UseCaseInterface<void, void>` to `Dependencies`; verify with `pnpm --filter @weight-tracker/core typecheck`
- [x] 1.2 Add `core/test/application/use-cases/auth/logout-use-case.test.ts`, following `get-session-use-case.test.ts`: both keys removed and `ok` returned, `ok` with nothing stored, `UNEXPECTED` when `remove` rejects; AAA with one Act each; verify with `pnpm --filter @weight-tracker/core test`
- [x] 1.3 Review core against `get-session-use-case.ts` and its test (same shape, no throw, no API call, no business rule); verify that no difference remains other than removing instead of reading

## 2. Backoffice wiring and translations

- [x] 2.1 Register `logoutUseCase: (c) => new LogoutUseCase(c.resolve("keyValueStore"))` in `backoffice/src/infrastructure/container.ts`, and add a `logoutUseCase` placeholder in `backoffice/test/support/render-with-providers.tsx`; verify with `pnpm --filter backoffice typecheck` and `test/infrastructure/container.test.ts`
- [x] 2.2 Add `home.logout` to `backoffice/src/i18n/es/common.json` ("Cerrar sesión") and `en/common.json` ("Log out"); verify `test/i18n/translation-parity.test.ts` passes

## 3. Backoffice logout button

- [x] 3.1 Read the Next 16 `useRouter` (`next/navigation`) guide in `backoffice/node_modules/next/dist/docs/`, then add to `backoffice/src/presentation/common/views/home-view.tsx`, inside `RequireSession`, an MUI outlined `Button` with `t("home.logout")`, disabled while logging out, that calls `logoutUseCase`, `router.replace("/login")` on success, and shows `<Alert severity="error">` with `errors:<code>` on failure; verify with the backoffice typecheck and lint
- [x] 3.2 Extend `backoffice/test/presentation/common/views/home-view.test.tsx`: the button shows with a session; click calls the use case and `replace("/login")`; button disabled while pending; `UNEXPECTED` shows "Ha ocurrido un error inesperado." and `replace` is not called; the no-session case asserts the button is absent; AAA with one Act each; verify with `pnpm --filter backoffice test`
- [x] 3.3 Review the backoffice against its `login-view.tsx` handler and tests (state names, error rendering, i18n, no server code); verify the quality checklist in `front/AGENTS.md` holds for the backoffice

## 4. App wiring and translations

- [x] 4.1 Register `logoutUseCase: (c) => new LogoutUseCase(c.resolve("keyValueStore"))` in `app/src/infrastructure/container.ts`, and add a `logoutUseCase` placeholder in `app/test/support/render-with-providers.tsx`; verify with `pnpm --filter app typecheck` and the app container test if present
- [x] 4.2 Add `home.logout` to `app/src/i18n/es/common.json` ("Cerrar sesión") and `en/common.json` ("Log out"); verify the app translation parity test passes

## 5. App logout button

- [x] 5.1 Add to `app/src/presentation/common/views/home-view.tsx` a Paper outlined `Button` with `t("home.logout")`, `disabled` and `loading` while logging out, that calls `logoutUseCase`, then `await session.refresh()` on success with no navigation call, and shows `<HelperText type="error">` with `errors:<code>` on failure; verify with the app typecheck and lint
- [x] 5.2 Extend `app/test/presentation/common/views/home-view.test.tsx`: the button shows; press calls the use case and `getSessionUseCase` runs a second time; button disabled while pending; `UNEXPECTED` shows "Ha ocurrido un error inesperado."; AAA with one Act each; verify with `pnpm --filter app test`
- [x] 5.0 Add `redirectTo="/login"` to the `(app)` guard in `app/src/app/_layout.tsx` so a signed-out athlete on `/` lands on login instead of `+not-found` (see design.md); verify with the app typecheck
- [ ] 5.3 Check by hand on a running app (`pnpm dev:app`) that logging out shows login, and that login is still shown after a restart
- [x] 5.4 Review the app against its `login-view.tsx` handler and tests (state names, `session.refresh()` without navigation, error rendering, i18n); verify the quality checklist in `front/AGENTS.md` holds for the app

## 6. Verification

- [x] 6.1 Check `.stpr/template/` has not drifted with the loop in `front/AGENTS.md`; verify it prints no `DRIFT:` line
- [x] 6.2 Run `pnpm typecheck && pnpm lint && pnpm test` from `front/`; verify it passes

## Workflow follow-up

- Archive the change after review (`/opsx:archive`).
