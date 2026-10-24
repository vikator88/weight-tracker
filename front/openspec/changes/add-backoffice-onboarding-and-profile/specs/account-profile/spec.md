# Spec Delta

## MODIFIED Requirements

### Requirement: Updating the profile replaces name and surname

Updating the profile SHALL trim the given name and surname, check them with the shared name rules, and send only the
trimmed values to the API. It SHALL return a successful result holding the updated profile the API answers with. It
SHALL NOT add any other field.

#### Scenario: Profile updated

- **WHEN** the profile is updated with name "Ana" and surname "García" and the API accepts it
- **THEN** exactly one `PUT /users/me` request is sent with the body `{ name: "Ana", surname: "García" }`
- **AND** the result is successful and its value is the profile the API answered with

#### Scenario: Values trimmed before sending

- **WHEN** the profile is updated with name " Ana " and surname "García "
- **THEN** the request body is `{ name: "Ana", surname: "García" }`

#### Scenario: Invalid value rejected before the request

- **WHEN** the profile is updated with name "Al" and surname "García"
- **THEN** the result fails with code `VALIDATION`
- **AND** no request is sent

## ADDED Requirements

### Requirement: Name and surname follow shared length rules

Both fronts SHALL validate a profile name or surname with one shared rule set: after trimming, an empty value is
`required`, fewer than 3 characters is `minLength`, more than 100 characters is `maxLength`, and anything else is valid.
The rules SHALL be pure: no request, no storage, no exception.

#### Scenario: Empty or whitespace-only

- **WHEN** the value is "" or " "
- **THEN** the rule answers `required`

#### Scenario: Boundaries

- **WHEN** the value has 2, 3, 100 or 101 characters
- **THEN** the rule answers `minLength`, valid, valid and `maxLength` respectively

#### Scenario: Surrounding spaces do not count

- **WHEN** the value is " ab "
- **THEN** the rule answers `minLength`
