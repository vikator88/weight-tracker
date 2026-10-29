# Design

## Context

See `proposal.md` for the motivation and `specs/` for the behaviour. This is the current state that shapes the
approach:

- **Contract.** `core/openapi/v1.json`'s `ExerciseResponse` has `muscle` and `bodyPart` as plain strings, and no
  `type`. `Exercise` in `core/src/domain/exercises/exercise.ts` is an alias of that schema. The API already returns
  `Type` as the enum name (`ExerciseResponse.cs`).
- **Theme.** `backoffice/src/presentation/common/theme.ts` sets only `palette.background.default`. MUI's palette has no
  yellow (`warning` is orange). The invariants forbid colour literals outside the theme.
- **`useExerciseLabels`** holds `BODY_PART_COLORS` and `LabelColor` (MUI palette keys), `effortLabel` (`efforts.<code>`
  with the code as fallback), and `exerciseLabelsOf`.
- **`LabelFilter`** renders coloured pills, outlined or filled. The catalog dialog uses it for the body-part and muscle
  rows, and the view holds those selections.
- **`ExerciseListItem`** renders every exercise card (catalog and workout list) on the `Card` atom. Its only
  background today is `action.selected` when `selected`.
- **Fixtures.** Exercises in tests come from `catalogExercise(...)` in `workout-view.test.tsx` and from the fixtures in
  `e2e/workout.spec.ts`. Several existing tests assert generic effort labels ("Alto", "Medio").

## Goals / Non-Goals

**Goals:** one source for type names, colours, pastel tones, icons and effort scales (`useExerciseLabels` plus the theme); no
colour literal outside `theme.ts`; an unknown type always degrades to today's look and labels.

**Non-Goals:** a type label on cards; type-dependent body-part or muscle options; tinting the configuration dialog;
any app UI.

## Decisions

### Contract first

With the API running on its Debug API port 5199, run `pnpm --filter @weight-tracker/core openapi:refresh`, then
`openapi:generate`.

- If the generated `api.ts` loses the committed formatting, format it with Prettier, as the reorder change did.
- Expected diff: `type` (required, `string`) on `ExerciseResponse` only; any other diff stops the change.
- Commit the snapshot alone.
- `Exercise` gains `type` with no code change. TypeScript then flags every exercise fixture without `type`, which marks
  the fixtures to update.

### Theme: three palette colours with a pastel tone

`theme.ts` adds `strength`, `cardio` and `stretching` with `palette.augmentColor`. Each gets an extra `pastel` tone,
made with `lighten(main, 0.88)` from `@mui/material/styles`:

| Type       | `main`    | `pastel` (about) |
| ---------- | --------- | ---------------- |
| strength   | `#2F80ED` | `#E5EFFD`        |
| cardio     | `#E5484D` | `#FCE9EA`        |
| stretching | `#27AE60` | `#E4F5EB`        |

- **Contrast**: `augmentColor` computes `contrastText` for each filled pill.
- **Module augmentation** (in `theme.ts`):
  - `Palette` and `PaletteOptions` gain the three colours;
  - `PaletteColor` and `SimplePaletteColorOptions` gain an optional `pastel`;
  - `ChipPropsColorOverrides` lets `<Chip color="strength">` type-check.
- **Usage**: pills use `color="strength"`. A card's type icon uses the `sx` token `color: "strength.pastel"`, which MUI
  resolves against the palette. No hex appears outside `theme.ts`.
- _Alternative_: reuse `warning` / `error` / `info`. Rejected: there is no yellow, and the types would share colours
  with the body parts (`error` is chest, `info` is back).

### `useExerciseLabels` gains the type

- **`LabelColor`** widens with `"strength" | "cardio" | "stretching"`.
- **`EXERCISE_TYPES`** is `["STRENGTH", "CARDIO", "STRETCHING"]`: the known types, in pill order.
- **`TYPE_COLORS`** maps `STRENGTH → "strength"`, `CARDIO → "cardio"` and `STRETCHING → "stretching"`. An unknown type
  maps to `"default"`.
