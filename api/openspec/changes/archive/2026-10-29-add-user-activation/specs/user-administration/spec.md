# Spec Delta

## ADDED Requirements

### Requirement: Users start active
Every user SHALL start active with no deactivation date, whether created by signup, by an administrator, or already stored before this change.

#### Scenario: Created user is active
- **WHEN** an `ADMIN` creates a user and then calls `GET /users`
- **THEN** that user has `isActive` `true` and `deactivatedAtUtc` `null`

#### Scenario: Seeded users are active
- **WHEN** an `ADMIN` calls `GET /users` on a freshly migrated and seeded database
- **THEN** every user has `isActive` `true` and `deactivatedAtUtc` `null`

### Requirement: Only administrators can activate or deactivate users
The system SHALL expose `PUT /users/{id}/activate` and `PUT /users/{id}/deactivate` to `ADMIN` callers only. A caller without a valid access token SHALL get `401 Unauthorized`, and a `USER` or `TRAINER` caller SHALL get `403 Forbidden` with no change.

#### Scenario: Anonymous caller
- **WHEN** either route is called without an access token, or with a malformed one
- **THEN** the response is `401 Unauthorized`

#### Scenario: Athlete caller
- **WHEN** a `USER` calls either route for an existing user
- **THEN** the response is `403 Forbidden` and the target user is unchanged

#### Scenario: Trainer caller
- **WHEN** a `TRAINER` calls either route for an existing user
- **THEN** the response is `403 Forbidden` and the target user is unchanged

### Requirement: An administrator deactivates an active user
`PUT /users/{id}/deactivate` SHALL mark an active user as deactivated, set `deactivatedAtUtc` to the current UTC time, and return `200 OK` with the updated user item.

#### Scenario: Deactivate an active user
- **WHEN** an `ADMIN` deactivates an active `USER`
- **THEN** the response is `200 OK` with `isActive` `false` and a non-null `deactivatedAtUtc`
- **AND** `GET /users` shows the same state for that user

#### Scenario: Deactivate a trainer
- **WHEN** an `ADMIN` deactivates an active `TRAINER`
- **THEN** the response is `200 OK` with `isActive` `false`

### Requirement: An already deactivated user cannot be deactivated again
Deactivating a user who is already deactivated SHALL be rejected with `409 Conflict` and SHALL NOT change the stored deactivation date.

#### Scenario: Deactivate twice
- **WHEN** an `ADMIN` deactivates a user who is already deactivated
- **THEN** the response is `409 Conflict`
- **AND** `deactivatedAtUtc` keeps its original value

### Requirement: An administrator cannot deactivate themselves
`PUT /users/{id}/deactivate` with the caller's own id SHALL be rejected with `403 Forbidden` and change nothing.

#### Scenario: Admin targets themselves
- **WHEN** an `ADMIN` calls `PUT /users/{id}/deactivate` with their own id
- **THEN** the response is `403 Forbidden`
- **AND** the admin stays active and can still log in

### Requirement: An administrator activates a deactivated user
`PUT /users/{id}/activate` SHALL mark a deactivated user as active, clear `deactivatedAtUtc`, and return `200 OK` with the updated user item.

#### Scenario: Reactivate a user
- **WHEN** an `ADMIN` activates a deactivated user
- **THEN** the response is `200 OK` with `isActive` `true` and `deactivatedAtUtc` `null`

#### Scenario: Reactivated user can log in
- **WHEN** a user is deactivated, then activated, and then logs in with valid credentials
- **THEN** the login succeeds

### Requirement: An already active user cannot be activated
Activating a user who is already active SHALL be rejected with `409 Conflict` and change nothing.

#### Scenario: Activate an active user
- **WHEN** an `ADMIN` activates a user who is active
- **THEN** the response is `409 Conflict`

### Requirement: Activation targets must exist
Both routes SHALL answer `404 Not Found` when no user has the given id, and when the id is not a valid identifier, since such a path matches no route.

#### Scenario: Unknown user
- **WHEN** an `ADMIN` calls either route with an id that matches no user
- **THEN** the response is `404 Not Found`

#### Scenario: Malformed id
- **WHEN** an `ADMIN` calls either route with an id that is not a valid identifier
- **THEN** the response is `404 Not Found`

### Requirement: Deactivated users cannot log in
`POST /auth/login` with valid credentials of a deactivated user SHALL respond `403 Forbidden` with a problem body whose `code` is `USER_DEACTIVATED`, and SHALL NOT issue any token. Invalid credentials SHALL keep answering `401 Unauthorized` without revealing the account state.

#### Scenario: Valid credentials of a deactivated user
- **WHEN** a deactivated user logs in with the correct email and password
- **THEN** the response is `403 Forbidden` with `code` `USER_DEACTIVATED`
- **AND** the body contains no access token or refresh token

#### Scenario: Wrong password of a deactivated user
- **WHEN** someone logs in with a deactivated user's email and a wrong password
- **THEN** the response is `401 Unauthorized` with no `code` `USER_DEACTIVATED`

### Requirement: Deactivated users cannot renew sessions
`POST /auth/refresh` with an otherwise valid refresh token of a deactivated user SHALL respond `403 Forbidden` with `code` `USER_DEACTIVATED` and SHALL NOT issue new tokens.

#### Scenario: Refresh after deactivation
- **WHEN** a user logs in, an `ADMIN` deactivates them, and they call `POST /auth/refresh` with their refresh token
- **THEN** the response is `403 Forbidden` with `code` `USER_DEACTIVATED`
- **AND** the body contains no tokens

## MODIFIED Requirements

### Requirement: Listed users expose only public profile fields
Each listed user SHALL contain exactly `id`, `email`, `name`, `surname`, `role`, `isActive` and `deactivatedAtUtc`. `deactivatedAtUtc` SHALL be `null` for an active user. It SHALL NOT contain the password, the password hash, the date of birth, the creation date, or any token.

#### Scenario: Item shape
- **WHEN** an `ADMIN` calls `GET /users`
- **THEN** every item contains exactly `id`, `email`, `name`, `surname`, `role`, `isActive` and `deactivatedAtUtc`

#### Scenario: Deactivated user in the list
- **WHEN** an `ADMIN` deactivates a user and then calls `GET /users`
- **THEN** that user is still listed, with `isActive` `false` and a non-null `deactivatedAtUtc`
