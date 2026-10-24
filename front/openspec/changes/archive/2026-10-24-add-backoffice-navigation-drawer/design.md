# Design

## Context

See proposal.md for motivation and scope, and the `backoffice-navigation` and `backoffice-logout` specs for the
required behaviour.

Today the backoffice has three routes (`/`, `/login`, `/signup`), and the root layout only renders `AppProviders`.
`HomeView` wraps itself in `RequireSession`, which shows a progress indicator, then either its children or
`router.replace("/login")`. Since `add-logout`, `HomeView` also holds a logout button with local `loggingOut` /
`errorCode` state. The backoffice has `@mui/material` but no icon package. It has no `public/` folder yet.

## Goals / Non-Goals

**Goals:**

- One wrapper a signed-in view puts itself in to get the guard and the drawer, so adding a screen later is one line.
- The menu is declared once, as data, so giving an item its screen later only adds an `href`.

**Non-Goals:**

- A shared route-group layout, a responsive drawer, an app bar, icons, collapsible sections.
- Any change to `RequireSession`, core or the app.

## Decisions

### `AppShell` (`common/components/app-shell.tsx`)

`AppShell({ children })` renders `RequireSession` around a flex `Box`: `NavigationDrawer` first, then a
`Box component="main"` that takes the remaining width and holds `children`.

- _Why inside `RequireSession`:_ the drawer then shares the guard's behaviour exactly. Nothing renders while the session
  is read, and nothing renders when it is missing. No new guard code.
- _Alternative rejected:_ `src/app/(app)/layout.tsx` rendering the shell. The invariants allow a backoffice layout to
  render only the root providers. A wrapper per view keeps to that. The drawer remounts on navigation, which is
  invisible for a permanent drawer with no state.
- `HomeView` replaces `<RequireSession>` with `<AppShell>` and drops its own `Container component="main"`, since the
  shell provides the `main` landmark. It keeps a `Container` for padding.

### `NavigationDrawer` (`common/components/navigation-drawer.tsx`)

- MUI `Drawer` with `variant="permanent"` and a fixed width of 260px, set on the drawer and its paper. Its paper renders
  as a `nav` landmark named by `t("menu.label")`. Check how MUI 9 passes props to the paper (`slotProps.paper`) before
  writing it.
- The menu is a module-level constant, not translated text:

  ```ts
  const sections = [
    {
      title: "menu.athletes.title",
      items: ["menu.athletes.all", "menu.athletes.pendingInvitations"],
    },
    // programming, library, tracking, settings
  ] as const;
  ```

  Each section renders a `List` with a `ListSubheader` (the header) and one `ListItemButton` per item. A placeholder
  `ListItemButton` has no `href` and no `onClick`. When an item gets its screen, the change that adds it gives the
  entry an `href` and renders it like Inicio.

- **Inicio**: first `List`. A `ListItemButton component={NextLink} href="/"` with `selected={pathname === "/"}`
  (`usePathname` from `next/navigation`). It holds the logo (`Box component="img" src="/logo.svg"`,
  `alt={t("menu.logoAlt")}`, about 32px) and `ListItemText` "Inicio".
  - _Why a plain `img`:_ `next/image` with the default loader is not supported by `output: "export"`. `Box component="img"`
    stays inside MUI and avoids the `@next/next/no-img-element` lint rule on a bare `<img>`.
- **Cerrar sesión**: last `List`, after a `Divider`. A `ListItemButton` with `disabled={loggingOut}` and an `onClick`
  that awaits `logout()` from `useLogout`, then calls `router.replace("/login")` when it returns `true`. On failure the
  drawer shows `<Alert severity="error">` with `t(\`errors:${errorCode}\`)` above the entry.
- Namespace: `useTranslation("common")`. The error key is read as `errors:<code>`, as `HomeView` does today.

### `useLogout` (`common/hooks/use-logout.ts`)

`useLogout()` returns `{ loggingOut, errorCode, logout }`. `logout()` clears `errorCode`, sets `loggingOut`, calls
`container.resolve("logoutUseCase").execute()`, and returns `true` on success. On failure it stores the code, clears
`loggingOut`, and returns `false`.

- It holds codes, never translated text, and never navigates. The drawer navigates after the result, as the invariants
  ask of hooks in the app and as the backoffice already does in its views.
- It is a hook that holds one component's local state, not a state hook: no context and no provider.

### Logo placeholder

`backoffice/public/logo.svg`: a small neutral square SVG (a rounded square with a "WT" monogram). The user replaces it later under the same name. `public/` is copied as-is by the static
export.

### Copy

New keys in `backoffice/src/i18n/{es,en}/common.json`, under `menu` (values from the brief): `label`, `logoAlt`,
`home`, the five sections each with `title` and its items, and `logout`. `home.logout` is removed from both files. No
new error key: `errors:UNEXPECTED` already exists.

### Held state

| State                     | Holder                                 | Changed by                  | Resets                        |
| ------------------------- | -------------------------------------- | --------------------------- | ----------------------------- |
| `loggingOut`, `errorCode` | `useLogout`, inside `NavigationDrawer` | the logout entry            | on remount (every navigation) |
| Stored tokens             | `KeyValueStoreInterface`               | `LogoutUseCase` (unchanged) | on logout                     |

The drawer has no open/closed state: it is permanent.

### Tests

- **Component** `backoffice/test/presentation/common/components/navigation-drawer.test.tsx`, rendered through
  `renderWithProviders` with `next/navigation` mocked (`useRouter` → `{ replace }`, `usePathname` → `"/"`):
  - every section title and item is shown in order, the logo has alt "Weight Tracker", and the `nav` landmark is named
    "Menú principal";
  - Inicio's link has `href="/"` and is selected;
  - clicking "Todos los atletas" and the "Atletas" header calls no use case and `replace` is not called;
  - logout calls the use case once and `replace("/login")`;
  - the logout entry is disabled while in flight;
  - `UNEXPECTED` shows "Ha ocurrido un error inesperado." and `replace` is not called.
- **Component** `home-view.test.tsx`: the four logout cases move to the drawer test and are removed here. The "with a
  session" case also asserts the drawer (`navigation` "Menú principal") is shown. The "no session" and "read failure"
  cases assert it is absent. The `next/navigation` mock gains `usePathname`.
- **E2E** `backoffice/e2e/login.spec.ts` gains a flow. Log in through the intercepted `POST /auth/login`, click
  "Cerrar sesión" in the drawer, end on `/login`. Then open `/` and end on `/login` again, with no "Menú principal"
  navigation.
- The test helpers do not change: `renderWithProviders` already registers `logoutUseCase`.

## Risks / Trade-offs

- [Each new signed-in view must remember to wrap itself in `AppShell`] → The pattern is visible in `HomeView`, and the
  E2E for each future screen will catch a missing drawer. A shared layout stays possible as a later template change.
- [Placeholder items look clickable but do nothing] → Accepted by the request. They gain an `href` one by one.
- [A permanent 260px drawer is cramped on small screens] → Accepted. A responsive drawer is out of scope; the
  backoffice is used on desktop.
- [Archive order] → `add-logout` must be archived before this change, or the `backoffice-logout` delta has no base.
