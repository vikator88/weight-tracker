# Design

## Context

See `proposal.md` for the motivation and `specs/` for the behaviour. Current state that shapes the approach:

- `LoadedWorkout` in `backoffice/src/presentation/workouts/views/workout-view.tsx` holds `savedExercises` (seeded from
  `workout.exercises`) and `pending`. It renders `items = [...saved, ...pending]` through `WorkoutExercisesList`.
  Exercise saves and removals update `savedExercises` in place.
- `WorkoutExercisesList` (organism) renders `ExerciseListItem` (molecule) with a trailing `actions` slot. The same
  molecule is used by the catalog.
- `Dependencies` keys are declared once in `core/src/application/dependencies.ts`, so both containers must register
  every key.
- `409` already maps to `CONFLICT`, and `errors:CONFLICT` exists in `es` and `en`.

## Goals / Non-Goals

**Goals:** establish the drag-and-drop pattern in the backoffice; keep the optimistic order consistent with
concurrent exercise saves and removals.

**Non-Goals:** a reusable sortable abstraction for other screens; drag between columns; any app UI.

## Decisions

### Core

- **Contract first.** With the API running on the `add-reorder-workout-exercises` change (Debug API, port 5199), run
  `openapi:refresh` then `openapi:generate`, and commit that as a step of its own. Expected diff: the
  `PUT /workouts/{workoutId}/exercises/order` path and the `ReorderWorkoutExercisesRequest` schema only. A missing
  endpoint or any other diff stops the change.
- **Domain** (`core/src/domain/workouts/workout.ts`): `ReorderWorkoutExercisesRequest` aliases the generated schema.
  `ReorderWorkoutExercisesInput = { workoutId: string } & ReorderWorkoutExercisesRequest`, the same shape
  `UpdateWorkoutUseCase` uses for its id plus body.
- **Port**: `reorderExercises(workoutId: string, request: ReorderWorkoutExercisesRequest): Promise<Result<WorkoutDetail, DomainError>>`
  on `WorkoutRepositoryInterface`.
- **Repository**: `WorkoutRepository.reorderExercises` copies `update`: `PUT` with a JSON body,
  `ok(response)`, and `fail(mapHttpError(error))`.
- **Use case**: `ReorderWorkoutExercisesUseCase` in `use-cases/workouts/`, exported from the `index.ts`. It splits
  `workoutId` from the body and returns the repository `Result` unchanged, with no validation. It is modelled on
  `update-workout-use-case.ts` for the split and `remove-exercise-from-workout-use-case.ts` for size.
- **Key**: `reorderWorkoutExercisesUseCase: UseCaseInterface<ReorderWorkoutExercisesInput, WorkoutDetail>` in
  `dependencies.ts`. It is registered in `backoffice/src/infrastructure/container.ts` and
  `app/src/infrastructure/container.ts`, with placeholders in both `test/support/render-with-providers.tsx` and the
  key in both `test/infrastructure/container.test.ts`.

### Backoffice

- **Dependency**: from `backoffice/`, `pnpm add @dnd-kit/core @dnd-kit/sortable @dnd-kit/utilities`. These are
  client-only and run under the existing `"use client"` components, so the static export is unaffected.
- **State hook** `useReorderWorkoutExercises()` in `presentation/workouts/hooks/`, modelled on
  `use-remove-workout-exercise.ts`. It returns `{ reordering: boolean, errorCode, reorder(workoutId, exerciseIds) }`,
  and `reorder` resolves to `true` on success. `errorCode` is cleared when a reorder starts. The call goes through
  `runWithSession`.
- **View** (`LoadedWorkout`) owns the order. `handleReorder(activeId, overId)`:
  1. Returns early when `activeId === overId`, or when either id is not a saved entry.
  2. Keeps `previousIds` (the saved entry ids, in order), then sets `savedExercises` to
     `arrayMove(current, from, to)`.
  3. Calls `reorder(workout.id, next.map(e => e.exerciseId))`.
  4. On failure it restores the order **by id**: it sorts the current `savedExercises` by their index in
     `previousIds`, and anything not found goes last. A save or removal that landed during the request is then kept.
     It does not reinstate a stale snapshot.
  5. On success it does nothing more. The returned detail is not merged, for the same reason.
- **Selection** is keyed by entry id, so a move does not affect it.
- **Organism** `WorkoutExercisesList` gains `onReorder(activeId, overId)`, `reordering` and `reorderErrorCode`.
  It hosts `DndContext` with a stable `id` (from `useId`) to avoid hydration id mismatches in the static export, plus
  `closestCenter`, `PointerSensor` and `KeyboardSensor` with `sortableKeyboardCoordinates`.
  - Inside it, `SortableContext` holds the saved entry ids only, with `verticalListSortingStrategy`.
  - `accessibility.announcements` and `screenReaderInstructions` come from translated copy.
  - `onDragEnd` calls `onReorder` only when `over` exists and differs from `active`.
  - The reorder error shows as an `Alert` next to the existing removal error.
- **Molecule** `SortableExerciseListItem` (new, `components/molecules/sortable-exercise-list-item.tsx`). It calls
  `useSortable({ id, disabled })` and applies `CSS.Transform.toString(transform)` and `transition` to a wrapper. It
  renders `ExerciseListItem`, passing the handle into a new optional `leading` slot.
  - The handle is the `IconButton` atom with `DragIndicator`, labelled "Reordenar ejercicio", with `setActivatorNodeRef`,
    `listeners` and `attributes` spread on it and `disabled` when not sortable.
  - Pending items render through the same molecule with `disabled: true`. They are outside the `SortableContext`
    items, so they never become drop targets.
