# Spec Delta

## Purpose

Lets each front read and replace the signed-in user's own profile through the API, and get every failure back as a
`DomainError` code instead of an exception.

## ADDED Requirements

### Requirement: Reading the profile returns the signed-in user's data

Reading the profile SHALL ask the API for the signed-in user's own profile and return a successful result holding it
unchanged: id, email, name, surname and role. It SHALL NOT take any user id as input.

#### Scenario: Profile read

- **WHEN** the API answers the profile request with a profile
- **THEN** the result is successful and its value is that profile, unchanged

#### Scenario: Request sent

- **WHEN** the profile is read
- **THEN** exactly one `GET /users/me` request is sent, with no body

### Requirement: Updating the profile replaces name and surname

Updating the profile SHALL send the given name and surname to the API unchanged and return a successful result holding
the updated profile the API answers with. It SHALL NOT validate, trim or add any other field.

#### Scenario: Profile updated

- **WHEN** the profile is updated with name "Ana" and surname "García" and the API accepts it
- **THEN** exactly one `PUT /users/me` request is sent with the body `{ name: "Ana", surname: "García" }`
- **AND** the result is successful and its value is the profile the API answered with

### Requirement: API failures are returned as domain error codes

Neither operation SHALL throw. A failed request SHALL return a failed result whose code comes from the shared HTTP error
mapping.

#### Scenario: Invalid name or surname

- **WHEN** the API answers the update with `400`
- **THEN** the result fails with code `VALIDATION`
- **AND** the message keeps the API's problem detail when the response has one

#### Scenario: Not signed in

- **WHEN** the API answers either request with `401`
- **THEN** the result fails with code `UNAUTHORIZED`

#### Scenario: No response

- **WHEN** the request gets no response
- **THEN** the result fails with code `NETWORK`

#### Scenario: Unexpected status

- **WHEN** the API answers with `500`
- **THEN** the result fails with code `UNEXPECTED`

### Requirement: The profile is not stored by the front

Reading or updating the profile SHALL NOT write to key-value storage and SHALL NOT change the stored session tokens.

#### Scenario: Read leaves storage untouched

- **WHEN** the profile is read successfully
- **THEN** nothing is written to or removed from key-value storage
