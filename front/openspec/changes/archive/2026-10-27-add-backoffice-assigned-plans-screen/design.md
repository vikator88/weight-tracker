# Design

## Context

See proposal.md for the motivation and specs for the behaviour. Current state that shapes the approach:

- `core/src/domain/workouts/workout.ts` already aliases `Workout = WorkoutResponse`; `WorkoutDetailResponse` has no alias.
- `WorkoutRepositoryInterface` / `WorkoutRepository` have `create`, `update` and `addExercise`, each a `try` around
  `httpClient.request` returning `ok(response)` or `fail(mapHttpError(error))`.
- `mapHttpError` already maps 400, 401, 403, 404 and 409; everything else is `UNEXPECTED`, no response is `NETWORK`.
- Signed-in screens wrap their content in `AppShell`; use cases are called through `useSession().runWithSession`, which
  handles the `401` → refresh → retry flow. The role is on `useCurrentUser().profile.role`.
- `NavigationDrawer` renders section items as plain `ListItemButton`s; only Inicio uses `NextLink` + `selected`.

## Goals / Non-Goals

**Goals:**

- Add the two reads to `core` in the same shape as the existing workout operations.
- Build the first list screen of the backoffice, with a screen-local state hook and table atoms reusable by later lists.

**Non-Goals:**

- No shared list/table abstraction (pagination, sorting, generic column config): one table, one screen.
- No cache of the workout list across screens, and no provider.
- `GetWorkoutByIdUseCase` gets no consumer here.

## Decisions

### Core

| Piece                   | Shape                                                                                                        |
| ----------------------- | ------------------------------------------------------------------------------------------------------------ |
| `WorkoutDetail`         | `components["schemas"]["WorkoutDetailResponse"]`, in `domain/workouts/workout.ts`, re-exported               |
| `GetWorkoutByIdInput`   | `{ id: string }`, beside `UpdateWorkoutInput` in the same file                                               |
| `getAll()`              | `Promise<Result<Workout[], DomainError>>`, `GET /workouts`, no body                                          |
| `getById(id)`           | `Promise<Result<WorkoutDetail, DomainError>>`, `GET /workouts/${id}`, no body                                |
| `GetWorkoutsUseCase`    | `UseCaseInterface<void, Workout[]>`, delegates to `getAll`, modelled on `GetMyProfileUseCase`                |
| `GetWorkoutByIdUseCase` | `UseCaseInterface<GetWorkoutByIdInput, WorkoutDetail>`, delegates `getById(id)`, like `UpdateWorkoutUseCase` |

New `Dependencies` keys in `core/src/application/dependencies.ts`:

- `getWorkoutsUseCase: UseCaseInterface<void, Workout[]>`
- `getWorkoutByIdUseCase: UseCaseInterface<GetWorkoutByIdInput, WorkoutDetail>`

Registered in **both** `backoffice/src/infrastructure/container.ts` and `app/src/infrastructure/container.ts` as
`(c) => new GetWorkoutsUseCase(c.resolve("workoutRepository"))` and the same for `GetWorkoutByIdUseCase`, next to the
other workout use cases. Both `test/support/render-with-providers.tsx` get placeholders for the two keys, following the
existing placeholder pattern; no other test-helper change.

Input as `{ id }` rather than a bare `string`: it matches `UpdateWorkoutInput` and keeps every use case input an object.

### Backoffice structure

```
src/app/assigned-plans/page.tsx                              route: default-exports AssignedPlansView
src/presentation/workouts/views/assigned-plans-view.tsx      view: AppShell + content
src/presentation/workouts/hooks/use-workouts.ts              state hook
src/presentation/workouts/components/organisms/workouts-table.tsx   organism
src/presentation/common/components/atoms/table*.tsx          atoms
src/i18n/{es,en}/workouts.json                               copy, registered in i18n/resources.ts
```

A new `workouts` domain area (not `assigned-plans`): the create/edit workout screens will live in the same area.

### State hook `useWorkouts`

- Holds `status` (`"loading" | "loaded" | "failed"`), `workouts: Workout[]`, `errorCode: DomainErrorCode | null`, and
  exposes `reload`.
- Calls `runWithSession(() => container.resolve("getWorkoutsUseCase").execute())` on mount and on `reload`.
- Guards against out-of-order answers with a generation counter, as `CurrentUserProvider` does, and ignores answers
  after unmount.
- Local to the screen: state resets on screen exit. Nothing survives logout because nothing outlives the screen.

Alternative rejected: a `WorkoutsProvider` like `CurrentUserProvider`. No other screen needs the list yet, and a
provider would need its own reset on session end.

### View `AssignedPlansView`

Modelled on `ProfileView`: `AppShell` around an inner content component. Inside, a row with the `h1` title and the
"Nuevo entrenamiento" `Button` atom (`variant="contained"`, `startIcon={<AddOutlined />}`, no `onClick`), then:

- profile not loaded or `useWorkouts` loading → skeleton (reuse `HomeSkeleton`, or the `skeleton` atom if the home layout
  looks wrong in a table slot; decided while implementing, no new molecule);
- `failed` → `LoadError` with `code={errorCode}` and `onRetry={reload}`;
- `loaded` → `WorkoutsTable` with `workouts` and `showTrainer={profile.role === "ADMIN"}`.

