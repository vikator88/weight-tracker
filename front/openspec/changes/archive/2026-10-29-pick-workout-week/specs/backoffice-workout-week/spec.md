# Spec Delta

## Purpose

Lets trainers and admins pick a workout's date as a Monday-to-Sunday week and see any workout date as that week, the
same way on every backoffice screen.

## ADDED Requirements

### Requirement: A date is shown as its week

Wherever the backoffice shows a workout's date, it SHALL show the Monday-to-Sunday week that contains it, as "Semana
del <Monday> – <Sunday>". Each day SHALL be its day number and short month name in the active language. Any stored day
of the week SHALL give the same week, and the browser's time zone SHALL NOT move a date into another week.

#### Scenario: Week label in Spanish

- **WHEN** a workout dated 2026-10-28 is shown in Spanish, in 2026
- **THEN** it reads "Semana del 26 oct – 1 nov"

#### Scenario: Any day of the week

- **WHEN** workouts dated 2026-10-26 (Monday), 2026-10-28 (Wednesday) and 2026-11-01 (Sunday) are shown
- **THEN** all three read "Semana del 26 oct – 1 nov"

#### Scenario: Time zone behind UTC

- **WHEN** a workout dated 2026-10-26 is shown in a time zone behind UTC
- **THEN** it reads "Semana del 26 oct – 1 nov", not the week before

### Requirement: The year is shown only when it is needed

The week label SHALL leave the year out when the whole week falls in the current year. It SHALL show the year on both
days when the week is in another year or spans two years.

#### Scenario: Current year

- **WHEN** the current year is 2026 and a workout dated 2026-10-28 is shown
- **THEN** it reads "Semana del 26 oct – 1 nov"

#### Scenario: Week spanning two years

- **WHEN** the current year is 2026 and a workout dated 2026-12-30 is shown
- **THEN** it reads "Semana del 28 dic 2026 – 3 ene 2027"

#### Scenario: Another year

- **WHEN** the current year is 2026 and a workout dated 2025-10-29 is shown
- **THEN** it reads "Semana del 27 oct 2025 – 2 nov 2025"

### Requirement: The week field shows and steps through weeks

The week field SHALL be labelled "Semana" and SHALL show the selected week's label. Its "Semana anterior" button SHALL
select the previous week and its "Semana siguiente" button the next one, without sending any request. While its form is
saving, the field SHALL be disabled.

#### Scenario: Field shown

- **WHEN** the field holds the week of 2026-10-28
- **THEN** it shows the label "Semana", the text "Semana del 26 oct – 1 nov" and the buttons "Semana anterior",
  "Semana siguiente" and "Elegir semana"

#### Scenario: Previous week

- **WHEN** the field shows "Semana del 26 oct – 1 nov" and the user clicks "Semana anterior"
- **THEN** it shows "Semana del 19 oct – 25 oct" and no request is sent

#### Scenario: Next week across a month

- **WHEN** the field shows "Semana del 26 oct – 1 nov" and the user clicks "Semana siguiente"
- **THEN** it shows "Semana del 2 nov – 8 nov"

### Requirement: The calendar picks the week of a day

The week field's "Elegir semana" button SHALL open the browser's date picker. Picking any day SHALL select the week that
contains it.

#### Scenario: Day picked

- **WHEN** the user clicks "Elegir semana" and picks Friday 2026-11-13
- **THEN** the field shows "Semana del 9 nov – 15 nov"

### Requirement: A week is sent as its Wednesday

When a form with the week field is sent, the workout's date SHALL be the Wednesday of the selected week, as
`YYYY-MM-DD`.

#### Scenario: Week sent

- **WHEN** a form whose week field shows "Semana del 26 oct – 1 nov" is sent
- **THEN** the request carries `workoutDate` "2026-10-28"

#### Scenario: Week spanning two years sent

- **WHEN** a form whose week field shows "Semana del 28 dic 2026 – 3 ene 2027" is sent
- **THEN** the request carries `workoutDate` "2026-12-30"

### Requirement: Week texts are translated

The field's label, its buttons and the week label SHALL be available in Spanish and English and SHALL follow the active
language.

#### Scenario: Week in English

- **WHEN** the active language is English and the field holds the week of 2026-10-28, in 2026
- **THEN** it shows "Week", "Week of Oct 26 – Nov 1", "Previous week", "Next week" and "Pick a week"
