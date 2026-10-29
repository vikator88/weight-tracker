# Tasks

## 1. API contract snapshot

- [x] 1.1 With the API running on `http://localhost:5199`, run `pnpm --filter @weight-tracker/core openapi:refresh` then `openapi:generate`. Verify `core/openapi/v1.json` lists `GET /users` and `POST /users`, and that the regenerated types include `CreateUserRequest` and `CreatedUserResponse`. Review the diff and commit snapshot and generated types alone. Never hand-edit `core/src/contract/generated`.

## 2. Core: users list and creation

- [x] 2.1 Add the `CreateUserRequest` and `CreatedUserResponse` aliases (and `UserRole` if the generated role is a union) to `core/src/domain/users/user.ts`, re-exported from the domain barrel. Verify with `pnpm --filter @weight-tracker/core typecheck`.
- [x] 2.2 Add `getAll()` and `create(request)` to `UserRepositoryInterface`, then implement them in `UserRepository` (`GET /users`, `POST /users` with the body as given, failures through `mapHttpError`). Extend `core/test/.../user-repository.test.ts`:
  - success for both methods;
  - `400`, `401`, `403`, `409`;
  - `501` → `UNEXPECTED`;
  - `500` → `UNEXPECTED`;
  - network → `NETWORK`.

  Use AAA with one Act each. Verify with `pnpm --filter @weight-tracker/core test`.

- [x] 2.3 Create `GetUsersUseCase` (template `get-workouts-use-case.ts`) and `CreateUserUseCase` (template `update-my-profile-use-case.ts`) in `core/src/application/use-cases/users/`, exported from the barrel. Add their tests:
  - success returned;
  - request forwarded unchanged;
  - repository failure returned unchanged.

  Verify with the core tests.

- [x] 2.4 Add `getUsersUseCase` and `createUserUseCase` to `Dependencies`. Verify that the core typecheck now fails only in the two containers, which group 3 fixes.
- [x] 2.5 Move the calendar-date check from `workout-rules.ts` into a shared internal validation module. Then create `core/src/application/validation/user-rules.ts` (`CreateUserFormValues`, `CreateUserFormViolations`, `validateCreateUserForm`; violations `required`, `email`, `maxLength` reusing `PROFILE_NAME_MAX_LENGTH`, `date`) and export it. Add `user-rules.test.ts` covering every scenario of the `user-administration` form requirement. Verify the core tests pass, workout rule tests included, unchanged.
- [x] 2.6 Review core against its references (`user-repository.ts`, `get-workouts-use-case.ts`, `update-my-profile-use-case.ts`, `workout-rules.ts`) and the invariants: aliases only, no throw, `Result` everywhere. Verify the drift loop from `AGENTS.md` prints nothing.

## 3. Wiring in both fronts

- [x] 3.1 Register `getUsersUseCase` and `createUserUseCase` in `backoffice/src/infrastructure/container.ts` and `app/src/infrastructure/container.ts`, and add their placeholders to both `test/support/render-with-providers.tsx`. Extend each container test with the two keys. Verify with `pnpm typecheck` and the container tests of both fronts.

## 4. Backoffice: copy and helpers

- [x] 4.1 Create `backoffice/src/i18n/{es,en}/users.json` with the `list.*`, `roles.*` and `create.*` keys (design → Translations) and register the namespace in `resources.ts`. Add `menu.users.title` / `menu.users.all` to both `common.json`. Verify the translation parity test passes.
- [x] 4.2 Create `src/presentation/users/filter-users.ts` (`filterUsers`, `fullNameOf`) and `test/presentation/users/filter-users.test.ts`. Cover:
  - matches on email, name and surname;
  - case and accent insensitivity;
  - role filter;
  - role and search combined;
  - empty search;
  - full name with empty parts → `null`.

  Verify with `pnpm --filter backoffice test`.

## 5. Backoffice: state hooks

- [x] 5.1 Create `hooks/use-users.ts` (copy of `use-workouts.ts` over `getUsersUseCase`) and `hooks/use-create-user.ts` (copy of `use-create-workout.ts` over `createUserUseCase`). Verify with the backoffice typecheck.

## 6. Backoffice: components

