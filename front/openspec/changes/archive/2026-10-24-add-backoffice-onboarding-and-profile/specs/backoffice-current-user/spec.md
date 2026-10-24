# Spec Delta

## Purpose

Gives every signed-in backoffice screen the signed-in user's own profile, loaded once from the API, kept in sync
after a save, and tells the screens whether the user still has onboarding to complete.

## ADDED Requirements

### Requirement: The profile is loaded once per signed-in screen tree

Once the session is confirmed, the backoffice SHALL read the signed-in user's profile once and share the result with
every signed-in screen. Moving between signed-in screens SHALL NOT read it again.

#### Scenario: Signed-in visitor opens home

- **WHEN** a visitor with a session opens `/`
- **THEN** exactly one profile read is made

#### Scenario: Navigating between signed-in screens

- **WHEN** the profile has loaded and the user goes from `/` to `/profile`
- **THEN** no new profile read is made and `/profile` shows the loaded profile

#### Scenario: No session

- **WHEN** a visitor without a session opens `/`
- **THEN** no profile read is made

### Requirement: A saved profile replaces the shared profile

When a profile update succeeds, the shared profile SHALL be replaced by the profile the API answered with, and every
signed-in screen SHALL show it without reading the profile again.

#### Scenario: Name saved

- **WHEN** an update succeeds and the API answers name "Ana" and surname "García"
- **THEN** the header shows "Ana García" and no new profile read is made

### Requirement: Onboarding is pending while the name is incomplete

The user SHALL be in onboarding when the loaded profile's name or surname is empty after trimming. Onboarding SHALL
NOT be considered pending while the profile is loading or after the load failed.

#### Scenario: Empty name

- **WHEN** the profile loads with name "" and surname "García"
- **THEN** onboarding is pending

#### Scenario: Whitespace-only surname

- **WHEN** the profile loads with name "Ana" and surname " "
- **THEN** onboarding is pending

#### Scenario: Complete profile

- **WHEN** the profile loads with name "Ana" and surname "García"
- **THEN** onboarding is not pending

### Requirement: A failed profile load can be retried

When the profile read fails, the signed-in screen SHALL show the translated message for the failure code and a
"Reintentar" button. Pressing it SHALL read the profile again.

#### Scenario: Load fails without a response

- **WHEN** the profile read fails with `NETWORK`
- **THEN** the screen shows the `NETWORK` message and "Reintentar"
- **AND** no onboarding dialog is shown

#### Scenario: Retry succeeds

- **WHEN** the user presses "Reintentar" and the second read succeeds with a complete profile
- **THEN** the error disappears and the regular home content shows

### Requirement: The shared profile ends with the signed-in session

The shared profile SHALL NOT be stored in key-value storage. Logging out or reloading the page SHALL discard it, and
the next signed-in visit SHALL read it again.

#### Scenario: Logout then login

- **WHEN** a user logs out and a user signs in again
- **THEN** the profile is read again and the previous profile is not shown
