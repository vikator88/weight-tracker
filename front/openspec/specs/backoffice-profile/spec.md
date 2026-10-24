# backoffice-profile Specification

## Purpose

Lets a signed-in backoffice user view and change their own name and surname on an Edit Profile screen.

## Requirements

### Requirement: The profile screen shows the user's current name

`/profile` SHALL show the title "Mi perfil" and the fields "Nombre" and "Apellidos" prefilled with the shared
profile's name and surname, and a "Guardar" button.

#### Scenario: Profile screen opened

- **WHEN** a user whose profile is "Ana" "García" opens `/profile`
- **THEN** "Nombre" holds "Ana", "Apellidos" holds "García" and "Guardar" shows

### Requirement: The profile form validates with the shared name rules

Each field SHALL be validated with the shared name rules (3 to 100 characters after trimming) when "Guardar" is
pressed, and an invalid field SHALL show its error under it without sending any request.

#### Scenario: Name too short

- **WHEN** the user changes "Nombre" to "Al" and presses "Guardar"
- **THEN** "Nombre" shows "Debe tener al menos 3 caracteres." and no update request is sent

### Requirement: Saving updates the profile in place

Pressing "Guardar" on a valid form SHALL send the trimmed values as a profile update, with "Guardar" disabled while it
is pending. On success the screen SHALL stay, show "Perfil actualizado." and the header SHALL show the new name. The
message SHALL disappear when a field changes.

#### Scenario: Name saved

- **WHEN** the user changes "Nombre" to " Lucía " and presses "Guardar" and the update succeeds
- **THEN** the update is sent with name "Lucía" and the current surname
- **AND** the screen shows "Perfil actualizado." and the header shows "Lucía García"

#### Scenario: Editing after saving

- **WHEN** "Perfil actualizado." shows and the user types in a field
- **THEN** the message disappears

### Requirement: A failed save keeps the values and shows the error

When the update fails, the screen SHALL stay, keep the values typed and show the translated message for the failure
code.

#### Scenario: API rejects the values

- **WHEN** the update fails with `VALIDATION`
- **THEN** the screen shows the `VALIDATION` message and the fields keep the values typed

#### Scenario: No response

- **WHEN** the update fails with `NETWORK`
- **THEN** the screen shows the `NETWORK` message
