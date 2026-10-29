# Tasks

## 0. Prerequisite

- [x] 0.1 Confirm `add-backoffice-users-screen` is complete and archived: `openspec list` no longer shows it, and
      `openspec/specs/backoffice-users` and `openspec/specs/user-administration` exist. If not, stop and finish that
      change first.

## 1. API contract snapshot

- [x] 1.1 With the API (branch with `add-user-activation`) running on `http://localhost:5199`, run
      `pnpm --filter @weight-tracker/core openapi:refresh` then `openapi:generate`. Then:
  - verify `core/openapi/v1.json` lists `PUT /users/{id}/activate` and `PUT /users/{id}/deactivate`;
  - verify `GET /users` answers `UserSummaryResponse[]` with `isActive` and `deactivatedAtUtc`;
  - review the diff and commit the snapshot and generated types alone.

  Never hand-edit `core/src/contract/generated`.

## 2. Core: list type and activation operations

- [x] 2.1 Add the `UserSummary` alias to `core/src/domain/users/user.ts`, exported from the domain barrel. Retype
      `UserRepositoryInterface.getAll`, `UserRepository.getAll` and `GetUsersUseCase` to `UserSummary[]`. Update the
      existing `getAll` and `GetUsersUseCase` test fixtures with `isActive` / `deactivatedAtUtc`, and add the "Activation
      state kept" scenario. Verify with `pnpm --filter @weight-tracker/core typecheck` and the core tests.
- [x] 2.2 Add `activate(id)` and `deactivate(id)` to `UserRepositoryInterface` and implement them in `UserRepository`
      (`PUT /users/${id}/activate|deactivate`, no body, failures through `mapHttpError`). Extend
      `user-repository.test.ts` for both methods, using AAA with one Act each:
  - `200` (the path is checked and no body is sent);
  - `400`, `401`, `403`, `404`, `409`;
  - `500` → `UNEXPECTED`;
  - network → `NETWORK`.

  Verify with `pnpm --filter @weight-tracker/core test`.

- [x] 2.3 Create `ActivateUserUseCase` and `DeactivateUserUseCase` in `core/src/application/use-cases/users/`
      (template `create-user-use-case.ts`), exported from the barrel. Add their tests (template
      `create-user-use-case.test.ts`):
  - the id is forwarded;
  - success is returned;
  - a repository failure is returned unchanged.

  Verify with the core tests.

- [x] 2.4 Add `activateUserUseCase` and `deactivateUserUseCase` to `Dependencies`. Verify that the core typecheck
      now fails only in the two containers, which group 3 fixes.
- [x] 2.5 Review core against its references (`user-repository.ts`, `create-user-use-case.ts`) and the invariants:
      aliases only, no throw, `Result` everywhere. Verify that the drift loop from `AGENTS.md` prints nothing.

## 3. Wiring in both fronts

- [x] 3.1 Register both use cases in `backoffice/src/infrastructure/container.ts` and
      `app/src/infrastructure/container.ts`. Add their placeholders to both `test/support/render-with-providers.tsx`, and
      extend each container test with the two keys. Verify with `pnpm typecheck` and the container tests of both fronts.
- [x] 3.2 Review the app side: only the container, the test support and the container test changed. Verify with
      `git diff --stat app/`.

## 4. Backoffice: copy and filter

- [x] 4.1 Add the `list.showInactive`, `list.columns.actions`, `list.status.*`, `list.actions.*` and `activation.*`
      keys to `backoffice/src/i18n/{es,en}/users.json`, using the texts in design → Translations. Verify the translation
      parity test passes.
- [x] 4.2 Retype `filter-users.ts` to `UserSummary` and add the `showInactive` argument. Extend
      `filter-users.test.ts`:
  - inactive users hidden when the flag is off;
  - inactive users shown when it is on;
  - combined with a role and a search.

  Verify with `pnpm --filter backoffice test`.

## 5. Backoffice: state hooks

