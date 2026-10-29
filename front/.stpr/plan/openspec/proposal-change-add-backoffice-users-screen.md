# Change Proposal: Add Backoffice Users Screen With Admin User Creation

Source brief for `/opsx:propose`. Artifacts go to `openspec/changes/add-backoffice-users-screen/`.

## Change

Refresh the OpenAPI snapshot to pick up `GET /users` and `POST /users`, add `GetUsersUseCase` and
`CreateUserUseCase` to `core`, and add a backoffice
"Todos los usuarios" screen. It shows a users table with a text search, profile filter pills and an "Añadir usuario"
button that opens a modal form. With that form an `ADMIN` can create a user of any role (`USER`, `TRAINER`, `ADMIN`).

## Objective

Administrators have no way to create accounts for other people. Self-service signup forbids `ADMIN` and logs the new
user in. The API now provides `GET /users` (see
`api/.stpr/plan/openspec/proposal-change-add-list-users-endpoint.md`) and `POST /users` (admin only, no session
issued; see `api/.stpr/plan/openspec/proposal-change-add-admin-create-user-endpoint.md`). `core` does not know
either operation, and the backoffice has no screen for them. The "Todos los atletas" drawer entry exists but leads
nowhere.

## Capabilities

- List users (`GET /users`): the API decides the scope. An `ADMIN` gets every user. A `TRAINER` gets `501` until the
  trainer's athletes are implemented in the API. This is wired in `core` and both containers.
- Create a user (`POST /users`) with email and password (required), role (required, chosen from the form), and name,
  surname and date of birth (optional). This is wired in `core` and both containers.
- Drawer section label depends on the role: `ADMIN` sees "Usuarios" > "Todos los usuarios", and `TRAINER` keeps
  "Atletas" > "Todos los atletas". The `ADMIN` entry navigates to the screen and shows as selected while on it. The
  `TRAINER` entry keeps its copy and stays without a link while the API answers `501` for trainers.
- Users table with these columns: "Nombre completo" (name + surname in one cell), email and profile (role).
- One search text field above the table. It filters rows client-side by email, name or surname (case-insensitive,
  substring).
- Profile filter pills above the table (Usuario, Entrenador, Administrador). Selecting one shows only that role.
  Selecting it again clears the filter. Search and pill filter combine.
- "Añadir usuario" button above the table. It opens a modal with the creation form. On success the modal closes and
  the table reloads. On failure the modal stays open and shows the error.
- Loading, empty, no-match and error states for the list.

## Fronts And Roles

- backoffice -> lands here: yes -> roles: `ADMIN`, `TRAINER`
  - `ADMIN`: sees "Usuarios" > "Todos los usuarios", the full table, the role pills and the "Añadir usuario" button.
  - `TRAINER`: keeps "Atletas" > "Todos los atletas" with no `href`, as today. Must NOT see the "Usuarios" copy,
    the "Añadir usuario" button or the creation modal. If a trainer opens `/users` by URL, `GET /users` answers
    `501` and the screen shows `LoadError`.
- app -> lands here: wiring only -> roles: `USER` -> the container registers `getUsersUseCase` and
  `createUserUseCase`. No screen, hook or copy changes.

The front does not decide who may create users. It hides the button for non-admins as a presentation choice. The API
enforces the rule with `403`. The role comes from `useCurrentUser().profile.role`.

## Classification

Core:

- `CreateUserRequest`, `CreatedUserResponse` (aliases over `components["schemas"]`) -> entity type ->
  `core/src/domain/users/user.ts`. The list reuses the existing `UserProfile` alias (`UserProfileResponse`).
- `getAll()` and `create(request)` -> repository contract methods -> the users repository contract in
  `core/src/application/ports/`
- `UserRepository.getAll` / `create` -> repository implementation ->
  `core/src/infrastructure/repositories/user-repository.ts`
- `GetUsersUseCase` -> use case -> `core/src/application/use-cases/users/get-users-use-case.ts`
- `CreateUserUseCase` -> use case -> `core/src/application/use-cases/users/create-user-use-case.ts`
- `Dependencies` keys `getUsersUseCase`, `createUserUseCase` -> composition contract ->
  `core/src/application/dependencies.ts`
- Barrels re-export the new aliases and use cases

Backoffice and app wiring:

