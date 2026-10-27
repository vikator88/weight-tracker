# Change Proposal: Reorder Workout Exercises By Drag And Drop

Source brief for `/opsx:propose`. Artifacts go to `openspec/changes/add-backoffice-workout-exercise-reorder/`.

## Change

Let trainers and admins reorder the exercises of a workout on the backoffice workout screen by dragging them. Each
exercise in the workout column gets a drag handle (`DragIndicator` icon) on its left. Dropping an exercise reorders the
list on screen at once and sends the new full order to the API. This brings the new
`PUT /workouts/{workoutId}/exercises/order` endpoint into `core` (snapshot refresh, alias, repository method and
`ReorderWorkoutExercisesUseCase`) and wires it in the backoffice.

## Objective

The workout screen lists the workout's exercises in the API's order, but that order can only be changed by removing
and re-adding exercises. The API now exposes a reorder endpoint (`api/` change `add-reorder-workout-exercises`). This
change makes the order editable where trainers already build the workout.

## Capabilities

- Every exercise in the workout column shows a drag handle (`DragIndicator`) on the left of the item.
- Dragging a saved exercise by its handle and dropping it at another position reorders the list in the component's
  state right away (optimistic), and the position numbers follow the new order.
- Every drop that changes the order sends `PUT /workouts/{workoutId}/exercises/order` with the catalog `exerciseId` of
  every **saved** exercise, in the new order.
- On success the list keeps the order it already shows (the one sent); the returned `WorkoutDetailResponse` is not
  merged back, so an exercise saved while the reorder was in flight is not lost.
- On failure the list goes back to the order it had before the drop, and an error is shown above the list.
- Dropping an exercise at its own position sends nothing.
- While a reorder request is in progress, dragging is disabled, so two reorders never race.
- Reordering also works from the keyboard (focus the handle, Space to pick up, arrows to move, Space to drop, Escape to
  cancel), using dnd-kit's keyboard sensor.
- Pending exercises (added from the catalog and not saved yet) are not part of the API's workout, so they cannot be
  reordered: they stay after the saved ones, and their handle is shown disabled.
- Selection (pencil) and removal (trash) keep working; the selected exercise stays selected after a reorder.

## Fronts And Roles

- backoffice -> lands here: yes -> roles: `ADMIN`, `TRAINER` -> both reorder the exercises of any workout the screen
  opens for them.
- app -> lands here: no -> roles: `USER` -> no screen changes and no new dependency. Its container registers the new
  use case only because the `Dependencies` keys are shared in `core`, as with every workout use case so far.

Both roles see the same handle. Who may reorder, and when a workout is locked (started or finished, for everyone but an
admin), is the API's rule: the front shows no role or status check and reports the API's `FORBIDDEN` / `CONFLICT`.

## Classification

Core:

- `ReorderWorkoutExercisesRequest` -> entity type (alias of the generated schema) -> `core/src/domain/workouts/workout.ts`
- `ReorderWorkoutExercisesInput` (`{ workoutId: string; exerciseIds: string[] }`) -> use case input type ->
  `core/src/domain/workouts/workout.ts`, next to `RemoveExerciseFromWorkoutInput`
- `WorkoutRepositoryInterface.reorderExercises(workoutId, exerciseIds): Promise<Result<WorkoutDetail, DomainError>>`
  -> repository contract -> `core/src/application/ports/workout-repository-interface.ts`
- `WorkoutRepository.reorderExercises` -> repository implementation -> `core/src/infrastructure/repositories/workout-repository.ts`
- `ReorderWorkoutExercisesUseCase` -> use case -> `core/src/application/use-cases/workouts/reorder-workout-exercises-use-case.ts`,
  exported from its `index.ts`
- Contract snapshot -> `core/openapi/v1.json` and `core/src/contract/generated` (regenerated, never hand-edited)

Backoffice:

