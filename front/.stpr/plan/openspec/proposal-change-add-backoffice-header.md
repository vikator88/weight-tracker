# Change Proposal: Add Backoffice Header

Source brief for `/opsx:propose`. Artifacts go to `openspec/changes/add-backoffice-header/`.

## Change

Add a common header bar to every signed-in backoffice screen. It has a search box, a language selector, an alerts
menu and a user menu. The drawer is reworked at the same time: it gets an app-name row and an icon per item, and it
loses Configuración and logout, which move to the user menu.

## Objective

The navigation drawer gives the backoffice its structure. The header adds the controls every screen needs at the top:
search (placeholder for now), notifications, language and the account menu. Settings and logout move into the
account menu, where users expect them. The language choice persists across reloads.

## Capabilities

- **Layout.** The drawer keeps the full height on the left. The header sits on top of the content, to the right of
  the drawer, and spans the remaining width. The content goes below the header.
- **Search** (left of the header). A text box with a search icon at its start and the placeholder "Escribe para
  buscar..." ("Type for search..." in English). It takes 50% of the header width. It is not actionable: typing
  changes the text and nothing else.
- **Right-aligned controls**, from right to left:
  - **User menu** (rightmost). The user's name with the role below it, a circular avatar with the initials, and a
    toggle button that opens the menu. The menu holds Mi perfil and Preferencias (the old Configuración items, still
    placeholders), a separator, and Cerrar sesión. The user is a mock (see Notes).
  - **Alerts**. A bell icon with a badge showing the mock alert count. It opens a dropdown with a title and three mock
    alerts. Clicking an alert closes the dropdown and does nothing else.
  - **Language selector**. A translate icon with the current language code. It opens a menu with "Español" and
    "English", and the current one is marked. Choosing one switches the whole UI at once and is remembered across
    reloads.
- **Drawer changes**:
  - The first row is the logo plus the app name "Weight Tracker", not a menu item. It lines up with the header's
    height.
  - Inicio becomes a regular item on the next row: a link to `/`, selected on `/`.
  - Every item gets an icon (see Notes for the mapping).
  - The Configuración section (Mi perfil, Preferencias) and Cerrar sesión are removed from the drawer.
- **Language at startup**: a stored choice wins. Without one, the browser language is detected as today, falling
  back to `es`.

## Fronts And Roles

- backoffice -> lands here: yes -> roles: `TRAINER`, `ADMIN` -> both see the same header and drawer. The mock user
  shows the role "Entrenador".
- app -> lands here: only wiring -> roles: `USER` -> the app registers the two new language use cases, because
  `Dependencies` is shared. It shows no header and no selector, and its behaviour does not change.

A visitor without a session must NOT see the header or the drawer, not even while the session is read. The login and
signup screens get no header and no language selector.

## Classification

Core:

- `GetPreferredLanguageUseCase` (reads the stored language code, or none) -> use case -> `core/src/application/use-cases/preferences/get-preferred-language-use-case.ts`
- `SetPreferredLanguageUseCase` (stores a language code) -> use case -> `core/src/application/use-cases/preferences/set-preferred-language-use-case.ts`
- `PREFERRED_LANGUAGE_KEY` (`preferences.language`) -> constant beside the use cases
- `Dependencies` keys `getPreferredLanguageUseCase` (`UseCaseInterface<void, string | null>`) and
  `setPreferredLanguageUseCase` (`UseCaseInterface<string, void>`) -> composition contract -> `core/src/application/dependencies.ts`
- Barrels re-export both use cases

Backoffice:

- Registrations of both use cases -> composition root -> `backoffice/src/infrastructure/container.ts`
- `AppProviders` (stored language first, then `detectLanguage()`; keeps `<html lang>` in sync on every change) ->
  root providers -> `backoffice/src/presentation/common/components/app-providers.tsx`
- `AppShell` (drawer, then a column with header and `main`) -> component -> `backoffice/src/presentation/common/components/app-shell.tsx`
- `AppHeader` -> component -> `backoffice/src/presentation/common/components/app-header.tsx`
- `HeaderSearch` -> component -> `backoffice/src/presentation/common/components/header-search.tsx`
- `LanguageMenu` -> component -> `backoffice/src/presentation/common/components/language-menu.tsx`
- `AlertsMenu` -> component -> `backoffice/src/presentation/common/components/alerts-menu.tsx`
- `UserMenu` -> component -> `backoffice/src/presentation/common/components/user-menu.tsx`
- `useCurrentUser` (returns the mock user; the one place real data plugs in later) -> hook -> `backoffice/src/presentation/common/hooks/use-current-user.ts`
- `useAlerts` (returns the mock alerts as translation keys) -> hook -> `backoffice/src/presentation/common/hooks/use-alerts.ts`
- `useLogout` (existing) -> reused by `UserMenu`
- `NavigationDrawer` (app-name row, Inicio as item, icons, no Configuración, no logout) -> component -> `backoffice/src/presentation/common/components/navigation-drawer.tsx`
- `header.*` keys, `menu.appName`, removals -> i18n -> `backoffice/src/i18n/{es,en}/common.json`

