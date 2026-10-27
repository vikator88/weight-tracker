## ADDED Requirements

### Requirement: Creating a workout sends its data without exercises

Creating a workout SHALL send `POST /workouts` with the given `name`, `workoutDate`, optional `description` and the optional `userId` and `trainerId`, unchanged, and SHALL return a successful result holding the workout the API answers with. It SHALL NOT send any exercises and SHALL NOT check the caller's role.

#### Scenario: Workout created with every field

- **WHEN** a workout is created with name "Pierna", a date, description "Día 1", a user id and a trainer id and the API accepts it
- **THEN** exactly one `POST /workouts` request is sent with that body, unchanged
- **AND** the result is successful and its value is the workout the API answered with

#### Scenario: Optional fields omitted

- **WHEN** a workout is created with only a name and a date
- **THEN** the request body holds only those two fields

#### Scenario: Rejected by the API

- **WHEN** the API refuses the creation (missing name, missing user or trainer for the caller's role, unknown user or trainer)
- **THEN** the result is a failure holding the matching `DomainError` code (`VALIDATION`, `FORBIDDEN` or `NOT_FOUND`) with the API's detail kept in the message

### Requirement: Updating a workout replaces its editable data

Updating a workout SHALL send `PUT /workouts/{id}` with the given workout id in the path and the given body unchanged, and SHALL return a successful result holding the updated workout. It SHALL NOT decide which fields a role may change.

#### Scenario: Workout updated

- **WHEN** a workout is updated with a new name, description and date and the API accepts it
- **THEN** exactly one `PUT /workouts/{id}` request is sent with that id in the path and the body unchanged
- **AND** the result is successful and its value is the workout the API answered with

#### Scenario: Reassignment by a non-admin

- **WHEN** a non-admin sends a different `userId` or `trainerId` and the API answers 403
- **THEN** the result is a failure with code `FORBIDDEN`

#### Scenario: Unknown workout

- **WHEN** the API answers 404
- **THEN** the result is a failure with code `NOT_FOUND`

### Requirement: Adding an exercise to a workout sends its prescription

Adding an exercise SHALL send `POST /workouts/{workoutId}/exercises` with the workout id in the path and a body holding `exerciseId`, `effort` and the list of set prescriptions, unchanged, and SHALL return a successful result holding what the API answers with.

#### Scenario: Exercise added

- **WHEN** an exercise is added with an exercise id, an effort and two set prescriptions and the API accepts it
- **THEN** exactly one `POST /workouts/{workoutId}/exercises` request is sent with that body, unchanged
- **AND** the result is successful and its value is the API's answer

#### Scenario: Exercise already in the workout

- **WHEN** the API answers 409
- **THEN** the result is a failure with code `CONFLICT`

#### Scenario: Invalid prescription

- **WHEN** the API answers 400
- **THEN** the result is a failure with code `VALIDATION` and the API's detail kept

### Requirement: Workout operations return failures as results

Every workout operation SHALL return a `DomainError` result instead of throwing: 401 as `UNAUTHORIZED`, 500 and other unlisted statuses as `UNEXPECTED`, and a request that gets no response as `NETWORK`.

#### Scenario: Missing token

- **WHEN** the API answers 401
- **THEN** the result is a failure with code `UNAUTHORIZED`

#### Scenario: No response

- **WHEN** the request is rejected without a response
- **THEN** the result is a failure with code `NETWORK`
