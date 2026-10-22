# Spec Delta

## Purpose

Lets a trainer or an admin start a backoffice session from a login screen. The screen gives structural feedback on
the form, a translated message for every failure the API reports, and a way to sign up instead.

## ADDED Requirements

### Requirement: The login screen is reachable at `/login`

The backoffice SHALL serve a login screen at `/login` with an email field, a password field and a submit button. It
SHALL be reachable without a session.

#### Scenario: Anonymous visitor opens login

- **WHEN** a visitor with no stored session opens `/login`
- **THEN** the screen shows the "Iniciar sesión" heading, the email and password fields and the submit button

### Requirement: The login form validates structure before sending

The form SHALL require an email in a valid format and a non-empty password. While a field is invalid, submitting
SHALL show that field's error and SHALL NOT send a request.

#### Scenario: Empty form

- **WHEN** the visitor submits with both fields empty
- **THEN** the email field shows "Introduce tu email." and the password field shows "Introduce tu contraseña."
- **AND** no login request is sent

#### Scenario: Malformed email

- **WHEN** the visitor submits `not-an-email` with a password
- **THEN** the email field shows "Introduce un email válido."
- **AND** no login request is sent

### Requirement: Submitting logs in

A valid submit SHALL send the entered email and password to login, with no role. The submit button SHALL be disabled
while the request is in flight.

#### Scenario: Request in flight

- **WHEN** the visitor submits a valid form and the API has not answered yet
- **THEN** the submit button is disabled

### Requirement: A successful login lands on home

When login succeeds, the backoffice SHALL replace `/login` with `/` in the history. Going back SHALL NOT return to the
login form.

#### Scenario: Credentials accepted

- **WHEN** the API accepts the login
- **THEN** the visitor is on `/` and sees the home heading

#### Scenario: Session survives a reload

- **WHEN** the visitor logs in and then reloads `/`
- **THEN** the home heading is shown and the visitor is not sent to `/login`

### Requirement: Login failures show a translated message

When login fails, the screen SHALL stay on `/login`, keep the entered email, store no session and show the message
for the failure code. `UNAUTHORIZED` SHALL use login-specific copy that does not say which credential was wrong.
Every other code SHALL use the shared error message.

#### Scenario: Wrong credentials

- **WHEN** login fails with `UNAUTHORIZED`
- **THEN** the screen shows "Email o contraseña incorrectos."
- **AND** the email field still holds the entered email

#### Scenario: API unreachable

- **WHEN** login fails with `NETWORK`
- **THEN** the screen shows "No hay conexión con el servidor."

### Requirement: Login links to signup

The login screen SHALL show a link to `/signup`.

#### Scenario: Visitor without an account

- **WHEN** the visitor follows "¿No tienes cuenta? Crea una"
- **THEN** the visitor is on `/signup`

### Requirement: Every login string is translated

Every string on the login screen SHALL exist in Spanish and English. Spanish SHALL be the default.

#### Scenario: Translation parity

- **WHEN** the backoffice translation parity check runs
- **THEN** every `auth` key exists in both `es` and `en`