- `reorderWorkoutExercisesUseCase` key -> `core/src/application/dependencies.ts`; registration -> wiring ->
  `backoffice/src/infrastructure/container.ts` and `app/src/infrastructure/container.ts`
- `useReorderWorkoutExercises()` (reordering, errorCode, reorder) -> state hook ->
  `backoffice/src/presentation/workouts/hooks/use-reorder-workout-exercises.ts`
- `WorkoutExercisesList` -> organism (extended in place) -> hosts the dnd-kit `DndContext` and `SortableContext`,
  receives `onReorder`, `reordering` and `reorderErrorCode`
- `SortableExerciseListItem` -> molecule -> `backoffice/src/presentation/workouts/components/molecules/sortable-exercise-list-item.tsx`:
  wraps `ExerciseListItem` with `useSortable` and renders the handle
- `ExerciseListItem` -> molecule (extended in place) -> gains an optional leading slot for the handle; the catalog
  keeps rendering it without one
- Drag handle -> reused atom `IconButton` with the `DragIndicator` icon (`@mui/icons-material/DragIndicator`), the
  dnd-kit listeners and attributes spread on it
- `WorkoutView` / `LoadedWorkout` -> view (extended in place) -> applies the optimistic order to `savedExercises`,
  calls the hook, reverts on failure
- Copy -> `backoffice/src/i18n/{es,en}/workouts.json`

## Canonical Template Mapping

- `ReorderWorkoutExercisesUseCase` -> `core/src/application/use-cases/workouts/remove-exercise-from-workout-use-case.ts`
  -> same shape: an input object passed straight to one repository method, returning its `Result`.
- `WorkoutRepository.reorderExercises` -> `WorkoutRepository.updateWorkout` in
  `core/src/infrastructure/repositories/workout-repository.ts` -> PUT with a JSON body, response body mapped to the
  domain alias, errors through the HTTP error mapper.
- Domain aliases -> existing aliases in `core/src/domain/workouts/workout.ts`.
- Core tests -> `core/test/application/use-cases/workouts/remove-exercise-from-workout-use-case.test.ts` and
  `core/test/infrastructure/repositories/workout-repository.test.ts`.
- `useReorderWorkoutExercises` -> `backoffice/src/presentation/workouts/hooks/use-remove-workout-exercise.ts` ->
  in-progress flag, errorCode, async action through `runWithSession` resolving to the outcome.
- `SortableExerciseListItem` -> `backoffice/src/presentation/workouts/components/molecules/exercise-list-item.tsx` ->
  same molecule level; it composes that molecule. dnd-kit has no reference yet: **this change establishes the pattern**
  (sensors and contexts in the organism, `useSortable` in the molecule, the handle is an atom).
- View test -> `backoffice/test/presentation/workouts/views/workout-view.test.tsx` (extended).
- E2E -> `backoffice/e2e/workout.spec.ts` (extended).

## Screens And Navigation

- backoffice -> `/workout?id=<id>` -> `WorkoutView` -> unchanged entry points -> drop: list reordered in place, no
  navigation; failure: previous order restored and an error alert above the list; the user stays on the screen.

## API Contract

- `PUT /workouts/{workoutId}/exercises/order` -> in `core/openapi/v1.json`: **no** -> body
  `ReorderWorkoutExercisesRequest` (`{ exerciseIds: string[] }`, catalog exercise ids, each once, in the new order),
  response `200` `WorkoutDetailResponse`; failures `400`, `401`, `403`, `404`, `409`.
- `GET /workouts/{id}` -> yes -> `WorkoutDetailResponse`, unchanged.

The endpoint is provided by the API change `add-reorder-workout-exercises` (in `api/openspec/changes/`). This change
**refreshes the snapshot**: with the API running on that change, `pnpm --filter @weight-tracker/core openapi:refresh`,
then `openapi:generate`; review the diff of `core/openapi/v1.json` and the regenerated types and commit them as a step
of their own, before the code that uses them.

## Held State

