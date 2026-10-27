# Proposal

## Why

Trainers and admins cannot see the workouts they have assigned. `core` can create and update a workout but cannot read
one (`WorkoutRepositoryInterface` has no read method, and no read use case is wired in either `container.ts`), and the
"Planes asignados" drawer item is a placeholder that leads nowhere. The upcoming create/edit workout screens also need
to load a workout, so the read operations belong in `core` now.

## What Changes

- **core**: `GetWorkoutsUseCase` (`GET /workouts`) and `GetWorkoutByIdUseCase` (`GET /workouts/{id}`), with
  `getAll` / `getById` on `WorkoutRepositoryInterface` and `WorkoutRepository`, a `WorkoutDetail` alias of the generated
  `WorkoutDetailResponse`, and two new `Dependencies` keys. Both use cases return `Result` and never throw.
- **backoffice and app**: both `container.ts` register the two new use cases. The app gets no screen.
- **backoffice**: a new `/assigned-plans` screen (inside the session guard) listing every workout `GET /workouts` returns
  in a table with the columns name, date, athlete (`userId`), trainer (`trainerId`, `ADMIN` only) and number of
  exercises; a "Nuevo entrenamiento" button with a plus icon; loading, empty and error states.
- **backoffice drawer**: "Planes asignados" stops being a placeholder; it links to `/assigned-plans` and is shown as
  selected there.
- `GetWorkoutByIdUseCase` is wired and tested but used by no screen in this change.

## Capabilities

Per front and role:

| Capability                | Front(s)                   | `ADMIN`                   | `TRAINER`                          | `USER` (app)   |
| ------------------------- | -------------------------- | ------------------------- | ---------------------------------- | -------------- |
| Workout reads in `core`   | core, wired in both fronts | —                         | —                                  | wiring only    |
| Assigned plans screen     | backoffice                 | table with trainer column | table, must NOT see trainer column | does not exist |
| Drawer "Planes asignados" | backoffice                 | links to the screen       | links to the screen                | —              |

The front renders whatever `GET /workouts` returns; which workouts each role sees is decided by the API. Only the
trainer column depends on the role, as a presentation choice.

### New Capabilities

- `backoffice-assigned-plans`: the backoffice screen that lists the caller's assigned workouts in a table, with its
  role-dependent trainer column, its empty, loading and error states, and the "Nuevo entrenamiento" button. Canonical
  references: route `backoffice/src/app/profile/page.tsx`, view `backoffice/src/presentation/profile/views/profile-view.tsx`,
  state hook `backoffice/src/presentation/profile/hooks/use-update-profile.ts`; atom, molecule and organism references are
  `pending` (atomic design) in `.stpr/template/README.md`, so they follow the shapes established in #30.

### Modified Capabilities

- `workout-management`: adds listing workouts and reading one workout by id. Canonical references:
  `core/src/application/use-cases/users/get-my-profile-use-case.ts` (no-input read),
  `core/src/application/use-cases/workouts/update-workout-use-case.ts` (path id input) and the existing
  `WorkoutRepository`.
- `backoffice-navigation`: "Planes asignados" links to `/assigned-plans` and is selected there, so it leaves the
  "Items without a screen do nothing" set. Canonical reference: the Inicio item of `navigation-drawer.tsx`.

## Components

- **Atoms (new)**: `table`, `table-container`, `table-head`, `table-body`, `table-row`, `table-cell` in
  `backoffice/src/presentation/common/components/atoms/`, themed MUI primitives with no hooks.
- **Atoms (reused)**: `button` (with a plus `startIcon`), `typography`, `skeleton`.
- **Molecules (reused)**: `LoadError`, `HomeSkeleton`. No new molecule.
- **Organisms (new)**: `WorkoutsTable` in `backoffice/src/presentation/workouts/components/organisms/`.
- **Organisms (reused / extended)**: `AppShell` (reused), `NavigationDrawer` (one item extended).

## Constraints And Decisions

- No business rule in the front: no role filtering of rows and no validation (the screen has no form).
- No snapshot refresh: both endpoints and `WorkoutResponse` / `WorkoutDetailResponse` are already in
  `core/openapi/v1.json`. No hand-written type.
- No new error code: `UNAUTHORIZED`, `FORBIDDEN`, `NOT_FOUND`, `NETWORK` and `UNEXPECTED` are already mapped.
- **"Nuevo entrenamiento" has no action yet.** It is rendered enabled and does nothing, like the drawer placeholders,
  until the create workout screen exists; that change wires it.
- **Athlete and trainer are shown as ids.** Resolving them to names needs an API lookup that does not exist yet.
- **Route** is `/assigned-plans`, mirroring the drawer label.
- No conflict with the invariants, the template or a stop case.

## Acceptance Criteria

- `GetWorkoutsUseCase` and `GetWorkoutByIdUseCase` send `GET /workouts` and `GET /workouts/{id}` and return the API's
  answer or a `DomainError`, resolvable from both containers.
- From the drawer, a trainer or an admin reaches `/assigned-plans` and sees one row per workout with name, formatted
  date, athlete id and exercise count; an admin also sees the trainer column, a trainer does not.
- An empty list shows an empty-state message; a failed load shows `LoadError` with a working retry.
- "Nuevo entrenamiento" with a plus icon is shown above the table.
- Every string exists in `es` and `en`.
- `pnpm typecheck && pnpm lint && pnpm test && pnpm test:e2e` passes from `front/`.

## Impact

- `core`: `domain/workouts/workout.ts`, `application/ports/workout-repository-interface.ts`,
  `application/use-cases/workouts/`, `application/dependencies.ts`, `infrastructure/repositories/workout-repository.ts`,
  barrels, tests.
- `backoffice`: `infrastructure/container.ts`, new route, `presentation/workouts/`, new table atoms, navigation drawer,
  new `workouts` i18n namespace, test helper placeholders, component and E2E tests.
- `app`: `infrastructure/container.ts`, test helper placeholders, container test.
- No API change, no new dependency, no platform port.
