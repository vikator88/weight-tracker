# Spec Delta

## ADDED Requirements

### Requirement: A reassigned owner or trainer must be active
When an `ADMIN` changes a workout's owner or trainer, the new owner and the new trainer SHALL be active. Otherwise the request SHALL be rejected with `400 Bad Request` and change nothing. An owner or trainer that is not changed SHALL NOT be checked, so workouts of a deactivated user stay editable.

#### Scenario: Admin reassigns to a deactivated owner
- **WHEN** an `ADMIN` sends as `userId` the id of a deactivated user
- **THEN** the response is `400 Bad Request`
- **AND** the workout keeps its previous owner

#### Scenario: Admin assigns a deactivated trainer
- **WHEN** an `ADMIN` sends as `trainerId` the id of a deactivated `TRAINER`
- **THEN** the response is `400 Bad Request`
- **AND** the workout keeps its previous trainer

#### Scenario: Unchanged deactivated owner
- **WHEN** an `ADMIN` renames a workout whose owner is deactivated, sending the current `userId` and `trainerId`
- **THEN** the response is `200 OK` with the new name
