# Proposal

Source brief: `.stpr/plan/openspec/proposal-change-show-exercise-type.md`.

## Why

The API now gives each exercise a type (`STRENGTH`, `CARDIO` or `STRETCHING`), through the API change
`add-exercise-type`, but the front's contract snapshot doesn't have it yet. Trainers build workouts from different
kinds of training, and need to filter the catalog by kind, see each exercise's kind at a glance, and prescribe effort
in the scale each kind uses. The generic "Bajo … Alto" scale fits none of them: strength uses RIR, cardio RPE, and
stretching an intensity scale.

## What Changes

- **Contract**: refresh the snapshot (`core/openapi/v1.json`, `core/src/contract/generated`), so `ExerciseResponse`,
  and so the `Exercise` alias, gains `type: string`. It is committed as a step of its own.
- **Type filter**: a new first row of pills in the catalog dialog, labelled "Tipo", with "Fuerza", "Cardio" and
  "Estiramiento".
  - It is always shown.
  - Selecting a type keeps only its exercises, and selecting it again clears it.
  - It combines with the text, body-part and muscle filters, and is kept when the dialog opens again.
  - It never shows or hides another row.
- **Type colours**: strength is blue, cardio red and stretching green (revised from yellow, red and blue while applying). The type pills use them like the other filters
  (outlined while unselected, filled while selected). Every exercise card, in the catalog and in the workout list, gets
  its type's icon in the background (a dumbbell, a heart, a stretching pose), in a very soft pastel tone of its
  type's colour. The icon sits bottom-right at about 75% of the card's height and leaves the card's content
  unchanged. The colours come from new MUI theme entries, because the theme
  has no yellow and components may not use colour literals.
- **BREAKING (effort labels)**: the five effort levels read in the exercise type's scale, both in the configuration
  dialog's selector and in the workout list's "Esfuerzo: …" line:
  - strength: RIR 5+, RIR 4, RIR 3, RIR 1-2, RIR 0;
  - cardio: RPE 1-2 … RPE 9-10;
  - stretching: Muy suave … Muy intenso.

  An exercise with no known type keeps "Bajo" … "Alto". The values sent to the API (`LOW` … `HIGH`) don't change.

## Capabilities

### New Capabilities

- `backoffice-exercise-type`: how the backoffice presents an exercise's type. It covers the type names, the fixed
  colour per type, the type icon in the card background, the effort scale per type with its fallback, and their translations.

### Modified Capabilities

- `exercise-catalog`: listed exercises now carry their type, which the use case returns unchanged.
- `backoffice-workout-editor`:
  - the catalog filters combine the type with the text, body part and muscle;
  - the type row has the label "Tipo" and comes before the other rows;
  - the type row itself, always shown, is added.

## Canonical References

`.stpr/template/README.md` lists the atomic UI references as `pending`, so each piece takes the closest backoffice
piece. No new atom or molecule is added.

- Snapshot refresh -> the refresh steps of the archived change `add-backoffice-workout-exercise-reorder`.
- Type colours and effort labels -> `BODY_PART_COLORS` and `effortLabel` in
  `backoffice/src/presentation/workouts/hooks/use-exercise-labels.ts` (state hook, extended).
- Type row -> the body-part row of `components/organisms/exercise-catalog-dialog.tsx`. It reuses the `LabelFilter`
  molecule for a third row of pills.
- Card icon -> `ExerciseListItem` (molecule, extended with an optional background icon slot) on the existing `Card`
  atom. Icons come from `@mui/icons-material`, as elsewhere.
- Theme -> `backoffice/src/presentation/common/theme.ts` (extended through MUI module augmentation).
- Organisms and views extended in place: `ExerciseCatalogDialog`, `WorkoutExercisesList`,
  `ExerciseConfigurationDialog`, `WorkoutView`.
- Tests:
  - `core/test/infrastructure/repositories/exercise-repository.test.ts` and
    `core/test/application/use-cases/exercises/get-exercises-use-case.test.ts`;
  - `backoffice/test/presentation/workouts/views/workout-view.test.tsx`;
  - `backoffice/e2e/workout.spec.ts`.

## Fronts And Roles

- backoffice: `ADMIN` and `TRAINER` see the type filter, card icons and effort labels the same way. Neither role is
  shown anything the other is not.
- app (`USER`): no UI change. It only compiles against the regenerated `Exercise` alias; it builds no exercise objects.

## API Endpoints

- `GET /exercises` (`getExercisesUseCase`) -> `ExerciseResponse.type`. It is **not** in the current snapshot. It is
  provided by the API change `add-exercise-type`, already served by the API (`ExerciseResponse.Type`, the enum name as
  text). This change refreshes the snapshot.
- `GET /exercises/{id}` -> the same schema; no front use case reads it.
- Workout endpoints: unchanged. A workout exercise takes its type from the catalog exercise with the same id.

## Constraints And Conflicts

- **Theme extension.** The invariants allow atoms to style only through the theme and `sx` tokens. So the type colours
  and their pastel tones are new palette entries in `theme.ts`, never literals in components. This is the only allowed
  deviation from the mapped references.
- **No business rule.** Filtering is local presentation. The effort values and their validation are unchanged; only
  their labels depend on the type.
- **Similar colours.** Body parts already use red (`error`, chest), blue (`info`, back) and green (`success`, legs). The type colours are
  separate palette entries, so they can be tuned on their own; close shades are accepted.
- **Reading of the request.** The brief's reading of "does not hide or show anything" is kept: the type row is always
  visible and never toggles other rows, but it does filter the list.
- **Stop case.** If the refreshed snapshot differs from the expected `type` property on `ExerciseResponse`, the change
  stops.

## Acceptance Criteria

- The snapshot diff is exactly `ExerciseResponse.type`, committed on its own.
- The delta specs' scenarios pass:
  - core unit tests: the exercise repository and use case;
  - `workout-view.test.tsx`, with use cases faked through the container.
- `workout.spec.ts` filters the catalog by type, adds a strength exercise, and sees the "RIR …" effort labels.
- `pnpm typecheck && pnpm lint && pnpm test && pnpm test:e2e` pass from `front/`.

## Impact

- `core`: `openapi/v1.json`, `src/contract/generated/api.ts` (regenerated), and test fixtures.
- `backoffice`:
  - `presentation/common/theme.ts`;
  - `presentation/workouts/hooks/use-exercise-labels.ts`;
  - `components/molecules/exercise-list-item.tsx`;
  - the organisms `exercise-catalog-dialog.tsx`, `workout-exercises-list.tsx` and `exercise-configuration-dialog.tsx`;
  - `views/workout-view.tsx`;
  - `i18n/{es,en}/workouts.json`;
  - tests and E2E fixtures.
- `app`: none beyond compiling against the regenerated alias.
- API: none. It depends on `add-exercise-type`, already served.
