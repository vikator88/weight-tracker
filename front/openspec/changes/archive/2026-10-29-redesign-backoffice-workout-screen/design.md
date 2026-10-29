# Design

## Context

See `proposal.md` for the motivation and `specs/backoffice-workout-editor/spec.md` for the behaviour. This is the
current state that shapes the approach:

- `LoadedWorkout` (`backoffice/src/presentation/workouts/views/workout-view.tsx`) renders three grid columns: the
  `ExerciseCatalogPanel`, the `WorkoutDetailsCard` with `WorkoutExercisesList` as its children, and the
  `ExerciseConfigurationPanel`. The panel is keyed by `selectedId` so each selection starts from the stored values.
- The view holds `savedExercises`, `pending`, `selectedId`, `lastSavedId` and the catalog filters. `items` is
  `[...saved, ...pending]`, and `available` is the catalog minus every listed exercise.
- `WorkoutExerciseItem` (exported by `workout-exercises-list.tsx`) carries `pending?`. The list uses it for the
  "Pendiente" note and the disabled handle. The configuration panel uses it to choose between adding and updating
  (`useSaveWorkoutExercise.save(workoutId, pending, request)`).
- `CreateWorkoutDialog` is the only dialog in the backoffice. It uses the `Dialog` atom with `open`, `onClose`,
  `fullWidth`, `maxWidth` and `aria-labelledby`, and puts the form inside, so it unmounts and resets when it closes.
- MUI is v9. `Dialog` renders in a portal with `role="dialog"`, traps focus, and makes the page behind it inert.

## Goals / Non-Goals

**Goals:**

- Move the catalog and configuration content into dialogs without changing that content's behaviour.
- Remove every trace of pending state.
- Keep reorder and removal untouched apart from the props they lose.

**Non-Goals:**

- A generic modal or wizard abstraction.
- Changes to `BodyPartFilter`, `ExerciseVideo` or any atom. `ExerciseListItem` only gains an opt-in card variant;
  the catalog keeps the row layout.
- Any app or `core` change.

## Decisions

### One dialog state in the view

`LoadedWorkout` replaces `pending`, `selectedId` and `lastSavedId` with one discriminated state:

```ts
type OpenDialog =
  | { kind: "none" }
  | { kind: "catalog" }
  | { kind: "configure"; item: WorkoutExerciseItem; adding: boolean };
```

- "Añadir ejercicio" sets it to `catalog`. Choosing an exercise sets it to `configure` with `adding: true` and an
  item built from the catalog exercise (`newItemOf`, which replaces `pendingItemOf`: placeholder entry id
  `new-<exerciseId>`, empty effort, no sets). The pencil sets it to `configure` with `adding: false` and the listed
  item. Any close sets it to `none`.
- A single state means only one dialog can be open at a time. The catalog-to-configuration handoff is then one state
  change, with no moment where both dialogs are open.
- _Alternative_: separate booleans for each dialog, plus a selected item. It was rejected because it allows invalid
  combinations, such as both open or a configuration with no item.

### `savedExercises` is the only list

- `items` becomes `savedExercises` mapped to their catalog entries. `available` filters out only saved exercise ids.
- `handleSaved(entry)` with `adding` appends `entry`. Without `adding`, it replaces the entry with the same
  `exerciseId`, as today. In both cases the dialog then closes.
- `handleRemove` loses its pending branch and its selection reset.
- `handleReorder` is unchanged.
- `WorkoutExerciseItem.pending` is deleted. Add versus update is decided by the dialog's `adding` prop, passed
  straight to `useSaveWorkoutExercise.save` as its boolean. The hook's behaviour does not change. Only its parameter
  name and doc comment change from `pending` to `adding`.

### Components (all organisms under `backoffice/src/presentation/workouts/components/organisms/`)

- **`ExerciseCatalogDialog`** (new, replaces `exercise-catalog-panel.tsx`). Props: `open`, `exercises`, the filter
  props the panel has today (`filterText`, `onFilterTextChange`, `selectedBodyPart`, `onToggleBodyPart`), `onChoose`
  and `onClose`.
  - It uses the `Dialog` atom with `fullWidth`, `maxWidth={false}`, a paper width of `{ md: "75vw" }` through
    `slotProps.paper.sx` (full width minus margins below `md`) and `aria-labelledby` its title.
  - `DialogTitle` holds "Catálogo de ejercicios" and a "Cerrar" `IconButton` with `CloseOutlined`. `DialogContent`
    holds the panel's current body. The list keeps `EXERCISE_LIST_MAX_HEIGHT` and its own scroll.
  - Each item's action is the `IconButton` atom with `AddOutlined`, labelled "Elegir ejercicio". Only the label
    changes.
