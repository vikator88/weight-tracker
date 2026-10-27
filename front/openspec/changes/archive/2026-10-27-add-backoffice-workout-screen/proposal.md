# Proposal

## Why

Assigned plans can be listed but not created or opened: "Nuevo entrenamiento" is inert and rows lead nowhere. This
change gives trainers and admins the first step of the workout editor, creating a workout and seeing it next to the
exercise catalog, while wiring only **one** mutation (creating a workout) so the editor layout can be reviewed before
its editing behaviour is planned.

## What Changes

- **"Nuevo entrenamiento" opens a create workout dialog** with Name (required), Description (optional), Date (required),
  Athlete ID (required) and, for `ADMIN` only, Trainer ID (required). Success navigates to the new workout's screen;
  failure keeps the dialog open with the error; Cancel closes it without a request.
- **Each workouts table row gets an edit icon button** that navigates to that workout's screen.
- **New workout screen at `/workout?id=<id>`**, three columns (25% / 50% / 25%, stacked on narrow screens):
  - catalog: text filter, wrapping body-part pills, catalog exercises with body-part and muscle labels and an inert plus;
  - workout: a card with editable name, date and description, saved with "Guardar" in the page header, then the
    workout's exercises with an
    inert trash and a pencil that selects the exercise;
  - configuration: the selected workout exercise's name, body part, muscle and video (when provided), then its effort
    and sets in fields that are not saved; "Selecciona un ejercicio para configurarlo." when nothing is selected.
- **core**: structural create-workout form rules (`workout-rules.ts`), shared by any front. No new use case: create,
  get-by-id and get-exercises are already registered in both containers.
- The app does not change.

## Capabilities

Per front and role:

| Capability                    | Front      | `ADMIN`                         | `TRAINER`                                       | `USER` (app) |
| ----------------------------- | ---------- | ------------------------------- | ----------------------------------------------- | ------------ |
| Create workout dialog         | backoffice | all fields, Trainer ID required | all fields, must NOT see Trainer ID nor send it | —            |
| Edit action on assigned plans | backoffice | yes                             | yes                                             | —            |
| Workout screen                | backoffice | same screen                     | same screen                                     | —            |

Which fields each role must send is the API's rule; the dialog only mirrors it in what it asks. The rows a role sees and
the workouts it may open are decided by the API (`403` / `404`).

### New Capabilities

- `backoffice-workout-creation`: the create workout dialog, its structural validation, the role-dependent Trainer ID
  field, the request it sends and where it leads. Canonical references: `onboarding-dialog.tsx` and `profile-form.tsx`
  (dialog organism with a validated form), `use-update-profile.ts` (action hook), `profile-name-rules.ts` (form rules in
  `core`).
- `backoffice-workout-editor`: the `/workout` screen, its three columns, local catalog filtering, the exercise selection,
  the configuration panel with its video, its inert actions and its loading, not-found and failure states. Canonical
  references: `assigned-plans-view.tsx` (view), `use-workouts.ts` (read hooks), `assigned-plans/page.tsx` (route); atom,
  molecule and organism references are `pending` (atomic design) in `.stpr/template/README.md`, so they follow the shapes
  of #30 and the assigned plans change. The `/workout?id=` route establishes the pattern for a screen keyed by an id in
  a static export.

### Modified Capabilities

- `backoffice-assigned-plans`: "Nuevo entrenamiento" stops being inert and opens the create dialog; each row gains an
  edit action. Canonical reference: the existing `assigned-plans-view.tsx` and `workouts-table.tsx`, extended in place.

## Components

- **Atoms (new)** in `common/components/atoms/`: `dialog`, `dialog-title`, `dialog-content`, `dialog-actions`, `card`,
  `card-content`, `chip`, `list`, `list-item`.
- **Atoms (reused)**: `button`, `icon-button`, `text-field`, `typography`, `alert`, `menu-item`, `link`, `skeleton`, the
  table atoms.
- **Molecules (new)** in `workouts/components/molecules/`: `ExerciseListItem` (name, labels, trailing actions; used by
  both lists), `BodyPartFilter` (wrapping pills), `ExerciseVideo` (YouTube player or link).
- **Molecules (reused)**: `LoadError`, `HomeSkeleton`.
- **Organisms (new)** in `workouts/components/organisms/`: `CreateWorkoutDialog`, `ExerciseCatalogPanel`,
  `WorkoutDetailsCard`, `WorkoutExercisesList`, `ExerciseConfigurationPanel`.
- **Organisms (reused / extended)**: `AppShell` (reused), `WorkoutsTable` (edit column).

## Constraints And Decisions

- **Two mutations**: `POST /workouts` (create dialog) and `PUT /workouts/{id}` (Guardar). The plus adds a catalog
  exercise to the workout as pending, on screen only, until its sets can be saved; trash renders enabled and sends
  nothing; the pencil only selects locally; the exercise configuration is not saved. The configuration
  card's Guardar adds a pending exercise (`POST /workouts/{workoutId}/exercises`) or replaces a saved one's effort and
  sets (`PUT /workouts/{workoutId}/exercises/{exerciseId}`, through a new `UpdateWorkoutExerciseUseCase` in `core`).
- **Static export**: the workout screen reads its id from the query string (`/workout?id=`), since a `/workouts/[id]`
  segment would need every id at build time.
- **No business rule in the front**: validation is structural (required, length, date, UUID) with the API's limits
  (name ≤ 256 after trimming, description ≤ 1024). Catalog filtering, body-part pills and joining a workout exercise to its
  catalog entry by `exerciseId` are display composition.
- **Video**: YouTube watch URLs are embedded with `youtube-nocookie.com`; other URLs are an external link; `null` shows
  no video.
- No snapshot refresh, no new error code, no new dependency.
- No conflict with the invariants, the template or a stop case.

## Acceptance Criteria

- From `/assigned-plans`, a trainer creates a workout (with name, date and athlete ID) and lands on its
  `/workout?id=<new id>` screen; an admin must also give a trainer ID, and a trainer never sees that field.
- Invalid fields show their message and send nothing; an API failure keeps the dialog open with the error and the values.
- The edit action of a row opens that workout's screen.
- The workout screen shows the three columns; the catalog filters by text and by body-part pill together; pills wrap.
- The pencil shows that exercise's details, video and effort/sets in the third column; nothing selected shows
  "Selecciona un ejercicio para configurarlo.".
- Guardar saves the card's name, date and description; the plus adds an exercise as pending and opens it in the
  configuration card without a request; trash sends no request.
- Every string exists in `es` and `en`.
- `pnpm typecheck && pnpm lint && pnpm test && pnpm test:e2e` passes from `front/`.

## Impact

- `core`: `application/validation/workout-rules.ts` + barrel + tests.
- `backoffice`: new route `app/workout/page.tsx`, `presentation/workouts/` (view, hooks, molecules, organisms), new
  atoms, `assigned-plans-view.tsx` and `workouts-table.tsx`, `workouts` i18n namespace, component and E2E tests.
- `app`: none.
- No API change, no new dependency, no platform port.
