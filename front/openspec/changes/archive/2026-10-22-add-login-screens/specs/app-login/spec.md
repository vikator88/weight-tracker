# Spec Delta

## Purpose

Lets an athlete start an app session from a login screen. The screen gives structural feedback on the form, a
translated message for every failure the API reports, and a way to sign up instead.

## ADDED Requirements

### Requirement: The login screen is served at `/login`

The app SHALL serve a login screen at `/login` with an email field, a password field and a submit button.

#### Scenario: Athlete without a session opens the app

- **WHEN** an athlete with no stored session opens the app
- **THEN** the screen shows the "Iniciar sesión" heading, the email and password fields and the submit button

### Requirement: The login form validates structure before sending

The form SHALL trim both values, then require an email in a valid format and a non-empty password. While a field is
invalid, submitting SHALL show that field's error under it and SHALL NOT send a request.

#### Scenario: Empty form

- **WHEN** the athlete submits with both fields empty
- **THEN** the email field shows "Introduce tu email." and the password field shows "Introduce tu contraseña."
- **AND** no login request is sent

#### Scenario: Malformed email

- **WHEN** the athlete submits `not-an-email` with a password
- **THEN** the email field shows "Introduce un email válido."
- **AND** no login request is sent

#### Scenario: Surrounding spaces are removed

- **WHEN** the athlete submits `  athlete@example.com  ` and a password
- **THEN** the login request is sent with `athlete@example.com`

### Requirement: Submitting logs in

A valid submit SHALL send the entered email and password to login, with no role. The submit button SHALL show progress
and be disabled while the request is in flight.

#### Scenario: Request in flight

- **WHEN** the athlete submits a valid form and the API has not answered yet
- **THEN** the submit button is disabled and shows progress

### Requirement: A successful login signs the athlete in

When login succeeds, the app SHALL treat the athlete as signed in and show the home screen, without the login screen
navigating. The login screen SHALL NOT be reachable by going back.

#### Scenario: Credentials accepted

- **WHEN** the API accepts the login
- **THEN** the athlete sees the home heading and the message "Has iniciado sesión."
- **AND** going back does not return to the login form

### Requirement: Login failures show a translated message

When login fails, the screen SHALL stay on `/login`, keep the entered email, leave the athlete signed out and show the
message for the failure code. `UNAUTHORIZED` SHALL use login-specific copy that does not say which credential was
wrong. Every other code SHALL use the shared error message.

#### Scenario: Wrong credentials

- **WHEN** login fails with `UNAUTHORIZED`
- **THEN** the screen shows "Email o contraseña incorrectos."
- **AND** the email field still holds the entered email

#### Scenario: API unreachable

- **WHEN** login fails with `NETWORK`
- **THEN** the screen shows "No hay conexión con el servidor."

### Requirement: Login links to signup

The login screen SHALL show a link to `/signup` that replaces login in the history, so moving between login and
signup does not stack screens.

#### Scenario: Athlete without an account

- **WHEN** the athlete follows "¿No tienes cuenta? Crea una"
- **THEN** the signup screen is shown
- **AND** going back does not return to login

### Requirement: Every login string is translated

Every string on the login screen SHALL exist in Spanish and English. Spanish SHALL be the default.

#### Scenario: Translation parity

- **WHEN** the app translation parity check runs
- **THEN** every `auth` key exists in both `es` and `en`
