# Change Proposal: Add Backoffice Navigation Drawer

Source brief for `/opsx:propose`. Artifacts go to `openspec/changes/add-backoffice-navigation-drawer/`.

## Change

Add a common MUI navigation drawer to every signed-in backoffice screen. It holds the full menu of the backoffice, but
only "Inicio" and "Cerrar sesión" do something for now. Logout moves from the home view into the drawer.

## Objective

The backoffice has one signed-in screen (home) and a temporary logout button on it. The trainer needs a stable
place to navigate from and to log out. This change fixes the menu structure now, so later changes only have to
give each item its screen.

## Capabilities

- A permanent drawer on the left of every signed-in screen, with the app logo at the top.
- Menu, in this order, with sections as non-interactive headers and their items below:
  - **Inicio** (with the logo) -> goes to `/`.
  - **Atletas** -> Todos los atletas, Invitaciones pendientes.
  - **Programación** -> Planes asignados, Calendario.
  - **Biblioteca** -> Plantillas de entrenamiento, Catálogo de ejercicios.
  - **Seguimiento** -> Feedback de entrenamientos, Revisiones semanales, Pendientes de revisión.
  - **Configuración** -> Mi perfil, Preferencias.
  - **Cerrar sesión** -> logs out and goes to `/login`.
- Every item other than Inicio and Cerrar sesión is a placeholder: rendered, but it does nothing and has no route.
- The item of the current screen is shown as selected (only Inicio, on `/`, for now).
- The logout button leaves `HomeView`. Home keeps its title and text inside the drawer layout.

## Fronts And Roles

- backoffice -> lands here: yes -> roles: `TRAINER`, `ADMIN` -> both see the same drawer, with the same items.
- app -> lands here: no -> the app has no drawer and does not change.

No role difference yet. A visitor without a session must NOT see the drawer: it renders only after the session is
confirmed, like home today. Hiding items per role is out of scope until an item needs it.

## Classification

Backoffice:

- `AppShell` (session guard + drawer + content area) -> component -> `backoffice/src/presentation/common/components/app-shell.tsx`
- `NavigationDrawer` (logo, menu, logout) -> component -> `backoffice/src/presentation/common/components/navigation-drawer.tsx`
- `useLogout` (in-flight flag and last error code around `logoutUseCase`) -> hook beside the component -> `backoffice/src/presentation/common/hooks/use-logout.ts`
- `HomeView` (wraps itself in `AppShell` instead of `RequireSession`; loses the logout button) -> view -> `backoffice/src/presentation/common/views/home-view.tsx`
- App logo placeholder -> static asset -> `backoffice/public/logo.svg`
- `menu.*` keys -> i18n -> `backoffice/src/i18n/{es,en}/common.json` (`home.logout` moves to `menu.logout`)

Core: none. `LogoutUseCase` and its registration exist (`add-logout`).

## Canonical Template Mapping

- `AppShell` -> `backoffice/src/presentation/auth/components/require-session.tsx` -> the existing wrapper a signed-in
  view puts itself in. `AppShell` uses it inside and adds the layout around the children. No shell reference exists:
  **this change establishes it**.
- `NavigationDrawer` -> no menu reference exists: **this change establishes it**, with MUI `Drawer`
  (`variant="permanent"`), `List`, `ListSubheader`, `ListItemButton` and `ListItemText`.
- `useLogout` and the drawer's logout handling -> the logout handling `add-logout` put in the backoffice `HomeView`
  (use case through the container, `router.replace("/login")` on success, `errors:<code>` on failure), moved out of
  the view.
- Inicio link -> `next/link` with MUI `component={NextLink}`, as in the backoffice `login-view.tsx`.
- Tests -> `backoffice/test/presentation/common/views/home-view.test.tsx` and
  `backoffice/test/presentation/auth/views/login-view.test.tsx` for structure; `backoffice/e2e/login.spec.ts` for the
  E2E flow.

The app is not a UI reference. `.stpr/template/` and its README are not updated.

## Screens And Navigation

- backoffice -> `/` -> `HomeView` inside `AppShell` -> unchanged access: no session goes to `/login`.
- Drawer, on every view inside `AppShell`:
  - Inicio -> `Link` to `/`; selected when the pathname is `/`.
  - Cerrar sesión -> `logoutUseCase`; success: `router.replace("/login")`; failure: stays and shows the error in the
    drawer.
  - Placeholder items -> no navigation, no action.

No new route.

## API Contract

None. Logout is local (`add-logout`). No snapshot refresh.

## Held State

- `useLogout` -> `loggingOut`, `errorCode` -> the drawer's logout item -> reset on screen exit.
- Stored tokens -> removed by `LogoutUseCase` -> unchanged from `add-logout`.
- No provider and no persisted drawer state (the drawer is permanent, so it has no open/closed state).

