# Spec Delta

## MODIFIED Requirements

### Requirement: Started and finished workouts are locked for non-admins
Every workout-editing operation (updating the name, description or date, adding, updating, removing or reordering exercises, creating or breaking superseries) on a `STARTED` or `FINISHED` workout SHALL be rejected with `409 Conflict` for every non-admin caller, including one the edit rule would otherwise allow. The workout SHALL be unchanged. Status changes are not edits and follow the workout status rules instead.

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

#### Scenario: Trainer reorders a started workout
- **WHEN** the prescribing trainer sends a new exercise order for a `STARTED` workout
- **THEN** the response is `409 Conflict`
- **AND** the order is unchanged
