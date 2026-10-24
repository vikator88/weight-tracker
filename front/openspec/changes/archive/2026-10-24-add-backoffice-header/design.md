# Design

## Context

See proposal.md for motivation and scope, and the five spec deltas for the required behaviour.

State after `add-backoffice-navigation-drawer`:

- `AppShell` = `RequireSession` around a flex row, with `NavigationDrawer` (permanent, 260px, paper is a `nav` named
  "Menú principal") and a `main` box.
- `NavigationDrawer` holds Inicio (logo + text, link to `/`), five sections from a module-level constant, and the
  logout entry through `useLogout`.
- `AppProviders` is the composition-root exception. It imports `container` and `i18n` from `src/infrastructure`, and
  on mount it calls `i18n.changeLanguage(detectLanguage())` and sets `document.documentElement.lang`.
- `KeyValueStoreInterface` (`get` / `set` / `remove`) is implemented by `LocalStorageKeyValueStore` in the backoffice
  and `SqliteKeyValueStore` in the app.
- No icon package. The API has no user name and no `/me`.

## Goals / Non-Goals

**Goals:**

- One header shared by every view inside `AppShell`, with each control a small component the later changes can wire to
  real data one at a time.
- Mock data isolated in two hooks (`useCurrentUser`, `useAlerts`), so replacing it touches one file each.
- The language choice goes through use cases, like every other access to storage.

**Non-Goals:**

- Search behaviour, real alerts, real user data, responsive layout.
- A language selector outside the signed-in screens.

## Decisions

### Core: language preference

- New area folder `core/src/application/use-cases/preferences/` with a barrel, re-exported from
  `use-cases/index.ts`.
- `PREFERRED_LANGUAGE_KEY = "preferences.language"`, exported from `get-preferred-language-use-case.ts`, like
  `ACCESS_TOKEN_KEY` in `signup-use-case.ts`.
- `GetPreferredLanguageUseCase implements UseCaseInterface<void, string | null>`. It takes `keyValueStore`, and
  `execute()` returns `ok(await get(KEY))`. If the store throws, it returns `UNEXPECTED`.
- `SetPreferredLanguageUseCase implements UseCaseInterface<string, void>`. It takes `keyValueStore`, and
  `execute(code)` calls `set(KEY, code)` and returns `ok(undefined)`. If the store throws, it returns `UNEXPECTED`.
- The use cases do not check the code against the supported list. Which languages a front supports is a presentation
  concern of that front, so the check lives in `AppProviders`.
- `LogoutUseCase` is unchanged. It removes only the two token keys, so the preference survives logout.
- `Dependencies` gains `getPreferredLanguageUseCase` and `setPreferredLanguageUseCase`. Both fronts register them
  with `(c) => new …(c.resolve("keyValueStore"))`, and both `render-with-providers.tsx` gain placeholders
  (`get…` resolving `ok(null)`, `set…` resolving `ok(undefined)`).

### Backoffice: startup language (`AppProviders`)

In its mount effect:

1. Subscribe `i18n.on("languageChanged", sync)`, where `sync` sets `document.documentElement.lang`. Unsubscribe on
   cleanup. This also covers changes made later by `LanguageMenu`.
2. Resolve `getPreferredLanguageUseCase`. If the result is `ok` and its value is in `supportedLanguages`, change to it.
   Otherwise change to `detectLanguage()`. A failed read counts as nothing stored.

- _Invariant refinement:_ the invariants say the initial language is detected from the browser. A stored choice now
  comes first, and detection is the fallback. Recorded here as the change's only deviation; updating
  `STPR_INVARIANTS.md` is a separate template change.
- The first paint is still Spanish (the static build), then the effect switches, exactly as today with detection.

### Backoffice: layout (`AppShell`)

```
RequireSession
└─ Box (flex row, min-height 100vh)
   ├─ NavigationDrawer            permanent, full height
   └─ Box (flex column, flexGrow 1, minWidth 0)
      ├─ AppHeader                AppBar position="sticky"
      └─ Box component="main"     children
```

- _Why sticky inside the column:_ the header stays to the right of the drawer and keeps the drawer at full height.
  A `fixed` AppBar would need offset maths against the drawer width.

### Backoffice: `AppHeader` and its parts

