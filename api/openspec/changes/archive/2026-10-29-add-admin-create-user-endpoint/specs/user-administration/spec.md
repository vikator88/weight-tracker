# Spec Delta

## Purpose

Lets an administrator manage other people's accounts. It starts with creating a user of any role on someone
else's behalf, without opening a session for that user.

## ADDED Requirements

### Requirement: Only administrators can create users
The system SHALL expose `POST /users` to authenticated callers whose stored role is `ADMIN`. A caller without a valid access token SHALL get `401 Unauthorized`, a `USER` or `TRAINER` caller SHALL get `403 Forbidden`, and in both cases no user SHALL be created.

#### Scenario: Anonymous caller
- **WHEN** `POST /users` is called without an access token, or with a malformed one
- **THEN** the response is `401 Unauthorized` and no user is created

#### Scenario: Athlete caller
- **WHEN** a `USER` calls `POST /users` with a valid body
- **THEN** the response is `403 Forbidden` and no user is created

#### Scenario: Trainer caller
- **WHEN** a `TRAINER` calls `POST /users` with a valid body
- **THEN** the response is `403 Forbidden` and no user is created

### Requirement: An administrator creates a user of any role
`POST /users` SHALL create a user with the given `email`, `password` and `role`, where `role` is any of `USER`, `TRAINER` or `ADMIN`. The created user SHALL be able to log in with that email and password.

#### Scenario: Create a user of each role
- **WHEN** an `ADMIN` calls `POST /users` with a new email, a strong password and role `USER`, `TRAINER` or `ADMIN`
- **THEN** the response is `201 Created`, the user is stored with that role, and `POST /auth/login` with that email and password succeeds

### Requirement: Name, surname and date of birth are optional
`name`, `surname` and `dateBirth` MAY be omitted. When given, they SHALL be stored; `name` and `surname` are trimmed. When omitted, `name` and `surname` SHALL be stored empty and `dateBirth` SHALL be stored as `0001-01-01`, the same unset values signup uses.

#### Scenario: Only required fields
- **WHEN** an `ADMIN` calls `POST /users` with only `email`, `password` and `role`
- **THEN** the response is `201 Created` with empty `name` and `surname` and `dateBirth` `0001-01-01`

#### Scenario: All fields
- **WHEN** an `ADMIN` calls `POST /users` with `email`, `password`, `role`, `name`, `surname` and `dateBirth`
- **THEN** the response is `201 Created` and echoes the given values, with `name` and `surname` trimmed

### Requirement: The creation result carries no authentication data
A successful `POST /users` SHALL respond with the created user's `id`, `email`, `name`, `surname`, `dateBirth` and `role` only. It SHALL NOT contain an access token, a refresh token, a password or a password hash, and SHALL NOT start a session for the created user.

#### Scenario: Response shape
- **WHEN** an `ADMIN` creates a user successfully
- **THEN** the body contains exactly `id`, `email`, `name`, `surname`, `dateBirth` and `role`, and no token or password field

#### Scenario: No session for the created user
- **WHEN** an `ADMIN` creates a user successfully
- **THEN** no refresh token exists for the created user

### Requirement: Invalid creation input is rejected without changes
`POST /users` SHALL respond `400 Bad Request` and create no user when `email`, `password` or `role` is missing, the email is malformed, the password is not strong enough, the role is not a known value, or a given `name` or `surname` is empty, whitespace only, or longer than 100 characters.

#### Scenario: Malformed email
- **WHEN** an `ADMIN` calls `POST /users` with an email that is not a valid address
- **THEN** the response is `400 Bad Request` and no user is created

#### Scenario: Weak password
- **WHEN** an `ADMIN` calls `POST /users` with a password that does not meet the strength rules
- **THEN** the response is `400 Bad Request` and no user is created

#### Scenario: Missing required field
- **WHEN** an `ADMIN` calls `POST /users` without `email`, `password` or `role`
- **THEN** the response is `400 Bad Request` and no user is created

#### Scenario: Unknown role
- **WHEN** an `ADMIN` calls `POST /users` with a `role` that is not `USER`, `TRAINER` or `ADMIN`
- **THEN** the response is `400 Bad Request` and no user is created

#### Scenario: Invalid name
- **WHEN** an `ADMIN` calls `POST /users` with a `name` or `surname` that is whitespace only or longer than 100 characters
- **THEN** the response is `400 Bad Request` and no user is created

### Requirement: Email must be unique
`POST /users` SHALL respond `409 Conflict` and create no user when a user with the same email already exists.

#### Scenario: Email already registered
- **WHEN** an `ADMIN` calls `POST /users` with the email of an existing user
- **THEN** the response is `409 Conflict` and the existing user is unchanged
