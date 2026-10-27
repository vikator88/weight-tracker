# Change Proposal: Add Backoffice Assigned Plans Screen

Source brief for `/opsx:propose`. Artifacts go to `openspec/changes/add-backoffice-assigned-plans-screen/`.

## Change

Add the workout read operations to `core` (`GetWorkoutsUseCase` for `GET /workouts`, `GetWorkoutByIdUseCase` for
`GET /workouts/{id}`), register them in both fronts, and add a backoffice screen behind the "Planes asignados" drawer
entry that lists every workout the API returns in a table, with a "Nuevo entrenamiento" button.

## Objective

Trainers and admins have no way to see the workouts they have assigned. `core` can create, update and add exercises
to a workout, but cannot read one: `WorkoutRepositoryInterface` has no read method and no read use case is wired in
either `container.ts`. The "Planes asignados" drawer item exists but leads nowhere.

After this change the backoffice shows the assigned workouts, and both read use cases are available from `core`, so
the upcoming create/edit workout screens can load a workout without touching `core` again.

## Capabilities

- List all workouts visible to the caller (`GET /workouts`); the API decides which ones each role sees.
- Get one workout with its exercises (`GET /workouts/{id}`): wired and tested in `core`, not used by any screen yet.
- Backoffice table with one row per workout: name, date, athlete (`userId`), trainer (`trainerId`, `ADMIN` only) and
  number of exercises.
- "Nuevo entrenamiento" button with a plus icon above the table.
- Loading, empty and error states for the list.
- "Planes asignados" in the drawer navigates to the screen and shows as selected while on it.

## Fronts And Roles

- backoffice -> lands here: yes -> roles: `ADMIN`, `TRAINER` -> both see the list and the button. `ADMIN` sees the
  trainer column; `TRAINER` must NOT see the trainer column (header nor cells).
- app -> lands here: wiring only -> roles: `USER` -> the container registers the two new use cases. No screen, hook
  or copy changes.

The front does not filter rows by role: it renders what `GET /workouts` returns. Only the trainer column depends on
the role, read from `useCurrentUser().profile.role`.

## Classification

Core:

- `WorkoutDetail` (alias of `WorkoutDetailResponse`) -> entity type -> `core/src/domain/workouts/workout.ts` (`Workout`
  already aliases `WorkoutResponse`)
- `getAll()` and `getById(id)` -> repository contract methods -> `core/src/application/ports/workout-repository-interface.ts`
- `WorkoutRepository.getAll` / `getById` -> repository implementation -> `core/src/infrastructure/repositories/`
- `GetWorkoutsUseCase` -> use case -> `core/src/application/use-cases/workouts/get-workouts-use-case.ts`
- `GetWorkoutByIdUseCase` -> use case -> `core/src/application/use-cases/workouts/get-workout-by-id-use-case.ts`
- `Dependencies` keys `getWorkoutsUseCase`, `getWorkoutByIdUseCase` -> composition contract -> `core/src/application/dependencies.ts`
- Barrels re-export the new alias and use cases

Backoffice and app wiring:

- Registrations of the two keys -> composition root -> `backoffice/src/infrastructure/container.ts` and `app/src/infrastructure/container.ts`
- Placeholders for the two keys -> test helper -> `backoffice/test/support/render-with-providers.tsx` and `app/test/support/render-with-providers.tsx`

Backoffice screen (new `workouts` domain area):

- `/assigned-plans` route -> route -> `backoffice/src/app/assigned-plans/page.tsx`
- `AssignedPlansView` -> view -> `backoffice/src/presentation/workouts/views/assigned-plans-view.tsx`
- `useWorkouts` (load, status, errorCode, reload) -> state hook -> `backoffice/src/presentation/workouts/hooks/use-workouts.ts`
- `WorkoutsTable` (columns, role-dependent trainer column, empty state) -> organism -> `backoffice/src/presentation/workouts/components/organisms/workouts-table.tsx`
- Table primitives (`Table`, `TableHead`, `TableBody`, `TableRow`, `TableCell`, `TableContainer`, as needed) -> atoms ->
  `backoffice/src/presentation/common/components/atoms/`
- "Nuevo entrenamiento" button -> reuse the `button` atom with MUI's `startIcon` (`AddOutlined` or `Add`); no new component
- Loading and error -> reuse `skeleton` atom / `HomeSkeleton` and `LoadError` molecules
- Drawer item -> extended in place -> `backoffice/src/presentation/common/components/organisms/navigation-drawer.tsx`:
  "Planes asignados" gets `href="/assigned-plans"` and `selected` like the home item
- Copy -> new `workouts` namespace -> `backoffice/src/i18n/es/workouts.json` and `backoffice/src/i18n/en/workouts.json`, registered where the other namespaces are

## Canonical Template Mapping

- `WorkoutDetail` alias -> `core/src/domain/workouts/workout.ts` -> extended in place, alias over `components["schemas"]` only.
- `getAll` / `getById` -> `core/src/infrastructure/repositories/user-repository.ts` and the existing `WorkoutRepository`
  -> `GET` with no body through `HttpClientInterface`, failures through `mapHttpError`.
- `GetWorkoutsUseCase` -> `core/src/application/use-cases/users/get-my-profile-use-case.ts` -> no-input
  `UseCaseInterface<void, …>` delegating to a repository.
- `GetWorkoutByIdUseCase` -> `core/src/application/use-cases/workouts/update-workout-use-case.ts` -> input with the path
  id, repository `Result` returned as is.
- Core tests -> the tests of those same files plus `workout-repository.test.ts`.
- Route -> `backoffice/src/app/profile/page.tsx` -> default-exports the view, nothing else.
- View -> `backoffice/src/presentation/profile/views/profile-view.tsx` -> `AppShell` + content, skeleton while loading,
  `LoadError` with retry on failure, `h1` title.
