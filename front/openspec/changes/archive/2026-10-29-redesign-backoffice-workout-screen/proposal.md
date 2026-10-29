# Proposal

Source brief: `.stpr/plan/openspec/proposal-change-redesign-backoffice-workout-screen.md`.

## Why

The backoffice workout screen splits its width into three columns (catalog 25%, workout 40%, configuration 35%). The
catalog and the configuration take space all the time, even when the trainer is only reviewing the workout, and the
exercise list is left narrow. The screen also needs a "pending" exercise: an entry shown on screen that does not exist
in the API until its sets are saved. Moving the catalog and the configuration into modals that open on demand gives the
workout the full width. Because an exercise is only sent once it is configured, the pending state goes away.

## What Changes

- **BREAKING (screen layout)**: the three columns become two cards stacked at the full page width: workout properties
  on top and the workout's exercises below. The page heading and the "Volver" link stay.
- **BREAKING (exercise list)**: each listed exercise becomes its own bordered card. The cards flow left to right in
  rows of at most three (fewer on narrower screens), and drag-and-drop and keyboard reordering keep working across the
  rows.
- The properties card holds the name, date, description and "Guardar", as today. The exercise list moves out of it
  into its own exercises card.
- The exercises card's header has an "Añadir ejercicio" button.
- **Add flow**: "Añadir ejercicio" opens the catalog in a large modal, at least 75% of the viewport width, with today's
  text filter, body-part pills and list. Choosing an exercise closes the catalog and opens the configuration modal for
  it, empty. Saving a valid configuration is the only point where the add is sent to the API. On success the modal
  closes and the exercise is appended to the list. On failure the modal stays open with the values and the error.
  Dismissing either modal (close or Cancelar, Escape, backdrop) adds nothing and sends nothing.
- **Edit flow**: the pencil on a listed exercise opens the **same** configuration modal component, prefilled with the
  stored effort and sets. Saving replaces them through the API. Success closes the modal and updates the entry; failure
  keeps the modal open. Dismissing discards the edits.
- **BREAKING (behaviour)**: pending exercises are removed. The list only shows exercises the API has saved. The
  "Pendiente" mark, the disabled handle of pending entries and the "Selecciona un ejercicio para configurarlo."
  placeholder no longer exist, and neither does the "Ejercicio guardado." confirmation, because the modal now closes
  on save.
- Reordering (drag handle and keyboard), removal, the properties save, the catalog filters, the labels, the video and
  the loading, not found and failure states keep their behaviour.
- No change in `core`, the API contract, the HTTP error mapper or the app.

## Capabilities

### New Capabilities

None.

### Modified Capabilities

- `backoffice-workout-editor`: the layout changes from three columns to two cards at full width. The catalog and the
  configuration become modals. Adding an exercise becomes catalog modal, then configuration modal, then API call.
  Editing opens the same configuration modal. Pending exercises and the column-only states (selection placeholder,
  saved confirmation, selection kept on reorder) are removed.

## Canonical References

`.stpr/template/README.md` lists the atomic UI references as `pending`, so each piece takes the closest component of
the same front:

- `ExerciseCatalogDialog` (organism, new) -> `backoffice/src/presentation/workouts/components/organisms/create-workout-dialog.tsx`
  for the `Dialog` atom with `open` / `onClose`. Its content is moved from `exercise-catalog-panel.tsx`, which it
  replaces.
- `ExerciseConfigurationDialog` (organism, new) -> `create-workout-dialog.tsx` for the dialog shell (title, content,
  actions with Cancelar and the submit button). Its form, validation and save are moved from
  `exercise-configuration-panel.tsx`, which it replaces.
- `WorkoutExercisesCard` (organism, new) -> `workout-details-card.tsx` for the outlined card with an `h2` title
  labelling its section. It hosts the existing `WorkoutExercisesList` organism.