- [x] 6.1 Create the `RoleFilter` molecule (`components/molecules/role-filter.tsx`), shaped like `label-filter.tsx`, using the `chip` and `typography` atoms. Verify with the backoffice typecheck and lint (no MUI widget outside atoms).
- [x] 6.2 Create the `UsersTable` organism (`components/organisms/users-table.tsx`), shaped like `workouts-table.tsx`, with "Nombre completo", "Email" and "Perfil", "—" for an empty name, and the empty and no-match messages. Verify with the backoffice typecheck and lint.
- [x] 6.3 Create the `CreateUserDialog` organism (`components/organisms/create-user-dialog.tsx`), shaped like `create-workout-dialog.tsx`:
  - fields:
    - email and password `TextField`s;
    - a profile select (`TextField` `select` + `MenuItem`) defaulting to `USER`;
    - the `profile-name-fields` molecule;
    - a date of birth `TextField` with `type="date"`;
  - submit runs `validateCreateUserForm`;
  - `toRequest` trims names and omits empty optional fields;
  - `onCreated` fires on success;
  - error `Alert` per code with fallback to `errors:<code>`.

  Add `test/presentation/users/components/organisms/create-user-dialog.test.tsx`. Cover:
  - required errors with no request sent;
  - malformed email;
  - only the required fields sent;
  - every field sent trimmed;
  - success fires `onCreated`;
  - `CONFLICT` and `VALIDATION` keep the values and show their messages;
  - cancel sends nothing;
  - reopening starts empty.

  Use fakes through the container and AAA. Verify with the backoffice tests.

## 7. Backoffice: view and route

- [x] 7.1 Create `views/users-view.tsx` (template `assigned-plans-view.tsx`): heading, search field, `RoleFilter`, admin-only "Añadir usuario" button, success `Alert`, `UsersTable` fed by `filterUsers`, skeleton and `LoadError`, and a reload after creation. Create `backoffice/src/app/users/page.tsx` that only default-exports the view. Verify with the backoffice typecheck.
- [x] 7.2 Add `test/presentation/users/views/users-view.test.tsx` (template: the assigned plans view test). Cover every `backoffice-users` screen scenario:
  - rows in API order, with full name, email and profile;
  - "—" for a user without a name;
  - search by email, name and surname;
  - pill select and clear;
  - pill and search combined;
  - loading, empty, no match;
  - failure with retry;
  - the admin's button opens the modal;
  - a trainer has no button and no modal;
  - a trainer's `UNEXPECTED` shows `LoadError`;
  - success closes the modal, shows "Usuario creado." and requests the users again;
  - English heading and button.

  Render in `es` unless the test is about language. Verify with the backoffice tests.

## 8. Backoffice: drawer

- [x] 8.1 Make the athletes section in `navigation-drawer.tsx` depend on `useCurrentUser()`: `ADMIN` gets `menu.users.*` with `href: "/users"`, anyone else keeps `menu.athletes.*` with no `href`. Extend the drawer test:
  - admin copy, link to `/users`, and selected on `/users`;
  - trainer copy with no link, and no admin copy;
  - "Atletas" copy while the profile loads.

  Verify with the backoffice tests.

- [x] 8.2 Review the backoffice against its references (assigned plans route, view, hooks, `workouts-table`, `create-workout-dialog`, `label-filter`) and the quality checklist in `AGENTS.md`: atomic levels, downward imports, routes only in `src/app`, no `core/infrastructure` import in presentation, every string in `es` and `en`. Verify by reading the diff against each reference.

## 9. Backoffice E2E

- [x] 9.1 Add `backoffice/e2e/users.spec.ts` (template `assigned-plans.spec.ts`). Flow: an admin session opens "Todos los usuarios" from the drawer, filters with search and a pill, opens "Añadir usuario", submits valid data, sees "Usuario creado." and the new row after the reload. Intercept `GET /users/me`, `GET /users` and `POST /users` with `page.route`. Verify with `pnpm test:e2e`.

## 10. Final verification

- [x] 10.1 From `front/`, run `pnpm typecheck && pnpm lint && pnpm test && pnpm test:e2e` and verify that all pass. Then run the template drift loop from `AGENTS.md` and verify it prints nothing.