- **`ExerciseConfigurationDialog`** (new, replaces `exercise-configuration-panel.tsx`). Props: `open`, `item`,
  `adding`, `workoutId`, `onSaved(entry)` and `onClose`.
  - It uses the `Dialog` atom with `fullWidth` and `maxWidth="md"`. `DialogTitle` reads "Configuración del ejercicio".
    `DialogContent` holds the panel's form unchanged (video, name and labels, error alert, effort, sets list, set
    form). `DialogActions` holds "Cancelar" and "Guardar".
  - The form is an inner component, as in `CreateWorkoutDialog`, and the view keys the dialog by the item's entry id.
    Each opening therefore starts from the stored values, and closing discards edits.
  - While `saving` is true, `onClose` is ignored (Escape and backdrop) and "Cancelar" is disabled. The request is
    then never orphaned with its result lost.
  - The `justSaved` and `onEdit` props, the success alert and the `item === null` placeholder are removed.
- **`WorkoutExercisesCard`** (new). It is an outlined `Card` with a section labelled by its `h2` "Ejercicios". A header
  row holds the title and an "Añadir ejercicio" `Button` (`startIcon` `AddOutlined`) that calls `onAdd`.
  `WorkoutExercisesList` sits below the header row. This mirrors `WorkoutDetailsCard`.
- **`WorkoutExercisesList`** (changed). It drops its own "Ejercicios" `h3`, because the card owns the title, and keeps
  the list's `aria-label`.
  - `selectedId` / `onSelect` become `onEdit(item)`. `selected` is no longer passed to the item.
  - The "Pendiente" note goes. The handle is disabled only while `reordering`. `sortableIds` covers every item.
- **`SortableExerciseListItem`** (molecule): no prop change. Only the call site stops passing `pending` into
  `disabled`.
- **`WorkoutDetailsCard`** (changed). It drops `children` and its `mt: 3` wrapper. Its title key changes from
  `workout.structure.title` to `workout.properties.title`.

Imports flow view → organisms → molecules → atoms. Only atoms import MUI widgets: the organisms use `Box` for layout,
as today, plus the `Dialog*`, `Card*`, `Button`, `IconButton` and `Typography` atoms. Icons come from
`@mui/icons-material`, as in the existing organisms.

### Muscle filter and label colours

- `BodyPartFilter` (molecule) becomes the generic `LabelFilter` (`components/molecules/label-filter.tsx`). Its props
  are `options: { code, label, color }[]`, `selected`, `onToggle` and a `label`. The label is shown as a small
  caption above the pills and names their `role="group"` through `aria-labelledby`.
  Each pill is the `Chip` atom in its option's colour, `outlined` while unselected and `filled` while selected. The
  catalog dialog renders it twice: "Parte del cuerpo", then "Músculo" only while a body part is selected.
- The view holds `selectedMuscle` beside `selectedBodyPart`, so it survives a reopening like the other filters.
  Toggling a body part (to another one or off) also clears `selectedMuscle`.
- The muscle options are the distinct muscles of the available exercises of the selected body part, sorted by their
  label. The list filter adds `exercise.muscle === selectedMuscle` when one is selected.
- Muscles have no colour of their own: the theme has seven chip colours for 17 muscles, and a muscle belongs to one
  body part. A muscle label and its pill therefore use the body part's colour (`bodyPartColor`).
- `ExerciseLabel` gains `variant?: "filled" | "outlined"` (default `"outlined"`). The workout list and the catalog
  pass `filled` for the body part, and its colour plus `outlined` for the muscle. The configuration dialog's chips
  follow the same rule, because the colours apply everywhere on the screen.
- The catalog list uses the card variant of `ExerciseListItem` too (no handle and no position, so its first row is the
  name and "Elegir ejercicio"). Both lists share `EXERCISE_GRID_COLUMNS` (`xs` 1, `sm` 2, `md` 3 columns), exported
  by the molecule next to `EXERCISE_LIST_MAX_HEIGHT`. The catalog keeps the maximum height and its own scroll.
  The row layout of `ExerciseListItem` then has no user left, so it and the `variant` prop are removed: the molecule
  always renders a card.
