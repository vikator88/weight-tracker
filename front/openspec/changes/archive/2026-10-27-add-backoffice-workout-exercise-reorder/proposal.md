# Proposal

Source brief: `.stpr/plan/openspec/proposal-change-add-backoffice-workout-exercise-reorder.md`.

## Why

The backoffice workout screen lists a workout's exercises in the API's order, but the only way to change that order
is to remove exercises and add them again. The API now exposes `PUT /workouts/{workoutId}/exercises/order` (API change
`add-reorder-workout-exercises`), so trainers and admins can reorder exercises where they already build the workout.

## What Changes

- Refresh the contract snapshot (`core/openapi/v1.json` and `core/src/contract/generated`) to include the reorder
  endpoint and its `ReorderWorkoutExercisesRequest` schema, committed as a step of its own.
- `core`: add the request alias and a `ReorderWorkoutExercisesInput` type, a `reorderExercises` repository method and
  `ReorderWorkoutExercisesUseCase` that returns the reordered `WorkoutDetail` as a `Result`. The `Dependencies` key is
  registered in both containers. The app registers it only because the keys are shared, and gets no UI.
- backoffice: each workout exercise gets a drag handle (`DragIndicator`) on its left. Dropping a saved exercise at a new
  position reorders the list at once and sends the full order of saved exercises to the API. On failure the
  previous order comes back and an error is shown. Dropping at the same position sends nothing. Dragging is
  disabled while a reorder is in progress. Keyboard reordering is supported.
- Pending (unsaved) exercises are not part of the API's workout: their handle is disabled and they are never sent.
- New dependency in `backoffice` only: `@dnd-kit/core`, `@dnd-kit/sortable`, `@dnd-kit/utilities`.
- No new error code. `409` already maps to `CONFLICT`, and every error message reuses the existing `errors` copy.

## Capabilities

### New Capabilities

None.

### Modified Capabilities

- `workout-management`: adds reordering a workout's exercises (request shape, success result, failures as results).
- `backoffice-workout-editor`: adds the drag handle, the optimistic drag-and-drop reorder with its API call, the revert
  on failure, the in-progress lock and keyboard reordering. The exercise list now follows the on-screen order rather
  than only the order the API returned.

## Canonical References

- Use case -> `core/src/application/use-cases/workouts/remove-exercise-from-workout-use-case.ts`.
- Repository method -> `WorkoutRepository.update` in `core/src/infrastructure/repositories/workout-repository.ts`.
- State hook -> `backoffice/src/presentation/workouts/hooks/use-remove-workout-exercise.ts`.
- `SortableExerciseListItem` (molecule) -> `backoffice/src/presentation/workouts/components/molecules/exercise-list-item.tsx`.
  There is no drag-and-drop reference yet (atomic references are `pending` in `.stpr/template/README.md`), so **this
  change establishes the dnd-kit pattern**: sensors and contexts live in the organism, `useSortable` in the molecule,
  and the handle is the existing `IconButton` atom.
- `WorkoutExercisesList` (organism), `ExerciseListItem` (molecule) and `WorkoutView` (view) are extended in place.
  `IconButton` is reused as an atom.
- Tests -> `core/test/application/use-cases/workouts/remove-exercise-from-workout-use-case.test.ts`,
  `core/test/infrastructure/repositories/workout-repository.test.ts`,
  `backoffice/test/presentation/workouts/views/workout-view.test.tsx`, `backoffice/e2e/workout.spec.ts`.

## Fronts And Roles

- backoffice: `ADMIN` and `TRAINER` see the same handle and the same behaviour. Who may reorder and when a workout is
  locked is the API's rule: the front checks no role or status, and reports `FORBIDDEN` or `CONFLICT`.
- app (`USER`): no screen changes. The container registers the key only.

## Constraints And Conflicts

- No business rule in the front. The full list of saved exercise ids is always sent, and whether it matches the
  workout is the API's call (`409`).
- dnd-kit is not a UI widget library, so the "only atoms import the UI library's widgets" rule still holds: the handle
  is the `IconButton` atom. `DragIndicator` comes from `@mui/icons-material`, like the organism's existing icons.
- The reorder endpoint is not yet merged on `api/` main. The snapshot refresh must run against the API on the
  `add-reorder-workout-exercises` change. If the refreshed snapshot lacks the endpoint, the change stops.

## Acceptance Criteria

- Specs in this change pass. Core unit tests cover the use case and every repository error code. The
  `workout-view.test.tsx` component tests cover the handle, the reorder, the revert, the same-position drop and pending
  items. The `workout.spec.ts` E2E reorders with the keyboard and asserts the request body.
- `pnpm typecheck && pnpm lint && pnpm test && pnpm test:e2e` pass from `front/`.

## Impact

- `core`: snapshot, `domain/workouts/workout.ts`, `ports/workout-repository-interface.ts`,
  `infrastructure/repositories/workout-repository.ts`, `use-cases/workouts/`, `dependencies.ts`.
- `backoffice`: `package.json` (dnd-kit), `container.ts`, workouts hooks, molecules, organism and view, `i18n/{es,en}/workouts.json`, tests and E2E.
- `app`: `container.ts`, test support placeholders and the container test only.
- API: none. It depends on `add-reorder-workout-exercises` in `api/`.
