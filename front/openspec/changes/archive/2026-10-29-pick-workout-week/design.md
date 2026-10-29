# Design

## Context

See `proposal.md` for the motivation and `specs/` for the behaviour. This is the current state that shapes the
approach:

- **Where the date is edited.** `WorkoutDetailsCard` and `CreateWorkoutDialog` edit it with the `TextField` atom
  (`type="date"`), labelled by `createWorkout.fields.date` ("Fecha"). Both forms keep `workoutDate` as a `YYYY-MM-DD`
  string.
- **Where requests are built.** `toUpdate` in `views/workout-view.tsx` and `toRequest` in `create-workout-dialog.tsx`
  build the requests, passing `values.workoutDate` through unchanged.
- **Validation.** `validateWorkoutDetailsForm` and `validateCreateWorkoutForm` in `core` check that `workoutDate` is
  present and is a calendar date (`isCalendarDate`, which parses the parts in UTC).
- **Where the date is shown.** `WorkoutsTable` formats it with a local `formatWorkoutDate`. That function builds a
  local `Date` from the parts and uses `Intl` `dateStyle: "medium"`.
- **Existing tests.** `create-workout-dialog.test.tsx` and `assigned-plans-view.test.tsx` use fixed dates
  (`2026-11-02`, `2026-10-27`). The dialog test fills "Fecha" by label. `workout.spec.ts` fills "Fecha" in its create
  flow.
- **No `core` or wiring change.** No new `Dependencies` key, no `container.ts` change, no new error code.

## Goals / Non-Goals

**Goals:** one week field and one week label used by every backoffice screen; week arithmetic that no time zone can
skew; the form keeps the value the stepper works on (the Monday), and only requests carry the Wednesday.

**Non-Goals:** a week-highlighting calendar; any app UI; changing the `core` validation; migrating stored dates.

## Decisions

### Week arithmetic on `YYYY-MM-DD` strings, in UTC

`backoffice/src/presentation/workouts/week.ts` is a presentation helper file: a family of related pure exports, the
shape the invariants allow (like `result.ts`).

- `weekStartOf(date: string): string` returns the Monday as `YYYY-MM-DD`. It parses the parts into
  `Date.UTC(y, m - 1, d)`, subtracts `(getUTCDay() + 6) % 7` days, and prints the UTC parts back with zero padding.
- `weekMiddleOf(monday: string): string` returns the Monday plus 2 days (the Wednesday).
- `shiftWeek(monday: string, weeks: number): string` returns the Monday plus `7 × weeks` days.
- `weekEndOf(monday: string): string` returns the Monday plus 6 days (the Sunday), for the label.
- `currentWeekStart(): string` is `weekStartOf` of today's local date. Its local `YYYY-MM-DD` is built from
  `getFullYear()`, `getMonth()` and `getDate()`, because "this week" is the user's week.

Strings in and out, never `Date` objects: the forms already hold strings, and `core` validates strings. UTC arithmetic
makes the result independent of the browser's time zone (spec: "Time zone behind UTC").

_Alternative_: a date library (date-fns or dayjs). Rejected: five short functions don't justify a dependency, and the
brief adds none.

### `useWeekLabel` turns a date into the week text

`hooks/use-week-label.ts` returns `weekLabelOf(date: string) => string`, memoised on the language, in the style of
`useExerciseLabels`.

- It computes `monday = weekStartOf(date)` and `sunday = weekEndOf(monday)`, and formats each with
  `Intl.DateTimeFormat(language, { day: "numeric", month: "short", timeZone: "UTC" })` on its UTC `Date`.
- When the Monday's or the Sunday's year differs from the current local year, both days get `year: "numeric"`.
- It returns `t("workout.week.label", { start, end })`.
- `WorkoutsTable` drops `formatWorkoutDate` and uses this hook. The `timeZone: "UTC"` option replaces the local-`Date`
  trick, with the same effect.

### `WeekField` molecule

`components/molecules/week-field.tsx` is built only from atoms and imports no MUI widget directly. Like
`LabelFilter`, it uses `Box` for layout.

- **Props**: `value` (a Monday), `onChange(monday)`, `disabled?`, `error?` (message text).
- **Structure**: a `Box role="group"` labelled through `aria-labelledby` by a caption `Typography`
  (`createWorkout.fields.date`, now "Semana"), styled like `LabelFilter`'s caption. One row inside holds:
  - the `IconButton` "Semana anterior" (`ChevronLeft`), calling `onChange(shiftWeek(value, -1))`;
  - the label `Typography` (`weekLabelOf(value)`, `aria-live="polite"`, so a change is announced);
  - the `IconButton` "Semana siguiente" (`ChevronRight`), calling `shiftWeek(value, 1)`;
  - the `IconButton` "Elegir semana" (`CalendarMonthOutlined`).
- **Calendar**: a native `<input type="date">` rendered through the `TextField` atom. It is visually hidden with
  `position: absolute`, `opacity: 0`, `width: 1`, `height: 1` and `pointer-events: none`; it is not `display: none`,
  because `showPicker()` needs a rendered input. It has `tabIndex={-1}`, the accessible name "Elegir semana", and a
  `value` of the selected Monday.
  - The calendar button calls `inputRef.current?.showPicker()`. Where `showPicker` is missing, it falls back to
    `focus()` and `click()`.
  - The input's `onChange` calls `onChange(weekStartOf(event.target.value))` when the value is not empty.
- **Error**: the `error` text renders under the row in `error` colour. The forms can still produce it as a structural
  safety net, though the UI never does.

### Forms keep the Monday; requests carry the Wednesday