The view waits for the profile so the trainer column never flashes in or out. The role check sits in the view because
it is a presentation choice, not a business rule; the organism only takes a boolean.

### Organism `WorkoutsTable`

- Props: `workouts: Workout[]`, `showTrainer: boolean`. Uses `useTranslation("workouts")`.
- Columns: name, date, athlete, trainer (only when `showTrainer`, both header and cells omitted otherwise), exercises
  (`workout.exercises.length`). Row `key` is `workout.id`.
- Empty list: a single message (`assignedPlans.empty`) instead of the table body.
- Date: `workoutDate` is `YYYY-MM-DD`. Split it into year/month/day and build `new Date(y, m - 1, d)` (local time), then
  format with `Intl.DateTimeFormat(i18n.language, { dateStyle: "medium" })`. `new Date("2026-10-27")` would parse as UTC
  midnight and show the previous day west of UTC. The formatting helper stays private to the organism file.

### Atoms

New, in `backoffice/src/presentation/common/components/atoms/`, each a pass-through of its MUI widget like `button.tsx`
(`ComponentProps<typeof MuiX>`, no hooks): `table.tsx`, `table-container.tsx`, `table-head.tsx`, `table-body.tsx`,
`table-row.tsx`, `table-cell.tsx`. `TableContainer` renders on a `Paper` surface itself, so no caller imports `Paper`
(it is a MUI widget). Only these atoms
import the MUI table widgets; the organism imports the atoms. Imports flow route → view → organism → molecule/atom.

`AddOutlined` is an icon component, not a widget; it is imported where used, as the drawer does with its icons.

### Navigation drawer

`MenuItemDefinition` gains an optional `href`. Items with `href` render `ListItemButton component={NextLink} href=…
selected={pathname === href}`, the same as Inicio; items without it stay inert buttons. Only `assignedPlans` gets
`href: "/assigned-plans"`. The comment on `sections` is updated accordingly.

### Errors and copy

| Failure                    | Code           | Shown                                                         |
| -------------------------- | -------------- | ------------------------------------------------------------- |
| Token expired / missing    | `UNAUTHORIZED` | handled by `runWithSession` (refresh, or end session → login) |
| Role not allowed           | `FORBIDDEN`    | `LoadError`, existing `errors` copy                           |
| No response                | `NETWORK`      | `LoadError`, existing `errors` copy                           |
| Anything else              | `UNEXPECTED`   | `LoadError`, existing `errors` copy                           |
| (`getById` only) not found | `NOT_FOUND`    | no screen yet; already mapped                                 |

No new error code and no mapper change. New `workouts` namespace, `es` / `en`:

| Key                               | es                                       | en                        |
| --------------------------------- | ---------------------------------------- | ------------------------- |
| `assignedPlans.title`             | Planes asignados                         | Assigned plans            |
| `assignedPlans.newWorkout`        | Nuevo entrenamiento                      | New workout               |
| `assignedPlans.columns.name`      | Nombre                                   | Name                      |
| `assignedPlans.columns.date`      | Fecha                                    | Date                      |
| `assignedPlans.columns.athlete`   | Atleta                                   | Athlete                   |
| `assignedPlans.columns.trainer`   | Entrenador                               | Trainer                   |
| `assignedPlans.columns.exercises` | Ejercicios                               | Exercises                 |
| `assignedPlans.empty`             | Todavía no hay entrenamientos asignados. | No workouts assigned yet. |

The drawer label stays in `common` (`menu.programming.assignedPlans`); the screen title lives in `workouts` so the area
owns its copy. The app gets no copy.

### Tests

- core: `get-workouts-use-case.test.ts`, `get-workout-by-id-use-case.test.ts` (fake repository), and new `getAll` /
  `getById` cases in `workout-repository.test.ts` (fake `HttpClientInterface`), mirroring the existing ones.
- both container tests gain the two keys.
- backoffice: `test/presentation/workouts/views/assigned-plans-view.test.tsx` modelled on `profile-view.test.tsx`
  (fakes via `renderWithProviders`, `next/navigation` mocked with `usePathname: () => "/assigned-plans"`), covering
  both roles, empty, failure + retry, the button; `navigation-drawer.test.tsx` gains the link/selected cases.
- backoffice E2E `e2e/assigned-plans.spec.ts` modelled on `profile.spec.ts`, intercepting `GET /workouts` with
  `page.route`: from home, click "Planes asignados", see the rows.
- Every test uses `// Arrange` / `// Act` / `// Assert` with one Act. No test-helper pattern changes beyond the
  placeholders.

## Risks / Trade-offs

- [Athlete and trainer shown as raw uuids are hard to read] → accepted for this version; names need an API lookup,
  tracked as a follow-up.
- [Inert "Nuevo entrenamiento" may look broken] → same convention as the drawer placeholders; the create workout
  change wires it.
- [Large lists render all rows] → acceptable now; pagination needs an API change and is out of scope.
- [Locale date output differs between Node and browsers in tests] → component tests assert the date with
  `Intl.DateTimeFormat("es", { dateStyle: "medium" })` computed in the test, not a hard-coded string.

## Migration Plan

None. Additive change, no data or contract migration; rollback is reverting the change.
