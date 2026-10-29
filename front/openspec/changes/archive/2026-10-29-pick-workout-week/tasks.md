# Tasks

No `core`, contract or wiring work: no new use case, `Dependencies` key or `container.ts` change.

## 1. Week helpers

- [x] 1.1 Create `backoffice/src/presentation/workouts/week.ts` with `weekStartOf`, `weekMiddleOf`, `weekEndOf`, `shiftWeek` and `currentWeekStart`, using UTC arithmetic on `YYYY-MM-DD` strings (`currentWeekStart` reads today's local date). Verify with `pnpm --filter backoffice typecheck`.
- [x] 1.2 Add `backoffice/test/presentation/workouts/week.test.ts`. Cover:
  - a Sunday and a Wednesday map to their Monday;
  - `weekMiddleOf` and `weekEndOf`;
  - month and year crossings (`2027-01-03` → `2026-12-28` → `2026-12-30`);
  - `shiftWeek` ±1;
  - `currentWeekStart` under a faked `Date`.

  Use AAA with one Act each. Verify with `pnpm --filter backoffice test`.

## 2. Copy and week label

- [x] 2.1 Add `workout.week.label`, `.previous`, `.next` and `.pick` to `backoffice/src/i18n/{es,en}/workouts.json`, and relabel `createWorkout.fields.date` and `assignedPlans.columns.date` to "Semana" / "Week". Verify the translation parity test passes.
- [x] 2.2 Create `hooks/use-week-label.ts` (`weekLabelOf`: day and short month in UTC, the year only when either day is outside the current year), modelled on `use-exercise-labels.ts`. Verify with the backoffice typecheck.

## 3. Week field and its users

- [x] 3.1 Create the `WeekField` molecule (`components/molecules/week-field.tsx`). It holds:
  - a caption naming the group;
  - the ‹ and › `IconButton`s, calling `shiftWeek`;
  - the label, with `aria-live="polite"`;
  - the "Elegir semana" `IconButton`, calling `showPicker()` (falling back to focus and click) on a visually hidden native date input made with the `TextField` atom, whose change calls `weekStartOf`;
  - `disabled` and `error` props.

  Verify with the backoffice typecheck and lint (no MUI widget import outside atoms).

- [x] 3.2 Use `WeekField` in `WorkoutDetailsCard`. In `LoadedWorkout`, seed `workoutDate` with `weekStartOf(workout.workoutDate)` and send `weekMiddleOf` from `toUpdate`. Verify with the backoffice typecheck.
- [x] 3.3 Use `WeekField` in `CreateWorkoutDialog`, with `workoutDate` defaulting to `currentWeekStart()` when the form mounts, and send `weekMiddleOf` from `toRequest`. Verify with the backoffice typecheck.
- [x] 3.4 Replace `formatWorkoutDate` in `WorkoutsTable` with `useWeekLabel`. Verify with the backoffice typecheck and lint.

## 4. Component tests

- [x] 4.1 Add or extend tests for the new `backoffice-workout-week` scenarios: the label, any day of the week, a time zone behind UTC, the year rule (current, spanning, another year), ‹ and ›, the calendar change, the Wednesday sent, and English. Use the view and dialog tests for the field, and the assigned-plans test for the label. Fake only `Date` in each file's `beforeEach` / `afterEach`. Verify with `pnpm --filter backoffice test`.
- [x] 4.2 Update `create-workout-dialog.test.tsx` for the modified creation scenarios:
  - the dialog opens on the current week;
  - "required" no longer covers Semana;
  - creating after "Semana siguiente" sends `2026-11-04`;
  - the English labels.

  Verify with `pnpm --filter backoffice test`.

- [x] 4.3 Update `assigned-plans-view.test.tsx`: the row shows "Semana del 26 oct – 1 nov", a time zone behind UTC does not shift the week, and the "Week" header shows in English. Verify with `pnpm --filter backoffice test`.
- [x] 4.4 Update `workout-view.test.tsx`:
  - the card shows "Semana del 2 nov – 8 nov";
  - "Workout saved" sends `2026-11-04`;
  - a "Week changed" test sends `2026-10-28`;
  - the date field queries are replaced.

  Verify with `pnpm --filter backoffice test`.

## 5. E2E

- [x] 5.1 Update `backoffice/e2e/workout.spec.ts`:
  - the create flow keeps the default week and asserts the `POST` body's Wednesday, computed in the test from the current date;
  - add a flow that clicks "Semana anterior" on the workout screen and asserts the `PUT` body's date;
  - existing flows keep passing.

  Verify with `pnpm test:e2e`.

## 6. Review and verification

- [x] 6.1 Review the backoffice against its references:
  - `WeekField` against `label-filter.tsx`;
  - `useWeekLabel` against `use-exercise-labels.ts`;
  - `week.ts` against `isCalendarDate`'s UTC parsing;
  - the changed tests against their previous structure.

  Verify there are no differences beyond those in the design.

- [x] 6.2 Run the `.stpr/template/` drift check from `AGENTS.md`. Verify it prints no `DRIFT`.
- [x] 6.3 From `front/`, run `pnpm typecheck && pnpm lint && pnpm test && pnpm test:e2e`. Verify everything passes.

## Workflow follow-up

- Archive the change once it is reviewed and merged.
