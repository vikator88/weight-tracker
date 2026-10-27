# Change Proposal: Add Backoffice Create Workout Dialog And Workout Screen

Source brief for `/opsx:propose`. Artifacts go to `openspec/changes/add-backoffice-workout-screen/`.

## Change

Wire the "Nuevo entrenamiento" button of the assigned plans screen to a dialog that creates a workout (name, description,
date, athlete id, and trainer id for admins) and then opens the new workout's screen. Add an edit action to each row of the workouts table that
opens the same screen. The workout screen is a first, read-only layout of three columns: the exercise catalog with
filters, the workout's details and exercises, and the configuration of the exercise selected in the workout.

## Objective

Assigned plans can be listed but not created or opened. This change gives trainers and admins the first step of the
workout editor: create a workout and see it next to the catalog. It deliberately wires **one** mutation (creating a
workout); every other action on the screen is laid out but inert, so the layout can be reviewed before the editing
behaviour is planned.

## Capabilities

- "Nuevo entrenamiento" opens a create workout dialog: Name (required), Description (optional), Date (required),
  Athlete id (required) and, for `ADMIN` only, Trainer id (required). Submitting creates the workout and navigates to
  its screen; Cancel closes the dialog.
- Each workouts table row has an edit icon button that navigates to that workout's screen.
- Workout screen, three columns (25% / 50% / 25% on wide screens, stacked on narrow ones):
  - **Catalog column (25%)**: a text filter, then every muscle in the catalog as a pill list that wraps onto new lines
    (no horizontal scroll), then the catalog exercises. Each item shows the name, and below it labels for body part and
    muscle, with a plus icon button on the right (inert).
  - **Workout column (50%)**: a card with the workout's name, date and description in editable fields and a "Guardar"
    button (inert); below it the workout's exercises, laid out like the catalog items, each with a trash icon button
    (inert) and a pencil icon button.
  - **Exercise configuration column (25%)**: configures the workout exercise chosen with the pencil in the workout
    column. It shows the exercise's details (name, body part, muscle, and its video when the catalog provides one),
    then the effort and sets of that exercise in the workout, in fields that are not saved (no save action yet). With
    nothing selected it shows "Selecciona un ejercicio para configurarlo".
- Catalog filtering is local: the text matches the exercise name in the active language; selecting a muscle pill
  narrows to that muscle; both combine. Selecting the active pill again clears it.
- Choosing an exercise with the pencil is local UI state, not a mutation.
- Loading, not found and error states for the workout and for the catalog.

## Fronts And Roles

- backoffice -> lands here: yes -> roles: `ADMIN`, `TRAINER` -> both open the dialog and the workout screen; see the
  admin note below.
- app -> lands here: no -> roles: `USER` -> nothing changes. The use cases it needs already exist in `core`.

Role differences come from the API, not from front checks: a `TRAINER` must send `userId` and must not send `trainerId`;
an `ADMIN` must send both. So the dialog shows the Trainer id field to `ADMIN` only; a `TRAINER` must NOT see it, and
the request from a trainer never carries `trainerId`. The role is read from `useCurrentUser().profile.role`; which
fields the API requires stays the API's rule, the dialog only mirrors it in what it asks.

Apart from that field, both roles see the same screens.

## Classification

Core:

- `validateCreateWorkoutRequest` and its limits (structural only) -> form validation rules ->
  `core/src/application/validation/workout-rules.ts`, next to `profile-name-rules.ts`
- No new use case, port, repository or alias: `CreateWorkoutUseCase`, `GetWorkoutByIdUseCase` and `GetExercisesUseCase`
  are already registered in both containers.

Backoffice:

- `/workout` route (`?id=<workoutId>`) -> route -> `backoffice/src/app/workout/page.tsx`
- `WorkoutView` -> view -> `backoffice/src/presentation/workouts/views/workout-view.tsx`
- `useWorkout(id)` (detail + status + reload) -> state hook -> `backoffice/src/presentation/workouts/hooks/use-workout.ts`
- `useExercises()` (catalog + status + reload) -> state hook -> `backoffice/src/presentation/workouts/hooks/use-exercises.ts`
- `useCreateWorkout()` (saving, errorCode, create) -> state hook -> `backoffice/src/presentation/workouts/hooks/use-create-workout.ts`
- `CreateWorkoutDialog` -> organism -> `backoffice/src/presentation/workouts/components/organisms/create-workout-dialog.tsx`
- `ExerciseCatalogPanel` (filter, pills, list) -> organism -> `.../workouts/components/organisms/exercise-catalog-panel.tsx`
- `WorkoutDetailsCard` -> organism -> `.../workouts/components/organisms/workout-details-card.tsx`
- `WorkoutExercisesList` -> organism -> `.../workouts/components/organisms/workout-exercises-list.tsx`
- `ExerciseConfigurationPanel` (details, video, effort and sets fields) -> organism ->
  `.../workouts/components/organisms/exercise-configuration-panel.tsx`
