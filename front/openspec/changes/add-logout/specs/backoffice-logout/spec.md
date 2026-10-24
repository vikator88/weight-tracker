# Spec Delta

## Purpose

Lets a trainer or an admin end their backoffice session from the home screen. The placement is provisional, a
development convenience until logout gets a proper place.

## ADDED Requirements

### Requirement: Home shows a logout button

The backoffice home screen SHALL show a "Cerrar sesión" button to a visitor with a stored session, whatever their
role. A visitor without a session SHALL NOT see it, since home is not rendered for them.

#### Scenario: Signed-in visitor opens home

- **WHEN** a trainer or an admin with a stored session opens `/`
- **THEN** home shows the "Cerrar sesión" button

#### Scenario: Visitor without a session opens home

- **WHEN** a visitor with no stored session opens `/`
- **THEN** the "Cerrar sesión" button is never rendered

### Requirement: Logging out from home ends on login

Clicking the logout button SHALL log out. On success the visitor SHALL end on `/login`. The button SHALL be disabled
while logout is in flight.

#### Scenario: Successful logout

- **WHEN** the visitor clicks "Cerrar sesión" and logout succeeds
- **THEN** the visitor ends on `/login`

#### Scenario: Back to home after logout

- **WHEN** the visitor has logged out and opens `/` again
- **THEN** the visitor ends on `/login` and no home content is rendered

#### Scenario: Logout in flight

- **WHEN** the visitor clicks "Cerrar sesión" and logout has not finished
- **THEN** the button is disabled

### Requirement: A failed logout stays on home

If logout fails, the visitor SHALL stay on home and the screen SHALL show the translated message for the failure code.

#### Scenario: Storage fails

- **WHEN** logout fails with `UNEXPECTED`
- **THEN** home stays and shows "Ha ocurrido un error inesperado."
- **AND** the visitor is not sent to `/login`
