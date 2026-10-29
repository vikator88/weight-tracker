# Spec Delta

## MODIFIED Requirements

### Requirement: The screen lists every user the API returns

On opening, the screen SHALL request the users once and SHALL show one table row per active user, in the order the API
returns them, until the visitor searches, picks a profile or shows inactive users. Inactive users SHALL be hidden until
the visitor turns on "Mostrar inactivos".

#### Scenario: Users shown

- **WHEN** the screen opens and the API returns three active users
- **THEN** the table shows three rows in that order

#### Scenario: Inactive users hidden on load

- **WHEN** the screen opens and the API returns two active users and one inactive user
- **THEN** the table shows only the two active users
- **AND** the "Mostrar inactivos" switch is off

### Requirement: The screen shows loading, empty, no-match and failure states

While the users load, the screen SHALL show a loading placeholder instead of the table. An empty list SHALL show
"Todavía no hay usuarios.". Filters that match nothing, including the hidden inactive users, SHALL show "Ningún usuario
coincide con la búsqueda.". A failed load SHALL show the error for its code with a retry action.

#### Scenario: Empty list

- **WHEN** the API returns no users
- **THEN** "Todavía no hay usuarios." is shown instead of rows

#### Scenario: No match

- **WHEN** users are loaded and the search matches none
- **THEN** "Ningún usuario coincide con la búsqueda." is shown instead of rows

#### Scenario: Only inactive users

- **WHEN** the API returns only inactive users and "Mostrar inactivos" is off
- **THEN** "Ningún usuario coincide con la búsqueda." is shown instead of rows

#### Scenario: Load fails and is retried

- **WHEN** the listing fails
- **THEN** the error for its code is shown with a retry action
- **AND** activating retry requests the users again

#### Scenario: Trainer opens the URL directly

- **WHEN** a trainer opens `/users` and the listing fails with `UNEXPECTED`
- **THEN** the generic error is shown with a retry action

### Requirement: The users screen is translated

Every text on the screen, in the modal and in the activation dialog SHALL be available in Spanish and English and
SHALL follow the active language.

#### Scenario: English

- **WHEN** the active language is English
- **THEN** the heading reads "All users" and the button reads "Add user"

#### Scenario: English activation copy

- **WHEN** the active language is English and an admin opens the deactivation of an active user
- **THEN** the switch reads "Show inactive", the menu item reads "Deactivate" and the confirm button reads "Deactivate"

## ADDED Requirements

### Requirement: A switch shows inactive users

The filter area SHALL show a "Mostrar inactivos" switch beside the profile pills. Turning it on SHALL add the inactive
users to the table in the API's order. Turning it off SHALL hide them again. The switch SHALL combine with the search
and the profile pills, and SHALL start off every time the screen opens.

#### Scenario: Switch turned on

- **WHEN** the API returns two active users and one inactive user and the visitor turns on "Mostrar inactivos"
- **THEN** the table shows all three users in the API's order

#### Scenario: Switch turned off again

- **WHEN** the switch is on and the visitor turns it off
- **THEN** the inactive user is no longer shown

#### Scenario: Switch combined with a pill

- **WHEN** the switch is on and the visitor selects the "Entrenador" pill
- **THEN** the table shows active and inactive trainers only

### Requirement: Inactive users are marked in the table

An inactive user's row SHALL be shown muted and SHALL show an "Inactivo" chip beside the full name. The chip's tooltip
SHALL read "Desactivado el <date>", with the deactivation date in the active language's short date format. Active rows
SHALL show no status marker.

#### Scenario: Inactive row

- **WHEN** the switch is on and a user deactivated on 20 October 2026 is listed
- **THEN** that row shows the "Inactivo" chip beside the full name
- **AND** hovering or focusing the chip shows "Desactivado el 20/10/2026"

#### Scenario: Active row

- **WHEN** an active user is listed
- **THEN** that row shows no "Inactivo" chip

### Requirement: Admins reach activation from each row

For an `ADMIN`, each row SHALL end with an actions button labelled "Acciones de <name>". It opens a menu that offers
"Desactivar" for an active user and "Activar" for an inactive one. The caller's own row SHALL have no actions button.
Any other role SHALL see no actions column, button or menu.

#### Scenario: Active user's menu

- **WHEN** an admin opens the actions of an active user
- **THEN** the menu offers "Desactivar" and not "Activar"

#### Scenario: Inactive user's menu

- **WHEN** the switch is on and an admin opens the actions of an inactive user
- **THEN** the menu offers "Activar" and not "Desactivar"

#### Scenario: Own row

- **WHEN** an admin views the table and their own user is listed
- **THEN** their own row has no actions button

#### Scenario: Trainer sees no actions

- **WHEN** a trainer's listing returns users
- **THEN** no actions column, actions button, menu or activation dialog is shown

### Requirement: Activation is confirmed before it is sent

Choosing "Desactivar" or "Activar" SHALL open a confirmation dialog naming the user (full name, or email when there is
none) and stating the effect, and SHALL send nothing until confirmed. Deactivation SHALL use a destructive confirm
button. "Cancelar" SHALL close the dialog without a request.

#### Scenario: Deactivation dialog

- **WHEN** an admin chooses "Desactivar" for "Ana García"
- **THEN** a dialog titled "¿Desactivar a Ana García?" explains that she will not be able to log in or be given new
  workouts and that her data is kept
- **AND** no request has been sent

#### Scenario: User without a name

- **WHEN** an admin chooses "Activar" for an inactive user with empty name and surname and the email "ana@example.com"
- **THEN** the dialog is titled "¿Activar a ana@example.com?"

#### Scenario: Cancelled

- **WHEN** the dialog is open and the admin chooses "Cancelar"
- **THEN** the dialog closes and no activation request is sent

### Requirement: A confirmed activation change refreshes the list

Confirming SHALL send the matching operation for that user. On success the dialog SHALL close, a success message
("Usuario desactivado." or "Usuario activado.") SHALL show, and the users SHALL be requested again. With the switch off,
a deactivated user then leaves the table.

#### Scenario: User deactivated

- **WHEN** an admin confirms the deactivation of an active user and it succeeds
- **THEN** the dialog closes, "Usuario desactivado." is shown and the users are requested again

#### Scenario: User activated

- **WHEN** the switch is on, an admin confirms the activation of an inactive user and it succeeds
- **THEN** the dialog closes, "Usuario activado." is shown and the users are requested again

### Requirement: A failed activation change keeps the dialog open

On failure the dialog SHALL stay open and show the message for the error code, and the confirm button SHALL be usable
again. Closing the dialog after a `CONFLICT` or `NOT_FOUND` failure SHALL request the users again, because the list is
out of date.

#### Scenario: Already in that state

- **WHEN** the confirmation fails with `CONFLICT`
- **THEN** the dialog stays open and shows "El usuario ya estaba en ese estado. Recarga la lista."

#### Scenario: User no longer exists

- **WHEN** the confirmation fails with `NOT_FOUND`
- **THEN** the dialog stays open and shows "Este usuario ya no existe."

#### Scenario: Not allowed

- **WHEN** the confirmation fails with `FORBIDDEN`
- **THEN** the dialog stays open and shows the existing message for `FORBIDDEN`

#### Scenario: Closed after a stale-list failure

- **WHEN** the confirmation failed with `CONFLICT` and the admin closes the dialog
- **THEN** the users are requested again