- `ExerciseVideo` (embedded YouTube player, or a link for any other URL) -> molecule ->
  `.../workouts/components/molecules/exercise-video.tsx`
- `ExerciseListItem` (name, body-part and muscle labels, trailing action slot) -> molecule -> `.../workouts/components/molecules/exercise-list-item.tsx`,
  shared by the catalog and the workout exercises
- `MuscleFilter` (wrapping pill list, selected pill) -> molecule -> `.../workouts/components/molecules/muscle-filter.tsx`
- New atoms as needed (`dialog`, `dialog-title`, `dialog-content`, `dialog-actions`, `card`, `card-content`, `chip`,
  `list`, `list-item`) -> atoms -> `backoffice/src/presentation/common/components/atoms/`
- Reused atoms: `button`, `icon-button`, `text-field`, `typography`, `skeleton`; reused molecules: `LoadError`,
  `HomeSkeleton`; reused organisms: `AppShell`, `WorkoutsTable` (gains the edit column)
- Copy -> existing `workouts` namespace in `backoffice/src/i18n/{es,en}/workouts.json`, plus muscle and body-part labels

## Canonical Template Mapping

- Workout form rules -> `core/src/application/validation/profile-name-rules.ts` -> same shape: limits as constants, a
  violation per field, trimmed before measuring.
- Route -> `backoffice/src/app/assigned-plans/page.tsx` -> default-exports the view.
- `WorkoutView` -> `backoffice/src/presentation/workouts/views/assigned-plans-view.tsx` -> `AppShell`, loading /
  `LoadError` states; adds a `Suspense` boundary around the part that reads `useSearchParams`.
- `useWorkout`, `useExercises` -> `backoffice/src/presentation/workouts/hooks/use-workouts.ts` -> same status /
  errorCode / reload shape, `runWithSession`, generation guard.
- `useCreateWorkout` -> `backoffice/src/presentation/profile/hooks/use-update-profile.ts` -> saving / errorCode / async
  action resolving to the result.
- `CreateWorkoutDialog` -> `backoffice/src/presentation/onboarding/components/organisms/onboarding-dialog.tsx` and
  `profile/components/organisms/profile-form.tsx` -> MUI dialog organism with a structurally validated form.
- `WorkoutsTable` edit column -> extended in place.
- Atoms -> `common/components/atoms/button.tsx` and the table atoms -> pass-through themed MUI widgets. Atom, molecule and
  organism references are `pending` (atomic design) in `.stpr/template/README.md`; follow the shapes from #30 and the
  assigned plans change. `onboarding-dialog.tsx` imports `@mui/material/Dialog` directly; the new dialog uses atoms.
- View tests -> `backoffice/test/presentation/workouts/views/assigned-plans-view.test.tsx`; dialog organism test ->
  `backoffice/test/presentation/onboarding/components/organisms/onboarding-dialog.test.tsx`.
- E2E -> `backoffice/e2e/assigned-plans.spec.ts`.

## Screens And Navigation

- backoffice -> `/assigned-plans` -> `AssignedPlansView` -> "Nuevo entrenamiento" opens `CreateWorkoutDialog` -> on
  success: `/workout?id=<new id>`; on failure: the dialog stays open with the error and the typed values; Cancel or
  Escape: closes, nothing sent.
- backoffice -> `/assigned-plans` -> row edit icon (label "Editar <name>") -> `/workout?id=<id>`.
- backoffice -> `/workout?id=<id>` -> `WorkoutView` -> reached only from the two above (or a reload) -> missing `id` or
  `NOT_FOUND`: a not-found message with a link back to `/assigned-plans`; other failures: `LoadError` with retry.

The id travels as a query parameter because the backoffice is a static export: a `/workouts/[id]` segment would need
`generateStaticParams`, which cannot know workout ids at build time.