- **New functions**:
  - `typeLabel(code)` reads `exerciseTypes.<code>`, with the code as default;
  - `typeColor(code)` returns the `LabelColor`;
  - `typeIcon(code)` returns the type's icon component (`FitnessCenter`, `Favorite` or `SelfImprovement` from
    `@mui/icons-material`), or `undefined` for an unknown type;
  - `typeIconColor(code)` returns the `sx` token `"<color>.pastel"`, or `undefined`.
- **`effortLabel(code, type?)`**:
  - for a known type it reads `effortsByType.<type>.<code>`, defaulting to `efforts.<code>`, then the code;
  - without a known type it behaves as today.
  - Existing callers without a type are unchanged.

### Components

- **`ExerciseListItem`** (molecule): a new optional `watermark?: ReactNode`, an icon drawn behind the content.
  - The `Card` becomes `position: relative` and `overflow: hidden`, and its rows sit in a `position: relative`,
    `zIndex: 1` wrapper.
  - The watermark is wrapped in an `aria-hidden` `Box`: `position: absolute`, `right: 0`, `bottom: 0`,
    `height: "75%"`, `aspectRatio: "1"`, `pointerEvents: "none"`, `zIndex: 0`, with `& svg` at full width and height.
    The MUI icons are square, so the aspect ratio is kept.
  - Being absolute, the watermark takes no part in the layout, so the card's content does not move.
- **`ExerciseCatalogDialog`** (organism):
  - new props `selectedType` and `onToggleType`;
  - a `LabelFilter` labelled `workout.catalog.types` ("Tipo"), rendered before the body-part row. Its options are the
    three `EXERCISE_TYPES` in that fixed order, coloured with `typeColor`;
  - the list filter adds `selectedType === null || exercise.type === selectedType`;
  - each card gets the `watermark` of its type: the `typeIcon` component, coloured with `typeIconColor`.
  - The body-part and muscle options are still computed from the available exercises, ignoring the type (brief: out
    of scope).
- **`WorkoutExercisesList`** (organism): each card gets the `watermark` of its catalog exercise's type (none when the
  exercise is missing from the catalog). The effort line uses `effortLabel(effort, exercise?.type)`.
- **`ExerciseConfigurationDialog`** (organism): the effort options use `effortLabel(code, item.exercise?.type)`. No
  type icon.
- **`LoadedWorkout`** (view): holds `selectedType`, toggled like the body part, and passes it to the catalog dialog. It
  does not clear the body part or the muscle.

Imports flow view → organisms → molecules → atoms. Only atoms import MUI widgets. `theme.ts` stays where it is, in
`presentation/common`, as today.

### Copy (`backoffice/src/i18n/{es,en}/workouts.json`)

- `workout.catalog.types`: "Tipo" / "Type"
- `exerciseTypes`: `STRENGTH` "Fuerza" / "Strength", `CARDIO` "Cardio" / "Cardio", `STRETCHING` "Estiramiento" /
  "Stretching". This is a top-level group, like `muscles`, `bodyParts` and `efforts`.
- `effortsByType.STRENGTH`: `LOW` … `HIGH` → "RIR 5+", "RIR 4", "RIR 3", "RIR 1-2", "RIR 0" (both languages).
- `effortsByType.CARDIO`: "RPE 1-2", "RPE 3-4", "RPE 5-6", "RPE 7-8", "RPE 9-10" (both languages).
- `effortsByType.STRETCHING`: "Muy suave", "Suave", "Moderado", "Intenso", "Muy intenso" / "Very light", "Light",
  "Moderate", "Intense", "Very intense".
- `efforts.*` is unchanged; it is the fallback.

Error codes: none new. The catalog failures (`UNEXPECTED`, `NETWORK`) keep their existing copy, and the mapper is
untouched.

### Held state

`LoadedWorkout.selectedType`: the selected type or `null`. It is changed by the type pills (a toggle) and resets on
screen exit. It survives closing and reopening the catalog dialog, like the other filters. No new provider.

### Tests

- **Core.** The exercise fixtures in `exercise-repository.test.ts` and `get-exercises-use-case.test.ts` gain `type`.
  One repository assertion checks that `type` is mapped through unchanged, including an unknown value.
