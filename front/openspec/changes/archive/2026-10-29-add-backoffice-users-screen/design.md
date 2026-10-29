# Design

## Context

See proposal.md (Why). Facts the approach builds on:

- `UserRepository` already reads and updates `/users/me`, and `UserProfile` already aliases `UserProfileResponse`,
  which is the item type of `GET /users`.
- Form rules live in `core/src/application/validation` (`workout-rules.ts`, `profile-name-rules.ts`). The create
  workout dialog consumes them, and the signup form still validates inline.
- The assigned plans screen sets the shape of a list screen: a local list hook with a generation guard, a view that
  waits for the role before rendering role-dependent parts, a create dialog driven by a `use-create-*` hook.
- The drawer's sections are a static array of i18n keys with an optional `href`; selection compares `href` with the
  pathname.
- The HTTP error mapper has no `501` entry, so a trainer's listing fails as `UNEXPECTED`.
- The API's `PersonName` limits only the maximum (100). The front's 3-character minimum belongs to the profile rules,
  not to this form.

## Goals / Non-Goals

**Goals:**

- Add the two operations in `core` once, and register them in both containers.
- Build the screen only from existing atoms and screen shapes. The only new reusable piece is a role pill molecule.

**Non-Goals:**

- Changing the error mapper, the signup form, the profile rules or the assigned plans screen.
- Server-side search, pagination, or caching the user list across screens.
- Any app UI.

## Decisions

### Core

- **Aliases** in `core/src/domain/users/user.ts`: `CreateUserRequest = components["schemas"]["CreateUserRequest"]`
  and `CreatedUserResponse = components["schemas"]["CreatedUserResponse"]`. The list reuses `UserProfile[]`. No new
  list alias.
- **Role encoding** (contract fact, found during the snapshot refresh). Requests carry `Role` as an integer: the
  existing `Role` alias, `1` USER, `2` TRAINER, `3` ADMIN, as the signup wiring already passes. Responses carry the
  role as the string `"USER" | "TRAINER" | "ADMIN"`. The dialog keeps one option list in presentation pairing each
  request value with its response code, used for the select. Pills and the table work on the response string. No
  extra alias.
- **Port**: `UserRepositoryInterface` gains `getAll(): Promise<Result<UserProfile[], DomainError>>` and
  `create(request: CreateUserRequest): Promise<Result<CreatedUserResponse, DomainError>>`.
- **Repository**: `UserRepository.getAll` (`GET /users`) and `create` (`POST /users`, body as given), same
  `try` / `ok` / `fail(mapHttpError(error))` shape as `getMe` / `updateMe`.
- **Use cases** in `core/src/application/use-cases/users/`:
  - `GetUsersUseCase implements UseCaseInterface<void, UserProfile[]>`, modelled on `GetWorkoutsUseCase`;
  - `CreateUserUseCase implements UseCaseInterface<CreateUserRequest, CreatedUserResponse>`, delegating unchanged.
- **Dependencies**: new keys `getUsersUseCase` and `createUserUseCase`, registered in `backoffice` and `app`
  `container.ts`, with placeholders in both `test/support/render-with-providers.tsx`.
- **Form rules** in `core/src/application/validation/user-rules.ts`, exported through the barrel:
  `CreateUserFormValues` (every field a string), `CreateUserFormViolations`, `validateCreateUserForm`. Violations are
  `required`, `email`, `maxLength` and `date`. The limit reuses `PROFILE_NAME_MAX_LENGTH`. The calendar date check is
  the private helper in `workout-rules.ts`, moved to a shared internal module rather than duplicated.
  - Why core and not inline like `SignupForm`: the newer reference (`create-workout-dialog` + `workout-rules`) keeps
    form rules pure, shared and unit-tested in core. Inline validation would leave them untested outside the view.

### Backoffice

New `users` domain area:

| Piece                       | Level       | Path                                                                 |
| --------------------------- | ----------- | -------------------------------------------------------------------- |
| `/users` route              | route       | `backoffice/src/app/users/page.tsx`                                  |
| `UsersView`                 | view        | `src/presentation/users/views/users-view.tsx`                        |
| `useUsers`                  | state hook  | `src/presentation/users/hooks/use-users.ts`                          |
| `useCreateUser`             | state hook  | `src/presentation/users/hooks/use-create-user.ts`                    |
| `filterUsers`, `fullNameOf` | pure helper | `src/presentation/users/filter-users.ts`                             |
| `RoleFilter`                | molecule    | `src/presentation/users/components/molecules/role-filter.tsx`        |
| `UsersTable`                | organism    | `src/presentation/users/components/organisms/users-table.tsx`        |
| `CreateUserDialog`          | organism    | `src/presentation/users/components/organisms/create-user-dialog.tsx` |

- **`useUsers`**: copy of `useWorkouts` resolving `getUsersUseCase`; returns `{ status, users, errorCode, reload }`.
- **`useCreateUser`**: copy of `useCreateWorkout` resolving `createUserUseCase`; `create` resolves to the created user
  or `null`.
- **`filterUsers(users, search, role)`**: pure presentation helper beside the area, like `workouts/week.ts`. It
  normalises with `toLocaleLowerCase()` and `normalize("NFD")` with diacritics stripped, and matches substrings on
  email, name and surname. It is a view filter, not a business rule. `fullNameOf` joins and trims name and surname,
  returning `null` when both are empty so the table renders "—".
- **`UsersView`**: `AppShell` + `Container`; `h1` "Todos los usuarios"; a toolbar row with the search `TextField`
  atom, the `RoleFilter` pills and, when `profile.role === "ADMIN"`, the "Añadir usuario" `Button` atom with
  `AddOutlined`. Search text, selected role, dialog open and the success flag are local `useState`. The list part
  waits for both `useUsers` and `useCurrentUser` like `AssignedPlansList`, so the button never appears after render.
  On success it shows an `Alert` atom (`severity="success"`, "Usuario creado.") above the table, closable, and
  cleared on the next opening of the modal. `notificationService` is a no-op in the backoffice, so it is not used.
