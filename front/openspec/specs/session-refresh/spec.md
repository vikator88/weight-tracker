# session-refresh Specification

## Purpose

Keeps a signed-in user working after the access token expires, by renewing it with the stored refresh token and
retrying the failed call, and ends the session in each front when the refresh token can no longer be used.

## Requirements

### Requirement: Refreshing sends the stored refresh token and stores the new tokens

Refreshing the session SHALL send the stored refresh token to `POST /auth/refresh`. On success it SHALL replace both
stored tokens with the ones the API answered and return a successful result holding them.

#### Scenario: Refresh accepted

- **WHEN** a refresh token is stored and the API answers the refresh with new access and refresh tokens
- **THEN** the request body holds the stored refresh token
- **AND** the stored access and refresh tokens are the new ones
- **AND** the result is successful and holds the new tokens

### Requirement: No stored refresh token ends the session without a request

When no refresh token is stored, refreshing SHALL NOT call the API. It SHALL remove both stored tokens and fail with
`UNAUTHORIZED`.

#### Scenario: Nothing to refresh with

- **WHEN** no refresh token is stored and the session is refreshed
- **THEN** no request is made
- **AND** no token is stored afterwards
- **AND** the result fails with `UNAUTHORIZED`

### Requirement: A rejected refresh token ends the session

When the API rejects the refresh with `401` or `400`, refreshing SHALL remove both stored tokens and fail with
`UNAUTHORIZED`.

#### Scenario: Refresh token expired or revoked

- **WHEN** the API answers the refresh with `401`
- **THEN** no token is stored afterwards
- **AND** the result fails with `UNAUTHORIZED`

#### Scenario: Refresh request rejected as malformed

- **WHEN** the API answers the refresh with `400`
- **THEN** no token is stored afterwards
- **AND** the result fails with `UNAUTHORIZED`

### Requirement: A transient refresh failure keeps the session

When the refresh fails with `NETWORK` or `UNEXPECTED`, refreshing SHALL keep the stored tokens and return that
failure unchanged.

#### Scenario: Offline while refreshing

- **WHEN** the refresh request gets no response
- **THEN** the stored tokens are unchanged
- **AND** the result fails with `NETWORK`

#### Scenario: Server error while refreshing

- **WHEN** the API answers the refresh with `500`
- **THEN** the stored tokens are unchanged
- **AND** the result fails with `UNEXPECTED`

### Requirement: A storage failure while refreshing is unexpected

If key-value storage fails while refreshing, refreshing SHALL NOT throw. It SHALL fail with `UNEXPECTED`.

#### Scenario: Storage rejects the write

- **WHEN** the API accepts the refresh and key-value storage fails to store the new tokens
- **THEN** the result fails with `UNEXPECTED`

### Requirement: Only one refresh is in flight

While a refresh is in flight, a further refresh in the same front SHALL NOT send a request. It SHALL wait for the
refresh in flight and return the same result. Once that refresh settles, the next refresh sends a new request.

#### Scenario: Two refreshes at once

- **WHEN** two refreshes start before the first one has settled
- **THEN** exactly one refresh request is sent
- **AND** both results are the same

#### Scenario: Refresh after a settled one

- **WHEN** a refresh starts after the previous one has settled
- **THEN** a new refresh request is sent with the refresh token stored at that moment

### Requirement: A protected call that fails as unauthorized is retried once after a refresh

In both fronts, when a protected call fails with `UNAUTHORIZED`, the front SHALL refresh the session once. If the
refresh succeeds, it SHALL run the call once more and use that second result, whatever it is.

#### Scenario: Expired access token, refresh accepted

- **WHEN** a protected call fails with `UNAUTHORIZED` and the refresh succeeds
- **THEN** the call runs a second time
- **AND** the screen shows the second result with no error

#### Scenario: The retried call fails again

- **WHEN** a protected call fails with `UNAUTHORIZED`, the refresh succeeds and the retried call also fails with
  `UNAUTHORIZED`
- **THEN** no further refresh or retry happens
- **AND** the screen shows the failure of the retried call

### Requirement: Other failures are not refreshed

A protected call that fails with any code other than `UNAUTHORIZED` SHALL NOT trigger a refresh or a retry, and its
failure SHALL reach the screen unchanged.

#### Scenario: Offline protected call

- **WHEN** a protected call fails with `NETWORK`
- **THEN** no refresh request is sent and the call does not run again
- **AND** the screen shows its usual network error

### Requirement: A failed refresh ends the session in the front

When the refresh fails with `UNAUTHORIZED`, the front SHALL end the session: its session state becomes signed out and
no signed-in screen stays shown. When the refresh fails with any other code, the session SHALL stay signed in and the
screen SHALL show that failure.

#### Scenario: Refresh token rejected

- **WHEN** a protected call fails with `UNAUTHORIZED` and the refresh fails with `UNAUTHORIZED`
- **THEN** the session state becomes signed out

#### Scenario: Offline while renewing

- **WHEN** a protected call fails with `UNAUTHORIZED` and the refresh fails with `NETWORK`
- **THEN** the session stays signed in
- **AND** the screen shows its usual network error

### Requirement: The previous user's profile is gone after the session ends

In the backoffice, when the session ends, the shared profile of the signed-in user SHALL be cleared, so no screen
shows it afterwards. This holds for `TRAINER` and `ADMIN` alike.

#### Scenario: Session ends on home

- **WHEN** a trainer is on home and the refresh token is rejected
- **THEN** the trainer ends on `/login`
- **AND** the trainer's name is not shown anywhere

### Requirement: Authentication calls never refresh

Login, signup, logout and the refresh itself SHALL NOT trigger a refresh or a retry. A login rejected with
`UNAUTHORIZED` SHALL show the login error as today.

#### Scenario: Wrong credentials

- **WHEN** a login fails with `UNAUTHORIZED`
- **THEN** no refresh request is sent
- **AND** the login screen shows its usual invalid credentials message
