# backoffice-workout-creation Specification

## Purpose

Lets trainers and admins create a workout for an athlete from the backoffice and continue straight to that workout's
screen.

## Requirements

### Requirement: The dialog asks for the workout's data

The create workout dialog SHALL be titled "Nuevo entrenamiento" and SHALL ask for Nombre, Descripción, Semana and ID
del atleta, with "Cancelar" and "Crear" actions. Semana SHALL be the week field and SHALL start on the current week;
every other field SHALL start empty.

#### Scenario: Trainer opens the dialog

- **WHEN** a trainer opens the dialog during the week of 26 Oct – 1 Nov 2026
- **THEN** the fields Nombre, Descripción and ID del atleta are shown empty
- **AND** the week field Semana shows "Semana del 26 oct – 1 nov"
- **AND** the actions "Cancelar" and "Crear" are shown

### Requirement: Only admins are asked for a trainer

For an `ADMIN`, the dialog SHALL also ask for "ID del entrenador". For a `TRAINER`, that field SHALL NOT be rendered and
the request SHALL NOT carry a trainer id.

#### Scenario: Admin sees the trainer field

- **WHEN** an admin opens the dialog
- **THEN** the field "ID del entrenador" is shown

#### Scenario: Trainer does not see the trainer field

- **WHEN** a trainer opens the dialog
- **THEN** no "ID del entrenador" field is shown

### Requirement: Invalid fields are reported and nothing is sent

On "Crear", the dialog SHALL check each field structurally and, if any fails, SHALL show the message under that field
and SHALL NOT send any request. Nombre is required and at most 256 characters after trimming; Descripción is optional
and at most 1024 characters; Semana always holds a week, so it is never reported; ID del atleta, and ID del entrenador
for an admin, are required UUIDs.

#### Scenario: Empty required fields

- **WHEN** a trainer clicks "Crear" with every text field empty
- **THEN** Nombre and ID del atleta each show "Este campo es obligatorio."
- **AND** Descripción and Semana show no message
- **AND** no request is sent

#### Scenario: Blank name

- **WHEN** the name holds only spaces
- **THEN** Nombre shows "Este campo es obligatorio." and no request is sent

#### Scenario: Name too long

- **WHEN** the name has 257 characters after trimming
- **THEN** Nombre shows "No puede superar los 256 caracteres." and no request is sent

#### Scenario: Description too long

- **WHEN** the description has 1025 characters
- **THEN** Descripción shows "No puede superar los 1024 caracteres." and no request is sent

#### Scenario: Athlete id is not a UUID

- **WHEN** ID del atleta is "ana"
- **THEN** ID del atleta shows "Introduce un identificador válido." and no request is sent

#### Scenario: Admin without trainer

- **WHEN** an admin fills every field except ID del entrenador and clicks "Crear"
- **THEN** ID del entrenador shows "Este campo es obligatorio." and no request is sent

### Requirement: Creating sends the workout once

When every field is valid, "Crear" SHALL send exactly one workout creation with the trimmed name, the Wednesday of the
selected week as `YYYY-MM-DD`, the athlete id, the description only when it is not blank, and the trainer id only for
an admin. While it is in progress, "Crear" SHALL be disabled.

#### Scenario: Trainer creates a workout

- **WHEN** a trainer enters " Pierna ", no description and a valid athlete id, moves Semana to "Semana del 2 nov – 8
  nov" and clicks "Crear"
- **THEN** exactly one creation is sent with name "Pierna", date "2026-11-04" and that athlete id
- **AND** it carries neither a description nor a trainer id

#### Scenario: Admin creates a workout

- **WHEN** an admin also enters the description "Día 1" and a valid trainer id and clicks "Crear"
- **THEN** the creation carries that description and that trainer id

#### Scenario: Create in progress

- **WHEN** the creation has been sent and has not answered yet
- **THEN** "Crear" is disabled

### Requirement: A created workout opens its screen

When the creation succeeds, the dialog SHALL close and the user SHALL end on `/workout?id=<id of the created workout>`.

#### Scenario: Navigate after creating

- **WHEN** the API answers with the created workout whose id is `w-1`
- **THEN** the dialog is closed
- **AND** the user ends on `/workout?id=w-1`

### Requirement: A failed creation keeps the dialog

When the creation fails, the dialog SHALL stay open with the typed values and SHALL show the message for the error code
above the fields. The user SHALL be able to correct the data and try again.

#### Scenario: Unknown athlete

- **WHEN** the API answers 404
- **THEN** the dialog stays open showing "No hemos encontrado lo que buscas."
- **AND** the typed values are kept

#### Scenario: Rejected data

- **WHEN** the API answers 400
- **THEN** the dialog stays open showing "Revisa los datos introducidos."

### Requirement: Cancel closes without creating

"Cancelar", Escape and a click outside the dialog SHALL close it without sending any request. Opening it again SHALL
show empty fields and no error.

#### Scenario: Cancel

- **WHEN** a trainer types a name and clicks "Cancelar"
- **THEN** the dialog is closed and no request is sent
- **AND** opening the dialog again shows an empty Nombre

### Requirement: The dialog is translated

Every text of the dialog SHALL be available in Spanish and English and SHALL follow the active language.

#### Scenario: Dialog in English

- **WHEN** the active language is English and an admin opens the dialog
- **THEN** it shows "New workout", "Name", "Description", "Week", "Athlete ID", "Trainer ID", "Cancel" and "Create"
