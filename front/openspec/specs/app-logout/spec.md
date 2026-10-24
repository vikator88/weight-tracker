# app-logout Specification

## Purpose

Lets an athlete end their app session from the home screen. The placement is provisional, a development convenience
until logout gets a proper place.

## Requirements

### Requirement: Home shows a logout button

The app home screen SHALL show a "Cerrar sesión" button. An athlete without a session SHALL NOT see it, since the gate
does not render home for them.

#### Scenario: Signed-in athlete on home

- **WHEN** an athlete with a stored session is on home
- **THEN** home shows the "Cerrar sesión" button

### Requirement: Logging out from home shows login

Tapping the logout button SHALL log out. On success the app SHALL show the login screen and SHALL NOT render home
content. The home screen SHALL NOT navigate; the access gate follows the session. The button SHALL be disabled while
logout is in flight.

#### Scenario: Successful logout

- **WHEN** the athlete taps "Cerrar sesión" and logout succeeds
- **THEN** the session is read again and is signed out
- **AND** the login screen is shown and no home content is rendered

#### Scenario: Logout in flight

- **WHEN** the athlete taps "Cerrar sesión" and logout has not finished
- **THEN** the button is disabled

#### Scenario: Restart after logout

- **WHEN** the athlete logs out and the app restarts
- **THEN** the login screen is shown

### Requirement: A failed logout stays on home

If logout fails, the athlete SHALL stay on home and the screen SHALL show the translated message for the failure code.

#### Scenario: Storage fails

- **WHEN** logout fails with `UNEXPECTED`
- **THEN** home stays and shows "Ha ocurrido un error inesperado."
