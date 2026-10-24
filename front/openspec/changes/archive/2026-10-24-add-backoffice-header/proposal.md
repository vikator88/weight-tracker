# Proposal

Source brief: `.stpr/plan/openspec/proposal-change-add-backoffice-header.md`.

## Why

The navigation drawer gives the backoffice its structure, but every screen also needs controls at the top: search
(a placeholder for now), notifications, language and an account menu. Settings and logout belong in that account menu,
not in the navigation. Today the language follows the browser and cannot be changed by the user.

## What Changes

- Core: add `GetPreferredLanguageUseCase` and `SetPreferredLanguageUseCase` in a new `preferences` area. They read and
  write a language code under `preferences.language` through `KeyValueStoreInterface`. `Dependencies` gains
  `getPreferredLanguageUseCase` and `setPreferredLanguageUseCase`.
- Backoffice and app: register both use cases in `container.ts` (the app only wires them, its UI does not change), and
  add both placeholders to each `render-with-providers.tsx`.
- Backoffice: `AppShell` lays out the full-height drawer on the left and, to its right, a column with a sticky header
  above the `main` content.
- Backoffice: a new `AppHeader` with:
  - a search box on the left, taking 50% of the header width, with a search icon at its start and the placeholder
    "Escribe para buscar...". It is not actionable.
  - right-aligned, from left to right: `LanguageMenu`, `AlertsMenu` (mock alerts) and `UserMenu` (mock user: name,
    role, avatar, toggle; menu with Mi perfil, Preferencias, a separator and Cerrar sesión).
- Backoffice: `NavigationDrawer` starts with a row showing the logo and the app name. Inicio becomes a regular item.
  Every item gets an icon. Configuración and Cerrar sesión leave the drawer.
- Backoffice: `AppProviders` applies the stored language at startup before falling back to browser detection, and
  keeps `<html lang>` in sync with every language change.
- Backoffice: new dependency `@mui/icons-material` 9.x.
- New `header.*` and `menu.appName` keys in the backoffice `common` namespace, in `es` and `en`. The settings and logout
  keys move from `menu.*` to `header.user.*`.
- No API change, no snapshot refresh, no new error code.

## Capabilities

### New Capabilities

- `language-preference`: the core operations that remember and recall the language the user chose on this device,
  without calling the API.
- `backoffice-header`: the header every signed-in backoffice screen shows: its placement beside the drawer, the
  search box, the alerts menu and the user menu.
- `backoffice-language`: choosing the backoffice language from the header, and which language the backoffice starts
  in.

### Modified Capabilities

These capabilities come from the in-flight change `add-backoffice-navigation-drawer`, which must be archived before
this one.

- `backoffice-navigation`: the drawer starts with the logo and app name, Inicio is a regular first item, every item has
  an icon, and Configuración and Cerrar sesión are no longer in the drawer.
- `backoffice-logout`: logout is triggered from the user menu in the header instead of the drawer, and a failure is
  shown in that menu.

## Fronts And Roles

- **backoffice**: `TRAINER` and `ADMIN` see the same header and drawer. The mock user shows the role "Entrenador". A
  visitor without a session must not see the header or the drawer, not even while the session is read. Login and
  signup have no header and no language selector.
- **app**: registers the two language use cases and changes nothing else. It has no header and no language selector.

## Canonical References

- Both language use cases and their tests: modelled on `get-session-use-case.ts` and `logout-use-case.ts` and their
  tests (only `KeyValueStoreInterface`, no API call, `UNEXPECTED` on a storage failure). The new
  `use-cases/preferences` folder mirrors `use-cases/auth`.
- Both `container.ts` and both `render-with-providers.tsx`: extended in place.
- `AppHeader`, `HeaderSearch`, `LanguageMenu`, `AlertsMenu`, `UserMenu`, `useCurrentUser`, `useAlerts`: no header
  reference exists; **this change establishes it**, with MUI only.
- `UserMenu` logout: the drawer's logout entry from `add-backoffice-navigation-drawer` (`useLogout`,
  `router.replace("/login")`, `errors:<code>`), moved.
- `NavigationDrawer`, `AppShell`: their versions from `add-backoffice-navigation-drawer`, extended.
- Tests: `navigation-drawer.test.tsx`, `home-view.test.tsx`, `e2e/login.spec.ts` of the backoffice. The app is not a
  UI reference. `.stpr/template/` is not updated.

## Constraints And Conflicts

- **Invariant refinement (language).** `STPR_INVARIANTS.md` says the initial language is detected from the device or
  browser. With this change a stored user choice comes first, and detection is the fallback. This is the only
  deviation; design.md records it. Updating the invariant is a separate template change.
- **No user endpoint.** The API has no user name and no `/me`. The user is a mock behind one hook. Real data needs an
  API change planned in `api/` first.
- **Composition root boundary.** Only `AppProviders` may import from `src/infrastructure`. So the supported-language
  filter at startup and the `<html lang>` sync live there, and the header reaches i18n only through `useTranslation()`.
- **Archive order.** `backoffice-navigation` and the drawer version of `backoffice-logout` exist only in
  `add-backoffice-navigation-drawer`. Archive that change first.
- No business rule, no form. No other conflict with the template, the invariants or a stop case.

## API Contract

None used. No snapshot refresh, no API change.

## Acceptance Criteria

- With a session, `/` shows the full-height drawer on the left and, to its right, the header above the home content.
- Without a session, or while the session is read, neither the header nor the drawer is rendered.
- The search box shows its icon and the placeholder "Escribe para buscar...", takes half the header width, and typing
  triggers nothing.
- The controls appear right-aligned as language, alerts, user. The user shows "Usuario de prueba", "Entrenador" and
  the initials "UP".
- The user menu lists Mi perfil, Preferencias, a separator and Cerrar sesión. Logging out ends on `/login`. A
  failure shows "Ha ocurrido un error inesperado." in the menu.
- The alerts badge shows 3, and the dropdown lists the three mock alerts.
- Choosing English switches every visible string to English at once. After a reload the backoffice is still in
  English. Without a stored choice, the browser language is used, as today.
- The drawer shows the logo with "Weight Tracker" first, then Inicio and every item with an icon. It no longer shows
  Configuración, Mi perfil, Preferencias or Cerrar sesión.
- Every new string exists in `es` and `en`.
- `pnpm typecheck && pnpm lint && pnpm test && pnpm test:e2e` passes from `front/`, with the API mocked in every tier.

## Impact

- `core`: new `application/use-cases/preferences/` (two use cases, constant, barrel), `application/use-cases/index.ts`,
  `application/dependencies.ts`, tests.
- `backoffice`: `src/infrastructure/container.ts`, `src/presentation/common/components` (`app-providers.tsx`,
  `app-shell.tsx`, `navigation-drawer.tsx`, new `app-header.tsx`, `header-search.tsx`, `language-menu.tsx`,
  `alerts-menu.tsx`, `user-menu.tsx`), `src/presentation/common/hooks` (new `use-current-user.ts`, `use-alerts.ts`),
  `src/i18n/{es,en}/common.json`, `package.json`, `test/`, `e2e/login.spec.ts`.
- `app`: `src/infrastructure/container.ts`, `test/support/render-with-providers.tsx`.
- New dependency: `@mui/icons-material` in `backoffice`.
