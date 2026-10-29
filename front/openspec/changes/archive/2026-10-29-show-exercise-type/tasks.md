# Tasks

No new use case, repository method, `Dependencies` key or `container.ts` change.

## 1. Contract snapshot

- [x] 1.1 With the API running on its Debug API port 5199 (start it, or ask the user to), run `pnpm --filter @weight-tracker/core openapi:refresh` and then `openapi:generate`. Format the generated `api.ts` with Prettier if it lost its committed formatting. Verify that the diff of `core/openapi/v1.json` and `core/src/contract/generated` adds only `type` (required `string`) to `ExerciseResponse` and, accepted by decision, to `WorkoutExerciseResponse`; stop and report any other diff.
- [ ] 1.2 Confirm nothing under `core/src/contract/generated` was edited by hand, and commit the snapshot as a step of its own. Verify with `pnpm --filter @weight-tracker/core typecheck`.

## 2. Core fixtures

- [x] 2.1 Add `type` to the exercise fixtures in `core/test/infrastructure/repositories/exercise-repository.test.ts` and `core/test/application/use-cases/exercises/get-exercises-use-case.test.ts`. Add one repository test showing `type` (a known value and an unknown one) is returned unchanged, in AAA form with one Act. Verify with `pnpm --filter @weight-tracker/core test`.
- [x] 2.2 Review core against the existing exercise repository and use-case tests. Verify there is no difference beyond the fixtures and the new assertion.

## 3. Backoffice theme and labels

- [x] 3.1 Extend `backoffice/src/presentation/common/theme.ts`:
  - add the `strength`, `cardio` and `stretching` palette colours (`augmentColor`), each with a `pastel` tone from `lighten(main, 0.88)`;
  - add module augmentation for `Palette`, `PaletteOptions`, `PaletteColor`, `SimplePaletteColorOptions` and `ChipPropsColorOverrides`.

  Verify with `pnpm --filter backoffice typecheck`.

- [x] 3.2 Add `workout.catalog.types`, `exerciseTypes.*` and `effortsByType.{STRENGTH,CARDIO,STRETCHING}.*` to `backoffice/src/i18n/{es,en}/workouts.json`. Verify the translation parity test passes.
- [x] 3.3 Extend `useExerciseLabels`:
  - widen `LabelColor`;
  - add `EXERCISE_TYPES`, `TYPE_COLORS`, `typeLabel`, `typeColor` and `typeBackground`;
  - make `effortLabel(code, type?)` use `effortsByType` with the `efforts` fallback.

  Verify with the backoffice typecheck and lint.

## 4. Backoffice components and view

- [x] 4.1 Add the optional `background` token to `ExerciseListItem` (the `Card`'s `bgcolor`). Verify with the backoffice typecheck and lint (no colour literal outside `theme.ts`).
- [x] 4.2 In `ExerciseCatalogDialog`, add:
  - the "Tipo" `LabelFilter` before the body-part row, with the three types in fixed order;
  - the `selectedType` / `onToggleType` props and the type filter;
  - the card backgrounds.

  In `LoadedWorkout`, hold `selectedType` and toggle it without touching the body part or the muscle. Verify with the backoffice typecheck.

- [x] 4.3 In `WorkoutExercisesList`, add the card backgrounds and the type-aware effort line. In `ExerciseConfigurationDialog`, add the type-aware effort options. Verify with the backoffice typecheck and lint.

## 5. Backoffice tests

- [x] 5.1 Update `workout-view.test.tsx` fixtures:
  - `catalogExercise` takes a `type`, and the squat, leg press and bench press are `STRENGTH`;
  - existing generic effort expectations become RIR labels;
  - add an unknown-type fixture for the fallback.

  Verify the existing tests pass with `pnpm --filter backoffice test`.

- [x] 5.2 Add one test per new or modified scenario:
  - **Type row**: first, always shown, coloured, filled when selected, cleared again;
  - **Filters**: type combined with a body part, kept on reopening, and the "Tipo" label before "Partes del cuerpo";
  - **Tints**: in the catalog and the workout list, and plain for an unknown type or a missing exercise;
  - **Effort scales**: strength, cardio and stretching in the selector, the RIR line in the workout list, `HIGH` sent for "RIR 0", and the unknown-type fallback;
  - **English** labels.

  Use AAA with exactly one Act. Verify with `pnpm --filter backoffice test`.

## 6. Backoffice E2E

- [x] 6.1 Update `backoffice/e2e/workout.spec.ts`:
  - give every catalog fixture a `type`;
  - the add flow chooses "RIR 0" instead of "Alto";
  - add a flow that selects "Fuerza", adds an exercise, sees "RIR 5+" … "RIR 0" offered, and asserts the `POST` body carries `effort: "HIGH"`.

  Verify with `pnpm test:e2e`.

## 7. Review and verification

- [x] 7.1 Review the backoffice against its references:
  - the type colours and effort lookup against `BODY_PART_COLORS` and `effortLabel`;
  - the type row against the body-part row;
  - the theme extension.

  Verify there are no differences beyond those in the design.

- [x] 7.2 Run the `.stpr/template/` drift check from `AGENTS.md`. Verify it prints no `DRIFT`.
- [x] 7.3 From `front/`, run `pnpm typecheck && pnpm lint && pnpm test && pnpm test:e2e`. Verify everything passes.

## 8. Type icon instead of the pastel background

- [x] 8.1 Replace `typeBackground` with `typeIcon` (`FitnessCenter`, `DirectionsRun`, `SelfImprovement`) and `typeIconColor` (the pastel token) in `useExerciseLabels`. Replace `ExerciseListItem`'s `background` with a `watermark` slot: absolute, bottom-right, 75% of the card's height, square, `aria-hidden`, no pointer events, behind the content. Pass the icon from the catalog dialog and the workout list. Verify with the backoffice typecheck and lint.
- [x] 8.2 Replace the tint tests in `workout-view.test.tsx` and `use-exercise-labels.test.ts` with icon tests:
  - the catalog shows the dumbbell, runner and pose in their pastel tones;
  - the workout card shows its catalog type's icon;
  - an unknown or missing exercise shows no icon;
  - the icon is `aria-hidden`, with no pointer events and absolutely positioned.

  Verify with `pnpm --filter backoffice test`.

## 9. Revised type colours, cardio icon and catalog inset

- [x] 9.1 Make strength blue and stretching green in `theme.ts`, and use `Favorite` (a heart) for cardio. Add `watermarkInsetRight` to `ExerciseListItem` and pass half the "Elegir ejercicio" button's width (2.5 spacing) from the catalog dialog. Verify with the backoffice typecheck and lint.
- [x] 9.2 Update the icon tests (cardio is `FavoriteIcon`), and assert that the catalog icon is inset from the right while the workout icon is not. Verify with `pnpm --filter backoffice test`, then run `pnpm typecheck && pnpm lint && pnpm test && pnpm test:e2e` from `front/`.

## Workflow follow-up

- Archive the change once it is reviewed and merged.
