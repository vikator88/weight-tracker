# Spec Delta

## Purpose

Defines which backoffice screens need a stored session, and keeps their content hidden from visitors who do not
have one. This is a navigation guard, not authorization: the API decides access to data.

## ADDED Requirements

### Requirement: Home requires a stored session

The backoffice home screen (`/`) SHALL show its content only when a session is stored. With no stored session, it
SHALL replace `/` with `/signup` in the history and SHALL NOT render any home content.

#### Scenario: Direct visit without a session

- **WHEN** a visitor with no stored session opens `/` directly
- **THEN** the visitor ends on `/signup`
- **AND** the home heading is never rendered

#### Scenario: Visit with a session

- **WHEN** a visitor with stored access and refresh tokens opens `/`
- **THEN** the home heading "Weight Tracker" and the message "Has iniciado sesión." are shown

### Requirement: Nothing protected shows while the session is read

While the stored session is being read, a guarded screen SHALL show only a progress indicator and SHALL NOT render
its content.

#### Scenario: Session not read yet

- **WHEN** a guarded screen has not finished reading the session
- **THEN** a progress indicator is shown and the screen's content is not rendered

### Requirement: Session presence is enough

The guard SHALL only check that a session is stored. It SHALL NOT check whether the tokens are expired or valid,
and SHALL NOT call the API.

#### Scenario: Stale tokens

- **WHEN** the stored tokens are expired
- **THEN** the home screen is still shown

### Requirement: A session read failure is treated as no session

If reading the session fails, the guarded screen SHALL behave as if no session were stored.

#### Scenario: Storage fails

- **WHEN** reading the session fails with `UNEXPECTED`
- **THEN** the visitor ends on `/signup` and no home content is rendered
