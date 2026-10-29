# Tasks

No `core` or wiring work: the change uses only existing use cases and `container.ts` keys.

## 1. Backoffice copy

- [x] 1.1 Add `workout.properties.title`, `workout.exercises.add`, `workout.catalog.choose`, `workout.catalog.close` and `workout.configuration.cancel` with the texts from the design to `backoffice/src/i18n/es/workouts.json` and `en/workouts.json`, keeping the old keys for now. Verify the translation parity test passes.

## 2. Backoffice dialog organisms

- [x] 2.1 Create `components/organisms/exercise-catalog-dialog.tsx` from the body of `exercise-catalog-panel.tsx`, shaped like `create-workout-dialog.tsx`. It uses the `Dialog` atom with `fullWidth`, `maxWidth={false}`, a paper width of `{ md: "75vw" }` and `aria-labelledby`. `DialogTitle` holds "Catálogo de ejercicios" and a "Cerrar" `IconButton`. Each item gets an "Elegir ejercicio" plus button calling `onChoose`, and the filter props are kept. Verify with `pnpm --filter backoffice typecheck` and `lint` (no MUI widget import outside atoms).
- [x] 2.2 Create `components/organisms/exercise-configuration-dialog.tsx` from the form of `exercise-configuration-panel.tsx`. It uses the `Dialog` atom with `fullWidth` and `maxWidth="md"`, the title "Detalles del ejercicio", the form in `DialogContent`, and "Cancelar" and "Guardar" in `DialogActions`. Its props are `open`, `item`, `adding`, `workoutId`, `onSaved(entry)` and `onClose`. While saving, `onClose` is ignored and "Cancelar" is disabled. There is no `justSaved`, no `onEdit`, no success alert and no empty placeholder. Verify with the backoffice typecheck and lint.
- [x] 2.3 Rename the `pending` parameter of `useSaveWorkoutExercise.save` to `adding` and update its doc comment, with no behaviour change. Verify with the backoffice typecheck.

## 3. Backoffice cards and list

- [x] 3.1 Change `WorkoutDetailsCard`: remove `children` and its wrapper, and set the title to `workout.properties.title`. Verify with the backoffice typecheck.
- [x] 3.2 Create `components/organisms/workout-exercises-card.tsx`, shaped like `workout-details-card.tsx`. It is an outlined card with the `h2` "Ejercicios", an "Añadir ejercicio" `Button` (`AddOutlined`) calling `onAdd`, and the list below them. Verify with the backoffice typecheck and lint.
- [x] 3.3 Change `WorkoutExercisesList`:
  - drop its `h3`;
  - replace `selectedId` / `onSelect` with `onEdit(item)`;
  - remove the "Pendiente" note and the `pending`-based disabling;
  - make `sortableIds` every item;
  - delete `pending` from `WorkoutExerciseItem`.

  Verify with the backoffice typecheck.

## 4. Backoffice view and component tests

- [x] 4.1 Rewire `LoadedWorkout` in `workout-view.tsx`.
  - Layout: `Container maxWidth={false}` and a `3fr / 7fr` grid at `md` holding `WorkoutDetailsCard` and `WorkoutExercisesCard`.
  - State: the single `OpenDialog` state replaces `pending`, `selectedId` and `lastSavedId`, and `newItemOf` replaces `pendingItemOf`.
  - Saving: `handleSaved` appends when adding and replaces when editing, then closes the dialog.
  - Lists: `available` excludes only saved exercises, and `handleRemove` has no pending branch.
  - Dialogs: the catalog dialog gets the kept filters. The configuration dialog is keyed by the item's entry id. Focus returns to "Añadir ejercicio" when an add flow ends.

  Verify with the backoffice typecheck.

