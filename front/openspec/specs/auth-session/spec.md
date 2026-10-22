# auth-session Specification

## Purpose

Lets each front find out whether it holds a stored session (the access and refresh tokens it kept after
authenticating) without calling the API, and defines how long that session survives in each front.

## Requirements

### Requirement: Reading the session returns the stored tokens

Reading the session SHALL return a successful result holding the stored access token and refresh token when both
are present in key-value storage. It SHALL NOT call the API.

#### Scenario: Both tokens stored

- **WHEN** key-value storage holds an access token and a refresh token
- **THEN** the result is successful and its value holds those two tokens
- **AND** no API request is made

### Requirement: A missing token means no session

Reading the session SHALL return a successful result with no session when either token is missing. A missing token
SHALL NOT be reported as a failure.

#### Scenario: Nothing stored

- **WHEN** key-value storage holds neither token
- **THEN** the result is successful and its value is empty (no session)

#### Scenario: Only the access token stored

- **WHEN** key-value storage holds an access token but no refresh token
- **THEN** the result is successful and its value is empty (no session)

### Requirement: A storage failure is returned as an unexpected error

If key-value storage fails to read, reading the session SHALL NOT throw. It SHALL return a failed result with code
`UNEXPECTED`.

#### Scenario: Storage rejects the read

- **WHEN** key-value storage fails to read
- **THEN** the result fails with code `UNEXPECTED`

### Requirement: The backoffice session survives a reload

In the backoffice, the stored tokens SHALL be kept in the browser's persistent storage for the site. They SHALL still
be there after a page reload or after opening a new tab of the same site.

#### Scenario: Reload after signup

- **WHEN** a trainer signs up in the backoffice and then reloads the page
- **THEN** reading the session returns the tokens from that signup

### Requirement: The app session survives a restart

In the app, the stored tokens SHALL be kept in persistent on-device storage. They SHALL still be there after the app
is closed and opened again.

#### Scenario: Restart after login

- **WHEN** an athlete logs in and the app restarts
- **THEN** reading the session returns the tokens from that login

#### Scenario: Restart after signup

- **WHEN** an athlete signs up and the app restarts
- **THEN** reading the session returns the tokens from that signup