- Single-muscle body parts: the view derives `singleMuscleBodyParts`, the body parts whose catalog exercises (the
  full catalog, not only the available ones) all work one muscle. It passes the set to `WorkoutExercisesList` and
  `ExerciseCatalogDialog`, which then leave the muscle out of those exercises' labels. The full catalog is used so a
  card's labels never change as exercises are added or removed. The muscle row is hidden when the options computed
  from the listed exercises number fewer than two, so it never offers a single pill. The configuration dialog keeps
  both labels.
- Copy: `workout.catalog.bodyParts` "Partes del cuerpo" / "Body parts", `workout.catalog.muscles` "Músculos" /
  "Muscles".

### Configuration dialog columns

The title is "Configuración del ejercicio" / "Exercise configuration" (`workout.configuration.title`, formerly
"Detalles del ejercicio" / "Exercise details"). `DialogContent` holds a grid of `{ xs: 1fr, md: 3fr 7fr }`. The left column holds the video, the name and the labels;
the right column holds the error, the effort, the sets and the set form. `ExerciseVideo` keeps `width: 100%` and
`16 / 9`, so the video now scales to the narrow column instead of the whole dialog, which keeps `maxWidth="md"`.

### Layout

`WorkoutView` sets `Container maxWidth={false}`. The two cards stack in one column (`display: "grid"`, `gap: 3`) on
every screen size, properties on top. Each card sits in a `section` labelled by its title id. The properties save
alerts stay above the grid.

### Exercise cards in rows

- `ExerciseListItem` gets `variant?: "row" | "card"` (default `"row"`). With `"card"`, the `<li>` drops its divider and
  padding and wraps its content in the `Card` atom (`variant="outlined"`, full height). The `<li>` stays the sortable
  node, so the `ul`/`li` structure and the `ref`/`style` forwarding are unchanged.
- `WorkoutExercisesList` passes `variant="card"`. Its `List` becomes a grid with
  `gridTemplateColumns` `{ xs: 1, sm: 2, md: 3 }` columns of `minmax(0, 1fr)` and `gap: 2`.
- The `SortableContext` strategy changes from `verticalListSortingStrategy` to `rectSortingStrategy`, the dnd-kit
  strategy for grids. `sortableKeyboardCoordinates` already moves in two dimensions, so the arrow keys move a card to
  its neighbour on the left, right, above or below.
- Inside a card, `ExerciseListItem` lays out three rows. The first holds the leading slot (the handle), the position,
  the name and the actions. The second holds the labels, and the lines follow below. The row variant used by the
  catalog keeps its current layout.
- The sets line is the compact summaries joined with ", ", with no "Series:" label. `useExerciseLabels` gains
  `compactSetSummary`, backed by `workout.exercises.compactSummary.*`:
  - es: `2 × 15`, `1 × 10–12`, `3 × 40 s`, `1 × máx`;
  - en: `2 × 15`, `1 × 10–12`, `3 × 40 s`, `1 × max`.

  The configuration dialog keeps the full `setSummary`. The effort line stays as it is.

- The exercises grid has no maximum height and no scroll of its own, so the card grows with its rows. The catalog list
  keeps `EXERCISE_LIST_MAX_HEIGHT`.
- _Alternative_: a horizontal-only list (`horizontalListSortingStrategy`) with wrapping. It was rejected because
  dnd-kit's list strategies assume a single line, and items jump wrongly once a row wraps.

### Copy (`backoffice/src/i18n/{es,en}/workouts.json`)

Added:

- `workout.properties.title`: "Propiedades del entrenamiento" / "Workout properties"
- `workout.exercises.add`: "Añadir ejercicio" / "Add exercise"
- `workout.catalog.choose`: "Elegir ejercicio" / "Choose exercise"
- `workout.catalog.close`: "Cerrar" / "Close"
- `workout.configuration.cancel`: "Cancelar" / "Cancel"

Removed from both languages: `workout.structure.title`, `workout.catalog.add`, `workout.exercises.pending`,
`workout.configuration.empty` and `workout.configuration.saved`.

Error codes involved, unchanged:

- `VALIDATION`, `FORBIDDEN`, `NOT_FOUND`, `CONFLICT`, `NETWORK` and `UNEXPECTED` use the existing `errors` copy. In
  the configuration dialog the message now shows inside the dialog.
- `UNAUTHORIZED` is handled by `runWithSession`.

There is no new code and no mapper change.

### Held state

- `LoadedWorkout.dialog` holds the open dialog and its item or mode. It is changed by "Añadir ejercicio", "Elegir
  ejercicio", the pencil, any dismiss and a successful save. It resets on screen exit.
