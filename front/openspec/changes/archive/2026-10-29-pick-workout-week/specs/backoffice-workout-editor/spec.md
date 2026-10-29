# Spec Delta

## MODIFIED Requirements

### Requirement: Guardar checks the workout's data and sends nothing when invalid

On "Guardar", the screen SHALL check the card's fields structurally and, if any fails, SHALL show the message under
that field and SHALL NOT send any request. Nombre is required and at most 256 characters after trimming; Descripción
is optional and at most 1024 characters; Semana always holds a week, so it is never reported.

#### Scenario: Blank name

- **WHEN** the user clears Nombre and clicks "Guardar"
- **THEN** Nombre shows "Este campo es obligatorio." and no request is sent

#### Scenario: Description too long

- **WHEN** the description has 1025 characters and the user clicks "Guardar"
- **THEN** Descripción shows "No puede superar los 1024 caracteres." and no request is sent

### Requirement: Guardar replaces the workout's editable data

When the fields are valid, "Guardar" SHALL send exactly one update of the workout with its id, the trimmed name, the
Wednesday of the selected week as its date, the trimmed description or none when blank, and the athlete and trainer
exactly as they were read. While it is in progress, "Guardar" SHALL be disabled.

#### Scenario: Workout saved

- **WHEN** workout `w-1` dated 2026-11-02 is loaded, the user changes the name to " Pierna B " and clicks "Guardar"
- **THEN** exactly one update of workout `w-1` is sent with name "Pierna B", date "2026-11-04" (the Wednesday of the
  loaded week), description "Día 1", and the loaded athlete and trainer

#### Scenario: Week changed

- **WHEN** workout `w-1` dated 2026-11-02 is loaded, the user clicks "Semana anterior" and clicks "Guardar"
- **THEN** the update carries the date "2026-10-28"

#### Scenario: Description cleared

- **WHEN** the user empties Descripción and clicks "Guardar"
- **THEN** the update carries no description

#### Scenario: Save in progress

- **WHEN** the update has been sent and has not answered yet
- **THEN** "Guardar" is disabled

### Requirement: The properties card shows the workout's data

The "Propiedades del entrenamiento" card SHALL hold the workout's Nombre and Descripción in editable fields and its
week in the week field Semana, filled with the loaded values, and no exercises. Editing them SHALL change nothing until
"Guardar" is pressed.

#### Scenario: Card filled

- **WHEN** workout "Pierna" dated 2026-11-02 with description "Día 1" is loaded, in 2026
- **THEN** the card's fields hold "Pierna" and "Día 1", and the week field shows "Semana del 2 nov – 8 nov"
- **AND** the card lists no exercises
