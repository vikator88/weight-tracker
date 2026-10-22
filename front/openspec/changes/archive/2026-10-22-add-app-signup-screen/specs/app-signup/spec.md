# Spec Delta

## Purpose

Lets an athlete create an app account from a signup screen, with structural feedback on the form and a translated
message for every failure the API reports.

## ADDED Requirements

### Requirement: The signup screen is served at `/signup`

The app SHALL serve a signup screen at `/signup` with an email field, a password field and a submit button.

#### Scenario: Athlete without a session opens the app

- **WHEN** an athlete with no stored session opens the app
- **THEN** the screen shows the "Crear cuenta" heading, the email and password fields and the submit button

### Requirement: The form validates structure before sending

The form SHALL trim both values, then require an email in a valid format and a non-empty password. While a field is
invalid, submitting SHALL show that field's error under it and SHALL NOT send a request. The form SHALL NOT check
password strength.

#### Scenario: Empty form

- **WHEN** the athlete submits with both fields empty
- **THEN** the email field shows "Introduce tu email." and the password field shows "Introduce una contraseña."
- **AND** no signup request is sent

#### Scenario: Malformed email

- **WHEN** the athlete submits `not-an-email` with a password
- **THEN** the email field shows "Introduce un email válido."
- **AND** no signup request is sent

#### Scenario: Surrounding spaces are removed

- **WHEN** the athlete submits `athlete@example.com` and a password
- **THEN** the signup request is sent with `athlete@example.com`

#### Scenario: Short password is sent anyway

- **WHEN** the athlete submits a valid email and the password `a`
- **THEN** the signup request is sent with that password

### Requirement: Submitting signs up an athlete

A valid submit SHALL send the entered email and password to signup. The account role SHALL be `USER`, and the screen
SHALL offer no way to choose a role. The submit button SHALL show progress and be disabled while the request is in
flight.

#### Scenario: Request in flight

- **WHEN** the athlete submits a valid form and the API has not answered yet
- **THEN** the submit button is disabled and shows progress

### Requirement: A successful signup signs the athlete in

When signup succeeds, the app SHALL treat the athlete as signed in and show the home screen. The signup screen SHALL
NOT be reachable by going back.

#### Scenario: Account created

- **WHEN** the API accepts the signup
- **THEN** the athlete sees the home heading and the message "Has iniciado sesión."
- **AND** going back does not return to the signup form

### Requirement: Signup failures show a translated message

When signup fails, the screen SHALL stay on `/signup`, keep the entered email, leave the athlete signed out and show
the message for the failure code. `VALIDATION`, `UNAUTHORIZED` and `CONFLICT` SHALL use signup-specific copy. Every
other code SHALL use the shared error message.

#### Scenario: Password rejected by the API

- **WHEN** signup fails with `VALIDATION`
- **THEN** the screen shows "La contraseña no cumple los requisitos de seguridad."

#### Scenario: Email rejected by the API

- **WHEN** signup fails with `UNAUTHORIZED`
- **THEN** the screen shows "El email no es válido."

#### Scenario: Email already registered

- **WHEN** signup fails with `CONFLICT`
- **THEN** the screen shows "Ya existe una cuenta con este email."
- **AND** the email field still holds the entered email

#### Scenario: API unreachable

- **WHEN** signup fails with `NETWORK`
- **THEN** the screen shows "No hay conexión con el servidor."

### Requirement: Every signup string is translated

Every string on the signup screen SHALL exist in Spanish and English. Spanish SHALL be the default.

#### Scenario: Translation parity

- **WHEN** the app translation parity check runs
- **THEN** every `auth` key exists in both `es` and `en`
