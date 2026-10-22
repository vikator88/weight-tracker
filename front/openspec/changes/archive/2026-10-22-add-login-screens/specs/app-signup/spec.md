# Spec Delta

## MODIFIED Requirements

### Requirement: The signup screen is served at `/signup`

The app SHALL serve a signup screen at `/signup` with an email field, a password field and a submit button. An
athlete without a session SHALL reach it from the login screen.

#### Scenario: Athlete without a session opens the app

- **WHEN** an athlete with no stored session opens the app
- **THEN** the login screen is shown first, not the signup screen

#### Scenario: Athlete without a session moves to signup

- **WHEN** an athlete with no stored session follows the signup link on the login screen
- **THEN** the screen shows the "Crear cuenta" heading, the email and password fields and the submit button

## ADDED Requirements

### Requirement: Signup links to login

The signup screen SHALL show a link to `/login` that replaces signup in the history, so moving between signup and
login does not stack screens.

#### Scenario: Athlete who already has an account

- **WHEN** the athlete follows "¿Ya tienes cuenta? Inicia sesión"
- **THEN** the login screen is shown
- **AND** going back does not return to signup
