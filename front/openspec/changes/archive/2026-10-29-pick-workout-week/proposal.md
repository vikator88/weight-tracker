# Proposal

Source brief: `.stpr/plan/openspec/proposal-change-pick-workout-week.md`.

## Why

Trainers plan workouts per week, not per day. The backoffice asks for an exact day and shows one, which is noise and
suggests a precision the plan doesn't have. The workout's date should be picked and shown as a Monday-to-Sunday week.

## What Changes

- **New week field** ("Semana") in the workout screen's properties card and in the create workout dialog. It replaces
  the date field.
  - It shows the selected week as "Semana del 26 oct – 1 nov".
  - "Semana anterior" (‹) and "Semana siguiente" (›) move one week.
  - "Elegir semana" opens the browser's date picker, and the week of the day picked becomes the selection.
- **BREAKING (what is sent)**: saving or creating sends the **Wednesday** of the selected week as `workoutDate`.
  Saving a workout whose stored day is not a Wednesday therefore changes that day to its week's Wednesday, even when
  only the name was edited. The week stays the same.
- **Create dialog default**: it starts on the current week instead of an empty date, so the date can no longer be
  reported as missing.
- **Assigned plans table**: the "Fecha" column becomes "Semana" and shows each workout's week instead of its day.
- **Any stored date** (a Wednesday, or an older arbitrary day) is shown as the week that contains it. The year appears
  only when the week is not in the current year, or spans two years.
- No API, contract, `core` or app change. The API stores the date it receives.

## Capabilities

### New Capabilities

- `backoffice-workout-week`: the week field and the week label shared by the backoffice screens. It covers how a
  week is shown and how it is picked (stepper and calendar), how any date maps to its Monday-to-Sunday week, the year
  rule, and translations.

### Modified Capabilities

- `backoffice-workout-editor`: the properties card holds the week field instead of Fecha. "Guardar" sends the
  selected week's Wednesday.
- `backoffice-workout-creation`: the dialog asks for a week (Semana) instead of a date, starts on the current week,
  and creates with that week's Wednesday. The week is no longer reported as missing.
- `backoffice-assigned-plans`: the date column becomes "Semana" and shows the week label.

## Canonical References

`.stpr/template/README.md` lists the atomic UI references as `pending`, so each piece takes the closest backoffice
piece:

- `WeekField` (molecule, new) -> `components/molecules/label-filter.tsx`: a labelled group with a caption, built only
  from atoms.
- `useWeekLabel` (state hook, new) -> `hooks/use-exercise-labels.ts`: text in the active language. Its date parsing
  and `Intl` formatting come from `formatWorkoutDate` in `components/organisms/workouts-table.tsx`, which it replaces.
- `week.ts` (presentation helper file, new) -> the UTC `YYYY-MM-DD` parsing of `isCalendarDate` in
  `core/src/application/validation/workout-rules.ts`.
- `WorkoutDetailsCard`, `CreateWorkoutDialog`, `WorkoutsTable` (organisms) and `WorkoutView` (view) change in place.
- Reused atoms: `IconButton`, `Typography`, `TextField`. No new atom.
- Tests:
  - `test/presentation/workouts/views/workout-view.test.tsx`, plus the existing create-dialog and assigned-plans tests;
  - a new `test/presentation/workouts/week.test.ts`;
  - `e2e/workout.spec.ts`.

## Fronts And Roles

- backoffice: `ADMIN` and `TRAINER` pick and see weeks the same way. No role sees anything different. The trainer
  field of the create dialog stays admin-only, as today.
- app (`USER`): no change; the app does not show or edit workout dates yet.

## API Endpoints

All of them are in `core/openapi/v1.json`, and their shape is unchanged:

- `POST /workouts` (`createWorkoutUseCase`): `workoutDate` (`format: date`) is sent as the week's Wednesday.
- `PUT /workouts/{id}` (`updateWorkoutUseCase`): the same.
- `GET /workouts` (`getWorkoutsUseCase`) and `GET /workouts/{id}` (`getWorkoutByIdUseCase`): `workoutDate` is shown as
  its week.

No snapshot refresh.

## Constraints And Conflicts

- **Conflict with the invariants, accepted by decision.** `STPR_INVARIANTS.md` gives the API any normalisation beyond
  trimming. Here the front picks the Wednesday: no API change is wanted. It is framed as presentation, the way a
  picked week becomes the date the contract carries. The API neither knows nor enforces it, so other clients may
  store any day. Every stored date still renders as its week, so this is harmless. If one rule is ever needed, it
  moves to the API's `Workout` domain.
- No business rule is added. Structural validation stays in `core` (`validateWorkoutDetailsForm`,
  `validateCreateWorkoutForm`), unchanged. It still checks a calendar date, now always the selected Monday.
- Week arithmetic is done on the `YYYY-MM-DD` parts in UTC, so the browser's time zone never moves a date into another
  week.

## Acceptance Criteria

- The delta specs' scenarios pass:
  - in `week.test.ts`, the workout view test, and the create-dialog and assigned-plans tests;
  - with the use cases faked through the container, and fake timers where "the current week" or "the current year"
    matters.
- `workout.spec.ts`:
  - creates a workout through the week field;
  - moves a week on the workout screen and asserts the `PUT` body carries the Wednesday.
- `pnpm typecheck && pnpm lint && pnpm test && pnpm test:e2e` pass from `front/`.

## Impact

- `backoffice/src/presentation/workouts/`: the new `week.ts`, `hooks/use-week-label.ts` and
  `components/molecules/week-field.tsx`; the changed `workout-details-card.tsx`, `create-workout-dialog.tsx`,
  `workouts-table.tsx` and `views/workout-view.tsx`.
- `backoffice/src/i18n/{es,en}/workouts.json`: new `workout.week.*` keys, and the relabelled `createWorkout.fields.date`
  and `assignedPlans.columns.date`.
- Tests under `backoffice/test` and `backoffice/e2e`.
- `core`, `app`, API: none.