App:

- Registrations of both use cases -> composition root -> `app/src/infrastructure/container.ts`
- Placeholders in `app/test/support/render-with-providers.tsx` (and the backoffice one)

## Canonical Template Mapping

- Both use cases and their tests -> `core/src/application/use-cases/auth/get-session-use-case.ts` and
  `logout-use-case.ts` and their tests -> the same shape: only `KeyValueStoreInterface`, no API call, `UNEXPECTED` on a
  storage failure. New `preferences` area folder, mirroring `auth`.
- Both `container.ts` and both `render-with-providers.tsx` -> extended in place.
- `AppHeader` and its menus -> no header reference exists: **this change establishes it**, with MUI `AppBar`
  (`position="sticky"`, `elevation={0}` with a bottom border), `Toolbar`, `TextField` + `InputAdornment`, `IconButton`,
  `Badge`, `Avatar`, `Menu`, `MenuItem`, `ListItemIcon`, `Divider`. Same front only.
- `UserMenu` logout -> the drawer's logout entry from `add-backoffice-navigation-drawer` (`useLogout`,
  `router.replace("/login")` on success, `errors:<code>` on failure), moved.
- `NavigationDrawer` and `AppShell` -> their versions from `add-backoffice-navigation-drawer`, extended.
- Tests -> `backoffice/test/presentation/common/components/navigation-drawer.test.tsx` and
  `backoffice/test/presentation/common/views/home-view.test.tsx` for structure; `backoffice/e2e/login.spec.ts`.

The app is not a UI reference. `.stpr/template/` and its README are not updated.

## Screens And Navigation

- backoffice -> `/` -> `HomeView` inside `AppShell` -> unchanged access.
- Header, on every view inside `AppShell`:
  - User menu → Cerrar sesión -> success: `router.replace("/login")`; failure: stays, and the menu shows the error.
  - User menu → Mi perfil / Preferencias -> close the menu, no navigation.
  - Alerts → any alert -> closes the dropdown, no navigation.
  - Language → Español / English -> switches language in place, no navigation.
- Drawer → Inicio -> `/`. Other items are unchanged placeholders.

No new route.

## API Contract

None. No endpoint provides user data (users have only email and role, and there is no `/me`). The user is mocked and
the alerts are mocked. No snapshot refresh, no API change.

## Held State

- Preferred language -> `KeyValueStoreInterface` under `preferences.language` -> written by
  `SetPreferredLanguageUseCase` when the user picks a language -> never cleared. Logout does not remove it: it is a
  device preference, not session data.
- Current language -> the i18n instance -> changed by `LanguageMenu` and by `AppProviders` at startup -> read again
  on reload.
- Menu anchors (open/closed) -> local state in each menu component -> reset on close and on screen exit.
- `useLogout` -> `loggingOut`, `errorCode` -> unchanged, now used by `UserMenu`.
- Mock user and mock alerts -> constants returned by their hooks -> no state.

## Forms And Structural Validation

None. The search box has no validation and no submit.

## Errors And Copy

- Logout fails -> `UNEXPECTED` -> `errors:UNEXPECTED` in an `Alert` inside the user menu.
- Storing the language fails -> `UNEXPECTED` -> the language still switches for the open tab. No message is shown,
  since nothing the user did failed visibly.
- Reading the stored language fails or returns an unsupported code -> treated as no stored choice, so the browser
  language is used.

No new error code.

New keys in `common.json` (Spanish as requested or proposed; English proposed):

- `menu.appName` -> "Weight Tracker" -> "Weight Tracker"
- `header.search.placeholder` -> "Escribe para buscar..." -> "Type for search..."
- `header.search.label` -> "Buscar" -> "Search"
- `header.language.label` -> "Cambiar idioma" -> "Change language"
- `header.language.es` -> "Español" -> "Español"
- `header.language.en` -> "English" -> "English"
- `header.alerts.label` -> "Alertas" -> "Alerts"
- `header.alerts.title` -> "Alertas" -> "Alerts"
- `header.alerts.mock.workoutCompleted` -> "Juan Pérez ha completado su entrenamiento." -> "Juan Pérez completed a workout."
- `header.alerts.mock.invitationAccepted` -> "María López ha aceptado tu invitación." -> "María López accepted your invitation."
- `header.alerts.mock.reviewPending` -> "Revisión semanal pendiente de Carlos Ruiz." -> "Weekly review pending for Carlos Ruiz."
- `header.user.menuLabel` -> "Menú de usuario" -> "User menu"
- `header.user.roles.TRAINER` -> "Entrenador" -> "Trainer"
- `header.user.roles.ADMIN` -> "Administrador" -> "Administrator"

Moved keys: `menu.settings.profile`, `menu.settings.preferences` and `menu.logout` become `header.user.profile`,
`header.user.preferences` and `header.user.logout`, with the same text. `menu.settings.title` is removed.

## Platform Capabilities And Dependencies