- Registrations of `getUsersUseCase` and `createUserUseCase` -> composition root ->
  `backoffice/src/infrastructure/container.ts` and `app/src/infrastructure/container.ts`
- Placeholders for the new keys -> test helper -> `backoffice/test/support/render-with-providers.tsx` and
  `app/test/support/render-with-providers.tsx`

Backoffice screen (new `users` domain area):

- `/users` route -> route -> `backoffice/src/app/users/page.tsx`
- `UsersView` -> view -> `backoffice/src/presentation/users/views/users-view.tsx`
- `useUsers` (load, status, errorCode, reload) -> state hook -> `backoffice/src/presentation/users/hooks/use-users.ts`
- `useCreateUser` (submit, submitting, errorCode) -> state hook -> `backoffice/src/presentation/users/hooks/use-create-user.ts`
- `UsersTable` (columns, empty and no-match states) -> organism ->
  `backoffice/src/presentation/users/components/organisms/users-table.tsx`
- `CreateUserDialog` (form inside a dialog) -> organism ->
  `backoffice/src/presentation/users/components/organisms/create-user-dialog.tsx`
- `RoleFilter` (pills) -> molecule -> `backoffice/src/presentation/users/components/molecules/role-filter.tsx`
- Search field -> reuse the `text-field` atom (no new molecule unless an icon adornment needs one)
- Table, dialog, chip, button, text field -> reuse existing atoms in `common/components/atoms/`
- Name / surname inputs -> reuse the `profile-name-fields` molecule
- Loading and error -> reuse `skeleton`, `HomeSkeleton` and `LoadError`
- Drawer -> extended in place -> `backoffice/src/presentation/common/components/organisms/navigation-drawer.tsx`: the
  athletes section's title and "all" label depend on the role, and "all" gets `href="/users"` for `ADMIN` only
- Copy -> new `users` namespace (`backoffice/src/i18n/{es,en}/users.json`) plus new `menu.users.*` keys in `common.json`

## Canonical Template Mapping

- Aliases -> `core/src/domain/users/user.ts` and `core/src/domain/auth/auth.ts` -> extended in place, aliases over
  `components["schemas"]` only.
- `UserRepository.getAll` -> `WorkoutRepository.getAll` -> `GET` with no body through `HttpClientInterface`,
  failures through `mapHttpError`.
- `UserRepository.create` -> existing `UserRepository` methods and the `WorkoutRepository` create method -> `POST`
  with body through `HttpClientInterface`, failures through `mapHttpError`.
- `GetUsersUseCase` -> `core/src/application/use-cases/workouts/get-workouts-use-case.ts` -> no-input
  `UseCaseInterface<void, …>` delegating to a repository.
- `CreateUserUseCase` -> `core/src/application/use-cases/users/update-my-profile-use-case.ts` -> input forwarded to
  the repository, `Result` returned as is.
- Core tests -> the tests of those same files plus `user-repository.test.ts`.
- Route -> `backoffice/src/app/assigned-plans/page.tsx` -> default-exports the view, nothing else.
- `UsersView` -> `backoffice/src/presentation/workouts/views/assigned-plans-view.tsx` -> `AppShell`, `h1`, button
  above a table, skeleton / `LoadError`, role-dependent element.
- `useUsers` -> `backoffice/src/presentation/workouts/hooks/use-workouts.ts` -> same `status` / `errorCode` / `reload`
  shape, local to the screen.
- `useCreateUser` -> the submit hook behind `CreateWorkoutDialog` -> `submitting` / `errorCode`, `Result`-driven.
- `UsersTable` -> `backoffice/src/presentation/workouts/components/organisms/workouts-table.tsx` -> organism over the
  table atoms, data by props.
- `CreateUserDialog` -> `backoffice/src/presentation/workouts/components/organisms/create-workout-dialog.tsx` -> modal
  form organism with submit, cancel and error `Alert`. Field validation follows
  `backoffice/src/presentation/auth/components/organisms/signup-form.tsx`.
- `RoleFilter` -> `backoffice/src/presentation/workouts/components/molecules/label-filter.tsx` -> same pill group
  (label, options, single selection, toggle to clear).
- Atom/molecule/organism references are `pending` in `.stpr/template/README.md`. Follow the backoffice shapes
  established in #30.
