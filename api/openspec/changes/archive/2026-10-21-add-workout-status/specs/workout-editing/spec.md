# Spec Delta

## ADDED Requirements

### Requirement: Started and finished workouts are locked for non-admins
Every workout-editing operation (adding, updating or removing exercises, creating or breaking superseries) on a `STARTED` or `FINISHED` workout SHALL be rejected with `409 Conflict` for every non-admin caller, including one the edit rule would otherwise allow. The workout SHALL be unchanged. Status changes are not edits and follow the workout status rules instead.

#### Scenario: Trainer edits a started workout
- **WHEN** the prescribing trainer adds an exercise to a `STARTED` workout
- **THEN** the response is `409 Conflict`
- **AND** the workout is unchanged

#### Scenario: Owner edits a finished self-planned workout
- **WHEN** the owner of a `FINISHED` workout with no prescribing trainer updates one of its exercises
- **THEN** the response is `409 Conflict`

#### Scenario: Unauthorized caller on a locked workout
- **WHEN** a caller the edit rule rejects tries to edit a `STARTED` workout
- **THEN** the response is `403 Forbidden`

### Requirement: Admins bypass the edit lock
A caller with the `ADMIN` role SHALL be allowed to edit a workout in any status.

#### Scenario: Admin edits a finished workout
- **WHEN** an admin removes an exercise from a `FINISHED` workout
- **THEN** the edit is applied

### Requirement: Draft and published workouts stay editable
The edit rule SHALL apply unchanged to `DRAFT` and `PUBLISHED` workouts. A `PUBLISHED` workout SHALL NOT need to go back to `DRAFT` before it is edited.

#### Scenario: Trainer edits a published workout
- **WHEN** the prescribing trainer updates an exercise of a `PUBLISHED` workout
- **THEN** the edit is applied
- **AND** the status is still `PUBLISHED`
