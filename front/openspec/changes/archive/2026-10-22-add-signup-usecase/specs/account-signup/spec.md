# Spec Delta

## Purpose

Lets each front create a new account through the API, with the account's role decided by the front that serves it
and every outcome returned as a result rather than an exception.

## ADDED Requirements

### Requirement: Signup sends the caller's credentials to the API

The signup operation SHALL send the email and password it receives to `POST /auth/signup` unchanged. It SHALL NOT
validate, trim or otherwise alter them.

#### Scenario: Credentials are forwarded as given

- **WHEN** signup runs with email `athlete@example.com` and password `Str0ng!pass`
- **THEN** the API receives a `POST /auth/signup` whose body has that email and that password

### Requirement: The role is fixed by the front

The signup operation SHALL send the role the front was configured with, and the caller SHALL NOT be able to choose
or override it. The app SHALL send `USER` (`1`). The backoffice SHALL send `TRAINER` (`2`). Neither front SHALL send
`ADMIN` (`3`).

#### Scenario: The app signs up an athlete

- **WHEN** signup runs from the app
- **THEN** the request body's role is `1` (`USER`)

#### Scenario: The backoffice signs up a trainer

- **WHEN** signup runs from the backoffice
- **THEN** the request body's role is `2` (`TRAINER`)

#### Scenario: The caller cannot pick a role

- **WHEN** a caller invokes signup
- **THEN** the operation accepts only email and password as input, so no other role can be requested

### Requirement: A successful signup returns the API's tokens unchanged

On a successful response, the signup operation SHALL return a successful result holding the API's `AuthResponse`
(`accessToken`, `refreshToken`, `expiresIn`) as received. It SHALL NOT store the tokens or start a session.

#### Scenario: Account created

- **WHEN** the API answers `200` with an `AuthResponse`
- **THEN** the result is successful and its value equals that `AuthResponse`
- **AND** nothing is written to key-value storage

### Requirement: Signup failures are returned as domain error codes

The signup operation SHALL NOT throw. Each failure SHALL be returned as a failed result whose `DomainError` code
comes from the existing HTTP error mapper, and whose message is the API's problem detail when one is present.

#### Scenario: Password not strong enough

- **WHEN** the API answers `400`
- **THEN** the result fails with code `VALIDATION`

#### Scenario: Malformed email

- **WHEN** the API answers `401`
- **THEN** the result fails with code `UNAUTHORIZED`

#### Scenario: Role not allowed

- **WHEN** the API answers `403`
- **THEN** the result fails with code `FORBIDDEN`

#### Scenario: Email already registered

- **WHEN** the API answers `409`
- **THEN** the result fails with code `CONFLICT`

#### Scenario: No response from the API

- **WHEN** the request fails before any response arrives
- **THEN** the result fails with code `NETWORK`

#### Scenario: Unexpected server error

- **WHEN** the API answers with a status the mapper does not list, such as `500`
- **THEN** the result fails with code `UNEXPECTED`