- `LoadedWorkout.savedExercises` holds the only exercise list. It changes on a successful add, edit, remove or
  reorder, and on a failed reorder (order restored). It resets on screen exit.
- `LoadedWorkout` catalog filters (`filterText`, `selectedBodyPart`) are kept while the catalog dialog closes and
  reopens. They reset on screen exit.
- The configuration form (`effort`, `sets`, `draft`, errors) is local to the dialog's form. It resets on every
  opening, because the dialog is keyed and unmounts.
- `useSaveWorkoutExercise` holds `saving` and `errorCode`. Because it lives in the dialog, it resets with each opening.

### Tests

- **`workout-view.test.tsx`**, rewritten with one test per spec scenario and the use cases faked through the
  container.
  - New helpers `openCatalog(user)` and `chooseFromCatalog(user, name)`. A `configurationDialog()` helper returns
    `screen.getByRole("dialog", { name: "Configuración del ejercicio" })`. The existing `addSet` and `chooseEffort` helpers
    are scoped `within` that dialog.
  - The catalog filter tests open the dialog first.
  - Pending, selection-placeholder, "Ejercicio guardado." and selection-kept-on-reorder tests are deleted with their
    requirements.
  - The local `getBoundingClientRect` stub for reordering stays as documented in the reorder change. No shared helper
    changes.
- **`workout.spec.ts`**:
  - The add flow becomes: "Añadir ejercicio", then choose in the catalog dialog, then configure in the configuration
    dialog, then "Guardar". It asserts the `POST .../exercises` body, the dialog closing and the new list entry after
    a reload.
  - A new edit flow uses the pencil, changes the sets and asserts the `PUT .../exercises/{exerciseId}` body.
  - The region names change to "Propiedades del entrenamiento" and "Ejercicios".
  - `mockWorkoutsApi` is reused unchanged.

## Risks / Trade-offs

- [Focus after the catalog-to-configuration handoff] When the catalog dialog closes, MUI restores focus to the item
  button, which unmounts with it. When the configuration dialog closes, focus would then fall to `body`. → The view
  keeps a ref to "Añadir ejercicio" and focuses it when an add flow ends (saved or dismissed). The pencil flow relies
  on MUI's own restore to the pencil.
- [Adding now takes two dialogs] One more click than the old plus button. → This is accepted: it is what the user
  asked for, and it removes the pending state.
- [Losing the "Ejercicio guardado." confirmation] → The dialog closing and the list updating are the confirmation.
  Failures stay visible inside the open dialog.
- [Test churn] Most configuration tests change their queries from region to dialog. → Keep the scenario names
  matching the spec, so coverage can be checked one to one.

- [Keyboard reorder tests need a grid layout] jsdom has no layout. → The view test's local rect stub places rows of
  three, each item in its own column, and the keyboard helpers use ArrowRight. The E2E checks the real row layout
  with bounding boxes.

## Implementation Notes

Found while applying:

- The view mounts `ExerciseConfigurationDialog` only while `dialog.kind === "configure"` and passes `open` as
  `true`. This keeps a single dialog state, but the configuration dialog has no exit fade: it unmounts at once. The
  catalog dialog stays mounted and fades out as usual.
- The add flow returns focus to "Añadir ejercicio" from a `useEffect` in the view. It runs after MUI's own focus
  restore, which targets the catalog's "Elegir ejercicio" button, already unmounted by then.
- `mockWorkoutsApi` in `workout.spec.ts` was extended, not reused unchanged:
  - it answers `PUT .../exercises/{exerciseId}` and replaces the entry;
  - it records the add and update bodies next to the reorder bodies, and `signIn` now resolves to `{ additions, updates, reorders }`;
  - the reorder flows destructure `reorders`.
- Spec scenarios that chain two user actions became two tests each, so every test keeps exactly one Act:
  - "Add dismissed" and "Edit dismissed" are each split into a dismiss test and a reopen test;
  - "Save in progress" is split into "Cancelar disabled" and "Escape ignored".
- The test "shows the stored values again when a dismissed exercise is opened again" dismisses with Cancelar, not
  Escape. Removing the only set unmounts the focused "Quitar serie" button, so focus falls to `body` and a key press
  there never reaches the dialog. Escape is covered by "closes an edited exercise with Escape".

## Migration Plan

No data or API migration. The change ships as a front deploy, and rolling it back reverts it.
