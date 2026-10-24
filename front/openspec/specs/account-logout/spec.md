# account-logout Specification

## Purpose

Lets each front end the stored session on the device by removing the access and refresh tokens it kept after
authenticating, without calling the API.

## Requirements

### Requirement: Logging out removes both stored tokens

Logging out SHALL remove the stored access token and refresh token from key-value storage and return a successful
result. It SHALL NOT call the API.

#### Scenario: Both tokens stored

- **WHEN** key-value storage holds an access token and a refresh token and logout runs
- **THEN** the result is successful
- **AND** neither token is stored any longer
- **AND** no API request is made

#### Scenario: Nothing stored

- **WHEN** key-value storage holds neither token and logout runs
- **THEN** the result is successful

### Requirement: After logout there is no session

Once logout succeeds, reading the session SHALL return no session.

#### Scenario: Read the session after logout

- **WHEN** logout succeeds and the session is read
- **THEN** the result is successful and its value is empty (no session)

### Requirement: A storage failure is returned as an unexpected error

If key-value storage fails to remove a token, logging out SHALL NOT throw. It SHALL return a failed result with code
`UNEXPECTED`.

#### Scenario: Storage rejects the removal

- **WHEN** key-value storage fails to remove a token
- **THEN** the result fails with code `UNEXPECTED`