- Key-value storage -> `KeyValueStoreInterface` (existing) -> `LocalStorageKeyValueStore` (unchanged) ->
  `SqliteKeyValueStore` (unchanged) -> development build: no.
- `@mui/icons-material` (9.x, matching `@mui/material` 9.5) -> `backoffice` -> MUI's own icon set for the drawer
  items and the header controls. No existing dependency provides icons. It is MUI community edition, so it stays
  inside the allowed UI library.

## Out Of Scope

- Real user data, real alerts, alert read/unread state, counts from the API.
- Search behaviour, results or keyboard shortcuts.
- Screens for Mi perfil, Preferencias, or any drawer placeholder.
- A responsive or collapsible drawer and header.
- A language selector on login and signup, or in the app.
- Must not change: the API, the snapshot, `RequireSession`, `LogoutUseCase`, the login and signup screens, the app UI.

## Notes

- **Builds on `add-backoffice-navigation-drawer`.** That change is at 12/13 and not archived; its last task is blocked
  only by the app form typecheck errors on this branch. Archive it first. This change then modifies its
  `backoffice-navigation` capability and the `backoffice-logout` capability (logout moves from the drawer to the user
  menu).
- **Mock user.** `useCurrentUser` returns `{ name: "Usuario de prueba", role: "TRAINER" }`. The name is data, not copy.
  The role is shown through `header.user.roles.<role>`. Initials come from the name ("UP"). A later change replaces the
  hook's body once the API exposes the user, which needs an API change (a name field and a `/me` endpoint) planned in
  `api/` first.
- **Mock alerts.** `useAlerts` returns the three `header.alerts.mock.*` keys. The badge shows their count (3).
- **Language.** The supported list is `supportedLanguages` from `src/infrastructure/i18n.ts`. `AppProviders` is the
  only presentation file allowed to import it, so `LanguageMenu` declares its two options itself, and `AppProviders`
  filters the stored code against the supported list. `LanguageMenu` switches with `i18n.changeLanguage`, reached
  through `useTranslation()`, then calls `setPreferredLanguageUseCase`. `AppProviders` subscribes to
  `languageChanged` to keep `document.documentElement.lang` in sync, since it is the only file that may touch the
  document.
- **Invariant refinement.** `STPR_INVARIANTS.md` says "the initial language is detected from the device or browser".
  A stored user choice now comes first. Record it in the design. Updating the invariant is a separate template change.
- **Icons** (`@mui/icons-material`, `Outlined` variants): Inicio `HomeOutlined`; Todos los atletas `GroupOutlined`;
  Invitaciones pendientes `PersonAddAltOutlined`; Planes asignados `AssignmentOutlined`; Calendario
  `CalendarMonthOutlined`; Plantillas de entrenamiento `DescriptionOutlined`; Catálogo de ejercicios
  `FitnessCenterOutlined`; Feedback de entrenamientos `RateReviewOutlined`; Revisiones semanales `EventNoteOutlined`;
  Pendientes de revisión `PendingActionsOutlined`. Header: `SearchOutlined`, `TranslateOutlined`,
  `NotificationsOutlined`, `ExpandMoreOutlined` (user toggle). User menu: `PersonOutlined`, `SettingsOutlined`,
  `LogoutOutlined`. Section headers have no icon.
- **Next 16.** Read the `next/link`, `usePathname` and `useRouter` guides in `backoffice/node_modules/next/dist/docs/`.
- **Verification tiers:**
  - core unit: `get-preferred-language-use-case.test.ts` (stored code, none, `UNEXPECTED`) and
    `set-preferred-language-use-case.test.ts` (stores under `preferences.language`, `UNEXPECTED`).
  - backoffice component: `app-header.test.tsx` (or one test per menu). Search: placeholder, start icon, typing calls
    no use case. Controls in order: language, alerts, user. User: name, role "Entrenador", initials. User menu items
    in order with the separator. Placeholders do nothing. Logout succeeds with `replace("/login")`, is disabled while
    in flight, and shows the `UNEXPECTED` message. Alerts: badge "3", three mock alerts. Language: marks the current
    one; choosing English switches the UI text and calls `setPreferredLanguageUseCase` with `"en"`.
  - backoffice component: `navigation-drawer.test.tsx`. App-name row, Inicio as the first item with an icon, every
    item with an icon, Configuración and Cerrar sesión absent. `home-view.test.tsx`: header present with a session,
    absent without.
  - backoffice component: `app-providers` startup language. Stored `"en"` wins, and no stored value falls back to
    detection. Pick the closest existing test or add one, if `AppProviders` is testable with a fake container;
    otherwise cover it in E2E.
  - backoffice E2E: in `login.spec.ts`, the logout flows click the user menu toggle, then "Cerrar sesión". A new flow
    switches to English, reloads, and the UI is still in English.
  - Translation parity covers the new and moved keys.
- Done means `pnpm typecheck && pnpm lint && pnpm test && pnpm test:e2e` passes from `front/`.
- No conflict with a stop case. The only allowed deviation is the language-startup refinement above, recorded in the
  design.
