# Spec Delta

## MODIFIED Requirements

### Requirement: Listing users returns what the API answers

Listing users SHALL send `GET /users` with no body and SHALL return a successful result holding the users the API
answers with, unchanged and in the same order: id, email, name, surname, role, whether the user is active, and when
the user was deactivated (empty for an active user). It SHALL NOT filter by role or by activation state.

#### Scenario: Users listed

- **WHEN** users are listed and the API answers with two users
- **THEN** exactly one `GET /users` request is sent with no body
- **AND** the result is successful and holds those two users in the API's order

#### Scenario: Empty list

- **WHEN** users are listed and the API answers with an empty list
- **THEN** the result is successful and holds an empty list

#### Scenario: Activation state kept

- **WHEN** users are listed and the API answers with one active user and one user deactivated on "2026-10-20T09:30:00Z"
- **THEN** the result holds the first user as active with no deactivation date
- **AND** the second as inactive with the deactivation date "2026-10-20T09:30:00Z", unchanged

## ADDED Requirements

### Requirement: Deactivating a user sends the user's id

Deactivating a user SHALL send `PUT /users/{id}/deactivate` with the given id and no body, and SHALL return a
successful result holding the user the API answers with. It SHALL NOT check the caller's role or whether the target is
the caller.

#### Scenario: User deactivated

- **WHEN** the user with id "7f1c…" is deactivated and the API accepts it
- **THEN** exactly one `PUT /users/7f1c…/deactivate` request is sent with no body
- **AND** the result is successful and its value is the user the API answered with

### Requirement: Activating a user sends the user's id

Activating a user SHALL send `PUT /users/{id}/activate` with the given id and no body, and SHALL return a successful
result holding the user the API answers with. It SHALL NOT check the caller's role.

#### Scenario: User activated

- **WHEN** the user with id "7f1c…" is activated and the API accepts it
- **THEN** exactly one `PUT /users/7f1c…/activate` request is sent with no body
- **AND** the result is successful and its value is the user the API answered with

### Requirement: Activation failures are returned as domain error codes

Neither activating nor deactivating SHALL throw. A failed request SHALL return a failed result whose code comes from
the shared HTTP error mapping. A status the mapping does not list SHALL become `UNEXPECTED`.

#### Scenario: Already in the requested state

- **WHEN** the API answers either operation with `409`
- **THEN** the result is a failure with code `CONFLICT` and the API's detail kept in the message

#### Scenario: Unknown user

- **WHEN** the API answers either operation with `404`
- **THEN** the result is a failure with code `NOT_FOUND`

#### Scenario: Caller not allowed

- **WHEN** the API answers either operation with `401` or `403`
- **THEN** the result is a failure with code `UNAUTHORIZED` or `FORBIDDEN` respectively

#### Scenario: Malformed id

- **WHEN** the API answers either operation with `400`
- **THEN** the result is a failure with code `VALIDATION`

#### Scenario: Server error

- **WHEN** the API answers either operation with `500`
- **THEN** the result is a failure with code `UNEXPECTED`

#### Scenario: No connection

- **WHEN** the request cannot reach the API
- **THEN** the result is a failure with code `NETWORK`
