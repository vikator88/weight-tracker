# Spec Delta

## MODIFIED Requirements

### Requirement: Each row shows the workout's name, date, athlete and exercise count

Each row SHALL show the workout's name, its date as the label of the week that contains it (under the "Semana"
column), the athlete's id and the number of exercises in the workout.

#### Scenario: Row content in Spanish

- **WHEN** the API returns a workout named "Pierna" dated 2026-10-27 for athlete id `u-1` with three exercises, in 2026
- **THEN** its row shows "Pierna", "Semana del 26 oct – 1 nov", `u-1` and `3`

#### Scenario: Date does not shift with the time zone

- **WHEN** a workout dated 2026-10-26 is shown in a time zone behind UTC
- **THEN** its row shows "Semana del 26 oct – 1 nov", not the week before

#### Scenario: Workout without exercises

- **WHEN** a workout has no exercises
- **THEN** its exercise count shows `0`

### Requirement: The screen is translated

Every text on the screen SHALL be available in Spanish and English and SHALL follow the active language.

#### Scenario: Screen in English

- **WHEN** the active language is English
- **THEN** the screen shows "Assigned plans", "New workout" and the column headers "Name", "Week", "Athlete" and
  "Exercises"
