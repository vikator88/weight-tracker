# Change Proposal: Activate And Deactivate Users From The Backoffice Users Table

Source brief for `/opsx:propose`. Artifacts go to `openspec/changes/add-backoffice-user-activation/`.

## Change

Refresh the OpenAPI snapshot to pick up `PUT /users/{id}/activate`, `PUT /users/{id}/deactivate` and the new
`GET /users` item shape (`isActive`, `deactivatedAtUtc`). Add `ActivateUserUseCase` and `DeactivateUserUseCase` to
`core`. In the backoffice "Todos los usuarios" screen:

- show which users are inactive;
- add a "Mostrar inactivos" switch next to the profile filter, off by default;
- let an `ADMIN` activate or deactivate a user from the row, after a confirmation dialog.

## Objective

The API can now switch an account off and back on (see `api/.stpr/plan/openspec/proposal-change-add-user-activation.md`),
but administrators have no way to do it, and the users table cannot tell an active account from an inactive one.
Inactive accounts also clutter the list an admin works with day to day, so the table hides them unless asked.

## Capabilities

- Activate a user (`PUT /users/{id}/activate`) and deactivate a user (`PUT /users/{id}/deactivate`). Both are wired in
  `core` and both containers.
- The list reads the new `GET /users` item type, which carries `isActive` and `deactivatedAtUtc`.
- "Mostrar inactivos" switch in the filter area, beside the profile pills. Off by default: the table shows active
  users only. On: the table also shows inactive users. It combines with the search and the profile pills.
- Inactive status in the table (proposed UX, see Notes): an inactive row is visually muted and shows an
  "Inactivo" chip beside the full name. A tooltip on the chip gives the deactivation date. Active rows show nothing
  extra. Without a switch-on, every visible row is active, so a dedicated "Estado" column would only add noise.
- Row actions (`ADMIN` only): a trailing actions column with an overflow (⋮) button per row. Its menu offers
  "Desactivar" for an active user and "Activar" for an inactive user. The caller's own row shows no action, because
  the API forbids self-deactivation.
- Confirmation dialog before each action. It names the user (full name, or email when there is none) and states the
  effect. Deactivation uses a destructive (error-coloured) confirm button. Activation uses the primary one.
- On success, the dialog closes, a success `Alert` shows, and the list reloads. A user deactivated while the switch is
  off then leaves the table, which is expected. On failure, the dialog stays open and shows the error.

## Fronts And Roles

- backoffice -> lands here: yes -> roles: `ADMIN`, `TRAINER`
  - `ADMIN`: sees the inactive chip and muted rows, the switch, the actions column and its menu, and the confirmation
    dialog.
  - `TRAINER`: the screen still answers `501` from `GET /users` and shows `LoadError`, as today. Must NOT see the
    actions column, the row menu or the confirmation dialog. The switch and status rendering are harmless, but the
    trainer never reaches a loaded table.
- app -> lands here: wiring only -> roles: `USER` -> the container registers `activateUserUseCase` and
  `deactivateUserUseCase`. No screen, hook or copy changes.

The front does not decide who may change activation. It hides the actions for non-admins and on the caller's own row
as a presentation choice. The API enforces the rules (`403`). The role and own id come from
`useCurrentUser().profile`.

## Classification

Core:

- `UserSummary` (alias over `components["schemas"]["UserSummaryResponse"]`) -> entity type ->
  `core/src/domain/users/user.ts`. `GET /users` now returns this type instead of `UserProfileResponse`, so
  `getAll()` and `GetUsersUseCase` change their return type. `UserProfile` stays for `/users/me`.
- `activate(id)` and `deactivate(id)` -> repository contract methods -> the users repository contract in
  `core/src/application/ports/`
- `UserRepository.activate` / `deactivate` -> repository implementation ->
  `core/src/infrastructure/repositories/user-repository.ts`
- `ActivateUserUseCase` -> use case -> `core/src/application/use-cases/users/activate-user-use-case.ts`
- `DeactivateUserUseCase` -> use case -> `core/src/application/use-cases/users/deactivate-user-use-case.ts`
- `Dependencies` keys `activateUserUseCase`, `deactivateUserUseCase` -> composition contract ->
  `core/src/application/dependencies.ts`
- Barrels re-export the new alias and use cases

Backoffice and app wiring:

- Registrations of both use cases -> composition root -> `backoffice/src/infrastructure/container.ts` and
  `app/src/infrastructure/container.ts`
- Placeholders for the new keys -> test helper -> `backoffice/test/support/render-with-providers.tsx` and
  `app/test/support/render-with-providers.tsx`

Backoffice screen (`users` domain area, extended in place):

- `UsersView` -> view -> `backoffice/src/presentation/users/views/users-view.tsx`: holds the switch state and the
  target of the pending action, and opens the dialog
- `useUsers` -> state hook -> `backoffice/src/presentation/users/hooks/use-users.ts`: typed to `UserSummary[]`
- `useChangeUserActivation` (submit, submitting, errorCode; activate or deactivate by the target's current state) ->
  state hook -> `backoffice/src/presentation/users/hooks/use-change-user-activation.ts`
- `filterUsers` -> view filter -> `backoffice/src/presentation/users/filter-users.ts`: gains a `showInactive`
  argument
- `UsersTable` -> organism -> `backoffice/src/presentation/users/components/organisms/users-table.tsx`: muted inactive
  rows, inactive chip, optional actions column (rendered only when it receives an action callback)
- `UserRowActions` (⋮ button + menu with one item) -> molecule ->
  `backoffice/src/presentation/users/components/molecules/user-row-actions.tsx`
- `UserStatusChip` ("Inactivo" chip + date tooltip) -> molecule ->
  `backoffice/src/presentation/users/components/molecules/user-status-chip.tsx`
- `ChangeUserActivationDialog` (confirmation, error `Alert`, confirm and cancel) -> organism ->
  `backoffice/src/presentation/users/components/organisms/change-user-activation-dialog.tsx`
- Inactive switch -> reuse the `switch` atom. If a labelled switch needs `FormControlLabel`, add it as a molecule
  (`labelled-switch`) in `common/components/molecules`; decide at proposal time.
- Chip, tooltip, icon button, menu, menu item, dialog, alert, button -> reuse existing atoms in
  `common/components/atoms/`
- Copy -> new keys in the `users` namespace (`backoffice/src/i18n/{es,en}/users.json`)

## Canonical Template Mapping

- `UserSummary` alias -> `core/src/domain/users/user.ts` -> extended in place, alias over `components["schemas"]` only.
- `UserRepository.activate` / `deactivate` -> the `WorkoutRepository` status-change or update methods -> `PUT` with no
  body through `HttpClientInterface`, failures through `mapHttpError`.
- `ActivateUserUseCase` / `DeactivateUserUseCase` -> `core/src/application/use-cases/users/create-user-use-case.ts` ->
  input forwarded to the repository, `Result` returned as is.
- Core tests -> the tests of those same files plus `user-repository.test.ts`.
- `useChangeUserActivation` -> `backoffice/src/presentation/users/hooks/use-create-user.ts` -> `submitting` /
  `errorCode`, `Result`-driven.
- `ChangeUserActivationDialog` -> `backoffice/src/presentation/users/components/organisms/create-user-dialog.tsx` ->
  dialog organism with confirm, cancel and error `Alert`, without a form.
- `UserRowActions` -> the backoffice's existing icon-button + menu usage (`common/components/organisms/user-menu.tsx`)
  -> same open/close pattern, reduced to a molecule because it calls no use case.
- `UserStatusChip` -> `backoffice/src/presentation/users/components/molecules/role-filter.tsx` -> molecule over the
  `chip` atom, text by props.
- Switch -> `common/components/organisms/theme-mode-switch.tsx` -> existing usage of the `switch` atom.
- Atom/molecule/organism references are `pending` in `.stpr/template/README.md`. Follow the backoffice shapes
  established in #30.
- View test -> `backoffice/test/presentation/users/views/users-view.test.tsx` -> extended in place.
- E2E -> `backoffice/e2e/users.spec.ts` -> extended in place, `page.route` interception.

UI references come from the backoffice only. `.stpr/template/` and its README are not updated here.

## Screens And Navigation

- backoffice -> `/users` -> `UsersView` -> unchanged entry (drawer "Usuarios" > "Todos los usuarios", `ADMIN`) ->
  table loads with the switch off (active users only).
- Row ⋮ > "Desactivar" / "Activar" (`ADMIN`, not on the caller's own row) -> `ChangeUserActivationDialog` -> confirm:
  success closes the dialog, shows the success `Alert` and reloads the list; failure keeps the dialog open with the
  error message. Cancel closes without a request. `401` follows the existing session handling.

## API Contract

- `GET /users` -> in `core/openapi/v1.json`: **changed, refresh needed** -> response `200` `UserSummaryResponse[]`
  (`id`, `email`, `name`, `surname`, `role`, `isActive`, `deactivatedAtUtc` nullable).
- `PUT /users/{id}/activate` -> in `core/openapi/v1.json`: **no, refresh needed** -> no body, response `200`
  `UserSummaryResponse`. Failures: `400` malformed id, `401`, `403` non-admin, `404` unknown id, `409` already active.
- `PUT /users/{id}/deactivate` -> in `core/openapi/v1.json`: **no, refresh needed** -> no body, response `200`
  `UserSummaryResponse`. Failures: `400`, `401`, `403` (non-admin, or the caller's own account), `404`, `409`
  already deactivated.

Provided by the API change `add-user-activation`. This change runs `openapi:refresh` + `openapi:generate` once, with
the API running, and commits the snapshot and the generated types as a step of their own. No hand-written type.

The returned `UserSummaryResponse` is not used to patch the list in place. The screen reloads, as after creation.

## Held State

- `useUsers` -> the loaded `UserSummary[]`, status and error code -> the hook on mount and on `reload` (also after a
  successful activation change) -> resets on screen exit.
- `UsersView` local state -> search text, selected role pill, `showInactive` (initially `false`), the user targeted by
  the pending action (`null` when the dialog is closed), the success message -> the user -> resets on screen exit.
  The switch is not persisted across visits.
- `useChangeUserActivation` -> `submitting` and `errorCode` -> the dialog's confirm -> resets when the dialog closes.

## Forms And Structural Validation

None. The confirmation dialog has no fields.

## Errors And Copy

- Session expired / missing -> `UNAUTHORIZED` -> existing session handling.
- Caller is not an admin, or targets their own account -> `FORBIDDEN` -> dialog error with the existing message.
  The own-row action is hidden, so this is only reachable through a race.
- User no longer exists -> `NOT_FOUND` -> dialog error "Este usuario ya no existe." The user can close and the list
  reloads.
- Already in the requested state (someone else changed it) -> `CONFLICT` -> dialog error
  "El usuario ya estaba en ese estado. Recarga la lista." Closing the dialog after a `CONFLICT` or `NOT_FOUND`
  reloads the list.
- No connection -> `NETWORK`. Anything else -> `UNEXPECTED` -> generic error.
- No new error code. The HTTP error mapper is not touched. (`USER_DEACTIVATED` on login is a separate change.)

New copy, `users` namespace:

- `list.showInactive` -> "Mostrar inactivos" -> "Show inactive"
- `list.columns.actions` -> "Acciones" (visually hidden header, for accessibility) -> "Actions"
- `list.status.inactive` -> "Inactivo" -> "Inactive"
- `list.status.deactivatedAt` -> "Desactivado el {{date}}" -> "Deactivated on {{date}}"
- `list.actions.open` -> "Acciones de {{name}}" (⋮ button `aria-label`) -> "Actions for {{name}}"
- `list.actions.activate` -> "Activar" -> "Activate"
- `list.actions.deactivate` -> "Desactivar" -> "Deactivate"
- `activation.deactivate.title` -> "¿Desactivar a {{name}}?" -> "Deactivate {{name}}?"
- `activation.deactivate.body` -> "No podrá iniciar sesión ni recibir nuevos entrenamientos. Sus datos se conservan
  y podrás reactivarlo cuando quieras." -> "They will not be able to log in or be given new workouts. Their data is
  kept and you can reactivate them at any time."
- `activation.deactivate.confirm` -> "Desactivar" -> "Deactivate"
- `activation.deactivate.success` -> "Usuario desactivado." -> "User deactivated."
- `activation.activate.title` -> "¿Activar a {{name}}?" -> "Activate {{name}}?"
- `activation.activate.body` -> "Podrá volver a iniciar sesión y recibir entrenamientos." -> "They will be able to
  log in and be given workouts again."
- `activation.activate.confirm` -> "Activar" -> "Activate"
- `activation.activate.success` -> "Usuario activado." -> "User activated."
- `activation.cancel` -> "Cancelar" -> "Cancel"
- `activation.errors.NOT_FOUND`, `activation.errors.CONFLICT` -> as above -> "This user no longer exists.",
  "The user was already in that state. Reload the list."

## Platform Capabilities And Dependencies

None.

## Out Of Scope

- Showing the `USER_DEACTIVATED` login error in either front's login screen. It needs a new error code in the mapper
  and is a separate change.
- Server-side filtering by active state. The API does not offer it, and the switch filters the loaded list.
- Bulk activation or deactivation, a deactivation reason, deleting users, editing role or password.
- Persisting the switch across visits.
- Hiding inactive users in other screens (for example, the owner or trainer pickers in workouts). The API already
  rejects assigning workouts to them. Follow up separately if the pickers need it.
- Any app screen.
- Must not change: the API, `/users/me` and `UserProfile`, the creation flow, or the drawer.

## Notes

- **No stop case.** Both endpoints exist in the API, and no business rule is added to the front. Hiding the action on
  the caller's own row mirrors an API rule as a presentation choice. The API remains the authority.
- **UX choice for status (proposed).** Muted row (secondary text colour) plus an "Inactivo" chip beside the name,
  with the deactivation date in its tooltip. This is the "exception marker" pattern common in modern admin tables
  (GitHub, Linear, Vercel): the normal state stays quiet and only the exception is marked. A Yes/No column or a full
  background tint were considered and discarded. With the switch off, the column would always say "Sí", and a tint
  harms contrast in dark mode. Confirm at proposal time.
- **UX choice for the action (proposed).** A row overflow menu (⋮) rather than an inline button. It keeps the table
  quiet, avoids an accidental click on a destructive action, and has room for future row actions. The confirmation
  dialog uses a destructive button for deactivation only.
- **Switch placement.** Same row as the profile pills, aligned to the end on wide screens and wrapping below on
  narrow ones.
- **Filter semantics.** `showInactive = false` keeps `isActive === true` only. `true` keeps all. When the switch hides
  every row but inactive users exist, the table shows the "no match" state, not "Todavía no hay usuarios."
  (`filtered` is true when search, a pill or the switch removed rows).
- **Date format.** `deactivatedAtUtc` is shown in the active language's short date format, in the browser's time
  zone.
- **Snapshot refresh** is its own commit: `core/openapi/v1.json` plus `core/src/contract/generated`, never edited by
  hand. Retyping `getAll` / `GetUsersUseCase` / `useUsers` from `UserProfile` to `UserSummary` belongs with it or
  right after it.
- **Verification tiers:**
  - core unit: `activate-user-use-case.test.ts` and `deactivate-user-use-case.test.ts` (id forwarded, success
    returned, repository failure returned unchanged). `user-repository.test.ts` gains `activate` and `deactivate`
    (`PUT /users/{id}/activate|deactivate`): `200`, `400`, `401`, `403`, `404`, `409`, `500` -> `UNEXPECTED`,
    network -> `NETWORK`. The `getAll` test fixtures carry `isActive` / `deactivatedAtUtc`.
  - container tests in both fronts gain the new keys.
  - backoffice component tests (`users-view.test.tsx`, extended):
    - the switch is off on load, and inactive users are absent; switching it on shows them muted with the
      "Inactivo" chip; the switch combines with search and pills.
    - `ADMIN`: the ⋮ menu offers "Desactivar" on an active row and "Activar" on an inactive one; the own row has no
      ⋮; confirm calls the matching use case, closes the dialog, shows the success alert and reloads; cancel sends
      nothing; `CONFLICT`, `NOT_FOUND` and `FORBIDDEN` keep the dialog open with their message.
    - `TRAINER`: no actions column, no ⋮ and no dialog (assert absence), with a fake that returns a list.
    - `filter-users` unit cases for `showInactive`.
  - backoffice E2E `e2e/users.spec.ts`: the admin turns on "Mostrar inactivos", deactivates an active user through
    the menu and dialog, and sees the reloaded table (`GET /users` and `PUT /users/{id}/deactivate` intercepted).
  - translation parity passes with the new keys.
- Done means `pnpm typecheck && pnpm lint && pnpm test && pnpm test:e2e` passes from `front/`.
- No deviation from the mapped references is allowed.