- `LoadedWorkout` `savedExercises` -> the saved exercises, now also in their current on-screen order -> the drop
  (optimistic), a failed reorder (restores the order kept before the drop), and
  the existing save / remove handlers -> screen exit.
- `useReorderWorkoutExercises` -> reordering, errorCode -> the view on drop -> cleared when the next reorder starts;
  screen exit.
- dnd-kit drag state (active item, overlay) -> internal to `DndContext` -> the drag -> drop or cancel.

## Forms And Structural Validation

None. No form is added. The full list of saved exercise ids is always sent; whether it matches the workout is the
API's rule (`409`).

## Errors And Copy

- Order out of date (exercise added or removed elsewhere) or workout locked -> `CONFLICT` -> previous order restored,
  error alert above the list with the existing `errors:CONFLICT` copy
- Not allowed -> `FORBIDDEN` -> same alert, existing copy
- Workout gone -> `NOT_FOUND` -> same alert, existing copy
- Invalid body -> `VALIDATION` -> same alert, existing copy
- Session -> `UNAUTHORIZED` -> `runWithSession`
- No response / other -> `NETWORK` / `UNEXPECTED` -> same alert, existing copy

No new error code: `409` already maps to `CONFLICT`; the HTTP error mapper is not touched.

New copy (`workouts` namespace):

- `workout.exercises.reorder` (handle label) -> "Reordenar ejercicio" -> "Reorder exercise"
- dnd-kit screen reader announcements (picked up, moved over, dropped, cancelled) and instructions -> to be proposed
  in the design, es and en

## Platform Capabilities And Dependencies

- `@dnd-kit/core`, `@dnd-kit/sortable`, `@dnd-kit/utilities` -> `backoffice` only, installed from `backoffice/` with
  `pnpm add @dnd-kit/core @dnd-kit/sortable @dnd-kit/utilities` -> MUI ships no sortable list, and no existing
  dependency handles drag and drop with keyboard and pointer sensors.
- No port and no adapter: drag and drop is presentation-only.

## Out Of Scope

- Reordering in the app.
- Reordering pending (unsaved) exercises, or mixing them with saved ones.
- Moving exercises between the catalog and the workout by dragging; adding stays on the plus button.
- Superseries grouping or keeping superseries contiguous (no such rule in the API).
- Up / down buttons or any partial move; the full order is always sent.
- Any change to the API, the HTTP error mapper or the catalog column.

## Notes

- Ids sent are catalog `exerciseId` values (`workoutExercise.exerciseId`), not the entry `id`; dnd-kit's sortable ids
  are the entry `id`s.
- Use `arrayMove` from `@dnd-kit/sortable` for the local reorder, `verticalListSortingStrategy`, `PointerSensor` and
  `KeyboardSensor` with `sortableKeyboardCoordinates`. Only the handle starts a drag, so the pencil and trash buttons
  keep their clicks.
- dnd-kit is not a UI widget library; the "only atoms import the UI library's widgets" rule is kept by rendering the
  handle with the `IconButton` atom. `DragIndicator` is imported from `@mui/icons-material` like the existing icons
  in the organism.
- The list keeps its current max height and scroll; dragging near the edge auto-scrolls (dnd-kit default).
- Verification tiers:
  - core unit: `ReorderWorkoutExercisesUseCase` (delegates and returns the repository `Result`);
    `WorkoutRepository.reorderExercises` success (method, path, body, mapped response) and every error code.
  - backoffice component (`workout-view.test.tsx`, use cases faked through the container): a handle per exercise;
    a keyboard reorder updates the numbering and calls the use case with the new `exerciseIds`; success keeps the new
    order; failure restores the previous order and shows the error; drop at the same position calls nothing; pending
    exercises' handles are disabled and they are not sent.
  - backoffice E2E (`workout.spec.ts`, API intercepted with `page.route`): reorder two exercises with the keyboard and
    assert the `PUT .../exercises/order` body and the new on-screen order.
- No deviations from the mapped references are allowed, apart from establishing the dnd-kit pattern named above.
