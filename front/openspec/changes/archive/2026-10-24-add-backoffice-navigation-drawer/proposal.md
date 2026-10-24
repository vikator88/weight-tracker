# Proposal

Source brief: `.stpr/plan/openspec/proposal-change-add-backoffice-navigation-drawer.md`.

## Why

The backoffice has one signed-in screen (home) with a temporary logout button on it. Trainers need a stable place to
navigate from and to log out. This change fixes the menu structure now, so later changes only have to give each item
its screen.

## What Changes

- Backoffice: a new `AppShell` component wraps every signed-in view. It keeps the existing session guard and lays out a
  permanent MUI navigation drawer on the left with the screen's content beside it.
- Backoffice: a new `NavigationDrawer` component holds the logo and the menu, in this order:
  - Inicio (with the logo), a link to `/`, selected on `/`;
  - Atletas: Todos los atletas, Invitaciones pendientes;
  - Programación: Planes asignados, Calendario;
  - Biblioteca: Plantillas de entrenamiento, Catálogo de ejercicios;
  - Seguimiento: Feedback de entrenamientos, Revisiones semanales, Pendientes de revisión;
  - Configuración: Mi perfil, Preferencias;
  - Cerrar sesión.

  Section titles are non-interactive headers. Every item other than Inicio and Cerrar sesión is a placeholder, with no
  route and no action.

- Backoffice: logout moves from `HomeView` into the drawer, through a `useLogout` hook beside the drawer. `HomeView`
  wraps itself in `AppShell` instead of `RequireSession` and loses its button.
- Backoffice: a placeholder logo at `backoffice/public/logo.svg`, to be replaced by the user under the same name.
- New `menu.*` keys in the backoffice `common` namespace, in `es` and `en`. `home.logout` is replaced by `menu.logout`.
- No change to core, the app, the API or the snapshot. No new dependency.

## Capabilities

### New Capabilities

- `backoffice-navigation`: the drawer every signed-in backoffice screen shows: its sections and items, which ones act,
  the selected item, and that it stays hidden without a session.

### Modified Capabilities

- `backoffice-logout`: logout is triggered from the drawer instead of a button on home, and a failure keeps the visitor
  on the current screen instead of on home. This capability is introduced by the in-flight change `add-logout`, which
  must be archived before this one.

## Fronts And Roles

- **backoffice**: `TRAINER` and `ADMIN` see the same drawer with the same items. A visitor without a session must not
  see the drawer or any item, not even while the session is read.
- **app**: does not change. The app has no drawer.

No role difference. Hiding items per role is deferred until an item needs it.

## Canonical References

- `AppShell`: builds on `backoffice/src/presentation/auth/components/require-session.tsx`, the wrapper a signed-in view
  already puts itself in. No shell reference exists: **this change establishes it**.
- `NavigationDrawer`: no menu reference exists: **this change establishes it**, with MUI `Drawer` (`permanent`),
  `List`, `ListSubheader`, `ListItemButton`, `ListItemText`.
- `useLogout` and the drawer's logout item: the logout handling `add-logout` put in the backoffice `HomeView`, moved
  out of the view unchanged in behaviour.
- Inicio link: `next/link` through MUI `component={NextLink}`, as in the backoffice `login-view.tsx`.
- Tests: the backoffice `home-view.test.tsx` and `login-view.test.tsx` for structure, `e2e/login.spec.ts` for the E2E
  flow. The app is not a UI reference.
- `.stpr/template/` and its README are not updated here.

## Constraints And Conflicts

- **No route-group layout.** The invariants say a backoffice layout file renders only the root providers around the
  outlet. So the drawer cannot live in `src/app/(app)/layout.tsx`. Each signed-in view wraps itself in `AppShell`,
  as home already wraps itself in `RequireSession`. A shared layout would be a template change first.
- **Static export.** `next/image` with its default loader is not supported with `output: "export"`, so the logo is a
  plain image served from `public/`.
- **Archive order.** `backoffice-logout` exists only in the in-flight `add-logout`. Archive that change first, or the
  delta here has nothing to modify.
- No business rule, no API call beyond the existing local logout. No other conflict with the template, the invariants
  or a stop case.

## API Contract

None used. Logout stays local. No snapshot refresh, no API change.

## Acceptance Criteria

- With a session, `/` shows the drawer, with the logo, every section and item in the order above, and the home content
  beside it.
- Without a session, or while the session is read, neither the drawer nor any item is rendered.
- Inicio links to `/` and is selected on `/`.
- Clicking a placeholder item does not navigate and calls no use case.
- Cerrar sesión logs out and ends on `/login`. It is disabled while in flight. On `UNEXPECTED` the screen stays and
  shows "Ha ocurrido un error inesperado.".
- Home no longer shows its own logout button.
- Every new string exists in `es` and `en`.
- `pnpm typecheck && pnpm lint && pnpm test && pnpm test:e2e` passes from `front/`, with the API mocked in every tier.

## Impact

- `backoffice`: `src/presentation/common/components` (new `app-shell.tsx`, `navigation-drawer.tsx`),
  `src/presentation/common/hooks` (new `use-logout.ts`), `src/presentation/common/views/home-view.tsx`,
  `src/i18n/{es,en}/common.json`, new `public/logo.svg`, `test/presentation/common`, `e2e/login.spec.ts`.
- No change to `core`, `app` or dependencies.
