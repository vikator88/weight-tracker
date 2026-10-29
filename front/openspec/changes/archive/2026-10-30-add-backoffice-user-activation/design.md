# Design

## Context

See proposal.md (Why) and the specs for behaviour. Current state this design builds on:

- `UserRepository.getAll` returns `UserProfile[]` (alias of `UserProfileResponse`). After the snapshot refresh,
  `GET /users` returns `UserSummaryResponse[]`, which adds `isActive` and `deactivatedAtUtc`.
- `UsersView` holds the search, the role pill, the create dialog flag and the "created" flag, and renders
  `UsersTable` over `filterUsers(users, search, role)`. `useUsers` exposes `{ status, users, errorCode, reload }`.
- `CreateUserDialog` + `useCreateUser` set the dialog pattern: `creating` / `errorCode` / `create` / `reset`, with
  errors read as `t("create.errors.<code>", { defaultValue: tc("errors:<code>") })`.
- The `switch` atom exists. Its only user, `ThemeModeSwitch`, labels it with `aria-label`.
- `add-backoffice-users-screen` is not archived and has open tasks (drawer, `e2e/users.spec.ts`, final checks).

## Goals / Non-Goals

**Goals:**

- Map both new operations into `core` with the same shapes as the existing user operations.
- Keep the activation flow local to the users screen, built from the existing dialog and filter patterns.

**Non-Goals:**

- No change to the HTTP error mapper, to `UserProfile` or to `/users/me`.
- No optimistic update or patching of the list from the `PUT` response.
- No shared confirmation-dialog component in `common`. Only one screen needs one today.

## Decisions

### Core

- **Alias.** `UserSummary = components["schemas"]["UserSummaryResponse"]` in `core/src/domain/users/user.ts`,
  re-exported from the domain barrel.
  - `UserRepositoryInterface.getAll`, `UserRepository.getAll` and `GetUsersUseCase` change their value type to
    `UserSummary[]`.
  - The alternative was to keep `UserProfile` and read the extra fields loosely. It was rejected: the contract has a
    distinct schema, and hand-widening a type is forbidden.
- **Repository.** `activate(id: string)` and `deactivate(id: string)` on `UserRepositoryInterface`, each returning
  `Result<UserSummary, DomainError>`.
  - `UserRepository` sends `PUT /users/${id}/activate|deactivate` with no `body`, and failures go through
    `mapHttpError`, like the existing methods.
  - The response is returned so the operation matches the contract, even though the screen ignores it.
- **Use cases.** `ActivateUserUseCase` and `DeactivateUserUseCase` in `core/src/application/use-cases/users/`.
  Each implements `UseCaseInterface<string, UserSummary>` and forwards the id. They are modelled on
  `CreateUserUseCase`.
  - Two use cases rather than one `ChangeUserActivationUseCase(id, active)`: they mirror two endpoints with
    different failure meanings, and keep the boolean out of the contract.
- **Dependencies.** New keys `activateUserUseCase` and `deactivateUserUseCase` in
  `core/src/application/dependencies.ts`. They are registered in `backoffice/src/infrastructure/container.ts` and
  `app/src/infrastructure/container.ts`, with placeholders in both `test/support/render-with-providers.tsx`.

### Backoffice

Components, all under `backoffice/src/presentation/users/components/`. Imports flow downward, and only atoms import
MUI widgets. Layout `Box` stays as already used across the backoffice.

- **`molecules/inactive-filter.tsx`** (`InactiveFilter`): `{ label, checked, onChange }`. It renders the `switch` atom
  with a visible label (a `Typography` beside it, linked with `aria-labelledby`), so no new `FormControlLabel` atom is
  needed.
  - It lives in `users`, not `common`, because no other screen uses it yet. If a second screen needs it, move it to
    `common/components/molecules`.
- **`molecules/user-status-chip.tsx`** (`UserStatusChip`): `{ label, tooltip }`. It renders a small outlined
  `chip` wrapped in the `tooltip` atom. The chip is focusable (`tabIndex={0}`), so keyboard users can reach the date.
- **`molecules/user-row-actions.tsx`** (`UserRowActions`): `{ label, actionLabel, onAction }`. It renders an
  `icon-button` with `MoreVert` and an `aria-label` from `label`, which opens a `menu` with one `menu-item`.
  - It holds only the menu's anchor element as local state, like `UserMenu`.
- **`organisms/users-table.tsx`** (extended):
  - New optional props: `onToggleActivation?: (user: UserSummary) => void` and `currentUserId?: string`.
  - The actions column (visually hidden header "Acciones") renders only when `onToggleActivation` is given. The view
    passes it for `ADMIN` only, so the role decision stays in one place.
  - The row of `currentUserId` gets an empty actions cell.
  - Inactive rows set `color: "text.secondary"` on their cells (`sx` on `TableRow`), and render `UserStatusChip`
    after the full name.
  - The date is formatted with `Intl.DateTimeFormat(i18n.language, { dateStyle: "short" })`, as in
    `use-week-label.ts`.
  - A background tint was rejected: it reads poorly in dark mode and competes with hover.
  - A Yes/No column was rejected: it is all "Sí" while the switch is off.
- **`organisms/change-user-activation-dialog.tsx`** (`ChangeUserActivationDialog`): `{ user: UserSummary | null,
onClose: (changed: boolean) => void, onChanged: (action) => void }`.
  - It is open while `user !== null`.
  - The action follows `user.isActive`: deactivate when active, activate when not.
  - The title and body come from `activation.<action>.*`, with `{{name}}` set to `fullNameOf(user) ?? user.email`.
  - Errors show in an `Alert` as `t("activation.errors.<code>", { defaultValue: tc("errors:<code>") })`.
  - The confirm button uses `color="error"` for deactivation and `color="primary"` for activation, and is disabled
    while submitting.
  - When the error is `CONFLICT` or `NOT_FOUND`, closing calls `onClose(true)` so the view reloads.
