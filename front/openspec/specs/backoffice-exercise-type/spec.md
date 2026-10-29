# backoffice-exercise-type Specification

## Purpose

Defines how the backoffice presents an exercise's type (strength, cardio or stretching): its name, its colour, the
icon shown in the background of exercise cards, and the effort scale that type is prescribed in.

## Requirements

### Requirement: Each exercise type has a name and a colour

The backoffice SHALL name the types "Fuerza" (`STRENGTH`), "Cardio" (`CARDIO`) and "Estiramiento" (`STRETCHING`). It
SHALL give each a fixed colour, the same everywhere on the screen: blue for strength, red for cardio and green for
stretching. An unknown type SHALL be named as it comes and use the neutral colour.

#### Scenario: Known types

- **WHEN** the types are shown in Spanish
- **THEN** `STRENGTH` reads "Fuerza" in blue, `CARDIO` reads "Cardio" in red and `STRETCHING` reads "Estiramiento"
  in green

#### Scenario: Unknown type

- **WHEN** an exercise's type is `YOGA`
- **THEN** it is named "YOGA" and uses the neutral colour

### Requirement: Exercise cards show their type's icon in the background

Each exercise card in the catalog dialog and in the workout list SHALL show its type's icon behind its content, in the
type's pastel tone: a dumbbell for strength, a heart for cardio and a stretching pose for stretching. The icon SHALL
NOT move or cover the card's content. A card with an unknown type, or a workout exercise missing from the catalog,
SHALL show no icon; nor SHALL the configuration dialog.

#### Scenario: Catalog cards show their type's icon

- **WHEN** the catalog lists a strength, a cardio and a stretching exercise
- **THEN** their cards show a dumbbell, a heart and a stretching pose, in pastel blue, pastel red and pastel green

#### Scenario: Workout card shows its catalog type's icon

- **WHEN** the workout lists an exercise whose catalog entry is a cardio exercise
- **THEN** its card shows the heart in pastel red

#### Scenario: Card without icon

- **WHEN** a listed exercise has an unknown type, or is missing from the catalog
- **THEN** its card shows no type icon

#### Scenario: Content unaffected

- **WHEN** a card shows its type's icon
- **THEN** the icon sits in the card's bottom-right corner, about 75% of the card's height with its aspect ratio kept
- **AND** in the catalog dialog, it keeps clear of the "Elegir ejercicio" button on the card's right
- **AND** it is behind the content, does not take part in the layout and does not react to the pointer

### Requirement: Effort reads in the exercise type's scale

The five effort levels, from `LOW` to `HIGH`, SHALL read in the scale of the exercise's type, both in the
configuration dialog's effort selector and in the workout list's "Esfuerzo: …" line. An exercise with no known type
SHALL keep the generic labels "Bajo", "Medio-bajo", "Medio", "Medio-alto" and "Alto". The values sent stay `LOW` …
`HIGH`.

#### Scenario: Strength

- **WHEN** the effort selector is open for a strength exercise
- **THEN** it offers "RIR 5+", "RIR 4", "RIR 3", "RIR 1-2" and "RIR 0", in that order

#### Scenario: Cardio

- **WHEN** the effort selector is open for a cardio exercise
- **THEN** it offers "RPE 1-2", "RPE 3-4", "RPE 5-6", "RPE 7-8" and "RPE 9-10", in that order

#### Scenario: Stretching

- **WHEN** the effort selector is open for a stretching exercise
- **THEN** it offers "Muy suave", "Suave", "Moderado", "Intenso" and "Muy intenso", in that order

#### Scenario: Effort line in the workout list

- **WHEN** a strength exercise with effort `MID` is listed in the workout
- **THEN** its card shows "Esfuerzo: RIR 3"

#### Scenario: Value sent unchanged

- **WHEN** the user chooses "RIR 0" for a strength exercise and saves it
- **THEN** the request carries effort `HIGH`

#### Scenario: Unknown type keeps the generic labels

- **WHEN** the effort selector is open for an exercise with an unknown type
- **THEN** it offers "Bajo", "Medio-bajo", "Medio", "Medio-alto" and "Alto"

### Requirement: Exercise types are translated

The type names and the effort labels SHALL be available in Spanish and English and SHALL follow the active language.

#### Scenario: Types and stretching effort in English

- **WHEN** the active language is English
- **THEN** the types read "Strength", "Cardio" and "Stretching"
- **AND** a stretching exercise's effort selector offers "Very light", "Light", "Moderate", "Intense" and "Very intense"

#### Scenario: RIR and RPE in English

- **WHEN** the active language is English
- **THEN** a strength exercise's effort selector offers "RIR 5+" … "RIR 0" and a cardio exercise's "RPE 1-2" …
  "RPE 9-10", as in Spanish
