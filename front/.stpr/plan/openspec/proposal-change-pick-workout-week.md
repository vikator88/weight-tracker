# Change Proposal: Pick A Week For A Workout

Source brief for `/opsx:propose`. Artifacts go to `openspec/changes/pick-workout-week/`.

## Change

In the backoffice, a workout's date is picked and shown as a week (Monday to Sunday) instead of a day. Trainers step
between weeks or pick any day in a calendar, which selects its week. Wherever the date is shown, it reads "Semana del
26 oct – 1 nov". The front sends the Wednesday of the chosen week, the middle day, so no time-zone shift or week-start
convention can move it into a neighbouring week. The API stores it as it does any date: no API change.

## Objective

Trainers plan workouts per week, not per day. Asking for an exact day is noise, and showing one suggests a precision
the plan doesn't have.

## Capabilities

- **Week field**: a field labelled "Semana" shows the selected week as "Semana del 26 oct – 1 nov". It has three
  buttons:
  - "Semana anterior" (‹) and "Semana siguiente" (›) move one week;
  - "Elegir semana" (calendar) opens the browser's date picker; the week of the day picked becomes the selection.
- **Where it is used**: in the workout screen's properties card and in the create workout dialog, replacing the date
  field.
- **Default**: the create dialog starts on the current week.
- **What is sent**: saving or creating sends the Wednesday of the selected week as `workoutDate`.
- **Assigned plans table**: the "Fecha" column becomes "Semana" and shows the week label.
- **Week of any stored date**: the shown week is derived from whatever date the API returns, whether a Wednesday or an
  older arbitrary day.
- **Year**: shown only when the week is not in the current year, or spans two years: "Semana del 28 dic 2026 – 3 ene
  2027".

## Fronts And Roles

- backoffice -> lands here: yes -> roles: `ADMIN`, `TRAINER` -> both pick and see weeks the same way.
- app -> lands here: no -> roles: `USER` -> the app does not show or edit workout dates yet.

No role-dependent UI: both roles see the same field, label and column.

## Classification

- Week helpers (`weekStartOf`, `weekMiddleOf`, `shiftWeek`, `currentWeekStart`) -> presentation helper file ->
  `backoffice/src/presentation/workouts/week.ts`
- `useWeekLabel` (the week as text in the active language, with the year rule) -> state hook ->
  `backoffice/src/presentation/workouts/hooks/use-week-label.ts`
- `WeekField` -> molecule -> `backoffice/src/presentation/workouts/components/molecules/week-field.tsx`
- `WorkoutDetailsCard`, `CreateWorkoutDialog`, `WorkoutsTable` -> organisms (changed in place)
- Reused atoms: `IconButton`, `Typography` and `TextField` (the visually hidden native date input).
- Copy -> `backoffice/src/i18n/{es,en}/workouts.json`

No change in `core`: the contract, the use cases and `validateWorkoutDetailsForm` / `validateCreateWorkoutForm` stay.
They still check a calendar date, now always a Monday in the form.

## Canonical Template Mapping

- `WeekField` -> `backoffice/src/presentation/workouts/components/molecules/label-filter.tsx` -> the nearest molecule:
  a labelled group with a caption, built from atoms. The atomic references in `.stpr/template/README.md` are
  `pending`.
- `useWeekLabel` -> `backoffice/src/presentation/workouts/hooks/use-exercise-labels.ts` -> the hook that turns codes
  into text in the active language. Its date formatting comes from `formatWorkoutDate` in
  `components/organisms/workouts-table.tsx`, which already parses `YYYY-MM-DD` and uses `Intl`.
- `week.ts` -> `core/src/application/validation/workout-rules.ts` (`isCalendarDate`) -> the same UTC parsing of
  `YYYY-MM-DD`.
- Tests:
  - `backoffice/test/presentation/workouts/views/workout-view.test.tsx` and the existing create-dialog and
    assigned-plans tests (extended);
  - a new `backoffice/test/presentation/workouts/week.test.ts`, modelled on the closest pure-function test in the
    backoffice;
  - `backoffice/e2e/workout.spec.ts` (extended).

## Screens And Navigation

- backoffice -> `/workout?id=<id>` -> `WorkoutView` -> the properties card shows the week field -> Guardar sends the
  week's Wednesday, then the confirmation and failures as today.
- backoffice -> `/assigned-plans` -> `AssignedPlansView` -> the create dialog shows the week field on the current week ->
  Crear sends the Wednesday and lands on the new workout, as today. The table shows the "Semana" column.

## API Contract

- `POST /workouts` -> in `core/openapi/v1.json`: yes -> `CreateWorkoutRequest.workoutDate` (`format: date`), now sent
  as the week's Wednesday.
