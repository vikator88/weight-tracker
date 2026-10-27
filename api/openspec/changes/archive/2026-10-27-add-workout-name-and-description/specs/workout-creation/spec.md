# Spec Delta

## ADDED Requirements

### Requirement: The workout name is required
The request SHALL carry `name`. It SHALL be trimmed, and SHALL NOT be blank or longer than 256 characters after trimming. A missing, blank or too long `name` SHALL be rejected with `400 Bad Request` and create nothing.

#### Scenario: Name is stored trimmed
- **WHEN** a caller creates a workout with `name` `"  Leg day  "`
- **THEN** the response is `201 Created` with `name` `"Leg day"`

#### Scenario: Missing name
- **WHEN** an authenticated caller sends `POST /workouts` without `name`
- **THEN** the response is `400 Bad Request`
- **AND** no workout is created

#### Scenario: Blank name
- **WHEN** an authenticated caller sends `name` `"   "`
- **THEN** the response is `400 Bad Request`
- **AND** no workout is created

#### Scenario: Name at the limit
- **WHEN** a caller sends a `name` of exactly 256 characters
- **THEN** the response is `201 Created`

#### Scenario: Name too long
- **WHEN** a caller sends a `name` of 257 characters
- **THEN** the response is `400 Bad Request`
- **AND** no workout is created

### Requirement: The workout description is optional
The request MAY carry `description`. It SHALL be trimmed and SHALL NOT be longer than 1024 characters after trimming, or the request SHALL be rejected with `400 Bad Request`. A missing, `null` or blank `description` SHALL be stored and returned as `null`.

#### Scenario: Workout without description
- **WHEN** a caller creates a workout without `description`
- **THEN** the response is `201 Created` with `description` `null`

#### Scenario: Blank description
- **WHEN** a caller creates a workout with `description` `"   "`
- **THEN** the response is `201 Created` with `description` `null`

#### Scenario: Description at the limit
- **WHEN** a caller sends a `description` of exactly 1024 characters
- **THEN** the response is `201 Created` with that description

#### Scenario: Description too long
- **WHEN** a caller sends a `description` of 1025 characters
- **THEN** the response is `400 Bad Request`
- **AND** no workout is created

### Requirement: Existing workouts get a default name
Workouts that existed before names were introduced SHALL be named `Workout` and have no description.

#### Scenario: Migrated workout
- **WHEN** a workout created before this change is read
- **THEN** its `name` is `Workout` and its `description` is `null`

## MODIFIED Requirements

### Requirement: Creation returns the new workout
A successful creation SHALL respond `201 Created` with the new workout: a server-generated `id`, `name`, `description` (or `null`), `workoutDate`, `userId`, `trainerId` (or `null`), `status` and an empty `exercises` list. The response SHALL carry a `Location` header with the URL of `GET /workouts/{id}` for the new workout. The new workout SHALL be visible through `GET /workouts` to its trainer when set, to admins, and to its owner when it has no trainer. A prescribed workout SHALL stay hidden from its owner while it is `DRAFT`.

#### Scenario: Created workout is readable
- **WHEN** a caller creates a workout successfully
- **THEN** the response is `201 Created` with the workout, its `name` and `description`, `status` `DRAFT` and an empty `exercises` list
- **AND** `GET /workouts` by the caller includes a workout with that `id`

#### Scenario: Location points at the new workout
- **WHEN** a caller creates a workout successfully
- **THEN** the `Location` header ends with `/workouts/{id}` of the created workout
- **AND** following it as the same caller returns `200 OK` with that workout

#### Scenario: Prescribed draft is hidden from its owner
- **WHEN** a trainer prescribes a workout to a user
- **THEN** `GET /workouts` by that user does not include the new workout

### Requirement: A regular user creates only their own workout
When the caller has the `USER` role, the system SHALL make the caller the owner and leave the trainer empty. `userId` MAY be omitted or equal to the caller. A different `userId` or any `trainerId` SHALL be rejected with `400 Bad Request`.

#### Scenario: User creates a workout without ids
- **WHEN** a `USER` sends only `name` and `workoutDate`
- **THEN** the response is `201 Created`
- **AND** `userId` is the caller and `trainerId` is `null`

#### Scenario: User sends their own id
- **WHEN** a `USER` sends their own id as `userId`
- **THEN** the response is `201 Created` with the caller as owner

#### Scenario: User names another owner
- **WHEN** a `USER` sends another user's id as `userId`
- **THEN** the response is `400 Bad Request`
- **AND** no workout is created

#### Scenario: User names a trainer
- **WHEN** a `USER` sends any `trainerId`
- **THEN** the response is `400 Bad Request`
- **AND** no workout is created
