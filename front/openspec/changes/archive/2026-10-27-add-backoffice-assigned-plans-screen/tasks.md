# Tasks

## 1. Core: workout reads

- [x] 1.1 Add `WorkoutDetail` (alias of `WorkoutDetailResponse`) and `GetWorkoutByIdInput` to `core/src/domain/workouts/workout.ts` and the domain barrel; verify with `pnpm --filter @weight-tracker/core typecheck`
- [x] 1.2 Add `getAll()` and `getById(id)` to `WorkoutRepositoryInterface`; implement them in `WorkoutRepository` (`GET /workouts`, `GET /workouts/{id}`, no body, failures through `mapHttpError`); verify with new cases in `core/test/infrastructure/repositories/workout-repository.test.ts`: method, path and no body sent, success returned, 401 / 403 (and 404 for `getById`), 500 → `UNEXPECTED`, network → `NETWORK`
- [x] 1.3 Add `GetWorkoutsUseCase` (modelled on `GetMyProfileUseCase`) and `GetWorkoutByIdUseCase` (modelled on `UpdateWorkoutUseCase`) in `core/src/application/use-cases/workouts/` and its barrel; verify with `get-workouts-use-case.test.ts` and `get-workout-by-id-use-case.test.ts` under `core/test/application/use-cases/workouts/` (id forwarded, success returned, failure returned unchanged)
- [x] 1.4 Add `getWorkoutsUseCase` and `getWorkoutByIdUseCase` to `Dependencies` in `core/src/application/dependencies.ts`; verify `pnpm --filter @weight-tracker/core test` passes
- [x] 1.5 Review core against `get-my-profile-use-case.ts`, `update-workout-use-case.ts`, the existing `WorkoutRepository` and their tests; verify no deviation and no hand-written type

## 2. Wiring in both fronts

- [x] 2.1 Register both use cases in `backoffice/src/infrastructure/container.ts`, add their placeholders in `backoffice/test/support/render-with-providers.tsx`, and add the two keys to `backoffice/test/infrastructure/container.test.ts`; verify `pnpm --filter backoffice test` passes
- [x] 2.2 Register both use cases in `app/src/infrastructure/container.ts`, add their placeholders in `app/test/support/render-with-providers.tsx`, and add the two keys to `app/test/infrastructure/container.test.ts`; verify `pnpm --filter app test` passes
- [x] 2.3 Review the app against its `container.ts` reference; verify no screen, hook or copy was added to the app

## 3. Backoffice: copy and atoms

- [x] 3.1 Add `backoffice/src/i18n/es/workouts.json` and `en/workouts.json` with the keys in design.md, and register the namespace in `backoffice/src/i18n/resources.ts`; verify `translation-parity.test.ts` passes
- [x] 3.2 Add the atoms `table`, `table-container`, `table-head`, `table-body`, `table-row`, `table-cell` in `backoffice/src/presentation/common/components/atoms/`, each a pass-through like `button.tsx`; verify `pnpm --filter backoffice typecheck` passes

## 4. Backoffice: assigned plans screen

- [x] 4.1 Add `useWorkouts` in `backoffice/src/presentation/workouts/hooks/use-workouts.ts` (status, workouts, errorCode, reload; `runWithSession`; generation guard); verify through the view test in 4.4
- [x] 4.2 Add the `WorkoutsTable` organism in `backoffice/src/presentation/workouts/components/organisms/workouts-table.tsx` (columns, `showTrainer`, empty message, time-zone-safe date formatting); verify through the view test in 4.4
- [x] 4.3 Add `AssignedPlansView` in `backoffice/src/presentation/workouts/views/assigned-plans-view.tsx` (AppShell, title, "Nuevo entrenamiento" with `AddOutlined`, skeleton / `LoadError` / table) and the route `backoffice/src/app/assigned-plans/page.tsx`; verify `pnpm --filter backoffice build` still produces a static export with `/assigned-plans`
- [x] 4.4 Add `backoffice/test/presentation/workouts/views/assigned-plans-view.test.tsx` modelled on `profile-view.test.tsx`: rows and columns rendered in order; date shown for its calendar day; exercise count (including `0`); `ADMIN` sees "Entrenador" and the trainer id, empty cell when none; `TRAINER` sees neither header nor id; empty message; failure shows `LoadError` and retry reloads into the table; button rendered and clicking it sends nothing; English copy; verify it passes

## 5. Backoffice: navigation

- [x] 5.1 Give `MenuItemDefinition` an optional `href`, render items with `href` as `NextLink` with `selected={pathname === href}`, and set `href: "/assigned-plans"` on Planes asignados; update the `sections` comment; verify with new cases in `navigation-drawer.test.tsx` (links to `/assigned-plans`, selected there and Inicio not, other items still inert)
- [x] 5.2 Add `backoffice/e2e/assigned-plans.spec.ts` modelled on `profile.spec.ts`, intercepting `GET /workouts` with `page.route`: from home, click "Planes asignados", land on `/assigned-plans` and see the rows; verify `pnpm test:e2e` passes
- [x] 5.3 Review the backoffice against `profile/page.tsx`, `profile-view.tsx`, `use-update-profile.ts` / `current-user-provider.tsx`, the existing atoms and their tests; verify atomic levels, downward imports, only atoms importing MUI widgets, `src/app` routes only, and no `@weight-tracker/core/infrastructure` import in presentation

## 6. Integration

- [x] 6.1 Run the `.stpr/template/` drift check from `AGENTS.md`; verify it prints no `DRIFT` line
- [x] 6.2 Run `pnpm typecheck && pnpm lint && pnpm test && pnpm test:e2e` from `front/`; verify everything passes

## Workflow follow-up

- Archive the change after review with `/opsx:archive`.
