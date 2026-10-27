# Tasks

## 1. Core: create workout form rules

- [x] 1.1 Add `core/src/application/validation/workout-rules.ts` (`WORKOUT_NAME_MAX_LENGTH`, `WORKOUT_DESCRIPTION_MAX_LENGTH`, `WorkoutFieldViolation`, `CreateWorkoutFormValues`, `validateCreateWorkoutForm(values, { withTrainer })`) and re-export it from the validation barrel; verify with `core/test/application/validation/workout-rules.test.ts` mirroring `profile-name-rules.test.ts`: each violation, 256/257 and 1024/1025 boundaries, trimmed name, `2026-02-30` rejected, UUID case-insensitive, `trainerId` ignored without `withTrainer`
- [x] 1.2 Review core against `profile-name-rules.ts` and its test; verify no use case, port or repository changed and `pnpm --filter @weight-tracker/core test` passes

## 2. Backoffice: copy and atoms

- [x] 2.1 Add every key of the design's copy table to `backoffice/src/i18n/es/workouts.json` and `en/workouts.json`; verify `translation-parity.test.ts` passes
- [x] 2.2 Add the atoms `dialog`, `dialog-title`, `dialog-content`, `dialog-actions`, `card`, `card-content`, `chip`, `list`, `list-item` in `backoffice/src/presentation/common/components/atoms/`, each a pass-through like `button.tsx`, and make `icon-button.tsx` generic over its `component` so it can render a link; verify `pnpm --filter backoffice typecheck` passes and existing tests still pass

## 3. Backoffice: create workout dialog and assigned plans

- [x] 3.1 Add `useCreateWorkout` in `backoffice/src/presentation/workouts/hooks/use-create-workout.ts` modelled on `use-update-profile.ts`; verify through the dialog test in 3.3
- [x] 3.2 Add the `CreateWorkoutDialog` organism (fields, admin-only Trainer ID, structural validation through `validateCreateWorkoutForm`, request built without blank optional keys, `Alert` on failure, navigation to `/workout?id=` on success, reset on open); verify through the dialog test in 3.3
- [x] 3.3 Add `backoffice/test/presentation/workouts/components/organisms/create-workout-dialog.test.tsx` covering every scenario of the `backoffice-workout-creation` spec for both roles (fields, trainer field per role, each validation message, request payloads, disabled while creating, navigation, failures keep values, Cancel, English); verify it passes
- [x] 3.4 Wire "Nuevo entrenamiento" in `AssignedPlansView` to open the dialog, and add the edit column to `WorkoutsTable` (`IconButton` as `NextLink` to `/workout?id=`, labelled "Editar <name>"); verify with updated cases in `assigned-plans-view.test.tsx` (dialog opens and no request; edit link `href`; English label) and that the existing cases still pass

## 4. Backoffice: workout screen data and molecules

- [x] 4.1 Add `useWorkout(id)` and `useExercises()` in `backoffice/src/presentation/workouts/hooks/` modelled on `use-workouts.ts` (`null` id → `failed` with `NOT_FOUND` and no request), and `useExerciseLabels()` (active-language name with fallbacks, muscle/body-part/effort/target-type labels with raw fallback); verify through the view test in 5.3
- [x] 4.2 Add the molecules `ExerciseListItem`, `BodyPartFilter` (wrapping chips, `aria-pressed`) and `ExerciseVideo` (YouTube embed via `youtube-nocookie.com`, link otherwise) in `backoffice/src/presentation/workouts/components/molecules/`; verify through the view test in 5.3

## 5. Backoffice: workout screen

- [x] 5.1 Add the organisms `ExerciseCatalogPanel` (text + muscle filters, accent- and case-insensitive, inert add), `WorkoutDetailsCard` (seeded fields, inert Guardar), `WorkoutExercisesList` (inert trash, pencil selects) and `ExerciseConfigurationPanel` (details, video, effort select, set fields, re-seeded by `key`); verify through the view test in 5.3
- [x] 5.2 Add `WorkoutView` (AppShell + `Suspense` around `WorkoutScreen` reading `useSearchParams`; three-column grid; loading, not-found and per-read failure states) in `backoffice/src/presentation/workouts/views/workout-view.tsx` and the route `backoffice/src/app/workout/page.tsx`; verify `pnpm --filter backoffice build` produces `/workout` in the static export
- [x] 5.3 Add `backoffice/test/presentation/workouts/views/workout-view.test.tsx` covering every scenario of the `backoffice-workout-editor` spec (reads once, columns, catalog and pills, combined filters and pill toggle, labels and active-language names, missing catalog match, inert add / Guardar / trash send nothing, pencil selection, configuration empty and filled, edits discarded on reselection, the three video cases, missing id, 404, failure and retry, English); verify it passes

