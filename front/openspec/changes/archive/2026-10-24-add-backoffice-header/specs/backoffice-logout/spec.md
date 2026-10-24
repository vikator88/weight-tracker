# Spec Delta

## ADDED Requirements

### Requirement: The user menu offers logout

The user menu in the header SHALL end with a "Cerrar sesión" entry, after a separator, for every visitor who sees the
header, whatever their role.

#### Scenario: Signed-in visitor opens the user menu

- **WHEN** a trainer or an admin with a stored session opens the user menu
- **THEN** the menu shows "Cerrar sesión" as its last entry

#### Scenario: Logout appears once

- **WHEN** a visitor with a stored session is on `/`
- **THEN** "Cerrar sesión" appears only in the user menu, not in the drawer or on home

### Requirement: Logging out from the user menu ends on login

Choosing "Cerrar sesión" in the user menu SHALL log out. On success the visitor SHALL end on `/login`. The entry SHALL
be disabled while logout is in flight.

#### Scenario: Successful logout

- **WHEN** the visitor chooses "Cerrar sesión" and logout succeeds
- **THEN** the visitor ends on `/login`

#### Scenario: Back to home after logout

- **WHEN** the visitor has logged out and opens `/` again
- **THEN** the visitor ends on `/login` and neither the header, the drawer nor home content is rendered

#### Scenario: Logout in flight

- **WHEN** the visitor chooses "Cerrar sesión" and logout has not finished
- **THEN** the entry is disabled

## MODIFIED Requirements

### Requirement: A failed logout stays on the current screen

If logout fails, the visitor SHALL stay on the current screen and the user menu SHALL stay open and show the translated
message for the failure code.

#### Scenario: Storage fails

- **WHEN** logout fails with `UNEXPECTED`
- **THEN** the user menu shows "Ha ocurrido un error inesperado."
- **AND** the visitor is not sent to `/login`

## REMOVED Requirements

### Requirement: The drawer offers logout

**Reason**: Logout moves from the drawer to the user menu in the header.
**Migration**: Use the "Cerrar sesión" entry of the user menu (see "The user menu offers logout").

### Requirement: Logging out from the drawer ends on login

**Reason**: Replaced by "Logging out from the user menu ends on login", with the same outcome.
**Migration**: Use the "Cerrar sesión" entry of the user menu.