- View test -> `backoffice/test/presentation/workouts/views/assigned-plans-view.test.tsx` -> fakes through the container.
- E2E -> `backoffice/e2e/assigned-plans.spec.ts` -> `page.route` interception, same session setup.

UI references come from the backoffice only. `.stpr/template/` and its README are not updated here.

## Screens And Navigation

- backoffice -> `/users` -> `UsersView` -> drawer "Usuarios" > "Todos los usuarios" (`ADMIN` only), inside the session
  guard -> success: table, or empty state; failure: `LoadError` with retry (including a `TRAINER` reaching the URL
  directly, `501`); `401` follows the existing session handling.
- "Añadir usuario" (`ADMIN` only) -> `CreateUserDialog` -> success: dialog closes, form resets, list reloads, success
  feedback (an `Alert` or similar existing atom; decide at proposal time); failure: dialog stays open with the error
  message and the entered values kept. Cancel closes without a request.

## API Contract

- `GET /users` -> in `core/openapi/v1.json`: **no, refresh needed** -> no body, response `200`
  `UserProfileResponse[]` (`id`, `email`, `name`, `surname`, `role`). Failures: `401`, `403` (`USER`), `501`
  (`TRAINER`, athletes not implemented yet). Provided by the API change `add-list-users-endpoint`.
- `POST /users` -> in `core/openapi/v1.json`: **no, refresh needed** -> request `CreateUserRequest` (`email`,
  `password`, `role`, optional `name`, `surname`, `dateBirth`), response `201` `CreatedUserResponse` (`id`, `email`,
  `name`, `surname`, `dateBirth`, `role`). Failures: `400`, `401`, `403`, `409`.
  Provided by the API change `add-admin-create-user-endpoint`.

This change runs `openapi:refresh` + `openapi:generate` once, with the API running, to pick up both endpoints. It
commits the snapshot and the generated types as a step of their own. No hand-written type.

## Held State

- `useUsers` -> the loaded user list, status and error code -> the hook on mount and on `reload` (also after a
  successful creation) -> resets on screen exit. Nothing is cached across screens or kept after logout.
- `UsersView` local state -> search text, selected role pill, dialog open/closed -> the user -> resets on screen exit.
- `CreateUserDialog` form state -> field values and field errors -> the user -> resets when the dialog closes after
  success or cancel.

## Forms And Structural Validation

- Create user -> email -> required, email format (as in `SignupForm`)
- Create user -> password -> required (strength is an API rule; a weak password comes back as `400`)
- Create user -> role -> required, one of `USER`, `TRAINER`, `ADMIN` (select or radio; no default, or `USER` as
  default; decide at proposal time)
