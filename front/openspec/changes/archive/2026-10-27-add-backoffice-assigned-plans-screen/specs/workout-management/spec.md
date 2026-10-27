# Spec Delta

## ADDED Requirements

### Requirement: Listing workouts returns what the API answers

Listing workouts SHALL send `GET /workouts` with no body and SHALL return a successful result holding the list the API
answers with, unchanged. It SHALL NOT filter, sort or check the caller's role.

#### Scenario: Workouts listed

- **WHEN** workouts are listed and the API answers with two workouts
- **THEN** exactly one `GET /workouts` request is sent without a body
- **AND** the result is successful and its value is that list, in the same order

#### Scenario: No workouts

- **WHEN** the API answers with an empty list
- **THEN** the result is successful and its value is an empty list

#### Scenario: Role not allowed

- **WHEN** the API answers 403
- **THEN** the result is a failure with code `FORBIDDEN`

### Requirement: Reading a workout by id returns it with its exercises

Reading a workout SHALL send `GET /workouts/{id}` with the given workout id in the path and no body, and SHALL return a
successful result holding the workout and its exercises as the API answers them.

#### Scenario: Workout read

- **WHEN** a workout is read by id and the API answers with it
- **THEN** exactly one `GET /workouts/{id}` request is sent with that id in the path
- **AND** the result is successful and its value is the API's answer

#### Scenario: Unknown workout

- **WHEN** the API answers 404
- **THEN** the result is a failure with code `NOT_FOUND`

#### Scenario: Workout of someone else

- **WHEN** the API answers 403
- **THEN** the result is a failure with code `FORBIDDEN`