- `WorkoutDetailsCard` (organism), `WorkoutExercisesList` (organism), `ExerciseListItem` (molecule, gains a card
  variant), `SortableExerciseListItem` (molecule) and `WorkoutView` (view) are changed in place.
- Reused atoms: `Dialog`, `DialogTitle`, `DialogContent`, `DialogActions`, `Card`, `CardContent`, `Button`,
  `IconButton`, `Typography`. Reused molecules: `ExerciseListItem`, `BodyPartFilter`, `ExerciseVideo`. No new atom or
  molecule.
- Reused state hooks: `useSaveWorkoutExercise`, `useRemoveWorkoutExercise`, `useReorderWorkoutExercises`,
  `useUpdateWorkout`, `useWorkout`, `useExercises`. None changes behaviour (`useSaveWorkoutExercise` only renames its
  `pending` parameter to `adding`).
- Tests -> `backoffice/test/presentation/workouts/views/workout-view.test.tsx` (rewritten for the new flows) and
  `backoffice/e2e/workout.spec.ts` (add and edit flows updated).

## Fronts And Roles

- backoffice: `ADMIN` and `TRAINER` see the same screen, cards and modals. Who may edit a workout and when it is
  locked is the API's rule. The front checks no role or status and reports `FORBIDDEN` or `CONFLICT`, as today.
  Neither role sees pending exercises any more.
- app (`USER`): no change, no new key.

## API Endpoints

Every endpoint is already in `core/openapi/v1.json` and reached through an existing use case. There is no snapshot
refresh.

- `GET /workouts/{id}` (`getWorkoutByIdUseCase`): read the workout.
- `GET /exercises` (`getExercisesUseCase`): read the catalog.
- `PUT /workouts/{id}` (`updateWorkoutUseCase`): save the properties.
- `POST /workouts/{workoutId}/exercises` (`addExerciseToWorkoutUseCase`): add, sent only from the configuration modal.
- `PUT /workouts/{workoutId}/exercises/{exerciseId}` (`updateWorkoutExerciseUseCase`): edit.
- `DELETE /workouts/{workoutId}/exercises/{exerciseId}` (`removeExerciseFromWorkoutUseCase`): remove.
- `PUT /workouts/{workoutId}/exercises/order` (`reorderWorkoutExercisesUseCase`): reorder.

## Constraints And Conflicts

- No business rule in the front. The configuration form keeps its structural validation (an effort is chosen, at least
  one set, set counts and targets are positive integers). Whether the exercise can be added is the API's call.
- This change contradicts several requirements of the archived `add-backoffice-workout-screen` and
  `add-backoffice-workout-exercise-reorder` changes: the three columns, the catalog plus adding locally, pending
  exercises and their disabled handle, the selection placeholder, and the selection kept on reorder. The delta spec
  removes or rewrites each one explicitly.
- No conflict with the invariants or a stop case: the change is presentation only, with no new error code, endpoint or
  dependency.

## Acceptance Criteria

- The delta spec's scenarios pass in `workout-view.test.tsx`, with the use cases faked through the container.
- `workout.spec.ts` adds an exercise through both modals, edits one through the pencil, and asserts both request bodies
  and the list. The existing properties, removal and reorder flows still pass.
- `pnpm typecheck && pnpm lint && pnpm test && pnpm test:e2e` pass from `front/`.

## Impact

- `backoffice/src/presentation/workouts/`: `views/workout-view.tsx`; the organisms `workout-details-card.tsx` and
  `workout-exercises-list.tsx`; the new `exercise-catalog-dialog.tsx`, `exercise-configuration-dialog.tsx` and
  `workout-exercises-card.tsx`; the deleted `exercise-catalog-panel.tsx` and `exercise-configuration-panel.tsx`; and
  the molecule `sortable-exercise-list-item.tsx`.
- `backoffice/src/i18n/{es,en}/workouts.json`: new and removed keys.
- `backoffice/test/presentation/workouts/views/workout-view.test.tsx`, `backoffice/e2e/workout.spec.ts`.
- `core`, `app`, API: none.