- Create user -> name, surname -> optional, max 100 characters each (the API's `PersonName` limit), as in
  `profile-name-fields`
- Create user -> date of birth -> optional, a valid date. No range rule.

Empty optional fields are omitted from the request, not sent as empty strings.

## Errors And Copy

- Session expired / missing -> `UNAUTHORIZED` -> existing session handling.
- Caller is not an admin -> `FORBIDDEN` -> dialog error with the existing message.
- Malformed email, weak password, invalid name -> `VALIDATION` -> dialog error "Revisa los datos. La contraseña debe
  cumplir los requisitos de seguridad." (a create-specific key, like signup's)
- Email already registered -> `CONFLICT` -> dialog error "Ya existe un usuario con este email."
- No connection -> `NETWORK`. Anything else -> `UNEXPECTED` -> generic error (list: `LoadError` with retry).
- Trainer listing (`501`) -> `UNEXPECTED` (the mapper has no `501` entry) -> `LoadError`. It is only reachable by
  typing the URL, because the trainer's drawer entry has no link.
- No new error code. The HTTP error mapper is not touched.

New copy, `common` namespace:

- `menu.users.title` -> "Usuarios" -> "Users"
- `menu.users.all` -> "Todos los usuarios" -> "All users"
- `menu.athletes.*` stays unchanged for `TRAINER`.

New copy, `users` namespace:

- `list.title` -> "Todos los usuarios" -> "All users"
- `list.search` -> "Buscar por nombre o email" -> "Search by name or email"
- `list.addUser` -> "Añadir usuario" -> "Add user"
- `list.columns.fullName` -> "Nombre completo" -> "Full name"
- `list.columns.email` -> "Email" -> "Email"
- `list.columns.profile` -> "Perfil" -> "Profile"
- `list.profileFilter` -> "Perfil" -> "Profile"
- `roles.USER` -> "Usuario" -> "User"
- `roles.TRAINER` -> "Entrenador" -> "Trainer"
- `roles.ADMIN` -> "Administrador" -> "Administrator"
- `list.empty` -> "Todavía no hay usuarios." -> "No users yet."
- `list.noMatches` -> "Ningún usuario coincide con la búsqueda." -> "No users match your search."
- `create.title` -> "Nuevo usuario" -> "New user"
- `create.fields.email`, `password`, `role`, `name`, `surname`, `dateBirth` -> "Email", "Contraseña", "Perfil",
  "Nombre", "Apellidos", "Fecha de nacimiento" -> "Email", "Password", "Profile", "Name", "Surname", "Date of birth"
- `create.submit` -> "Crear usuario" -> "Create user"
- `create.cancel` -> "Cancelar" -> "Cancel"
- `create.success` -> "Usuario creado." -> "User created."
- `create.errors.VALIDATION`, `create.errors.CONFLICT` -> as above -> "Check the details. The password must meet the
  security requirements.", "A user with this email already exists."

## Platform Capabilities And Dependencies

None.

## Out Of Scope

- The trainer's view of their athletes. When the API implements it, a later change links the trainer's drawer entry
  and adds the `TRAINER` copy to the screen.
- Editing, deleting or disabling users; changing a user's role or password; user detail screen.
- Invitations ("Invitaciones pendientes" stays as is), email verification, forcing a password change.
- Server-side search, sorting and pagination.
- Any app screen.
- Must not change: the API, signup, the other drawer entries, and `TRAINER`'s "Atletas" copy.

## Notes

- **No stop case.** Both endpoints exist in the API, and no business rule is added to the front.
- **Full name column.** `name` + `surname` joined with a space and trimmed. When both are empty, show a dash (`—`).
  The API stores empty names for users created without them.
- **Search.** Client-side over the loaded list, case- and accent-insensitive substring on `email`, `name` and
  `surname`. It is a view filter, not a business rule.
- **Profile column.** Shows the translated role (optionally as a `chip`). Pills use the same translated labels.
- **Drawer copy by role.** The drawer reads the role from `useCurrentUser`. While the profile loads, render the
  `TRAINER` copy or a skeleton; decide at proposal time to avoid a flash.
- **Date of birth.** Send `YYYY-MM-DD` as a calendar date with no time-zone shift.
- **Snapshot refresh** is its own commit: `core/openapi/v1.json` plus `core/src/contract/generated`, never edited by
  hand.
- **Verification tiers:**
  - core unit: `get-users-use-case.test.ts` and `create-user-use-case.test.ts` (request forwarded, success returned,
    repository failure returned unchanged). `user-repository.test.ts` gains `getAll` (`GET /users`): `200`, `401`,
    `403`, `501` -> `UNEXPECTED`, network -> `NETWORK`. It also gains `create` (`POST /users`, body sent as given):
    `201`, `400`, `401`, `403`, `409`, `500` -> `UNEXPECTED`, network -> `NETWORK`.
  - container tests in both fronts gain the new keys.
  - backoffice component tests:
    - `users-view.test.tsx`: rows with full name, email and profile; search by email, name and surname; pill filter;
      search and pill combined; empty and no-match states; `LoadError` with retry; `ADMIN` sees "Añadir usuario";
      `TRAINER` does not (button and dialog absent); a `UNEXPECTED` failure (the trainer's `501`) shows `LoadError`.
    - `create-user-dialog` (or inside the view test): required-field errors block submission; optional fields
      omitted when empty; success closes and reloads; `CONFLICT` and `VALIDATION` show their messages and keep the
      values.
  - backoffice drawer test: `ADMIN` sees "Usuarios" / "Todos los usuarios" linking to `/users`; `TRAINER` sees
    "Atletas" / "Todos los atletas" with no link, and not the admin copy.
  - backoffice E2E `e2e/users.spec.ts`: admin opens "Todos los usuarios" from the drawer, filters, creates a user
    through the modal, and sees it in the reloaded table (`GET /users` and `POST /users` intercepted).
  - translation parity passes with the new namespace and keys.
- Done means `pnpm typecheck && pnpm lint && pnpm test && pnpm test:e2e` passes from `front/`.
- No deviation from the mapped references is allowed.
