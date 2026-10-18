# Spec Delta

## Purpose

Lets authenticated callers read a single workout by id, with the visibility each role is entitled to, without catalog exercise data.

## ADDED Requirements

### Requirement: Authenticated callers may read a workout by id
The system SHALL expose `GET /workouts/{id}` to authenticated callers. A request without a valid access token SHALL be rejected with `401 Unauthorized`.

#### Scenario: No token
- **WHEN** a caller sends `GET /workouts/{id}` without an access token
- **THEN** the response is `401 Unauthorized`

#### Scenario: Invalid token
- **WHEN** a caller sends `GET /workouts/{id}` with a malformed or wrongly signed access token
- **THEN** the response is `401 Unauthorized`

### Requirement: The response contains the whole workout without catalog exercise data
A successful read SHALL respond `200 OK` with `id`, `workoutDate`, `userId`, `trainerId` (or `null`) and the `exercises` list. Each entry SHALL carry its own `id`, the catalog `exerciseId`, `effort`, its `sets` and its `superSeriesId` (or `null`). Entries SHALL NOT carry catalog fields such as `name` or `bodyPart`.

#### Scenario: Workout with exercises and sets
- **WHEN** the owner reads a workout that has exercises with sets
- **THEN** the response is `200 OK` with every entry, its sets and its superseries membership
- **AND** no entry contains `name` or `bodyPart`

#### Scenario: Empty workout
- **WHEN** the owner reads a workout with no exercises
- **THEN** the response is `200 OK` with an empty `exercises` list

### Requirement: Visibility depends on the caller's role
An ADMIN SHALL read any workout. A TRAINER SHALL read workouts they own and workouts where they are the trainer. A USER SHALL read only workouts they own. An existing workout outside the caller's scope SHALL be rejected with `403 Forbidden` and its content SHALL NOT be disclosed.

#### Scenario: Admin reads any workout
- **WHEN** an ADMIN reads a workout owned by another user with another trainer
- **THEN** the response is `200 OK`

#### Scenario: Trainer reads a prescribed workout
- **WHEN** a TRAINER reads a workout where they are the trainer
- **THEN** the response is `200 OK`

#### Scenario: Trainer reads their own workout
- **WHEN** a TRAINER reads a workout they own
- **THEN** the response is `200 OK`

#### Scenario: Trainer reads an unrelated workout
- **WHEN** a TRAINER reads a workout they neither own nor train
- **THEN** the response is `403 Forbidden`

#### Scenario: User reads their own workout
- **WHEN** a USER reads a workout they own
- **THEN** the response is `200 OK`

#### Scenario: User reads another user's workout
- **WHEN** a USER reads a workout owned by someone else, even one where their trainer is set
- **THEN** the response is `403 Forbidden`

### Requirement: Unknown workouts are not found
A well-formed id that matches no workout SHALL be rejected with `404 Not Found`. An id that is not a valid GUID SHALL NOT match the route.

#### Scenario: Missing workout
- **WHEN** an authenticated caller reads a GUID that matches no workout
- **THEN** the response is `404 Not Found`

#### Scenario: Malformed id
- **WHEN** an authenticated caller sends `GET /workouts/not-a-guid`
- **THEN** the response is `404 Not Found`
