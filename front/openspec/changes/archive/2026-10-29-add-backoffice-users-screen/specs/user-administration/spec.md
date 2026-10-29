# Spec Delta

## Purpose

Lets both fronts list the users the caller may see and create a user of any role through the API, with the structural
rules of the create form shared in one place and every failure returned as a `DomainError` code.

## ADDED Requirements

### Requirement: Listing users returns what the API answers

Listing users SHALL send `GET /users` with no body and SHALL return a successful result holding the users the API
answers with, unchanged and in the same order: id, email, name, surname and role. It SHALL NOT filter by role.

#### Scenario: Users listed

- **WHEN** users are listed and the API answers with two users
- **THEN** exactly one `GET /users` request is sent with no body
- **AND** the result is successful and holds those two users in the API's order

#### Scenario: Empty list

- **WHEN** users are listed and the API answers with an empty list
- **THEN** the result is successful and holds an empty list

### Requirement: Creating a user sends the given data unchanged

Creating a user SHALL send `POST /users` with the given email, password, role and the optional name, surname and date
of birth, unchanged. It SHALL return a successful result holding the created user the API answers with. It SHALL NOT
check the caller's role and SHALL NOT change the stored session.

#### Scenario: User created with every field

- **WHEN** a user is created with an email, a password, role `TRAINER`, a name, a surname and a date of birth, and the API accepts it
- **THEN** exactly one `POST /users` request is sent with that body, unchanged
- **AND** the result is successful and its value is the user the API answered with

#### Scenario: Only the required fields

- **WHEN** a user is created with only an email, a password and a role
- **THEN** the request body holds only those three fields

#### Scenario: Session untouched

- **WHEN** a user is created successfully
- **THEN** the stored session tokens are unchanged

### Requirement: User administration failures are returned as domain error codes

Neither operation SHALL throw. A failed request SHALL return a failed result whose code comes from the shared HTTP error
mapping. A status the mapping does not list SHALL become `UNEXPECTED`.

#### Scenario: Email already registered

- **WHEN** the API answers the creation with `409`
- **THEN** the result is a failure with code `CONFLICT` and the API's detail kept in the message

#### Scenario: Rejected data

- **WHEN** the API answers the creation with `400`
- **THEN** the result is a failure with code `VALIDATION`

#### Scenario: Caller not allowed

- **WHEN** the API answers either operation with `401` or `403`
- **THEN** the result is a failure with code `UNAUTHORIZED` or `FORBIDDEN` respectively

#### Scenario: Trainer listing not implemented

- **WHEN** the API answers the listing with `501`
- **THEN** the result is a failure with code `UNEXPECTED`

#### Scenario: No connection

- **WHEN** the request cannot reach the API
- **THEN** the result is a failure with code `NETWORK`

### Requirement: The create user form has shared structural rules

Both fronts SHALL check the create user form with one shared, pure rule set before sending it: email required and
shaped like an email; password required; role required; name and surname optional, at most 100 characters after
trimming; date of birth optional, a real calendar day in `YYYY-MM-DD`. It SHALL NOT check password strength.

#### Scenario: Required fields missing

- **WHEN** the form is checked with an empty email, an empty password and no role
- **THEN** email, password and role are each reported as `required`

#### Scenario: Malformed email

- **WHEN** the form is checked with the email "ana.example.com"
- **THEN** email is reported as `email`

#### Scenario: Optional fields empty

- **WHEN** the form is checked with a valid email, a password, a role and empty name, surname and date of birth
- **THEN** no violation is reported

#### Scenario: Name too long

- **WHEN** the form is checked with a name of 101 characters after trimming
- **THEN** name is reported as `maxLength`

#### Scenario: Impossible date

- **WHEN** the form is checked with the date of birth "2001-02-30"
- **THEN** date of birth is reported as `date`

#### Scenario: Weak password accepted

- **WHEN** the form is checked with the password "a"
- **THEN** no violation is reported for the password
