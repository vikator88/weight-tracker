# account-signup Specification

## Purpose

Lets each front create a new account through the API, with the account's role decided by the front that serves it
and every outcome returned as a result rather than an exception.

## Requirements

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

On a successful response, the signup operation SHALL store the access token and the refresh token in the front's
key-value storage, and then return a successful result holding the API's `AuthResponse` (`accessToken`,
`refreshToken`, `expiresIn`) as received. `expiresIn` SHALL NOT be stored.

#### Scenario: Account created

- **WHEN** the API answers `200` with an `AuthResponse`
- **THEN** the result is successful and its value equals that `AuthResponse`
- **AND** the access token and the refresh token from that response are in key-value storage

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

### Requirement: A failed signup stores nothing

When the API rejects the signup or does not answer, the signup operation SHALL NOT write to key-value storage.

#### Scenario: Email already registered

- **WHEN** the API answers `409`
- **THEN** the result fails with code `CONFLICT`
- **AND** nothing is written to key-value storage

### Requirement: A storage failure is returned as an unexpected error

If storing the tokens fails after the API accepted the signup, the signup operation SHALL NOT throw. It SHALL return
a failed result with code `UNEXPECTED`.

#### Scenario: Storage rejects the write

- **WHEN** the API answers `200` and the key-value storage fails to write
- **THEN** the result fails with code `UNEXPECTED`