- `AppHeader`: `AppBar position="sticky" color="inherit" elevation={0}` with a bottom divider border, then a `Toolbar`.
  Inside: `HeaderSearch`, a flex spacer, then a row of `LanguageMenu`, `AlertsMenu` and `UserMenu`, in that DOM order.
- `HeaderSearch`: an uncontrolled `TextField size="small"` with `sx={{ width: "50%" }}`, the placeholder
  `header.search.placeholder`, `aria-label` `header.search.label` on the input, and a start `InputAdornment` with
  `SearchOutlined`. No `onChange`, no `onSubmit`. Check the MUI 9 `slotProps.input` / `slotProps.htmlInput` names
  before writing it.
- `LanguageMenu`:
  - An `IconButton`-style `Button` labelled `header.language.label`, showing `TranslateOutlined` and the current code
    in upper case (from `useTranslation().i18n.language`), with `aria-haspopup` and `aria-expanded`.
  - A `Menu` with one `MenuItem` per option of a module-level `["es", "en"] as const`, labelled
    `header.language.<code>`. The current one gets `selected`.
  - On pick: close the menu, `await i18n.changeLanguage(code)`, then call `setPreferredLanguageUseCase.execute(code)`
    and ignore its result (see "Errors").
- `AlertsMenu`: an `IconButton` labelled `header.alerts.label` with `Badge badgeContent={alerts.length} color="error"`
  around `NotificationsOutlined`. A `Menu` with a `ListSubheader` `header.alerts.title`, then one `MenuItem` per key
  from `useAlerts()`. Clicking one closes the menu.
- `UserMenu`:
  - A text column (`Typography variant="body2"` with the name, `variant="caption"` with
    `header.user.roles.<role>`), an `Avatar` with the initials, and an `IconButton` labelled `header.user.menuLabel`
    with `ExpandMoreOutlined`, `aria-haspopup` and `aria-expanded`.
  - Its `Menu`, in order: `MenuItem` Mi perfil (`PersonOutlined`), `MenuItem` Preferencias (`SettingsOutlined`), a
    `Divider`, then `MenuItem` Cerrar sesión (`LogoutOutlined`), `disabled={loggingOut}`.
  - Mi perfil and Preferencias close the menu.
  - Cerrar sesión keeps the menu open, awaits `useLogout().logout()`, then `router.replace("/login")` on `true`. On
    failure an `Alert severity="error"` with `errors:<code>` renders inside the menu, below the logout entry.
  - Initials: the first letter of each of the first two words of the name, upper case.
- `useCurrentUser()` returns the constant `{ name: "Usuario de prueba", role: "TRAINER" }`, typed with a local
  `role: "TRAINER" | "ADMIN"`. This is a presentation placeholder, not a domain type; the real one will come from the
  generated contract once the API exposes it.
- `useAlerts()` returns the three `header.alerts.mock.*` keys as a constant array.
- Every component is `"use client"` and lives in `src/presentation/common/{components,hooks}`. None imports from
  `src/infrastructure`.

### Backoffice: `NavigationDrawer`

