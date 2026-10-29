# Proposal

Source brief: `.stpr/plan/openspec/proposal-change-add-backoffice-user-activation.md`.

## Why

The API can now switch an account off and back on (`PUT /users/{id}/deactivate`, `PUT /users/{id}/activate`, API
change `add-user-activation`), and `GET /users` now tells whether each user is active. The backoffice cannot use
either. Admins have no way to suspend or restore an account. The users table cannot tell active from inactive users,
and inactive accounts clutter the list admins work with every day.

## What Changes

- **Snapshot refresh**: `openapi:refresh` + `openapi:generate` pick up both new endpoints and the new `GET /users`
  item type (`UserSummaryResponse`: the current fields plus `isActive` and `deactivatedAtUtc`), in a commit of their
  own. No hand-written type.
- **core**:
  - a `UserSummary` alias for the list item; the list operation (`getAll`, `GetUsersUseCase`) is retyped to it, and
    `UserProfile` stays for `/users/me`;
  - `activate(id)` / `deactivate(id)` on the user repository;
  - `ActivateUserUseCase` and `DeactivateUserUseCase`;
  - the keys `activateUserUseCase` / `deactivateUserUseCase` in `Dependencies`.

  Both fronts register them. The app only gets the wiring.

- **backoffice `/users` screen**:
  - a "Mostrar inactivos" switch in the filter area, beside the profile pills. It is off on load, so only active
    users show. It combines with the search and the pills.
  - an inactive row is muted and shows an "Inactivo" chip beside the name. The chip's tooltip gives the
    deactivation date. Active rows show nothing extra.
  - for `ADMIN`, a trailing actions column with a ⋮ button per row. Its menu offers "Desactivar" on an active user
    and "Activar" on an inactive one. The caller's own row has no ⋮.
  - a confirmation dialog before each action. It names the user and states the effect. Deactivation uses a
    destructive confirm button. Success closes the dialog, shows a success message and reloads the list. Failure
    keeps the dialog open with the error. Cancel sends nothing.

### Fronts and roles

- backoffice, `ADMIN`: sees the switch, the inactive marker, the actions column, its menu and the confirmation dialog.
- backoffice, `TRAINER`: `GET /users` still answers `501`, so the screen shows the load error as today. Must NOT see
  the actions column, the ⋮ menu or the confirmation dialog, even if a list were returned.
- app, `USER`: wiring only (container keys). No screen, hook or copy changes.

Hiding the actions, and hiding them on the caller's own row, is a presentation choice. The API enforces both rules
(`403`).

### Components (atomic level, reuse)

- Reused atoms: `switch`, `chip`, `tooltip`, `icon-button`, `menu`, `menu-item`, `dialog`, `dialog-title`,
  `dialog-content`, `dialog-actions`, `alert`, `button`, the table atoms and `typography`.
- New molecules (`users/components/molecules`): `UserStatusChip` ("Inactivo" chip with the date tooltip),
  `UserRowActions` (⋮ button + one-item menu), `InactiveFilter` (labelled switch).
- New organism (`users/components/organisms`): `ChangeUserActivationDialog`.
- Extended organism: `UsersTable` (muted rows, status chip, optional actions column).
- Extended view: `UsersView`.

### Canonical references

`.stpr/template/README.md` marks the atom, molecule and organism references as `pending`. This change follows the
backoffice shapes established in #30 and the users screen. UI references come from the backoffice only.

- Aliases -> `core/src/domain/users/user.ts`, extended in place.
- `UserRepository.activate` / `deactivate` -> the existing `UserRepository` methods: `PUT` through
  `HttpClientInterface`, failures through `mapHttpError`.
- `ActivateUserUseCase` / `DeactivateUserUseCase` -> `core/src/application/use-cases/users/create-user-use-case.ts`.
- `useChangeUserActivation` -> `backoffice/src/presentation/users/hooks/use-create-user.ts`.
- `ChangeUserActivationDialog` -> `backoffice/src/presentation/users/components/organisms/create-user-dialog.tsx`.
- `UserRowActions` -> the icon-button + menu usage in `common/components/organisms/user-menu.tsx`.
- `UserStatusChip` -> `backoffice/src/presentation/users/components/molecules/role-filter.tsx`, a molecule over `chip`.
- `InactiveFilter` -> `common/components/organisms/theme-mode-switch.tsx`, the existing `switch` usage.
- Tests -> `users-view.test.tsx`, `create-user-dialog.test.tsx`, `filter-users.test.ts`, `user-repository.test.ts`
  and the create-user use case test, all extended or mirrored. E2E -> `backoffice/e2e/users.spec.ts`.

### Conflicts and stop cases

- **No stop case.** Both endpoints exist in the API, and the front adds no business rule. Hiding the action on the
  caller's own row mirrors an API rule as a presentation choice.
- **Sequencing.** This change builds on `add-backoffice-users-screen`, which is implemented but still has open tasks
  (drawer, E2E `users.spec.ts`, final checks) and is not archived. That change must be completed and archived first:
  this change modifies its `backoffice-users` and `user-administration` requirements and extends its E2E spec.

## Capabilities

### New Capabilities

None.

### Modified Capabilities

- `user-administration`: the listed users carry their activation state; new requirements to activate and deactivate a
  user, with their failures returned as domain error codes. (Introduced by `add-backoffice-users-screen`.)
- `backoffice-users`: inactive users are hidden by default and shown by a switch; inactive rows are marked; admins
  activate and deactivate users from the row after a confirmation. (Introduced by `add-backoffice-users-screen`.)

## Impact

- `core`: `core/openapi/v1.json`, `core/src/contract/generated` (regenerated), `domain/users/user.ts`, the user
  repository contract and implementation, two use cases, `dependencies.ts`, barrels and their tests.
- `backoffice`: `container.ts`, the test support placeholders, `presentation/users` (view, table, filter, hooks, new
  molecules and organism), `i18n/{es,en}/users.json`, component tests and `e2e/users.spec.ts`.
- `app`: `container.ts`, the test support placeholders and the container test only.
- No new dependency, no new error code, no change to the HTTP error mapper, the API, `/users/me`, the creation flow
  or the drawer.