- **Backoffice fixtures.** `catalogExercise(...)` gains a `type` argument, and the fixtures get realistic types (the
  squat, leg press and bench press are `STRENGTH`).
  - Existing tests that assert generic effort labels ("Alto", "Medio") switch to the RIR labels.
  - A fixture with an unknown type covers the fallback.
  - One new test per scenario of the new and modified requirements: the type row, colours, filtering, reopening,
    type icons, effort scales and English.
- **E2E** (`workout.spec.ts`):
  - every catalog fixture gains `type`, and the add flow's "Alto" choice becomes "RIR 0";
  - a new flow selects "Fuerza", adds an exercise, and asserts the effort options and the saved `effort: "HIGH"`.

## Risks / Trade-offs

- [The API must be running for the refresh] → The task starts the API's Debug API on 5199, or asks the user to.
  Without it, the change stops at the snapshot step.
- [Pastel tones may be too faint, or too strong, on some screens] → They are single theme values, tuned in one place.
- [Red, blue and green are close to the chest, back and legs colours] → Accepted in the brief. The types still read differently,
  because only types draw a large icon behind the card, while body parts colour small chips.
- [Existing effort tests change their expected text] → Intended: the fixtures now have types. The fallback keeps one
  test on the generic labels.

## Implementation Notes

Found while applying:

- **The refreshed snapshot has two diffs, not one.** It adds the required `type: string` to `ExerciseResponse`, as
  expected, and also to `WorkoutExerciseResponse` (each exercise entry of a workout). The extra field was accepted by
  decision.
  - The front keeps taking a workout exercise's type from its catalog entry, as designed; the entry's own `type` is
    left unused.
  - Its only effect is that every workout-exercise fixture (core, backoffice tests, E2E mock) gains `type` to
    compile.
- **The pastel card background became a background icon** (a user request while applying). The first version tinted
  the whole card in the type's pastel tone. It was replaced by the type's icon in that tone, bottom-right at 75% of
  the card's height, behind the content. The `background` prop of `ExerciseListItem` and `typeBackground` were
  replaced by `watermark`, `typeIcon` and `typeIconColor`, and the tint tests by icon tests that find the icons by
  their MUI `data-testid`. The hook also offers `typeWatermark(code)`, the coloured icon built with
  `createElement`, so the two organisms don't each assemble it.
- **Revised colours, cardio icon and catalog inset** (a user request while applying).
  - Strength is now blue (`#2F80ED`) and stretching green (`#27AE60`); cardio stays red.
  - Cardio's icon is `Favorite` (a heart) instead of `DirectionsRun`; a heartbeat (`MonitorHeart`) was tried first.
  - `ExerciseListItem` takes `watermarkInsetRight` (`sx` spacing). The catalog passes 2.5, half the width of its
    "Elegir ejercicio" button, which moves the icon off the card's edge, away from the plus. A full button width was
    tried first and judged too much. The workout list keeps the icon at the card's edge.
- **Known types are checked with `Object.hasOwn`.** `code in TYPE_COLORS` would also be true for inherited keys such
  as `toString`. A hook test guards this.
- **`catalogExercise` defaults its `type` to `STRENGTH`**, so the existing calls stay as they are. The type tests use
  their own fixtures: a cardio bike, a hamstring stretch, and a "tree pose" of the unknown type `YOGA`.
- **A dedicated hook test for unknown types.** A new `test/presentation/workouts/hooks/use-exercise-labels.test.ts`
  covers the scenarios with nothing to show on screen: an unknown type's name and colour, and RIR and RPE in English.
  Unknown types get no pill and cards show no type name.
- **English effort test.** Rendering resets the language to Spanish, so the English stretching-scale test opens the
  effort options in Spanish and then switches the language. The open options are re-rendered in English.
- **Two existing test changes.** One test that collected every unpressed pill in the catalog is now scoped to the
  "Partes del cuerpo" group, because the type pills are unpressed pills too. The other existing tests only changed
  their expected effort labels to RIR.
- **Formatting the generated file.** `core/src/contract/generated` is listed in `.prettierignore`, so the regenerated
  `api.ts` was formatted with `--ignore-path /dev/null` to match the committed formatting. The diff is then the two
  `type` lines only.

## Migration Plan

Merge the snapshot commit first, then the front change. No data migration. Rolling back reverts the front; the
snapshot can stay.