- [x] 4.2 Delete `exercise-catalog-panel.tsx` and `exercise-configuration-panel.tsx`. Verify that nothing imports them, with typecheck and lint.
- [x] 4.3 Rewrite `backoffice/test/presentation/workouts/views/workout-view.test.tsx` with one test per scenario of the delta spec, using AAA with exactly one Act each.
  - Add the `openCatalog`, `chooseFromCatalog` and `configurationDialog` helpers, and scope `addSet` and `chooseEffort` to the dialog.
  - Delete the tests for removed requirements.
  - Keep the local rect stub for reordering.

  Verify with `pnpm --filter backoffice test`.

- [x] 4.4 Remove `workout.structure.title`, `workout.catalog.add`, `workout.exercises.pending`, `workout.configuration.empty` and `workout.configuration.saved` from both languages. Verify that nothing references them (grep) and that the parity test passes.

## 5. Backoffice E2E

- [x] 5.1 Update `backoffice/e2e/workout.spec.ts`.
  - Rename the regions to "Propiedades del entrenamiento" and "Ejercicios".
  - The add flow goes through "Añadir ejercicio", the catalog dialog's "Elegir ejercicio" and the configuration dialog's "Guardar". Assert the `POST .../exercises` body, that the dialog closes, and that the entry is listed after a reload.
  - Add an edit flow through the pencil that asserts the `PUT .../exercises/{exerciseId}` body.
  - Keep the properties, removal and reorder flows passing.

  Verify with `pnpm test:e2e`.

## 6. Review and verification

- [x] 6.1 Review the backoffice against its references:
  - the dialogs against `create-workout-dialog.tsx`;
  - the exercises card against `workout-details-card.tsx`;
  - the moved content against the deleted panels;
  - the view test against its previous structure.

  Verify there are no differences beyond those in the design.

- [x] 6.2 Run the `.stpr/template/` drift check from `AGENTS.md`. Verify it prints no `DRIFT`.
- [x] 6.3 From `front/`, run `pnpm typecheck && pnpm lint && pnpm test && pnpm test:e2e`. Verify everything passes.

## 7. Stacked layout and exercise cards

- [x] 7.1 Stack the two cards at full width in `workout-view.tsx` (one column on every screen size, properties on top). Verify with the backoffice typecheck.
- [x] 7.2 Add the `variant?: "row" | "card"` prop to `ExerciseListItem`: the card variant wraps the content in the outlined `Card` atom and drops the divider. Verify the catalog still renders rows (existing catalog tests pass).
- [x] 7.3 In `WorkoutExercisesList`, pass `variant="card"`, lay the `List` out as a grid of at most four columns (`xs` 1, `sm` 2, `md` 3, `lg` 4), and switch to `rectSortingStrategy`. Verify with the backoffice typecheck and lint.
- [x] 7.4 Update `workout-view.test.tsx`. The rect stub lays items out in rows of four, and the reorder helpers use ArrowRight. Add a test that every listed exercise sits in its own outlined card, and assert the properties card comes before the exercises card. Verify with `pnpm --filter backoffice test`.
- [x] 7.5 Update `workout.spec.ts`. `moveFirstExerciseDown` becomes a move to the right with ArrowRight. Add a flow that checks two exercises sit side by side in one row, and five exercises wrap after the fourth, using bounding boxes. Also check the properties card sits above the exercises card. Verify with `pnpm test:e2e`.
- [x] 7.6 Format the artifacts with Prettier and run `pnpm typecheck && pnpm lint && pnpm test && pnpm test:e2e` from `front/`. Verify everything passes.

## 8. Three per row, card rows and compact sets