## 6. Backoffice: end to end and review

- [x] 6.1 Add `backoffice/e2e/workout.spec.ts` modelled on `assigned-plans.spec.ts`: create from the dialog and land on `/workout?id=…` with the name in the card; open a workout from the table edit action; with `GET`/`POST /workouts`, `GET /workouts/{id}` and `GET /exercises` intercepted; verify `pnpm test:e2e` passes
- [x] 6.2 Review the backoffice against `assigned-plans-view.tsx`, `use-workouts.ts`, `use-update-profile.ts`, `profile-form.tsx` and the existing atoms and tests; verify atomic levels and folders, downward imports, only atoms importing MUI widgets, `src/app` routes only, no `@weight-tracker/core/infrastructure` import in presentation, and no request sent by Guardar, plus or trash

## 7. Integration

- [x] 7.1 Run the `.stpr/template/` drift check from `AGENTS.md`; verify it prints no `DRIFT` line
- [x] 7.2 Run `pnpm typecheck && pnpm lint && pnpm test && pnpm test:e2e` from `front/`; verify everything passes

## 8. Visual pass

- [x] 8.1 Add the copy keys of the design's visual pass to `workouts.json` (es / en); verify the parity test passes
- [x] 8.2 Add `bodyPartColor` to `useExerciseLabels`, and `position` and coloured labels to `ExerciseListItem`; verify through the view test in 8.4
- [x] 8.3 Turn the three columns into titled cards, put the exercises list (with its header and inert "Añadir ejercicio") inside the middle card, give both lists a maximum height with internal scroll, move Guardar to the new page header with "Volver" and the workout name, and show the video first in the configuration card; verify `pnpm --filter backoffice build` still exports `/workout`
- [x] 8.4 Update `workout-view.test.tsx` and `e2e/workout.spec.ts` for the new region names, header, numbering, inert add in the middle card, scroll containers, chip colours and video order; verify `pnpm --filter backoffice test` and `pnpm test:e2e` pass

## 9. Save the workout

- [x] 9.1 Add `validateWorkoutDetailsForm` to `core/src/application/validation/workout-rules.ts` and reuse it from `validateCreateWorkoutForm`; verify with new cases in `workout-rules.test.ts` and that the existing ones still pass
- [x] 9.2 Add the `workout.details.saved` copy (es / en) and `useUpdateWorkout` modelled on `use-create-workout.ts`; verify the parity test passes
- [x] 9.3 Make `WorkoutDetailsCard` controlled and move the form state, validation, save and outcome into a `LoadedWorkout` component of `workout-view.tsx`, with Guardar in the header disabled while saving; verify `pnpm --filter backoffice build` still exports `/workout`
- [x] 9.4 Update `workout-view.test.tsx` for every new scenario (field messages and no request, request payload with athlete and trainer as read, blank description omitted, disabled while saving, confirmation and new heading, failure keeps values and heading) and remove the inert Guardar case; extend `e2e/workout.spec.ts` to edit and save a workout with `PUT /workouts/{id}` intercepted; verify `pnpm typecheck && pnpm lint && pnpm test && pnpm test:e2e` passes from `front/`

## 10. Add exercises from the catalog

- [x] 10.1 Remove the middle card's "Añadir ejercicio" button and its copy; add `workout.exercises.pending` (es / en); verify the parity test passes
- [x] 10.2 Hold pending exercises in `LoadedWorkout`, append and select one on the catalog plus, show it numbered after the saved ones with a "Pendiente" note, seed it with one empty set, and hide exercises already in the workout from the catalog; verify `pnpm --filter backoffice build` still exports `/workout`
- [x] 10.3 Update `workout-view.test.tsx` (plus appends, selects and opens with one empty set and no request; catalog hides saved and added exercises; no add button in the workout card) and remove the old inert-plus and middle-card add cases; verify `pnpm typecheck && pnpm lint && pnpm test && pnpm test:e2e` passes from `front/`

