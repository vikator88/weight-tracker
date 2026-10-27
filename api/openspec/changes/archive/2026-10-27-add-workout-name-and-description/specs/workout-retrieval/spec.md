# Spec Delta

## ADDED Requirements

### Requirement: The workout list carries name and description
Every workout returned by `GET /workouts` SHALL include its `name` and its `description` (or `null`).

#### Scenario: Listed workout shows its name
- **WHEN** a caller lists workouts and one of them has a name and a description
- **THEN** that entry carries the same `name` and `description`

#### Scenario: Listed workout without description
- **WHEN** a caller lists workouts and one of them has no description
- **THEN** that entry carries `description` `null`

## MODIFIED Requirements

### Requirement: The response contains the whole workout without catalog exercise data
A successful read SHALL respond `200 OK` with `id`, `name`, `description` (or `null`), `workoutDate`, `userId`, `trainerId` (or `null`), `status` and the `exercises` list. Each entry SHALL carry its own `id`, the catalog `exerciseId`, `effort`, its `sets` and its `superSeriesId` (or `null`). Entries SHALL NOT carry catalog fields such as `name` or `bodyPart`.

#### Scenario: Workout with exercises and sets
- **WHEN** the owner reads a workout that has exercises with sets
- **THEN** the response is `200 OK` with its `name`, `description`, `status`, every entry, its sets and its superseries membership
- **AND** no entry contains `name` or `bodyPart`

#### Scenario: Empty workout
- **WHEN** the owner reads a workout with no exercises
- **THEN** the response is `200 OK` with an empty `exercises` list