- The first child is a `Toolbar` (the same height as the header's `Toolbar`) with the logo as `Box component="img"`
  (`alt=""`, decorative because the name sits beside it) and `Typography` `menu.appName`. It is not a button.
- Next, a `List` with Inicio: `ListItemButton component={NextLink} href="/" selected`, with a `ListItemIcon` holding
  `HomeOutlined` and a `ListItemText`.
- The `sections` constant becomes `{ title, items: { label, icon }[] }`, with the icon components from the brief. Each
  item renders `ListItemIcon` + `ListItemText`. The settings section is removed.
- The `Divider`, the logout `List`, `useLogout`, `useRouter` and the `Alert` are removed from the drawer.

### Copy

In `backoffice/src/i18n/{es,en}/common.json`:

- Add `menu.appName` and every `header.*` key from the brief.
- Move `menu.settings.profile`, `menu.settings.preferences` and `menu.logout` to `header.user.profile`,
  `header.user.preferences` and `header.user.logout`, with the same text.
- Remove `menu.settings` and `menu.logoAlt`.
- `header.language.es` / `header.language.en` hold each language's own name in both files ("Español", "English").

### Errors

- `UNEXPECTED` on logout → `errors:UNEXPECTED` in the user menu.
- `UNEXPECTED` when storing the language → ignored. The switch already happened, and the only cost is that the next
  load falls back to detection. Showing an error for an invisible side effect would confuse more than it helps.
- `UNEXPECTED` when reading the language at startup → treated as nothing stored.

No new error code, no HTTP error mapper change.

### Held state

| State                     | Holder                                            | Changed by                                | Resets                  |
| ------------------------- | ------------------------------------------------- | ----------------------------------------- | ----------------------- |
| Preferred language        | `KeyValueStoreInterface` (`preferences.language`) | `SetPreferredLanguageUseCase`             | never (survives logout) |
| Current language          | i18n instance                                     | `AppProviders` at startup, `LanguageMenu` | reload                  |
| Menu anchors              | each menu component (`useState`)                  | toggle, item click, close                 | on close / remount      |
| `loggingOut`, `errorCode` | `useLogout` inside `UserMenu`                     | logout entry                              | on remount              |

### Dependency

`@mui/icons-material` at the same minor as `@mui/material` (9.5.x), added with `pnpm --filter backoffice add`. Icons
are imported one per path (`@mui/icons-material/HomeOutlined`), so only the used icons are bundled.

### Tests

- **core**: `test/application/use-cases/preferences/get-preferred-language-use-case.test.ts` (stored, none,
  `UNEXPECTED`) and `set-preferred-language-use-case.test.ts` (stores under the key, `UNEXPECTED`). Both shaped like
  `get-session-use-case.test.ts`.
- **backoffice component**, in `test/presentation/common/components/`:
  - `app-header.test.tsx`:
    - the search placeholder and label, with the search icon (`data-testid="SearchOutlinedIcon"`);
    - typing calls no use case;
    - the three controls appear in DOM order language, alerts, user.
  - `user-menu.test.tsx`:
    - name, "Entrenador" and "UP" are shown;
    - the menu items appear in order, with the separator (`role="separator"`);
    - Mi perfil closes the menu without calling anything;
    - logout success calls `replace("/login")`;
    - the logout entry is disabled while in flight;
    - `UNEXPECTED` shows the message and does not call `replace`.
  - `alerts-menu.test.tsx`: the badge shows "3", the three texts appear, and clicking one closes the menu.
  - `language-menu.test.tsx`:
    - "ES" is shown and Español is selected;
    - choosing English shows "EN" and calls the set use case with `"en"`;
    - a failing set use case still switches.

    Its last line resets to `es`, since the i18n instance is shared.

  - `navigation-drawer.test.tsx`, updated:
    - the app-name row comes first and is not a button;
    - Inicio is the first link;
    - each item has an icon (`data-testid` ending in `Icon`);
    - Configuración, Mi perfil, Preferencias and Cerrar sesión are absent;
    - the logout cases move to `user-menu.test.tsx`.
  - `app-providers.test.tsx`, new, with `@/infrastructure/container` mocked by `vi.mock` to a `Container` of fakes and
    `navigator.languages` stubbed:
    - a stored `"en"` wins over a Spanish browser;
    - no stored value gives the browser's `en`;
    - a stored `"fr"` gives the browser language;
    - `document.documentElement.lang` follows.

    This is the first test of `AppProviders`, so it establishes that shape.
- **backoffice component** `home-view.test.tsx`: also asserts the header (the "Buscar" search box) is present with a
  session and absent without one, and that "Cerrar sesión" appears once, in the user menu.
- **backoffice E2E** `e2e/login.spec.ts`:
  - the two logout flows open "Menú de usuario" first, then choose "Cerrar sesión";
  - a new flow logs in, chooses English, reloads, and sees the "Type for search..." placeholder and `html[lang="en"]`.
- **app**: only the container and helper placeholders change. The existing app tests must still pass.

## Risks / Trade-offs

- [Mock user and alerts can be mistaken for real data] → They live in two clearly named hooks, with the placeholder
  name "Usuario de prueba". Replacing them is planned as separate changes after the API exposes the data.
- [A language-preference write failure is silent] → Accepted. The visible switch still works, and the worst case is
  falling back to browser detection on the next load.
- [`AppProviders` gains logic and its first test, which mocks a module] → Kept to one effect. The module mock is the
  only way to swap the real container there, since the root providers import it directly by design.
- [Fixed widths (260px drawer, 50% search) on small screens] → Accepted. Responsive layout is out of scope.
- [Archive order] → `add-backoffice-navigation-drawer` must be archived first. Until then, `openspec validate` reports
  the `backoffice-navigation` and `backoffice-logout` deltas as not applicable on archive.