## API Contract

- `POST /workouts` -> in `core/openapi/v1.json`: yes -> `CreateWorkoutRequest` (`name`, `workoutDate` required;
  `description`, `userId`, `trainerId`), response `WorkoutResponse` (201); failures `400`, `401`, `403`, `404`.
- `GET /workouts/{id}` -> yes -> `WorkoutDetailResponse` (`exercises` are `WorkoutExerciseDetailResponse`: `exerciseId`,
  `effort`, `sets`, no name or muscle); failures `401`, `403`, `404`.
- `GET /exercises` -> yes, public -> `ExerciseResponse[]` (`muscle`, `bodyPart`, `translations[{ languageCode, name }]`).
- `PUT /workouts/{id}`, `POST`/`PUT`/`DELETE /workouts/{workoutId}/exercises...` -> yes -> **not used** in this change.

No snapshot refresh. A workout exercise's name, muscle and body part come from joining `exerciseId` with the loaded
catalog in presentation; that join is display composition, not a business rule.

## Held State

- `useWorkout(id)` -> the workout detail, status, errorCode -> itself on mount, on `id` change and on reload -> screen exit.
- `useExercises()` -> the catalog, status, errorCode -> itself on mount and reload -> screen exit. Not cached across screens.
- `WorkoutView` local state -> filter text, selected muscle, selected workout exercise id -> the user -> screen exit.
- `ExerciseConfigurationPanel` local state -> the edited effort and sets of the selected exercise -> the user -> reset
  when another exercise is selected, discarded on screen exit (nothing is saved yet).
- `WorkoutDetailsCard` local state -> the edited name, date and description -> the user -> screen exit, discarded
  (Guardar does nothing yet).
- `useCreateWorkout` -> saving, errorCode -> the dialog -> dialog close.

## Forms And Structural Validation

- Create workout -> Name -> required, at most 256 characters after trimming (sent trimmed)
- Create workout -> Description -> optional, at most 1024 characters; empty is sent as omitted
- Create workout -> Date -> required, a valid `YYYY-MM-DD` date (date input)
- Create workout -> Athlete id -> required, UUID format
- Create workout -> Trainer id (`ADMIN` only) -> required, UUID format; not rendered nor sent for `TRAINER`
- Workout details card -> same Name / Description / Date rules shown, but nothing is submitted
- Exercise configuration panel -> effort is a select over the API's `Effort` values; sets show count and target as the
  API returns them; no validation (nothing is submitted)

Limits mirror the API's declared limits as recorded in the `add-workout-usecases` change; check them against
`api/` while implementing. They live once in `core` validation, not in the component.

## Errors And Copy

- Invalid data / missing athlete for the role -> `VALIDATION` -> error in the dialog, existing `errors` copy
- Not allowed -> `FORBIDDEN` -> error in the dialog / `LoadError`
- Unknown athlete or workout -> `NOT_FOUND` -> error in the dialog; on the workout screen, the not-found state
- Session -> `UNAUTHORIZED` -> `runWithSession`
- No response / other -> `NETWORK` / `UNEXPECTED` -> existing copy
- No new error code; the mapper is not touched.

New copy (`workouts` namespace, es / en), to be proposed in full in the design:

- `createWorkout.title` -> "Nuevo entrenamiento" -> "New workout"
- `createWorkout.fields.name|description|date|userId|trainerId` -> "Nombre", "Descripción", "Fecha", "ID del atleta",
  "ID del entrenador" -> "Name", "Description", "Date", "Athlete ID", "Trainer ID"
- `createWorkout.actions.cancel|create` -> "Cancelar", "Crear" -> "Cancel", "Create"
- `createWorkout.validation.required|maxLength|uuid|date` -> to be proposed
- `assignedPlans.edit` -> "Editar {{name}}" -> "Edit {{name}}"
- `workout.catalog.filter` -> "Buscar ejercicio" -> "Search exercise"; `workout.catalog.empty` -> to be proposed
- `workout.details.save` -> "Guardar" -> "Save"; `workout.exercises.empty` -> "Este entrenamiento aún no tiene
  ejercicios." -> "This workout has no exercises yet."
- `workout.configuration.empty` -> "Selecciona un ejercicio para configurarlo." -> "Select an exercise to configure it."
- `workout.configuration.bodyPart`, `.muscle`, `.video`, `.effort`, `.sets` -> "Parte del cuerpo", "Músculo", "Vídeo",
  "Esfuerzo", "Series" -> "Body part", "Muscle", "Video", "Effort", "Sets"
