# Spec Delta

## Purpose

Lets a trainer create a backoffice account from a signup screen, with fast structural feedback on the form and a
translated message for every failure the API reports.

## ADDED Requirements

### Requirement: The signup screen is reachable at `/signup`

The backoffice SHALL serve a signup screen at `/signup` with an email field, a password field and a submit button.
It SHALL be reachable without a session.

#### Scenario: Anonymous visitor opens signup

- **WHEN** a visitor with no stored session opens `/signup`
- **THEN** the screen shows the "Crear cuenta" heading, the email and password fields and the submit button

### Requirement: The form validates structure before sending

The form SHALL require an email in a valid format and a non-empty password. While a field is invalid, submitting
SHALL show that field's error and SHALL NOT send a request. The form SHALL NOT check password strength.

#### Scenario: Empty form

- **WHEN** the trainer submits with both fields empty
- **THEN** the email field shows "Introduce tu email." and the password field shows "Introduce una contraseña."
- **AND** no signup request is sent

#### Scenario: Malformed email

- **WHEN** the trainer submits `not-an-email` with a password
- **THEN** the email field shows "Introduce un email válido."
- **AND** no signup request is sent

#### Scenario: Short password is sent anyway

- **WHEN** the trainer submits a valid email and the password `a`
- **THEN** the signup request is sent with that password

### Requirement: Submitting signs up a trainer

A valid submit SHALL send the entered email and password to signup. The account role SHALL be `TRAINER`. The screen
SHALL offer no way to choose a role. The submit button SHALL be disabled while the request is in flight.

#### Scenario: Request in flight

- **WHEN** the trainer submits a valid form and the API has not answered yet
- **THEN** the submit button is disabled

### Requirement: A successful signup lands on home

When signup succeeds, the backoffice SHALL replace `/signup` with `/` in the history. Going back SHALL NOT return to
the signup form.

#### Scenario: Account created

- **WHEN** the API accepts the signup
- **THEN** the trainer is on `/` and sees the home heading

### Requirement: Signup failures show a translated message

When signup fails, the screen SHALL stay on `/signup`, keep the entered email and show the message for the failure
code. `VALIDATION`, `UNAUTHORIZED` and `CONFLICT` SHALL use signup-specific copy. Every other code SHALL use the
shared error message.

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

- **WHEN** the backoffice translation parity check runs
- **THEN** every `auth` key exists in both `es` and `en`
