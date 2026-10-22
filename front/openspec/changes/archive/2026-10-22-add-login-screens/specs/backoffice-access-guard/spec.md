# Spec Delta

## MODIFIED Requirements

### Requirement: Home requires a stored session

The backoffice home screen (`/`) SHALL show its content only when a session is stored. With no stored session, it
SHALL replace `/` with `/login` in the history and SHALL NOT render any home content.

#### Scenario: Direct visit without a session

- **WHEN** a visitor with no stored session opens `/` directly
- **THEN** the visitor ends on `/login`
- **AND** the home heading is never rendered

#### Scenario: Visit with a session

- **WHEN** a visitor with stored access and refresh tokens opens `/`
- **THEN** the home heading "Weight Tracker" and the message "Has iniciado sesión." are shown

### Requirement: A session read failure is treated as no session

If reading the session fails, the guarded screen SHALL behave as if no session were stored.

#### Scenario: Storage fails

- **WHEN** reading the session fails with `UNEXPECTED`
- **THEN** the visitor ends on `/login` and no home content is rendered