## Forms And Structural Validation

None.

## Errors And Copy

- Logout fails in storage -> `UNEXPECTED` -> `errors:UNEXPECTED` in an `Alert` inside the drawer.

No new error code.

New keys in `common.json` (Spanish from the request; English proposed):

- `menu.home` -> "Inicio" -> "Home"
- `menu.athletes.title` -> "Atletas" -> "Athletes"
- `menu.athletes.all` -> "Todos los atletas" -> "All athletes"
- `menu.athletes.pendingInvitations` -> "Invitaciones pendientes" -> "Pending invitations"
- `menu.programming.title` -> "Programación" -> "Programming"
- `menu.programming.assignedPlans` -> "Planes asignados" -> "Assigned plans"
- `menu.programming.calendar` -> "Calendario" -> "Calendar"
- `menu.library.title` -> "Biblioteca" -> "Library"
- `menu.library.workoutTemplates` -> "Plantillas de entrenamiento" -> "Workout templates"
- `menu.library.exerciseCatalog` -> "Catálogo de ejercicios" -> "Exercise catalog"
- `menu.tracking.title` -> "Seguimiento" -> "Tracking"
- `menu.tracking.workoutFeedback` -> "Feedback de entrenamientos" -> "Workout feedback"
- `menu.tracking.weeklyReviews` -> "Revisiones semanales" -> "Weekly reviews"
- `menu.tracking.pendingReview` -> "Pendientes de revisión" -> "Pending review"
- `menu.settings.title` -> "Configuración" -> "Settings"
- `menu.settings.profile` -> "Mi perfil" -> "My profile"
- `menu.settings.preferences` -> "Preferencias" -> "Preferences"
- `menu.logout` -> "Cerrar sesión" -> "Log out" (replaces `home.logout`)
- `menu.logoAlt` -> "Weight Tracker" -> "Weight Tracker"
- `menu.label` -> "Menú principal" -> "Main menu" (accessible name of the navigation)

## Platform Capabilities And Dependencies

None. No icon package: items are text only, and the only image is the logo file.

## Out Of Scope

- Screens, routes or behaviour for any placeholder item.
- Icons per item, collapsible sections, badges or counters (e.g. pending invitations).
- A responsive or temporary drawer, an app bar and a menu button. The drawer is permanent at every width.
- Hiding items per role.
- A drawer in the Expo app.
- The final logo. `public/logo.svg` is a placeholder the user will replace with the same file name.
- Must not change: core, the API, the snapshot, `RequireSession`, `LogoutUseCase`, the login and signup screens.

## Notes

- **Builds on `add-logout`.** That change is not archived yet. Archive it first; this change then modifies its
  `backoffice-logout` capability (the button moves from home to the drawer).
- **Why a wrapper component and not a route-group layout.** `STPR_INVARIANTS.md` says a backoffice layout file renders
  only the root providers around the outlet, so `src/app/(app)/layout.tsx` rendering the drawer would be a deviation.
  Each signed-in view wraps itself in `AppShell`, as home wraps itself in `RequireSession` today. The cost is that
  the drawer remounts on every navigation, which is invisible for a permanent drawer with no state. If a shared
  layout becomes necessary, it is a template change first.
- **Logo.** A placeholder SVG in `backoffice/public/` (served as `/logo.svg` by the static export), rendered with
  `Box component="img"`. Check in `backoffice/node_modules/next/dist/docs/` how `public/` and images work with
  `output: "export"` before choosing between that and `next/image` with `unoptimized`.
- **Next 16.** Read the `next/link`, `usePathname` and `useRouter` guides in `backoffice/node_modules/next/dist/docs/`.
- **Verification tiers:**
  - backoffice component: `navigation-drawer.test.tsx` (or `app-shell.test.tsx`): every section and item renders in
    `es`; Inicio links to `/` and is selected on `/`; clicking a placeholder calls no use case and does not navigate;
    logout calls the use case and `replace("/login")`; the logout item is disabled while in flight; `UNEXPECTED`
    shows the message and does not navigate.
  - backoffice component: `home-view.test.tsx` drops its logout cases; with no session, neither the drawer nor home
    content is rendered.
  - backoffice E2E: in `login.spec.ts`, log in, click "Cerrar sesión" in the drawer, end on `/login`; opening `/`
    again ends on `/login`.
  - Translation parity test covers the new keys.
- Done means `pnpm typecheck && pnpm lint && pnpm test && pnpm test:e2e` passes from `front/`.
- No conflict with a stop case. Where the reference is missing, this change establishes it as described above; no
  other deviation is allowed.
