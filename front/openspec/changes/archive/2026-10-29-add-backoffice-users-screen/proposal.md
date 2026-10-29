# Proposal

Source brief: `.stpr/plan/openspec/proposal-change-add-backoffice-users-screen.md`.

## Why

Administrators cannot create accounts for other people. Self-service signup forbids `ADMIN` and logs the new user in.
The API now offers `GET /users` (API change `add-list-users-endpoint`) and `POST /users` (API change
`add-admin-create-user-endpoint`, admin only, no session issued), but `core` does not know either operation. The
backoffice has no screen for them, and the "Todos los atletas" drawer entry leads nowhere.

## What Changes

- **Snapshot refresh**: `openapi:refresh` + `openapi:generate` pick up `GET /users` and `POST /users`, in a commit of
  their own. No hand-written type.
- **core**: `GetUsersUseCase` (`GET /users`) and `CreateUserUseCase` (`POST /users`), `getAll` / `create` on the
  user repository, aliases `CreateUserRequest` / `CreatedUserResponse` (the list reuses `UserProfile`), structural
  rules for the create user form, and the keys `getUsersUseCase` / `createUserUseCase` in `Dependencies`. Both fronts
  register them. The app only gets the wiring.
- **backoffice `/users` screen** ("Todos los usuarios"):
  - table with "Nombre completo" (name + surname), email and profile (role);
  - one search field that filters the loaded list by email, name or surname;
  - profile pills (Usuario, Entrenador, Administrador) that filter by role, combined with the search;
  - "Añadir usuario" button (`ADMIN` only) opening a modal form: email and password required, role required
    (defaults to Usuario), name, surname and date of birth optional. On success the modal closes, a confirmation
    shows and the table reloads. On failure the modal stays open with the error and the typed values.
  - loading, empty, no-match and error states.
- **Drawer**: for an `ADMIN` the athletes section reads "Usuarios" > "Todos los usuarios" and links to `/users`. For
  a `TRAINER` it keeps "Atletas" > "Todos los atletas" with no link, as today.

### Fronts and roles

- backoffice, `ADMIN`: sees the "Usuarios" copy, the screen, the table, the filters and the "Añadir usuario" button.
- backoffice, `TRAINER`: keeps the "Atletas" copy with no link. Must NOT see the "Usuarios" copy, the "Añadir
  usuario" button or the creation modal. `GET /users` answers `501` for trainers, so opening `/users` by URL shows
  the load error.
- app, `USER`: wiring only (container keys). No screen, hook or copy changes.

Hiding the button is a presentation choice; the API enforces who may create users (`403`).

### Components (atomic level, reuse)

- Atoms reused, none added: `table*`, `dialog*`, `button`, `text-field`, `chip`, `menu-item`, `alert`, `typography`,
  `skeleton`.
- Molecules: new `RoleFilter` (pills, shaped like `workouts/.../label-filter`); reused `profile-name-fields`,
  `HomeSkeleton`, `LoadError`.
- Organisms: new `UsersTable`, `CreateUserDialog`; extended `navigation-drawer`.

### Canonical references

All UI references come from the backoffice. Atom, molecule and organism references are `pending` in
`.stpr/template/README.md`, so this change follows the shapes established in #30.

- Use cases -> `get-workouts-use-case.ts` (list), `update-my-profile-use-case.ts` (create).
- Repository -> the existing `UserRepository` and `WorkoutRepository`.
- Form rules -> `core/src/application/validation/workout-rules.ts`.
- Route, view and list hook -> the assigned plans screen (`assigned-plans/page.tsx`, `assigned-plans-view.tsx`,
  `use-workouts.ts`).
- Create hook and dialog -> `use-create-workout.ts`, `create-workout-dialog.tsx`.
- Table -> `workouts-table.tsx`. Pills -> `label-filter.tsx`.
- Tests -> the assigned plans view test and `e2e/assigned-plans.spec.ts`.

### Conflicts and stop cases

None. Both endpoints exist in the API. No business rule enters the front: search and pills are view filters, and form
rules are structural (required, email format, the API's 100-character name limit, a real calendar date). Password
strength, email uniqueness and who may create users stay in the API. The trainer's `501` maps to `UNEXPECTED` through
the existing mapper; no new error code.

## Capabilities

### New Capabilities

- `user-administration`: `core` lists users and creates a user through the API, with structural rules for the create
  form, returning every failure as a `DomainError` code.
- `backoffice-users`: the backoffice users screen, with table, search, profile pills, and the admin-only creation
  modal.

### Modified Capabilities

- `backoffice-navigation`: the athletes section's copy depends on the role, and "Todos los usuarios" links to
  `/users` for admins and shows as selected there.

## Impact

- `core/openapi/v1.json`, `core/src/contract/generated` (regenerated).
- `core/src/domain/users`, `core/src/application/{ports,use-cases/users,validation,dependencies.ts}`,
  `core/src/infrastructure/repositories/user-repository.ts`, barrels, `core/test`.
- `backoffice/src/infrastructure/container.ts`, `app/src/infrastructure/container.ts`, both
  `test/support/render-with-providers.tsx`.
- `backoffice/src/app/users`, `backoffice/src/presentation/users`, `navigation-drawer.tsx`, `backoffice/src/i18n/{es,en}`
  (new `users` namespace, new `menu.users.*` keys), `backoffice/test`, `backoffice/e2e/users.spec.ts`.
- No new dependency, no API change, no error mapper change.
