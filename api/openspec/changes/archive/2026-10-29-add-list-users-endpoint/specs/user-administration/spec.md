# Spec Delta

## ADDED Requirements

### Requirement: Only administrators and trainers can list users
The system SHALL expose `GET /users` to authenticated callers whose stored role is `ADMIN` or `TRAINER`. A caller without a valid access token SHALL get `401 Unauthorized`, and a `USER` caller SHALL get `403 Forbidden` with no user data.

#### Scenario: Anonymous caller
- **WHEN** `GET /users` is called without an access token, or with a malformed one
- **THEN** the response is `401 Unauthorized`

#### Scenario: Athlete caller
- **WHEN** a `USER` calls `GET /users`
- **THEN** the response is `403 Forbidden` and contains no user data

### Requirement: An administrator lists every user
For an `ADMIN` caller, `GET /users` SHALL return `200 OK` with every stored user, whatever their role, including the caller, ordered by email ascending.

#### Scenario: Users of every role
- **WHEN** an `ADMIN` calls `GET /users`
- **THEN** the response is `200 OK` and lists every stored user, including users with role `USER`, `TRAINER` and `ADMIN` and the caller

#### Scenario: A newly created user is listed
- **WHEN** an `ADMIN` creates a user and then calls `GET /users`
- **THEN** the created user is in the list

#### Scenario: Order
- **WHEN** an `ADMIN` calls `GET /users`
- **THEN** the users are ordered by email ascending

### Requirement: Listed users expose only public profile fields
Each listed user SHALL contain exactly `id`, `email`, `name`, `surname` and `role`. It SHALL NOT contain the password, the password hash, the date of birth, the creation date, or any token.

#### Scenario: Item shape
- **WHEN** an `ADMIN` calls `GET /users`
- **THEN** every item contains exactly `id`, `email`, `name`, `surname` and `role`

### Requirement: Trainer listing is not implemented yet
Until a trainer–athlete relationship exists, `GET /users` for a `TRAINER` caller SHALL respond `501 Not Implemented` and SHALL NOT return any user data.

#### Scenario: Trainer caller
- **WHEN** a `TRAINER` calls `GET /users`
- **THEN** the response is `501 Not Implemented` and contains no user data
