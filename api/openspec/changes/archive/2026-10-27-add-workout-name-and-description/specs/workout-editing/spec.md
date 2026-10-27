# Spec Delta

## ADDED Requirements

### Requirement: Updating a workout's details is an edit
Updating a workout's `name`, `description` or `workoutDate` through `PUT /workouts/{id}` SHALL follow the same edit rule as changing its content: admins always, the prescribing trainer of a prescribed workout, and the owner of a workout without trainer. Every other caller SHALL be rejected with `403 Forbidden` and the workout SHALL be unchanged.

#### Scenario: Prescribing trainer renames a workout
- **WHEN** the prescribing trainer of a workout updates its name
- **THEN** the update is applied

#### Scenario: Owner renames a self-planned workout
- **WHEN** the owner of a workout without trainer updates its name
- **THEN** the update is applied

#### Scenario: Owner of a prescribed workout is rejected
- **WHEN** the owner of a workout that has a prescribing trainer updates its name
- **THEN** the response is `403 Forbidden`
- **AND** the workout is unchanged

#### Scenario: Unrelated caller is rejected
- **WHEN** a user who is neither the owner nor the trainer updates a workout
- **THEN** the response is `403 Forbidden`

#### Scenario: Admin renames any workout
- **WHEN** an admin updates the name of a workout they neither own nor train
- **THEN** the update is applied

## MODIFIED Requirements

### Requirement: Started and finished workouts are locked for non-admins
Every workout-editing operation (updating the name, description or date, adding, updating or removing exercises, creating or breaking superseries) on a `STARTED` or `FINISHED` workout SHALL be rejected with `409 Conflict` for every non-admin caller, including one the edit rule would otherwise allow. The workout SHALL be unchanged. Status changes are not edits and follow the workout status rules instead.

#### Scenario: Trainer edits a started workout
- **WHEN** the prescribing trainer adds an exercise to a `STARTED` workout
- **THEN** the response is `409 Conflict`
- **AND** the workout is unchanged

#### Scenario: Owner edits a finished self-planned workout
- **WHEN** the owner of a `FINISHED` workout with no prescribing trainer updates one of its exercises
- **THEN** the response is `409 Conflict`

#### Scenario: Trainer renames a started workout
- **WHEN** the prescribing trainer updates the name of a `STARTED` workout
- **THEN** the response is `409 Conflict`
- **AND** the workout is unchanged

#### Scenario: Unauthorized caller on a locked workout
- **WHEN** a caller the edit rule rejects tries to edit a `STARTED` workout
- **THEN** the response is `403 Forbidden`

### Requirement: Admins bypass the edit lock
A caller with the `ADMIN` role SHALL be allowed to edit a workout in any status.

#### Scenario: Admin edits a finished workout
- **WHEN** an admin removes an exercise from a `FINISHED` workout
- **THEN** the edit is applied

#### Scenario: Admin renames a finished workout
- **WHEN** an admin updates the name of a `FINISHED` workout
- **THEN** the update is applied