- **Molecule** `ExerciseListItem` gets an optional `leading?: ReactNode`, rendered before the position number. The
  catalog passes nothing, so it renders as it does today.
- **Atomic levels and imports**: view → organism → molecules → atoms. MUI widgets are imported only by atoms. dnd-kit
  hooks live in the organism and molecule, because they are behaviour, not UI widgets. `DragIndicator` comes from
  `@mui/icons-material` like the organism's existing icons.

### Copy (`backoffice/src/i18n/{es,en}/workouts.json`)

- `workout.exercises.reorder`: "Reordenar ejercicio" / "Reorder exercise"
- `workout.exercises.dnd.instructions`: "Pulsa espacio o intro para coger un ejercicio, las flechas para moverlo, espacio o intro para soltarlo y escape para cancelar." / "Press space or enter to pick up an exercise, the arrow keys to move it, space or enter to drop it, and escape to cancel."
- `workout.exercises.dnd.picked`: "Has cogido {{name}}." / "Picked up {{name}}."
- `workout.exercises.dnd.over`: "{{name}} está en la posición {{position}}." / "{{name}} is at position {{position}}."
- `workout.exercises.dnd.dropped`: "{{name}} se ha soltado en la posición {{position}}." / "{{name}} was dropped at position {{position}}."
- `workout.exercises.dnd.cancelled`: "Se ha cancelado el movimiento de {{name}}." / "Moving {{name}} was cancelled."

Error codes involved: `VALIDATION`, `FORBIDDEN`, `NOT_FOUND`, `CONFLICT`, `NETWORK`, `UNEXPECTED` (all shown via
existing `errors` copy), and `UNAUTHORIZED` (handled by `runWithSession`). No new code and no mapper change.

### Held state

- `LoadedWorkout.savedExercises`: the saved exercises in on-screen order. It changes on drop (optimistic), on a failed
  reorder (order restored by id), and on exercise save and remove. It resets on screen exit.
- `useReorderWorkoutExercises`: `reordering` and `errorCode`. It is changed by the view on drop, and `errorCode` is
  cleared on the next reorder. It resets on screen exit.
- dnd-kit drag state, internal to `DndContext`. It resets on drop or cancel.

### Tests

- **Core**: `reorder-workout-exercises-use-case.test.ts` (forwards the id and body, returns success and failure
  unchanged, calls the repository once). `workout-repository.test.ts` gains `reorderExercises`: method, path and
  body; success; `400`, `401`, `403`, `404`, `409` and `500`; and a rejected request.
- **Backoffice component** (`workout-view.test.tsx`, use case faked via the container): one test per spec scenario,
  reordering with the keyboard sensor. jsdom has no layout, so the test file stubs
  `HTMLElement.prototype.getBoundingClientRect` for the list items so each row gets its own vertical position. The
  stub lives in that file with `beforeEach`/`afterEach` restore, and is not added to shared test helpers (documented
  exception). The in-progress case uses a use case fake whose promise the test resolves.
- **E2E** (`workout.spec.ts`): `page.route` answers the workout and catalog reads and records the
  `PUT .../exercises/order` body. The test reorders two exercises with the keyboard and asserts the body and the new
  numbering. A second case answers `409` and asserts the order is restored and the message is shown.

## Risks / Trade-offs

- [jsdom keyboard sensor needs element rects] → stub rects locally in the view test, and the real drag is covered by
  the Playwright E2E. If the stub proves flaky, the component tests drive the drag through dnd-kit's keyboard events
  only, and the E2E remains the source of truth for movement.
- [Hydration id mismatch from `DndContext` in the static export] → stable `id` from `useId`.
- [Not merging the API response] → the screen could hide a server-side order change made elsewhere. That is
  acceptable, because the next reorder with a stale set answers `409`, which restores the order and shows the message.
  A reload shows the true order.
- [`ExerciseListItem` is shared with the catalog] → the `leading` slot is optional, and the catalog render is
  unchanged. The existing catalog tests guard it.

## Implementation Notes

Found while applying:

- `ExerciseListItem` also forwards optional `ref` and `style` to its `ListItem` atom, so the `<li>` itself is the
  sortable node. A wrapper element would break the `<ul>`/`<li>` structure.
- Added the copy key `workout.exercises.dnd.roleDescription` ("ejercicio reordenable" / "reorderable exercise").
  Without it, assistive technology reads dnd-kit's English default "sortable".
- The generated `api.ts` was formatted with Prettier after `openapi:generate`, matching how the committed file was
  formatted. The snapshot commit shows only the new path and schema.
- Every per-file `FakeWorkoutRepository` in the core use-case tests gained a `reorderExercises` stub, as required by
  the extended interface.
- E2E: `mockWorkoutsApi` takes a catalog override and a reorder status, so the existing single-exercise flows keep
  working unchanged. Reorder flows open in a tall viewport, for two reasons: dnd-kit's keyboard sensor first scrolls
  the page when the next row sits below the viewport's middle, and it cancels a drag on resize. The helper waits for
  dnd-kit's announcements between keys, and it retries the arrow key, because Chromium under parallel load
  occasionally drops the first one. The flows passed 40 out of 40 repeated runs.

## Migration Plan

No migration. The front must not ship before the API change `add-reorder-workout-exercises` is deployed. Rollback
reverts this change. The snapshot commit can stay.