- [x] 5.1 Retype `use-users.ts` to `UserSummary[]`. Verify with `pnpm --filter backoffice typecheck`.
- [x] 5.2 Create `hooks/use-change-user-activation.ts` (template `use-create-user.ts`). It resolves
      `deactivateUserUseCase` when the user is active and `activateUserUseCase` otherwise, through `runWithSession`, and
      exposes `submitting`, `errorCode`, `change` and `reset`. Verify through the dialog tests in 6.3.

## 6. Backoffice: components

- [x] 6.1 Create the molecules `inactive-filter.tsx`, `user-status-chip.tsx` and `user-row-actions.tsx` in
      `users/components/molecules/` (templates `theme-mode-switch.tsx`, `role-filter.tsx`, `user-menu.tsx`). They use
      atoms only and take text by props. Verify with `pnpm --filter backoffice typecheck` and `pnpm lint`.
- [x] 6.2 Extend `organisms/users-table.tsx` with:
  - muted inactive rows;
  - `UserStatusChip` with the localized short date;
  - the optional actions column (`onToggleActivation`) with a visually hidden header;
  - no action on `currentUserId`'s row.

  Verify through the view tests in 7.2.

- [x] 6.3 Create `organisms/change-user-activation-dialog.tsx` (template `create-user-dialog.tsx`) and
      `test/presentation/users/components/organisms/change-user-activation-dialog.test.tsx` (template
      `create-user-dialog.test.tsx`). Cover:
  - deactivation copy and destructive button;
  - activation copy;
  - the email fallback in the title;
  - cancel sends nothing;
  - success calls `onChanged`;
  - `CONFLICT`, `NOT_FOUND` and `FORBIDDEN` messages with the dialog kept open;
  - closing after `CONFLICT` reports a stale list.

  Use AAA with one Act each. Verify with `pnpm --filter backoffice test`.

## 7. Backoffice: view

- [x] 7.1 Extend `views/users-view.tsx`:
  - add `showInactive` (off), `target` and `changed` state;
  - put `InactiveFilter` in the pill row;
  - compute the new `filtered` rule;
  - pass `onToggleActivation` for `ADMIN` only, plus `currentUserId`;
  - render the dialog for `ADMIN` only;
  - show the success `Alert` and reload on success or on a stale-list close.
- [x] 7.2 Extend `test/presentation/users/views/users-view.test.tsx` with the `backoffice-users` scenarios:
  - inactive hidden on load, with the switch off;
  - switch on and off, combined with a pill;
  - the inactive chip and its tooltip date;
  - only-inactive → no-match message;
  - the admin menu offers "Desactivar" or "Activar" by state;
  - no ⋮ on the admin's own row;
  - confirm → success message + reload;
  - the `TRAINER` fake list shows no actions column, ⋮ or dialog (assert absence);
  - English copy.

  Use AAA with one Act each. Verify with `pnpm --filter backoffice test`.

- [x] 7.3 Review the backoffice against its references (`create-user-dialog`, `use-create-user`, `role-filter`,
      `user-menu`, `theme-mode-switch`) and the `AGENTS.md` quality checklist:
  - atomic levels;
  - imports flow downward, and only atoms import MUI widgets;
  - no `core/infrastructure` import in presentation;
  - every string in `es` and `en`.

  Verify by reading the diff against each reference.

## 8. Backoffice E2E

- [x] 8.1 Extend `backoffice/e2e/users.spec.ts` with a flow:
  1. An admin session opens "Todos los usuarios".
  2. The admin turns on "Mostrar inactivos" and sees an inactive user marked "Inactivo".
  3. The admin opens the ⋮ of an active user, chooses "Desactivar" and confirms.
  4. The admin sees "Usuario desactivado." and the reloaded table.

  Intercept `GET /users/me`, `GET /users` (second response reflecting the change) and `PUT /users/{id}/deactivate`
  with `page.route`. Verify with `pnpm test:e2e`.

## 9. Final verification

- [x] 9.1 From `front/`, run `pnpm typecheck && pnpm lint && pnpm test && pnpm test:e2e` and verify that all pass.
      Then run the template drift loop from `AGENTS.md` and verify it prints nothing.

## Workflow follow-up

- Archive the change after review; it modifies `backoffice-users` and `user-administration`.