- **`RoleFilter`**: same layout and toggle semantics as `label-filter.tsx` (label, one `Chip` atom per option,
  outlined/filled, single selection, toggle to clear), with role options and no per-label colour. Kept separate rather
  than generalising `LabelFilter`, which is typed to exercise label colours in the `workouts` area. Sharing would mean
  moving it to `common`, which is out of scope.
- **`UsersTable`**: shaped like `workouts-table.tsx`; columns full name, email, profile (translated role text). It
  receives already-filtered rows and a flag telling empty list from no match, so it can show the right message.
- **`CreateUserDialog`**: shaped like `create-workout-dialog.tsx`. The form lives inside the dialog and unmounts on
  close, so every opening starts empty with role `USER`.
  - Fields: email and password are `TextField` atoms; profile is a `TextField` `select` with `MenuItem` atoms; name and
    surname reuse the `profile-name-fields` molecule (only `maxLength` can be reported); date of birth is a
    `TextField` with `type="date"`.
  - Submit runs `validateCreateUserForm`. `toRequest` trims name and surname and omits empty optional fields. The date
    is sent as the `YYYY-MM-DD` string the input holds, never through `Date`.
  - The error `Alert` uses `users:create.errors.<code>` with a fallback to `errors:<code>`.
- **Drawer**: the athletes section reads `useCurrentUser()`. When `profile?.role === "ADMIN"`, it uses
  `menu.users.title` / `menu.users.all` and `href: "/users"`. Otherwise it keeps the `menu.athletes.*` keys with no
  `href`. The static array becomes a function of the role. The rest of the drawer is unchanged.
- **Imports** flow downward: view → organisms → molecules → atoms. Only atoms import MUI widgets. `Box`, `Container`
  and icons follow the existing exceptions already used by the assigned plans view.

### Translations

- New `users` namespace in `backoffice/src/i18n/{es,en}/users.json`, registered in `resources.ts`. The keys are
  the ones in the brief: `list.*`, `roles.*`, `create.*`, plus `create.validation.{required,email,date}` ("Campo
  obligatorio." / "Introduce un email válido." / "Introduce una fecha válida."). `maxLength` reuses the profile
  molecule's own messages.
- `common.json` gains `menu.users.title` ("Usuarios" / "Users") and `menu.users.all` ("Todos los usuarios" / "All
  users").

### Errors

| Failure                          | Code                     | Shown                                          |
| -------------------------------- | ------------------------ | ---------------------------------------------- |
| list, any                        | as mapped                | `LoadError` with retry                         |
| list, trainer (`501`)            | `UNEXPECTED`             | `LoadError` generic                            |
| create, duplicate email          | `CONFLICT`               | `users:create.errors.CONFLICT`                 |
| create, bad data / weak password | `VALIDATION`             | `users:create.errors.VALIDATION`               |
| create, not admin                | `FORBIDDEN`              | `errors:FORBIDDEN`                             |
| create, other                    | `NETWORK` / `UNEXPECTED` | `errors:<code>`                                |
| session                          | `UNAUTHORIZED`           | existing session handling via `runWithSession` |

No new code. The mapper is untouched.

### State

| Holder                  | Holds                                        | Changed by                           | Resets                 |
| ----------------------- | -------------------------------------------- | ------------------------------------ | ---------------------- |
| `useUsers`              | list, status, error code                     | mount, `reload`, success of creation | screen exit            |
| `useCreateUser`         | `creating`, error code                       | submit                               | dialog close (unmount) |
| `UsersView`             | search, role pill, dialog open, success flag | the user                             | screen exit            |
| `CreateUserDialog` form | values, field violations                     | the user                             | dialog close           |

Nothing persists across screens or after logout.

### Tests

The templates are the existing tests of the referenced files. Test helpers change only to add the two container
placeholders.

- **core**: `get-users-use-case.test.ts`, `create-user-use-case.test.ts`, `user-repository.test.ts` (`getAll`,
  `create`, every listed status, `501`, network) and `user-rules.test.ts`.
- **backoffice component tests**:
  - `users-view.test.tsx` (admin and trainer branches);
  - `create-user-dialog.test.tsx`;
  - `filter-users.test.ts`;
  - the drawer test (admin copy and link, trainer copy and no link);
  - the container test.
- **app**: the container test.
- **E2E**: `e2e/users.spec.ts` intercepts `GET /users`, `POST /users` and `GET /users/me` (admin).
- **Translations**: the parity tests pick up the new namespace.

## Risks / Trade-offs

- [The drawer shows the "Atletas" copy for an instant before an admin's profile loads] → Accepted. It matches the
  trainer's state and has no link, so nothing actionable flickers. A skeleton would hide the whole section for every
  role.
- [Client-side filtering over the whole list grows with the user count] → Acceptable for the current scale. Server
  search and pagination are out of scope and would be an API change.
- [`RoleFilter` duplicates `LabelFilter`'s layout] → Small, area-local duplication. Merge into `common` in a later
  change if a third filter appears.
- [Trainers get a generic error if they type `/users`] → Their entry has no link. When the API serves trainers, a
  later change links it and adds their copy.
- [Moving the calendar date helper out of `workout-rules.ts`] → Internal only. Workout rule tests must stay green
  unchanged.

## Migration Plan

Snapshot refresh first, in its own commit with the API running locally. Then core, wiring, and backoffice. No data
migration. Rollback is reverting the commits. The API endpoints stay usable without the front.
