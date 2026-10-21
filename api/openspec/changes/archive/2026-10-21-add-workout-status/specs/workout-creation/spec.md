# Spec Delta

## ADDED Requirements

### Requirement: New workouts start as drafts
Every workout created through `POST /workouts` SHALL have status `DRAFT`, whatever the caller's role. The request SHALL NOT be able to choose the initial status.

#### Scenario: User creates a draft
- **WHEN** a user creates their own workout
- **THEN** the response carries `status` `DRAFT`

#### Scenario: Trainer creates a draft
- **WHEN** a trainer prescribes a workout to a user
- **THEN** the response carries `status` `DRAFT`

## MODIFIED Requirements

### Requirement: Creation returns the new workout
A successful creation SHALL respond `201 Created` with the new workout: a server-generated `id`, `workoutDate`, `userId`, `trainerId` (or `null`), `status` and an empty `exercises` list. The response SHALL carry a `Location` header with the URL of `GET /workouts/{id}` for the new workout. The new workout SHALL be visible through `GET /workouts` to its trainer when set, to admins, and to its owner when it has no trainer. A prescribed workout SHALL stay hidden from its owner while it is `DRAFT`.

#### Scenario: Created workout is readable
- **WHEN** a caller creates a workout successfully
- **THEN** the response is `201 Created` with the workout, `status` `DRAFT` and an empty `exercises` list
- **AND** `GET /workouts` by the caller includes a workout with that `id`

#### Scenario: Location points at the new workout
- **WHEN** a caller creates a workout successfully
- **THEN** the `Location` header ends with `/workouts/{id}` of the created workout
- **AND** following it as the same caller returns `200 OK` with that workout

#### Scenario: Prescribed draft is hidden from its owner
- **WHEN** a trainer prescribes a workout to a user
- **THEN** `GET /workouts` by that user does not include the new workout