- [x] 8.1 Cap the exercises grid at three columns (`xs` 1, `sm` 2, `md` 3), and drop its maximum height and scroll, so the card grows with its rows. Verify with the backoffice typecheck.
- [x] 8.2 Add `workout.exercises.compactSummary.*` (es and en) and a `compactSetSummary` to `useExerciseLabels`. Remove `workout.exercises.setsLine`, and keep `workout.configuration.summary.*` for the dialog. Verify the parity test passes.
- [x] 8.3 Lay the card variant of `ExerciseListItem` out in rows: handle, position, name and actions on the first row; labels on the second; lines below. The sets line joins the compact summaries with ", ", without a label. Verify with the backoffice typecheck and lint.
- [x] 8.4 Update the view test (rect stub in rows of three; compact sets line; a test for the rows inside a card; the exercises list has no maximum height while the catalog keeps it) and the E2E (four exercises wrap after the third; compact sets line). Verify with `pnpm --filter backoffice test` and `pnpm test:e2e`.
- [x] 8.5 Format the artifacts and run `pnpm typecheck && pnpm lint && pnpm test && pnpm test:e2e` from `front/`. Verify everything passes.

## 9. Muscle filter and label colours

- [x] 9.1 Add `workout.catalog.bodyParts` ("Partes del cuerpo" / "Body parts") and `workout.catalog.muscles` ("Músculos" / "Muscles") (es and en). Verify the parity test passes.
- [x] 9.2 Replace `BodyPartFilter` with the `LabelFilter` molecule (coloured options, outlined or filled, a visible caption naming the group), and add `variant` to `ExerciseLabel`. Verify with the backoffice typecheck and lint.
- [x] 9.3 In the catalog dialog, add the muscle row while a body part is selected, and filter by muscle. In the view, hold `selectedMuscle` and clear it when the body part changes. Colour the labels (body part filled, muscle outlined in the body part's colour) in the catalog, the workout list and the configuration dialog. Verify with the backoffice typecheck.
- [x] 9.4 Update `workout-view.test.tsx` with one test per new scenario: no muscle row without a body part, the muscles of the body part, filtering by muscle, the muscle cleared with its body part, the row labels, the label colours and variants, and the pill colours. Verify with `pnpm --filter backoffice test`.
- [x] 9.5 Show the catalog's exercises as cards in a grid of at most three columns, sharing `EXERCISE_GRID_COLUMNS` with the workout list, and keeping the catalog's maximum height and scroll. Add tests that each catalog exercise is an outlined card, and that the E2E catalog cards share a row. Verify with `pnpm --filter backoffice test` and `pnpm test:e2e`.
- [x] 9.6 Format the artifacts and run `pnpm typecheck && pnpm lint && pnpm test && pnpm test:e2e` from `front/`. Verify everything passes.

## 10. Single-muscle body parts

- [x] 10.1 Derive `singleMuscleBodyParts` from the full catalog in the view and pass it to `WorkoutExercisesList` and `ExerciseCatalogDialog`. They leave out the muscle label of those body parts, and the catalog hides the muscle row when it would hold fewer than two pills. Verify with the backoffice typecheck and lint.
- [x] 10.2 Update `workout-view.test.tsx`. Use a catalog with two legs muscles where a muscle label is expected. Add tests for the single-muscle label (catalog and workout list leave it out, the dialog keeps it) and for the hidden muscle row. Verify with `pnpm --filter backoffice test`.
- [x] 10.3 Format the artifacts and run `pnpm typecheck && pnpm lint && pnpm test && pnpm test:e2e` from `front/`. Verify everything passes.

## 11. Configuration dialog in two columns

- [x] 11.1 Lay `ExerciseConfigurationDialog` out as a `3fr / 7fr` grid at `md`: video, name and labels on the left, the configuration on the right, stacked below `md`. Verify with the backoffice typecheck, lint and tests.
- [x] 11.2 Rename the dialog title to "Configuración del ejercicio" / "Exercise configuration" in the copy, the tests and the spec. Verify with the backoffice tests.
- [x] 11.3 Add an E2E check that the name sits in the left column and the effort field to its right. Verify with `pnpm test:e2e`.
- [x] 11.4 Format the artifacts and run `pnpm typecheck && pnpm lint && pnpm test && pnpm test:e2e` from `front/`. Verify everything passes.

## Workflow follow-up

- Archive the change once it is reviewed and merged.
