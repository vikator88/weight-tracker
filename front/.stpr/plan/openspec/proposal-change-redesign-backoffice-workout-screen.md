# Change Proposal: Redesign The Backoffice Workout Screen

Source brief for `/opsx:propose`. Artifacts go to `openspec/changes/redesign-backoffice-workout-screen/`.

## Change

Redesign the backoffice edit workout screen (`/workout?id=<id>`). The three columns (catalog 25% / workout 40% /
configuration 35%) become two full-width columns: workout properties on the left (30%) and the workout's exercises on
the right (70%), each in its own card. The exercise catalog and the exercise configuration move into modals. Adding an
exercise becomes a two-step modal flow (pick from the catalog, then configure), and the API is only called when the
configuration is saved. Editing an exercise opens the same configuration modal.

## Objective

The three-column layout squeezes every column: the catalog and the configuration take room all the time, even when
the trainer is only reviewing the workout, and the exercise list is narrow. Showing only the workout and opening the
catalog and the configuration on demand gives the list the width it needs. It also removes the "pending" exercise,
an entry that sits on screen without existing in the API until its sets are saved.

## Capabilities

- The screen uses the full width of the page (no `xl` max width on the container) for the workout.
- From `md` up, two columns: workout properties (30%) on the left, exercises (70%) on the right. Below `md`, they
  stack, properties first.
- Each column is wrapped in its own card: a "properties" card (name, description, date, save button, as today) and an
  "exercises" card (the sortable list, as today). The exercise list is no longer nested inside the properties card.
- The exercises card has an "Add exercise" button in its header.
- **Add flow:**
  1. "Add exercise" opens the catalog modal.
  2. The catalog modal is large (at least 75% of the viewport width) and keeps today's catalog content: text filter,
     body-part filter and the list of exercises not yet in the workout. It is dismissible (close button, Escape,
     backdrop click); dismissing adds nothing.
  3. Choosing an exercise closes the catalog modal and opens the configuration modal for it, empty (no effort, no
     sets). It is dismissible the same way; dismissing adds nothing and nothing is sent.
  4. Saving a valid configuration calls the API to add the exercise to the workout. On success the modal closes and
     the exercise appears at the end of the list. On failure the modal stays open with the values entered and an error.
- **Edit flow:** the pencil button of an exercise in the list opens the **same** configuration modal component,
  prefilled with the stored effort and sets. Saving updates the exercise through the API; success closes the modal and
  updates the list entry; failure keeps it open with an error. Dismissing discards the changes.
- Reordering (drag handle and keyboard) and removal (trash button) keep working on the list as today.
- An exercise already in the workout stays out of the catalog, as today.
- No pending exercises: the list only ever shows exercises the API has saved.

## Fronts And Roles

- backoffice -> lands here: yes -> roles: `ADMIN`, `TRAINER` -> both edit the properties and the exercises of any
  workout the screen opens for them, as today.
- app -> lands here: no -> roles: `USER` -> no change.

Both roles see the same screen. Who may edit and when a workout is locked is the API's rule; the front shows no role
or status check and reports the API's `FORBIDDEN` / `CONFLICT`, as today.

## Classification

Backoffice only, all under `backoffice/src/presentation/workouts/`:

- `WorkoutView` / `LoadedWorkout` -> view (changed in place) -> `views/workout-view.tsx`: two-column layout,
  full-width container, owns which modal is open and for which exercise; drops the pending state.
- `WorkoutDetailsCard` -> organism (changed in place) -> `components/organisms/workout-details-card.tsx`: properties
  only; no longer receives the exercise list as children.
- `WorkoutExercisesCard` (or `WorkoutExercisesList` extended in place, to be settled in the design) -> organism ->
  `components/organisms/`: card with the title, the "Add exercise" button and the sortable list.
- `ExerciseCatalogDialog` -> organism -> `components/organisms/exercise-catalog-dialog.tsx`: the `Dialog` atom
  wrapping the catalog content; emits the chosen exercise. Replaces `ExerciseCatalogPanel` (its content may be kept
  as the dialog body; the card wrapper goes).
- `ExerciseConfigurationDialog` -> organism -> `components/organisms/exercise-configuration-dialog.tsx`: the
  `Dialog` atom wrapping today's configuration form; one component for both add (new exercise, empty values) and
  edit (existing entry, stored values). Replaces `ExerciseConfigurationPanel`.
- `useSaveWorkoutExercise` -> state hook (reused) -> unchanged unless the design shows a need.
- Existing atoms reused: `Dialog`, `DialogTitle`, `DialogContent`, `DialogActions`, `Card`, `CardContent`, `Button`,
  `IconButton`. No new atom or molecule is expected.
- Copy -> `backoffice/src/i18n/{es,en}/workouts.json`.

No change in `core`: no new use case, repository or domain type.

## Canonical Template Mapping

- `ExerciseCatalogDialog`, `ExerciseConfigurationDialog` ->
  `backoffice/src/presentation/workouts/components/organisms/create-workout-dialog.tsx` -> the nearest dialog in the
  same front: `Dialog` atom with title, content and actions, `open` / `onClose` props, form inside.
