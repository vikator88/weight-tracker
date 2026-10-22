# Spec Delta

## Purpose

Lets each front start a session for an existing account through the API. On success it stores the returned tokens,
and it returns every outcome as a result rather than an exception.

## ADDED Requirements

### Requirement: Login sends the caller's credentials to the API

The login operation SHALL send the email and password it receives to `POST /auth/login` unchanged. It SHALL NOT
validate, trim or otherwise alter them, and SHALL NOT send a role.

#### Scenario: Credentials are forwarded as given

- **WHEN** login runs with email `athlete@example.com` and password `Str0ng!pass`
- **THEN** the API receives a `POST /auth/login` whose body has exactly that email and that password

### Requirement: A successful login stores the session

On a successful response, the login operation SHALL store the access token and the refresh token in key-value
storage. It SHALL then return a successful result holding the API's `AuthResponse` as received.

#### Scenario: Credentials accepted

- **WHEN** the API answers `200` with an `AuthResponse`
- **THEN** the result is successful and its value equals that `AuthResponse`
- **AND** key-value storage holds its access token and refresh token, so reading the session returns them

### Requirement: A failed login stores nothing

When the API rejects the login or cannot be reached, the login operation SHALL NOT write to key-value storage.

#### Scenario: Credentials rejected

- **WHEN** the API answers `401`
- **THEN** nothing is written to key-value storage

### Requirement: A storage failure is returned as an unexpected error

If the API accepts the login but storing either token fails, the login operation SHALL NOT throw. It SHALL return a
failed result with code `UNEXPECTED`.

#### Scenario: Storage rejects the write

- **WHEN** the API answers `200` and key-value storage fails to write
- **THEN** the result fails with code `UNEXPECTED`

### Requirement: Login failures are returned as domain error codes

The login operation SHALL NOT throw. Each failure SHALL be returned as a failed result whose `DomainError` code comes
from the existing HTTP error mapper.

#### Scenario: Missing field

- **WHEN** the API answers `400`
- **THEN** the result fails with code `VALIDATION`

#### Scenario: Wrong credentials

- **WHEN** the API answers `401`, whether the email is malformed or unknown or the password is wrong
- **THEN** the result fails with code `UNAUTHORIZED`

#### Scenario: No response from the API

- **WHEN** the request fails before any response arrives
- **THEN** the result fails with code `NETWORK`

#### Scenario: Unexpected server error

- **WHEN** the API answers with a status the mapper does not list, such as `500`
- **THEN** the result fails with code `UNEXPECTED`