## 11. Sets and saving an exercise

- [x] 11.1 Add `updateExercise` to `WorkoutRepositoryInterface` and `WorkoutRepository` (`PUT /workouts/{workoutId}/exercises/{exerciseId}`), `UpdateWorkoutExerciseInput`, `UpdateWorkoutExerciseUseCase` and the `updateWorkoutExerciseUseCase` key; add `updateExercise` to every fake repository; verify with new cases in `workout-repository.test.ts` and `update-workout-exercise-use-case.test.ts`
- [x] 11.2 Add `validateSetForm` and `validateExerciseConfiguration` to `workout-rules.ts`; verify with new cases in `workout-rules.test.ts`
- [x] 11.3 Register the use case in both `container.ts`, add its placeholders in both test helpers and its key in both container tests; verify `pnpm --filter backoffice test` and `pnpm --filter app test` pass
- [x] 11.4 Add the copy keys of the design's sets section (es / en) and `useSaveWorkoutExercise`; verify the parity test passes
- [x] 11.5 Rebuild `ExerciseConfigurationPanel` (effort, sets list with "Quitar serie", set form, "Añadir serie", "Guardar", errors and outcome) and move saved/pending bookkeeping into `LoadedWorkout`; verify `pnpm --filter backoffice build` still exports `/workout`
- [x] 11.6 Update `workout-view.test.tsx` for the list summaries, removal, form validation and adding, save validation, the add and update payloads, pending becoming saved with the confirmation, and a rejected save; extend `e2e/workout.spec.ts` to add an exercise with two sets and save it; verify `pnpm typecheck && pnpm lint && pnpm test && pnpm test:e2e` passes from `front/`

## 12. Title, layout and removing exercises

- [x] 12.1 Add `removeExercise` to `WorkoutRepositoryInterface` and `WorkoutRepository` (`DELETE /workouts/{workoutId}/exercises/{exerciseId}`), `RemoveExerciseFromWorkoutInput`, `RemoveExerciseFromWorkoutUseCase` and the `removeExerciseFromWorkoutUseCase` key; add `removeExercise` to every fake repository; verify with new cases in `workout-repository.test.ts` and `remove-exercise-from-workout-use-case.test.ts`
- [x] 12.2 Register the use case in both `container.ts`, add its placeholders in both test helpers and its key in both container tests; verify `pnpm --filter backoffice test` and `pnpm --filter app test` pass
- [x] 12.3 Add `workout.editTitle` (es / en), set the heading to it, move "Guardar" into the workout card between the fields and the list, and change the columns to 25 / 40 / 35; verify `pnpm --filter backoffice build` still exports `/workout`
- [x] 12.4 Add `useRemoveWorkoutExercise`, wire "Quitar ejercicio" (saved through the API, pending locally, selection cleared, error shown) so a removed exercise returns to the catalog; verify through the view test in 12.5
- [x] 12.5 Update `workout-view.test.tsx` (fixed heading, Guardar inside the card and its position, the removal cases of the spec) and `e2e/workout.spec.ts` (fixed heading; remove a saved exercise and see it in the catalog again); verify `pnpm typecheck && pnpm lint && pnpm test && pnpm test:e2e` passes from `front/`

## 13. Sets and effort in the workout's list

- [x] 13.1 Move the set summary into `useExerciseLabels.setSummary`, add `lines` to `ExerciseListItem`, the `setsLine` / `effortLine` copy (es / en), and show both lines in `WorkoutExercisesList`; verify `pnpm --filter backoffice build` still exports `/workout`
- [x] 13.2 Add view tests for the two lines (several sets comma separated, effort label, neither line for an undefined pending exercise, lines updated after an exercise save); verify `pnpm typecheck && pnpm lint && pnpm test && pnpm test:e2e` passes from `front/`

## Workflow follow-up

- Archive the change after review with `/opsx:archive`.