- `PUT /workouts/{id}` -> yes -> `UpdateWorkoutRequest.workoutDate`, same.
- `GET /workouts`, `GET /workouts/{id}` -> yes -> `workoutDate` (`format: date`), rendered as its week.

No contract change, no snapshot refresh and no API change. The API stores the date it receives (a `date` column), so
the Wednesday comes from the front.

## Held State

- `LoadedWorkout` form values -> `workoutDate` now holds the selected week's Monday, seeded with
  `weekStartOf(workout.workoutDate)` -> the week field -> screen exit (unchanged). The request carries
  `weekMiddleOf(monday)`, the Wednesday.
- `CreateWorkoutForm` values -> `workoutDate` seeded with `currentWeekStart()` -> the week field -> dialog close
  (unchanged). The request carries the Wednesday, as above.

## Forms And Structural Validation

- Properties form and create form -> `workoutDate` -> still a required calendar date, checked by the existing core
  rules. The week field always holds a valid Monday, so the "required" and "date" messages can no longer appear from
  the UI. They stay as a structural safety net.

## Errors And Copy

- No new failure and no new error code. Save and create failures show as today.
- New copy (`workouts` namespace):
  - `workout.week.label` -> "Semana del {{start}} – {{end}}" -> "Week of {{start}} – {{end}}"
  - `workout.week.previous` -> "Semana anterior" -> "Previous week"
  - `workout.week.next` -> "Semana siguiente" -> "Next week"
  - `workout.week.pick` -> "Elegir semana" -> "Pick a week"
  - the field label, `createWorkout.fields.date` -> "Semana" -> "Week" (was "Fecha" / "Date")
  - the table column, `assignedPlans.columns.date` -> "Semana" -> "Week"
- Day and month come from `Intl.DateTimeFormat(language, { day: "numeric", month: "short", timeZone: "UTC" })`, with
  `year: "numeric"` added per the year rule: es "26 oct", en "Oct 26".

## Platform Capabilities And Dependencies

None. The calendar is the browser's own date picker, opened with `HTMLInputElement.showPicker()` on a visually hidden
`<input type="date">`. No date library or MUI X picker is added.

## Out Of Scope

- The app: it shows no workout dates yet.
- Migrating stored dates. Old workouts keep their day and render as their week.
- A calendar that highlights whole weeks (MUI X). The stepper and native calendar were chosen instead.
- Filtering or grouping the assigned plans by week.
- Any change to the API contract, `core` or the HTTP error mapper.

## Notes

- Choosing the Wednesday is done in the front, by decision (no API change). This conflicts with the invariant that
  any normalisation beyond trimming is the API's. Here it is framed as presentation: it is how a picked week becomes
  the date the contract carries. The API neither enforces nor knows about it, so other clients (the future app, direct
  API calls) may store any day. The front renders any stored date as its week, so this is harmless. If a single rule
  is ever needed, it moves to the API's `Workout` domain.
- `weekMiddleOf(monday)` is the Monday plus two days. It is used only where requests are built (`toUpdate` in
  `workout-view.tsx` and `toRequest` in `create-workout-dialog.tsx`), so the form keeps the Monday the stepper works on.
- Weeks are Monday to Sunday. `weekStartOf(d)` is `d` minus `(dayOfWeek + 6) % 7` days, computed in UTC on the
  `YYYY-MM-DD` parts, so the result never depends on the browser's time zone.
- Archive `redesign-backoffice-workout-screen` before proposing this change. Several requirements it adds (the
  properties card, the configuration dialog) are modified here.
- Spec deltas:
  - `backoffice-workout-editor`: the properties card shows the week and saving sends its Wednesday.
  - `backoffice-workout-creation`: the dialog defaults to the current week and creates with its Wednesday.
  - `backoffice-assigned-plans`: the column shows the week label.
- Verification tiers:
  - backoffice unit (`week.ts`): a Sunday maps to its Monday, `weekMiddleOf` gives the Wednesday, weeks cross months
    and years (Sunday 2027-01-03 → Monday 2026-12-28 → Wednesday 2026-12-30), and `shiftWeek` moves by
    whole weeks;
  - backoffice component tests, with use cases faked through the container:
    - the card shows "Semana del 2 nov – 8 nov" for 2026-11-04;
    - ‹ and › change the label, and Guardar after one ‹ sends `workoutDate: "2026-10-28"`;
    - the create dialog defaults to the current week (fake timers) and sends its Wednesday;
    - the table shows week labels, with the year only for other years;
  - backoffice E2E (`workout.spec.ts`, API intercepted with `page.route`):
    - the create flow uses the week field instead of filling "Fecha";
    - a flow on the workout screen moves one week and asserts that the `PUT` body carries the Wednesday.
- No deviations from the mapped references are allowed.
