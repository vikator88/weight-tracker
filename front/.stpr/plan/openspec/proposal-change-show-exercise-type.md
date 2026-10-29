# Change Proposal: Show The Exercise Type In The Backoffice

Source brief for `/opsx:propose`. Artifacts go to `openspec/changes/show-exercise-type/`.

## Change

Exercises from `GET /exercises` now carry a type: `STRENGTH`, `CARDIO` or `STRETCHING`. The front brings the type in
through a contract snapshot refresh and uses it in three places:

- a new type filter, the first row of pills in the catalog dialog;
- a soft pastel background, by type, on every exercise card, in the catalog and in the workout list;
- effort labels that depend on the exercise's type (RIR for strength, RPE for cardio, an intensity scale for
  stretching).

## Objective

A trainer builds a workout from different kinds of training. Today the catalog can only be narrowed by body part and
muscle. Effort is shown as a generic "Bajo … Alto" scale that fits none of them: strength is prescribed in reps in
reserve (RIR), cardio in perceived exertion (RPE), and stretching by feel. The type lets the trainer filter the catalog,
see each exercise's kind at a glance, and read and pick effort in the scale that kind uses.

## Capabilities

- **Type filter**: the first row of pills in the catalog dialog, before the body-part row, labelled "Tipo".
  - One pill per exercise type: "Fuerza", "Cardio" and "Estiramiento".
  - Selecting a pill keeps only the exercises of that type; selecting it again clears it. It combines with the text,
    body-part and muscle filters.
  - The row is always shown. Choosing a type does not hide or show any other row; the body-part and muscle rows behave
    as today.
- **Pill colours**: each type has a fixed colour, in the same style as the other filters (outlined in its colour while
  unselected, filled while selected). Strength is yellow, cardio red and stretching blue.
- **Card background**: every exercise card in the catalog dialog and in the workout list has a very soft pastel
  background in its type's colour. An exercise with an unknown type, or a workout exercise missing from the catalog,
  keeps the plain card background.
- **Effort by type**: the five effort levels (`LOW`, `MIDLOW`, `MID`, `MIDHIGH`, `HIGH`, in that order) read differently
  for each type:

  | Level | Strength | Cardio   | Stretching (es / en)       |
  | ----- | -------- | -------- | -------------------------- |
  | 1     | RIR 5+   | RPE 1-2  | Muy suave / Very light     |
  | 2     | RIR 4    | RPE 3-4  | Suave / Light              |
  | 3     | RIR 3    | RPE 5-6  | Moderado / Moderate        |
  | 4     | RIR 1-2  | RPE 7-8  | Intenso / Intense          |
  | 5     | RIR 0    | RPE 9-10 | Muy intenso / Very intense |
  - This applies in the configuration dialog's effort selector and in the "Esfuerzo: …" line of the workout list.
  - An exercise with no known type keeps today's labels ("Bajo" … "Alto").

## Fronts And Roles

- backoffice -> lands here: yes -> roles: `ADMIN`, `TRAINER` -> both see and use the type filter, the card
  backgrounds and the effort labels the same way.
- app -> lands here: no -> roles: `USER` -> no UI change. The app keeps compiling: the regenerated `Exercise` alias
  gains a field, and the app builds no exercises in its code or tests.

No role-dependent UI.

## Classification

- Contract snapshot -> `core/openapi/v1.json` and `core/src/contract/generated` (refreshed and regenerated, never
  hand-edited).
- `Exercise` -> entity type (alias of `ExerciseResponse`, unchanged in `core/src/domain/exercises/exercise.ts`) -> it
  gains `type: string` from the regenerated schema.
- Type colours and pastel tones -> MUI theme -> `backoffice/src/presentation/common/theme.ts`, with module
  augmentation so `Chip` accepts the new colours and `sx` can name the pastel tones.
- `useExerciseLabels` -> state hook (extended) -> `typeLabel`, `typeColor` and `typeBackground`, and `effortLabel` made
  type-aware.
- `LabelFilter` -> molecule (reused as is) -> the third row of pills.
- `ExerciseListItem` -> molecule (extended) -> an optional background for its card.
- `ExerciseCatalogDialog` -> organism (extended) -> the type row, the type filter and the card background.
- `WorkoutExercisesList` -> organism (extended) -> the card background and type-aware effort line.
- `ExerciseConfigurationDialog` -> organism (extended) -> type-aware effort options.
- `WorkoutView` / `LoadedWorkout` -> view (extended) -> holds `selectedType` beside the other catalog filters.
- Copy -> `backoffice/src/i18n/{es,en}/workouts.json`.

Use cases, repositories and the HTTP error mapper do not change.

## Canonical Template Mapping

- Snapshot refresh -> the earlier refresh in archived change `add-backoffice-workout-exercise-reorder` -> the same
  steps: refresh, generate, and commit the snapshot on its own.
- Type colours -> `BODY_PART_COLORS` in `backoffice/src/presentation/workouts/hooks/use-exercise-labels.ts` -> one fixed
  colour per code, unknown codes neutral.
- Type filter -> the body-part row in `components/organisms/exercise-catalog-dialog.tsx` (`LabelFilter` with coloured
  options) -> the same molecule and filter behaviour.
- Type-aware effort labels -> `effortLabel` in `use-exercise-labels.ts` -> the same lookup, keyed by type as well.
- Tests:
  - `core/test/infrastructure/repositories/exercise-repository.test.ts` and
    `core/test/application/use-cases/exercises/get-exercises-use-case.test.ts`: their fixtures gain `type`;
  - `backoffice/test/presentation/workouts/views/workout-view.test.tsx` (extended);
  - `backoffice/e2e/workout.spec.ts`: fixtures gain `type`, plus one flow.