- Catalog dialog body -> `components/organisms/exercise-catalog-panel.tsx` -> its current content moves over.
- Configuration dialog body -> `components/organisms/exercise-configuration-panel.tsx` -> its current form,
  validation and save move over.
- Exercises card -> `components/organisms/workout-details-card.tsx` -> same card structure (outlined `Card`, `h2`
  title labelling the section).
- View test -> `backoffice/test/presentation/workouts/views/workout-view.test.tsx` (rewritten for the new flows).
- E2E -> `backoffice/e2e/workout.spec.ts` (updated for the new flows).

## Screens And Navigation

- backoffice -> `/workout?id=<id>` -> `WorkoutView` -> unchanged entry points (assigned plans list, create workout) ->
  no navigation inside the screen: modals open over it. Add success: modal closes, exercise appended to the list. Edit
  success: modal closes, entry updated. Failure: the modal stays open with an error alert inside it. Properties save
  and failures behave as today (alerts above the columns).

## API Contract

No change. Endpoints already in `core/openapi/v1.json`, used through their existing use cases:

- `GET /workouts/{id}` -> yes -> `WorkoutDetailResponse`
- `GET` exercise catalog -> yes -> as used by `useExercises`
- `PUT /workouts/{id}` -> yes -> properties save, as today
- add / update workout exercise -> yes -> as used by `useSaveWorkoutExercise` today
- remove workout exercise -> yes -> as used by `useRemoveWorkoutExercise` today
- `PUT /workouts/{workoutId}/exercises/order` -> yes -> as used by `useReorderWorkoutExercises` today

No snapshot refresh.

## Held State

- `LoadedWorkout` open modal -> none / catalog / configuration (with the catalog exercise being added, or the saved
  entry being edited) -> "Add exercise", choosing in the catalog, the pencil button, dismiss, successful save -> screen
  exit.
- `LoadedWorkout` `savedExercises` -> unchanged role; now the only exercise list (the `pending` list is removed) ->
  successful add / edit / remove / reorder -> screen exit.
- Catalog filters (text, body part) -> kept in the view, so they survive closing and reopening the catalog while on the
  screen -> the user -> screen exit.
- Configuration form values -> inside the configuration dialog, initialised from the item each time it opens ->
  discarded on dismiss.

## Forms And Structural Validation

- Workout properties form -> unchanged.
- Exercise configuration form -> unchanged: the same structural validation it has today, now run inside the modal.

## Errors And Copy

- Add or edit refused -> existing codes (`VALIDATION`, `CONFLICT`, `FORBIDDEN`, `NOT_FOUND`, `NETWORK`, `UNEXPECTED`)
  -> error alert inside the configuration modal, existing `errors:` copy; the modal stays open.
- Session -> `UNAUTHORIZED` -> `runWithSession`, as today.
- Remove, reorder and properties save errors -> unchanged.

No new error code; the HTTP error mapper is not touched.

New or changed copy (`workouts` namespace), to be confirmed in the design:

- `workout.exercises.add` (button) -> "Añadir ejercicio" -> "Add exercise"
- `workout.details.title` (properties card title) -> "Propiedades" -> "Properties", if the card has no title today
- configuration modal title for add vs edit -> to be proposed, es and en
- modal close / cancel labels -> to be proposed, es and en
- Keys that only served removed UI (`workout.configuration.empty`, `workout.exercises.pending`, possibly
  `workout.configuration.saved`) are removed from both languages.

## Platform Capabilities And Dependencies

None.

## Out Of Scope

- Any change to the API, `core`, the HTTP error mapper or the app.
- Adding several exercises in one go from the catalog.
- Drag and drop from the catalog into the list.
- Changing the properties form, the configuration form fields or the reorder behaviour.
- A separate edit screen or route for an exercise; everything stays on `/workout?id=<id>`.

## Notes

- "Full screen" is read as full page width inside the `AppShell` (header and drawer stay), not a browser fullscreen
  mode.
- The catalog modal width: `Dialog` with `maxWidth={false}` and a paper width of at least 75vw (capped at the
  viewport on small screens). The configuration modal size is to be settled in the design (`md` or `lg` is expected to
  fit the sets form and video).
- One configuration modal component serves add and edit; it is keyed by the item it opens for, so each opening starts
  from the stored values.
- Removing pending exercises simplifies the view (no `pendingItemOf`, no `pending` flag on `WorkoutExerciseItem`, no
  disabled handle for pending entries). The reorder change's pending rules no longer apply.
- Verification tiers:
  - core unit: none (no core change).
  - backoffice component (`workout-view.test.tsx`, use cases faked through the container): two cards render; "Add
    exercise" opens the catalog; dismissing the catalog adds nothing; choosing an exercise opens the configuration
    modal empty; dismissing it calls no use case; saving calls the add use case and appends the exercise; a failed
    save keeps the modal open with the error; the pencil opens the same modal prefilled and saving calls the update;
    exercises in the workout are absent from the catalog; reorder and remove still work.
  - backoffice E2E (`workout.spec.ts`, API intercepted with `page.route`): add an exercise through both modals and
    assert the request body and the new list entry; edit an exercise through the pencil and assert the request.
- No deviations from the mapped references are allowed.