- **`hooks/use-change-user-activation.ts`** (`useChangeUserActivation`): `{ submitting, errorCode,
change(user): Promise<boolean>, reset }`.
  - It resolves `deactivateUserUseCase` or `activateUserUseCase` from `user.isActive` and runs it through
    `runWithSession`, modelled on `useCreateUser`.
  - The choice by current state is presentation routing, not a business rule: the API still answers `409` when the
    state is stale.
- **`filter-users.ts`**: `filterUsers(users, search, role, showInactive)` adds `(showInactive || user.isActive)`. Its
  parameter type becomes `UserSummary[]`, and `fullNameOf` keeps its `Pick` signature.
- **`hooks/use-users.ts`**: retyped to `UserSummary[]`. No behaviour change.
- **`views/users-view.tsx`**: new state `showInactive` (`false`), `target: UserSummary | null` and
  `changed: "activated" | "deactivated" | null`.
  - `filtered` becomes `search.trim() !== "" || role !== null || (!showInactive && users.some((u) => !u.isActive))`.
    An all-inactive list therefore shows the no-match message, not the empty one.
  - `InactiveFilter` sits in the same row as `RoleFilter`: a flex row that wraps, with the switch pushed to the end.
  - On success, the view sets `changed`, clears `created`, closes the dialog and calls `reload()`. Opening the create
    dialog clears `changed`, so only one success `Alert` shows at a time.
  - `UsersTable` gets `onToggleActivation={isAdmin ? setTarget : undefined}` and
    `currentUserId={current.profile?.id}`.

### Translations

New keys in `backoffice/src/i18n/{es,en}/users.json`, with exactly the texts listed in the brief's Errors And Copy:

- `list.showInactive`;
- `list.columns.actions`;
- `list.status.inactive`, `list.status.deactivatedAt`;
- `list.actions.open`, `list.actions.activate`, `list.actions.deactivate`;
- `activation.{deactivate,activate}.{title,body,confirm,success}`, `activation.cancel`;
- `activation.errors.NOT_FOUND`, `activation.errors.CONFLICT`.

The parity test covers them. The app gets no copy.

### Errors

- `CONFLICT` -> "El usuario ya estaba en ese estado. Recarga la lista." / "The user was already in that state. Reload
  the list."
- `NOT_FOUND` -> "Este usuario ya no existe." / "This user no longer exists."
- `FORBIDDEN`, `VALIDATION`, `NETWORK`, `UNEXPECTED` -> the existing `errors:<code>` messages through `defaultValue`.
- `UNAUTHORIZED` -> the existing session handling in `runWithSession`.
- No new `DomainErrorCode`. `http-error-mapper.ts` is untouched.

### State

- `useUsers`: the list, its status and its error code. It is set on mount and on `reload`, which also runs after a
  successful change or a stale-list close, and resets on screen exit.
- `UsersView`: `search`, `role`, `showInactive` (starts `false`, not persisted), `target`, `creating`, `created` and
  `changed`. All are set by the user and reset on screen exit.
- `useChangeUserActivation`: `submitting` and `errorCode`. `reset` runs when the dialog closes.

### Tests

They follow the existing tier templates, and the test helpers do not change apart from the new placeholders.

- **core.**
  - `user-repository.test.ts` gains `activate` and `deactivate`: `200`, `400`, `401`, `403`, `404`, `409`, `500`
    -> `UNEXPECTED`, and network -> `NETWORK`.
  - The `getAll` fixtures gain `isActive` and `deactivatedAtUtc`, including the "activation state kept" scenario.
  - `activate-user-use-case.test.ts` and `deactivate-user-use-case.test.ts` mirror `create-user-use-case.test.ts`.
- **Containers.** Both fronts' container tests gain the two keys.
- **backoffice.**
  - `filter-users.test.ts` gains the `showInactive` cases.
  - `users-view.test.tsx` covers the `backoffice-users` scenarios, including the `TRAINER` absence assertions and the
    own-row case.
  - `change-user-activation-dialog.test.tsx` (template `create-user-dialog.test.tsx`) covers the copy for each
    action, the email fallback, cancel without a request, each failure and the stale-list close.
- **E2E.** `backoffice/e2e/users.spec.ts` gains a flow: the admin turns on the switch, deactivates a user through the
  menu and the dialog, and sees the reloaded table. `GET /users/me`, `GET /users` and
  `PUT /users/{id}/deactivate` are intercepted.

## Risks / Trade-offs

- [The previous change is not archived] → The MODIFIED deltas target requirements that exist only in
  `add-backoffice-users-screen`. Finish and archive that change before applying this one. Task 0 checks it.
- [The `GET /users` type change touches existing code and tests] → Retype in the same step as the snapshot refresh so
  typecheck stays green, and update the existing fixtures in the same group.
- [Stale list after a concurrent change] → The API answers `409`/`404`, the dialog explains it, and closing reloads.
- [The switch filters on the client] → The whole list is already loaded, which is acceptable at today's size.
  Server-side filtering is out of scope, as the API offers none.
- [An access token issued before deactivation stays valid up to 15 minutes] → This is API behaviour and outside this
  change.

## Migration Plan

1. Complete and archive `add-backoffice-users-screen`.
2. Refresh the snapshot with the API running and commit it alone. Rollback is a revert of that commit plus the
   front code.
