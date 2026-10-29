# Spec Delta

## Purpose

Lets administrators see every user in one searchable, filterable table in the backoffice and create users of any role
from it, without leaving the screen.

## ADDED Requirements

### Requirement: Signed-in staff reach the users screen

The backoffice SHALL offer a signed-in screen at `/users` with the heading "Todos los usuarios", shown with the
navigation drawer and header like every signed-in screen. A visitor without a session SHALL end on `/login`.

#### Scenario: Admin opens the screen

- **WHEN** an admin with a stored session opens `/users`
- **THEN** the heading "Todos los usuarios" is shown with the drawer and header

#### Scenario: Visitor without a session

- **WHEN** a visitor with no stored session opens `/users`
- **THEN** the visitor ends on `/login`

### Requirement: The screen lists every user the API returns

On opening, the screen SHALL request the users once and SHALL show one table row per user, in the order the API returns
them, until the visitor searches or picks a profile.

#### Scenario: Users shown

- **WHEN** the screen opens and the API returns three users
- **THEN** the table shows three rows in that order

### Requirement: Each row shows the full name, email and profile

Each row SHALL show the user's name and surname joined in one "Nombre completo" cell, the email, and the translated
profile (Usuario, Entrenador or Administrador). When both name and surname are empty, the cell SHALL show "—".

#### Scenario: Row content

- **WHEN** a user "Ana" "García", "ana@example.com", role `TRAINER` is listed
- **THEN** the row shows "Ana García", "ana@example.com" and "Entrenador"

#### Scenario: User without a name

- **WHEN** a user with empty name and surname is listed
- **THEN** the "Nombre completo" cell shows "—"

### Requirement: The search field filters by email, name or surname

The screen SHALL show one search field above the table. While it holds text, only rows whose email, name or surname
contain that text SHALL be shown, ignoring case and accents. It SHALL NOT send any request.

#### Scenario: Search by surname

- **WHEN** the visitor types "garcia" and a user's surname is "García"
- **THEN** that user's row is shown and rows that do not match are hidden
- **AND** no request is sent

#### Scenario: Search by email

- **WHEN** the visitor types "ana@"
- **THEN** only rows whose email contains "ana@" are shown

#### Scenario: Search cleared

- **WHEN** the visitor empties the search field
- **THEN** every loaded row is shown again

### Requirement: Profile pills filter by role

The screen SHALL show one pill per profile (Usuario, Entrenador, Administrador) above the table. Selecting a pill SHALL
show only users of that role. Selecting it again SHALL clear the filter. At most one pill SHALL be selected. Pills and
search SHALL combine.

#### Scenario: Pill selected

- **WHEN** the visitor selects "Entrenador"
- **THEN** only users with role `TRAINER` are shown and "Entrenador" is shown as selected

#### Scenario: Pill cleared

- **WHEN** the visitor selects the selected "Entrenador" pill again
- **THEN** no pill is selected and every loaded row is shown

#### Scenario: Pill and search combined

- **WHEN** "Administrador" is selected and the visitor types "ana"
- **THEN** only admins whose email, name or surname contain "ana" are shown

### Requirement: The screen shows loading, empty, no-match and failure states

While the users load, the screen SHALL show a loading placeholder instead of the table. An empty list SHALL show
"Todavía no hay usuarios.". Filters that match nothing SHALL show "Ningún usuario coincide con la búsqueda.". A failed
load SHALL show the error for its code with a retry action.

#### Scenario: Empty list

- **WHEN** the API returns no users
- **THEN** "Todavía no hay usuarios." is shown instead of rows

#### Scenario: No match

- **WHEN** users are loaded and the search matches none
- **THEN** "Ningún usuario coincide con la búsqueda." is shown instead of rows

#### Scenario: Load fails and is retried

- **WHEN** the listing fails
- **THEN** the error for its code is shown with a retry action
- **AND** activating retry requests the users again

#### Scenario: Trainer opens the URL directly

- **WHEN** a trainer opens `/users` and the listing fails with `UNEXPECTED`
- **THEN** the generic error is shown with a retry action

### Requirement: Only admins can open the creation modal

For an `ADMIN`, the screen SHALL show an "Añadir usuario" button above the table. Activating it SHALL open the "Nuevo
usuario" modal and SHALL NOT send any request. For a `TRAINER`, neither the button nor the modal SHALL be rendered.

#### Scenario: Admin opens the modal

- **WHEN** an admin activates "Añadir usuario"
- **THEN** the "Nuevo usuario" modal opens with empty fields and the profile set to "Usuario"
- **AND** no request is sent

#### Scenario: Trainer does not see the button

- **WHEN** a trainer's screen is shown
- **THEN** no "Añadir usuario" button and no "Nuevo usuario" modal are rendered

### Requirement: The creation form checks its fields before sending

The modal SHALL ask for email, password, profile, name, surname and date of birth. It SHALL apply the shared create
user rules and, on any violation, SHALL show the message under the field and SHALL NOT send any request. Empty optional
fields SHALL be left out of the request, and name and surname SHALL be sent trimmed.

#### Scenario: Required fields empty

- **WHEN** the admin submits the form with empty email and password
- **THEN** a required message is shown under email and under password
- **AND** no request is sent

#### Scenario: Optional fields left out

- **WHEN** the admin submits a valid email, a password and profile "Entrenador", with name, surname and date of birth empty
- **THEN** the creation request holds only email, password and the `TRAINER` role (the contract's value `2`)

### Requirement: A created user closes the modal and refreshes the list

On a successful creation the modal SHALL close, the screen SHALL show "Usuario creado." and SHALL request the users
again. Reopening the modal SHALL show empty fields.

#### Scenario: User created

- **WHEN** the admin submits valid data and the creation succeeds
- **THEN** the modal closes and "Usuario creado." is shown
- **AND** the users are requested again

### Requirement: A failed creation keeps the modal open

On a failed creation the modal SHALL stay open with the typed values and SHALL show the message for the code:
`CONFLICT` "Ya existe un usuario con este email.", `VALIDATION` "Revisa los datos. La contraseña debe cumplir los
requisitos de seguridad.", any other code its generic message. Cancel SHALL close the modal without sending a request.

#### Scenario: Email already registered

- **WHEN** the creation fails with `CONFLICT`
- **THEN** the modal stays open showing "Ya existe un usuario con este email." and the typed values

#### Scenario: Rejected by the API

- **WHEN** the creation fails with `VALIDATION`
- **THEN** the modal stays open showing "Revisa los datos. La contraseña debe cumplir los requisitos de seguridad."

#### Scenario: Cancelled

- **WHEN** the admin activates "Cancelar"
- **THEN** the modal closes and no request is sent

### Requirement: The users screen is translated

Every text on the screen and in the modal SHALL be available in Spanish and English and SHALL follow the active
language.

#### Scenario: English

- **WHEN** the active language is English
- **THEN** the heading reads "All users" and the button reads "Add user"