- `workout.configuration.openVideo` -> "Ver vídeo" -> "Watch video" (link for a non-YouTube URL)
- `workout.notFound`, `workout.backToList` -> to be proposed
- Icon button labels: "Añadir ejercicio", "Quitar ejercicio", "Editar ejercicio" -> "Add exercise", "Remove exercise",
  "Edit exercise"
- `muscles.<CODE>` for the 17 API muscles (`ABDOMINALS` … `TRICEPS`), `bodyParts.<CODE>` for the 7 body parts (`ARMS`,
  `LEGS`, `CHEST`, `BACK`, `SHOULDERS`, `CORE`, `NECK`), `efforts.<CODE>` for `LOW` … `HIGH`; an unknown code is shown raw.

## Platform Capabilities And Dependencies

None.

## Out Of Scope

- Updating the workout (Guardar), adding (plus), removing (trash) or editing (effort and sets) workout exercises:
  laid out only, no request is sent.
- Picking the athlete from a list or showing athlete names; the athlete is typed as an id.
- Superseries, workout status changes, deleting workouts.
- Any app screen.
- Drawer changes: the drawer does not mark an item as selected on `/workout`.
- Must not change: the API, `core` use cases and repositories, the assigned plans list behaviour other than the new
  edit column and the wired button.

## Notes

- **Admins and `trainerId`.** Decided: the dialog adds a required "ID del entrenador" field for `ADMIN` only, because the
  API requires it from admins and rejects it from trainers.
- **Third column.** Decided: it configures the exercise selected in the workout column, not a catalog exercise. Details
  (name, body part, muscle, video) come from the catalog entry matching its `exerciseId`; effort and sets come from the
  workout exercise. Catalog items cannot be selected in this change.
- **Video.** `videoUrl` values are YouTube watch URLs (`https://www.youtube.com/watch?v=<id>`). Those are embedded as a
  player (`https://www.youtube-nocookie.com/embed/<id>` in an `iframe` with a title, 16:9). Any other non-empty URL is
  shown as an external link; `null` shows no video section. Parsing the URL is display logic and stays in the molecule.
- **Inert actions.** Plus, trash and Guardar render enabled and do nothing, like the drawer placeholders. The pencil is
  the only exercise action that does something, and only locally (it selects the exercise).
- **Exercise name** is the translation for the active language, falling back to the first translation.
- **Muscle pills** are the distinct `muscle` values of the loaded catalog, sorted by their translated label; pills
  wrap (`flex-wrap`), never scroll horizontally.
- **Exercise without catalog match** (an id the catalog does not return): shown with its id as name and no labels.
- **Route.** `/workout?id=` is a proposal; rename at proposal time if preferred. Read with `useSearchParams` inside a
  `Suspense` boundary, per the Next 16 docs in `backoffice/node_modules/next/dist/docs/`.
- **No stop case**: every endpoint exists, and the only validation is structural.
- **Verification tiers:**
  - core unit: `workout-rules.test.ts` for every rule and boundary.
  - backoffice component tests: `create-workout-dialog` (validation messages, trimmed payload, omitted empty description,
    failure keeps the dialog, success navigates to `/workout?id=`, Cancel sends nothing; `ADMIN` sees and must fill
    Trainer id and it is sent; `TRAINER` does not see it and the request has no `trainerId`);
    `assigned-plans-view` gains the edit link and the wired button; `workout-view` (three columns rendered, catalog
    filter by text and by pill combined, pill toggling, joined names and labels, pencil selects, inert plus / trash /
    Guardar send nothing, not found, failure and retry, missing `id`; configuration panel: empty message, details of the
    selected exercise, embedded video for a YouTube URL, link for another URL, no video section for `null`, effort and
    sets of the selected exercise, values reset when another exercise is selected).
  - backoffice E2E `e2e/workout.spec.ts`: create from the dialog and land on the workout screen; open a workout from the
    table edit action. `POST /workouts`, `GET /workouts`, `GET /workouts/{id}` and `GET /exercises` intercepted.
  - translation parity with the new keys.
- Done means `pnpm typecheck && pnpm lint && pnpm test && pnpm test:e2e` passes from `front/`.
- No deviation from the mapped references is allowed.