`.stpr/template/README.md` lists the atomic UI references as `pending`; no new atom or molecule is added.

## Screens And Navigation

- backoffice -> `/workout?id=<id>` -> `WorkoutView` -> unchanged entry points.
  - The catalog dialog gains the type row.
  - The exercise cards get their type background.
  - The configuration dialog and the workout list show type-aware effort labels.
  - There is no new navigation.

## API Contract

- `GET /exercises` -> in `core/openapi/v1.json`: **no** (`ExerciseResponse` lacks `type` in the current snapshot) ->
  `ExerciseResponse.type: string`, one of `STRENGTH`, `CARDIO`, `STRETCHING`, sent as plain text like `muscle` and
  `bodyPart`.
- `GET /exercises/{id}` -> same schema.
- Workout endpoints -> unchanged. A workout exercise takes its type from the catalog exercise with the same id.

The API already serves the field: `ExerciseResponse.Type` in `api/src/WeightTracker.Api/Endpoints/Exercises/Dtos/`,
from the API change `add-exercise-type`. This change **refreshes the snapshot**:

1. With the API running (Debug API, port 5199), run `pnpm --filter @weight-tracker/core openapi:refresh`, then
   `openapi:generate`.
2. Review the diff of `core/openapi/v1.json` and the regenerated types. The expected diff is the `type` property on
   `ExerciseResponse`; anything else stops the change.
3. Commit them as a step of their own, before the code that uses them.

No hand-written domain type.

## Held State

- `LoadedWorkout` `selectedType` -> the selected type pill, or none -> the type row -> screen exit. It is kept when the
  catalog dialog closes and opens again, like the other filters.
- Nothing else. Effort labels and backgrounds are derived from the catalog exercise.

## Forms And Structural Validation

None. The effort selector keeps the same five values and validation; only their labels change.

## Errors And Copy

- No new failure and no new error code; the HTTP error mapper is not touched.
- New copy (`workouts` namespace):
  - `workout.catalog.types` (row label) -> "Tipo" -> "Type"
  - `exerciseTypes.STRENGTH` -> "Fuerza" -> "Strength"
  - `exerciseTypes.CARDIO` -> "Cardio" -> "Cardio"
  - `exerciseTypes.STRETCHING` -> "Estiramiento" -> "Stretching"
  - `effortsByType.STRENGTH.{LOW…HIGH}` -> "RIR 5+", "RIR 4", "RIR 3", "RIR 1-2", "RIR 0" (the same in en)
  - `effortsByType.CARDIO.{LOW…HIGH}` -> "RPE 1-2", "RPE 3-4", "RPE 5-6", "RPE 7-8", "RPE 9-10" (the same in en)
  - `effortsByType.STRETCHING.{LOW…HIGH}` -> "Muy suave", "Suave", "Moderado", "Intenso", "Muy intenso" -> "Very light",
    "Light", "Moderate", "Intense", "Very intense"
- `efforts.*` stays as the fallback for an unknown type.

## Platform Capabilities And Dependencies

None.

## Out Of Scope

- The app.
- Showing the type as a label or pill on the card; the background is the only indicator.
- Changing which body parts or muscles the other rows offer when a type is selected. They are computed as today.
- Any change to the effort values sent to the API (`LOW` … `HIGH`), to superseries, or to the workout endpoints.
- A type-coloured background in the configuration dialog.

## Notes

- **Colours go through the theme.** The invariants allow atoms to style only through the theme and `sx` theme tokens,
  never colour literals, and MUI has no yellow (`warning` is orange). So `theme.ts` gains three palette colours,
  `strength` (yellow), `cardio` (red) and `stretching` (blue), each made with `augmentColor`. Each also has a pastel
  tone for card backgrounds, very light and low in saturation. The exact shades are settled in the design.
  - Module augmentation adds them to `Palette`, `PaletteOptions` and `ChipPropsColorOverrides`.
  - `LabelColor` in `use-exercise-labels.ts` widens to include them.
- **Two reds and two blues.** Body parts already use `error` (red, chest) and `info` (blue, back). The type colours are
  their own palette entries, so they can be tuned without changing the body parts. Close shades are accepted.
- **"Does not hide or show anything"** is read as: the type row is always visible, unlike the muscle row, and selecting
  a type never shows or hides other rows. It still filters the exercise list, like every other pill row. Confirm before
  proposing if a different meaning was intended.
- The type-aware `effortLabel(code, type?)` looks up `effortsByType.<type>.<code>` and falls back to `efforts.<code>`,
  then to the code itself.
- The type of a workout exercise comes from `catalogById` in `workout-view.tsx`, like its name and labels.
- Verification tiers:
  - core unit: the exercise repository and use-case fixtures gain `type`, and the mapped response keeps it;
  - backoffice component (`workout-view.test.tsx`, use cases faked through the container):
    - the type row comes first and is always shown;
    - each pill's colour, outlined or filled;
    - filtering by type alone and combined with a body part, and clearing it;
    - the filter is kept when the dialog opens again;
    - card backgrounds by type in the catalog and the workout list, plain for an unknown type;
    - effort labels by type in the dialog's selector and in the workout list, and the fallback;
    - English labels;
  - backoffice E2E (`workout.spec.ts`, API intercepted with `page.route`): catalog fixtures gain `type`; one flow
    filters by type and adds a strength exercise whose effort selector shows "RIR …".
- No deviations from the mapped references are allowed, apart from extending the theme as described.
