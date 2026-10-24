# backoffice-onboarding Specification

## Purpose

Makes a signed-in backoffice user complete a step-by-step onboarding on the home screen before using anything else,
starting with a step that asks for their name and surname.

## Requirements

### Requirement: Home shows a skeleton until the profile is known and complete

While the profile is loading, after it failed to load, or while onboarding is pending, `/` SHALL show placeholder
skeleton sections instead of the regular home content. Once onboarding is not pending, it SHALL show the regular home
content.

#### Scenario: Profile loading

- **WHEN** a signed-in user opens `/` and the profile has not loaded yet
- **THEN** the skeleton sections show and the regular home content is not shown

#### Scenario: Onboarding complete

- **WHEN** the profile loads with name "Ana" and surname "García"
- **THEN** the regular home content shows and no skeleton or onboarding dialog is shown

### Requirement: Pending onboarding shows a blocking dialog over the skeleton

While onboarding is pending, `/` SHALL show the onboarding dialog over the skeleton. The dialog SHALL have no close
control, and pressing Escape or clicking outside it SHALL NOT close it. The header and the drawer SHALL NOT be usable
while it is open.

#### Scenario: Empty name

- **WHEN** the profile loads with an empty name
- **THEN** the onboarding dialog shows over the skeleton

#### Scenario: Trying to dismiss

- **WHEN** the dialog is open and the user presses Escape or clicks the backdrop
- **THEN** the dialog stays open

### Requirement: The dialog shows the step position and step-dependent actions

The dialog title SHALL read "Paso <current> de <total> · <step title>". Its bottom SHALL show "Atrás" on every step
but the first, "Siguiente" on every step but the last, and "Finalizar" on the last step only.

#### Scenario: Single step in Spanish

- **WHEN** the onboarding has only the name step
- **THEN** the title reads "Paso 1 de 1 · Dinos cómo te llamas"
- **AND** only "Finalizar" shows; "Atrás" and "Siguiente" are not shown

#### Scenario: Moving between steps

- **WHEN** the onboarding has two steps and the user presses "Siguiente" on a valid first step
- **THEN** the title reads "Paso 2 de 2 · …" and "Atrás" and "Finalizar" show
- **WHEN** the user presses "Atrás"
- **THEN** the first step shows again with the values the user had typed

#### Scenario: Invalid step blocks Siguiente

- **WHEN** the user presses "Siguiente" with an invalid field
- **THEN** the step does not change and the field shows its error

### Requirement: The name step asks for name and surname

The first step, titled "Dinos cómo te llamas", SHALL ask for "Nombre" and "Apellidos". Each field SHALL be validated
with the shared name rules (3 to 100 characters after trimming) and show its error under the field.

#### Scenario: Field too short

- **WHEN** the user enters "Al" as name and presses "Finalizar"
- **THEN** "Nombre" shows "Debe tener al menos 3 caracteres." and no update request is sent

#### Scenario: Field empty

- **WHEN** the user leaves "Apellidos" empty and presses "Finalizar"
- **THEN** "Apellidos" shows "Este campo es obligatorio." and no update request is sent

#### Scenario: Field too long

- **WHEN** the user enters 101 characters as surname and presses "Finalizar"
- **THEN** "Apellidos" shows "No puede superar los 100 caracteres." and no update request is sent

### Requirement: Finalizar saves the collected values and ends onboarding

Pressing "Finalizar" on a valid last step SHALL send the trimmed name and surname as a profile update, with
"Finalizar" disabled while it is pending. On success the dialog SHALL close and the regular home content SHALL show.
On failure the dialog SHALL stay open with the translated message for the failure code.

#### Scenario: Onboarding completed

- **WHEN** the user enters " Ana " and "García" and presses "Finalizar" and the update succeeds
- **THEN** the update is sent with name "Ana" and surname "García"
- **AND** the dialog closes, the regular home content shows and the header shows "Ana García"

#### Scenario: Update fails

- **WHEN** the update fails with `NETWORK`
- **THEN** the dialog stays open, keeps the values typed and shows the `NETWORK` message

### Requirement: Other screens redirect to home while onboarding is pending

While onboarding is pending, opening any signed-in screen other than `/` SHALL replace the location with `/`.

#### Scenario: Typed profile URL

- **WHEN** a user in onboarding opens `/profile`
- **THEN** they are sent to `/` and the onboarding dialog shows
