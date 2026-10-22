# Spec Delta

## MODIFIED Requirements

### Requirement: A successful signup returns the API's tokens unchanged

On a successful response, the signup operation SHALL store the access token and the refresh token in the front's
key-value storage, and then return a successful result holding the API's `AuthResponse` (`accessToken`,
`refreshToken`, `expiresIn`) as received. `expiresIn` SHALL NOT be stored.

#### Scenario: Account created

- **WHEN** the API answers `200` with an `AuthResponse`
- **THEN** the result is successful and its value equals that `AuthResponse`
- **AND** the access token and the refresh token from that response are in key-value storage

## ADDED Requirements

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
