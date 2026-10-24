# user-profile Specification

## Purpose

Lets an authenticated caller read their own stored profile and edit the parts of it they own, name and surname, while keeping email and role out of reach of this flow.

## Requirements

### Requirement: Profile routes require an authenticated caller
The system SHALL expose `GET /users/me` and `PUT /users/me` to authenticated callers only. A request without a valid access token SHALL be rejected with `401 Unauthorized` and SHALL NOT change any user.

#### Scenario: Read without a token
- **WHEN** a caller sends `GET /users/me` without an access token
- **THEN** the response is `401 Unauthorized`

#### Scenario: Read with an invalid token
- **WHEN** a caller sends `GET /users/me` with a malformed or wrongly signed access token
- **THEN** the response is `401 Unauthorized`

#### Scenario: Edit without a token
- **WHEN** a caller sends `PUT /users/me` with a valid body and no access token
- **THEN** the response is `401 Unauthorized`
- **AND** no user is changed

#### Scenario: Edit with an invalid token
- **WHEN** a caller sends `PUT /users/me` with a valid body and a malformed or wrongly signed access token
- **THEN** the response is `401 Unauthorized`
- **AND** no user is changed

#### Scenario: Edit with the token of a removed user
- **WHEN** a user logs in, the user is then removed, and the token is used on `PUT /users/me`
- **THEN** the response is `401 Unauthorized`

### Requirement: The caller reads their own profile
`GET /users/me` SHALL respond `200 OK` with the stored `id`, `email`, `name`, `surname` and `role` of the user the token belongs to. The response SHALL NOT contain the password hash, the date of birth or the creation date. It SHALL be served to every role.

#### Scenario: Each role reads their own profile
- **WHEN** a seeded `USER`, `TRAINER` or `ADMIN` calls `GET /users/me` with their valid token
- **THEN** the response is `200 OK` with that user's `id`, `email`, `name`, `surname` and `role`

#### Scenario: Response excludes private fields
- **WHEN** any caller reads their profile
- **THEN** the response contains no password, password hash, date of birth or creation date

#### Scenario: Only the caller's profile is returned
- **WHEN** a seeded `ADMIN` calls `GET /users/me`
- **THEN** the response carries the admin's own `id` and `email`
- **AND** does not carry any other user's data

### Requirement: The caller edits their own name and surname
`PUT /users/me` SHALL replace the caller's `name` and `surname` and respond `200 OK` with the updated profile, in the same shape as `GET /users/me`. Both fields SHALL be required. Values SHALL be trimmed and stored as trimmed.

#### Scenario: Successful edit
- **WHEN** an authenticated caller sends `PUT /users/me` with a valid `name` and `surname`
- **THEN** the response is `200 OK` with the new `name` and `surname` and unchanged `id`, `email` and `role`
- **AND** a later `GET /users/me` returns the new values

#### Scenario: Surrounding whitespace is trimmed
- **WHEN** the caller sends `name` `"  Ana  "`
- **THEN** the stored and returned `name` is `"Ana"`

#### Scenario: Edit only affects the caller
- **WHEN** a caller edits their profile
- **THEN** no other user's name or surname changes

### Requirement: Invalid profile input is rejected without changes
`PUT /users/me` SHALL respond `400 Bad Request` when `name` or `surname` is missing, empty, whitespace only, or longer than 100 characters, and SHALL NOT change the stored profile.

#### Scenario: Missing field
- **WHEN** the caller sends `PUT /users/me` without `surname`
- **THEN** the response is `400 Bad Request`
- **AND** the stored profile is unchanged

#### Scenario: Blank value
- **WHEN** the caller sends `name` as `"   "`
- **THEN** the response is `400 Bad Request`
- **AND** the stored profile is unchanged

#### Scenario: Too long value
- **WHEN** the caller sends a `surname` of 101 characters
- **THEN** the response is `400 Bad Request`
- **AND** the stored profile is unchanged

### Requirement: Email and role cannot be changed through the profile edit
`PUT /users/me` SHALL NOT change the caller's `email` or `role`. If the body contains `email` or `role` fields, they SHALL be ignored and the request SHALL be processed on `name` and `surname` alone.

#### Scenario: Role in the body is ignored
- **WHEN** a seeded `USER` sends `PUT /users/me` with a valid `name` and `surname` and `role` set to `ADMIN`
- **THEN** the response is `200 OK` with `role` still `USER`
- **AND** a later `GET /users/me` still returns `role` `USER`

#### Scenario: Email in the body is ignored
- **WHEN** a caller sends `PUT /users/me` with a valid `name` and `surname` and a different `email`
- **THEN** the response is `200 OK` with the original `email`
- **AND** the caller can still log in with the original email