- `useWorkouts` -> `backoffice/src/presentation/common/hooks/use-current-user.ts` (and its provider) -> same
  `status` / `errorCode` / `reload` shape, but local to the screen (no provider).
- `WorkoutsTable` -> organism, `backoffice/src/presentation/profile/components/organisms/profile-form.tsx` -> organism
  with i18n receiving its data by props.
- Table atoms -> existing atoms such as `common/components/atoms/typography.tsx` -> themed MUI primitive, text by props, no hooks.
  Atom/molecule/organism references are `pending` (atomic design) in `.stpr/template/README.md`; follow the shapes
  established in #30.
- View test -> `backoffice/test/presentation/profile/views/` -> fakes through the container.
- E2E -> `backoffice/e2e/profile.spec.ts` -> `page.route` interception, same session setup.

UI references come from the backoffice only. `.stpr/template/` and its README are not updated here.

## Screens And Navigation

- backoffice -> `/assigned-plans` -> `AssignedPlansView` -> drawer "Planes asignados" (inside the session guard, like
  `/profile`) -> success: table, or empty state if the list is empty; failure: `LoadError` with retry; `401` follows
  the existing session handling.
- "Nuevo entrenamiento" -> no destination yet (the create workout screen is a separate change); rendered but not
  wired. See Notes.

## API Contract

- `GET /workouts` -> in `core/openapi/v1.json`: yes -> no body, response `WorkoutResponse[]` (`id`, `name`,
  `description`, `workoutDate`, `userId`, `trainerId`, `status`, `exercises`); failures `401`, `403`.
- `GET /workouts/{id}` -> yes -> path `id` (uuid), response `WorkoutDetailResponse`; failures `401`, `403` (and `404`
  if the snapshot lists it).

No snapshot refresh. No hand-written type.

## Held State

- `useWorkouts` -> the loaded workout list, status and error code -> the hook itself on mount and on `reload` ->
  resets on screen exit. Nothing is cached across screens or kept after logout.

## Forms And Structural Validation

None.

## Errors And Copy

- Session expired / missing -> `UNAUTHORIZED` -> existing session handling.
- Role not allowed -> `FORBIDDEN` -> `LoadError` with the existing message.
- No connection -> `NETWORK`; anything else -> `UNEXPECTED` -> `LoadError` with retry.
- No new error code; the mapper is not touched (unless `NOT_FOUND` for `getById` is missing, which is then added with
  its branch test and copy).

New copy (`workouts` namespace):

- `assignedPlans.title` -> "Planes asignados" -> "Assigned plans"
- `assignedPlans.newWorkout` -> "Nuevo entrenamiento" -> "New workout"
- `assignedPlans.columns.name` -> "Nombre" -> "Name"
- `assignedPlans.columns.date` -> "Fecha" -> "Date"
- `assignedPlans.columns.athlete` -> "Atleta" -> "Athlete"
- `assignedPlans.columns.trainer` -> "Entrenador" -> "Trainer"
- `assignedPlans.columns.exercises` -> "Ejercicios" -> "Exercises"
- `assignedPlans.empty` -> "Todavía no hay entrenamientos asignados." -> "No workouts assigned yet."

## Platform Capabilities And Dependencies

None.

## Out Of Scope

- The create workout screen and wiring the "Nuevo entrenamiento" button to it.
- A workout detail screen; `GetWorkoutByIdUseCase` is not used by any screen in this change.
- Resolving `userId` / `trainerId` to names (needs an API lookup that does not exist yet); ids are shown as is.
- Sorting, filtering, search and pagination; status column; row actions (edit, delete).
- Any app screen.
- Must not change: the API, the existing workout use cases, the other drawer entries.

## Notes

- **Columns.** Name = `name`; Date = `workoutDate` (`YYYY-MM-DD`) formatted with the active language's locale, without
  time-zone shifts (parse as a calendar date, not as UTC midnight); Athlete = `userId`; Trainer = `trainerId`, empty
  cell when `null`; Exercises = `exercises.length`. Rows in the order the API returns them.
- **Role branch.** The trainer column is a presentation choice, not a business rule; read the role from
  `useCurrentUser`.
- **"Nuevo entrenamiento" button.** Until the create screen exists it has no action. Decide at proposal time between
  rendering it without `onClick` or disabled; the create workout change wires it.
- **Route name.** `/assigned-plans` mirrors the drawer label; rename at proposal time if the create screen will live
  under `/workouts`.
- **No stop case**: both endpoints are in the snapshot and no business rule is added.
- **Verification tiers:**
  - core unit: `get-workouts-use-case.test.ts` and `get-workout-by-id-use-case.test.ts` (success returned, id
    forwarded, repository failure returned unchanged); `workout-repository.test.ts` gains `getAll` (`GET /workouts`)
    and `getById` (`GET /workouts/{id}`): success, every listed status, `500` -> `UNEXPECTED`, network -> `NETWORK`.
  - container tests in both fronts gain the two keys.
  - backoffice component test `test/presentation/workouts/views/assigned-plans-view.test.tsx`: rows rendered with
    every column; `ADMIN` sees the trainer column; `TRAINER` does not (header and cells absent); empty state; failure
    shows `LoadError` and retry reloads; button rendered.
  - backoffice drawer test: "Planes asignados" links to `/assigned-plans`.
  - backoffice E2E `e2e/assigned-plans.spec.ts`: from the drawer to the table with `GET /workouts` intercepted.
  - translation parity passes with the new namespace.
- Done means `pnpm typecheck && pnpm lint && pnpm test && pnpm test:e2e` passes from `front/`.
- No deviation from the mapped references is allowed.
