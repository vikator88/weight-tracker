# Spec Delta

## ADDED Requirements

### Requirement: The drawer offers logout

The navigation drawer SHALL end with a "Cerrar sesión" entry, shown to every visitor who sees the drawer, whatever
their role.

#### Scenario: Signed-in visitor sees logout

- **WHEN** a trainer or an admin with a stored session opens `/`
- **THEN** the drawer shows "Cerrar sesión" as its last entry

#### Scenario: Home has no logout button of its own

- **WHEN** a visitor with a stored session opens `/`
- **THEN** "Cerrar sesión" appears only in the drawer

### Requirement: Logging out from the drawer ends on login

Clicking "Cerrar sesión" in the drawer SHALL log out. On success the visitor SHALL end on `/login`. The entry SHALL be
disabled while logout is in flight.

#### Scenario: Successful logout

- **WHEN** the visitor clicks "Cerrar sesión" and logout succeeds
- **THEN** the visitor ends on `/login`

#### Scenario: Back to home after logout

- **WHEN** the visitor has logged out and opens `/` again
- **THEN** the visitor ends on `/login` and neither the drawer nor home content is rendered

#### Scenario: Logout in flight

- **WHEN** the visitor clicks "Cerrar sesión" and logout has not finished
- **THEN** the entry is disabled

### Requirement: A failed logout stays on the current screen

If logout fails, the visitor SHALL stay on the current screen and the drawer SHALL show the translated message for the
failure code.

#### Scenario: Storage fails

- **WHEN** logout fails with `UNEXPECTED`
- **THEN** the drawer shows "Ha ocurrido un error inesperado."
- **AND** the visitor is not sent to `/login`

## REMOVED Requirements

### Requirement: Home shows a logout button

**Reason**: Logout moves into the navigation drawer; home no longer has its own button.
**Migration**: Use the "Cerrar sesión" entry of the drawer (see "The drawer offers logout").

### Requirement: Logging out from home ends on login

**Reason**: Replaced by "Logging out from the drawer ends on login", with the same outcome.
**Migration**: Use the "Cerrar sesión" entry of the drawer.

### Requirement: A failed logout stays on home

**Reason**: Replaced by "A failed logout stays on the current screen", since logout is no longer tied to home.
**Migration**: None; the failure still shows the translated message and does not navigate.