- **Properties card.** `LoadedWorkout` seeds `values.workoutDate` with `weekStartOf(workout.workoutDate)`.
  - `WorkoutDetailsCard` replaces its date `TextField` with `WeekField`, wired to `onChange("workoutDate", monday)`.
  - `toUpdate` sends `weekMiddleOf(values.workoutDate)`.
- **Create dialog.** `CreateWorkoutForm`'s `emptyValues.workoutDate` becomes `currentWeekStart()`, computed when the
  form mounts (the form already remounts on every opening).
  - The date `TextField` becomes `WeekField`.
  - `toRequest` sends `weekMiddleOf(values.workoutDate)`.
- **Validation.** `core` validation keeps running on the Monday string, unchanged.

### Copy (`backoffice/src/i18n/{es,en}/workouts.json`)

- `workout.week.label`: "Semana del {{start}} – {{end}}" / "Week of {{start}} – {{end}}"
- `workout.week.previous`: "Semana anterior" / "Previous week"
- `workout.week.next`: "Semana siguiente" / "Next week"
- `workout.week.pick`: "Elegir semana" / "Pick a week"
- `createWorkout.fields.date`: "Fecha" → "Semana" / "Date" → "Week". It is used by both forms.
- `assignedPlans.columns.date`: "Fecha" → "Semana" / "Date" → "Week"

Error codes: no new ones. Create and save failures show the existing `errors:` copy, as today. `UNAUTHORIZED` is
handled by `runWithSession`.

### Held state

- `LoadedWorkout.values.workoutDate`: the selected week's Monday. It is seeded from the loaded workout and changed by
  `WeekField`, and resets on screen exit.
- `CreateWorkoutForm.values.workoutDate`: the current week's Monday when the dialog opens. It is changed by
  `WeekField`, and resets when the dialog closes (the form unmounts).
- No new provider. `useWeekLabel` holds no state; it reads the language from i18next.

### Imports and levels

Imports flow organisms (`WorkoutDetailsCard`, `CreateWorkoutDialog`, `WorkoutsTable`) → molecule (`WeekField`) → atoms.
The hook and `week.ts` are imported by the molecule, the organisms and the view. Icons come from `@mui/icons-material`,
as elsewhere. `backoffice` and `app` share nothing new.

### Tests

- **`test/presentation/workouts/week.test.ts`** (new): pure function tests in AAA form, like the core rule tests.
  - Sunday → its Monday, Wednesday → its Monday;
  - month and year crossings (`2027-01-03` → `2026-12-28` → `2026-12-30`);
  - `shiftWeek` ±1;
  - `currentWeekStart` under a faked `Date`.
- **Fake `Date` only.** The component tests fake only `Date` (`vi.useFakeTimers({ toFake: ["Date"] })`, then
  `vi.setSystemTime`), so `userEvent` and promises keep running.
  - The fake is set in each test file's `beforeEach` / `afterEach`, the same way the view test keeps its local rect
    stub.
  - No shared helper changes.
- **Calendar in jsdom.** jsdom has no `showPicker`, so a test fires `change` on the hidden input, found as
  `getByLabelText("Elegir semana", { selector: "input" })`. Clicking the button is not exercised there.
- **Updated tests:**
  - `create-workout-dialog.test.tsx`: the empty-dialog test now expects the current week. The "required" test no
    longer expects Fecha. The create test moves one week with "Semana siguiente" and expects the Wednesday.
  - `assigned-plans-view.test.tsx`: rows and headers show week labels.
  - `workout-view.test.tsx`: the card's week field; "Workout saved" expects the loaded week's Wednesday, and a new
    "Week changed" test.
- **E2E** (`workout.spec.ts`):
  - the create flow uses the default week, and the test asserts the `POST` body's Wednesday computed from the
    browser's current week;
  - a new flow clicks "Semana anterior" on the workout screen and asserts the `PUT` body;
  - Playwright never fills the hidden input.

## Risks / Trade-offs

- [The Wednesday is chosen in the front, against the invariant that gives normalisation to the API] → Accepted by
  decision and recorded in the proposal. Every stored day still renders as its week, so other clients cannot break the
  display.
- [Saving an old workout moves its stored day to its week's Wednesday] → The week is unchanged, which is all the UI
  shows. It is called out as BREAKING in the proposal.
- [`showPicker()` support] → It exists in current Chrome, Edge, Firefox and Safari. The fallback (`focus` + `click`)
  covers others, and the ‹ › stepper always works.
- [An E2E that depends on "today"] → The create flow asserts the Wednesday computed from the browser's current date
  inside the test, not a hard-coded one.

## Implementation Notes

Found while applying:

- **`useWeekLabel`'s shape.** It returns the formatter function itself (`const weekLabelOf = useWeekLabel()`) rather
  than an object of formatters like `useExerciseLabels`. It has only one formatter.
- **The hidden calendar input** is sized `"1px"`: in MUI `sx`, a bare `1` means 100%. It carries `aria-hidden` and
  `tabIndex={-1}`, so assistive technology only meets the "Elegir semana" button.
- **Properties card layout.** Its grid's second column went from `11rem` to `auto`, so a label with years
  ("Semana del 28 dic 2026 – 3 ene 2027") and the three buttons fit beside Nombre.
- **E2E clock.** The E2E flows pin the browser clock with Playwright's `page.clock.setFixedTime` (Thursday 29 Oct
  2026), instead of computing the expected Wednesday from the machine's date. The labels and the sent dates are then
  fixed, and they don't change across years.
- **One extra test.** A dialog test checks the week field is disabled while the creation is in progress. That is
  required by "The week field shows and steps through weeks", but it has no scenario of its own.

## Migration Plan

No data or API migration. The change ships as a front deploy, and rolling it back reverts it. Workouts saved in the
meantime keep their Wednesday, which the old screen shows as a plain day.
