# Spec Delta

## ADDED Requirements

### Requirement: The owner and the trainer of a new workout must be active
When a `TRAINER` or `ADMIN` creates a workout, the named owner SHALL be active, and a trainer named by an `ADMIN` SHALL be active. Otherwise the request SHALL be rejected with `400 Bad Request` and no workout SHALL be created. An unknown owner keeps answering `404 Not Found`.

#### Scenario: Trainer prescribes to a deactivated user
- **WHEN** a `TRAINER` sends as `userId` the id of a deactivated `USER`
- **THEN** the response is `400 Bad Request`
- **AND** no workout is created

#### Scenario: Admin creates a workout for a deactivated user
- **WHEN** an `ADMIN` sends as `userId` the id of a deactivated user
- **THEN** the response is `400 Bad Request`

#### Scenario: Admin assigns a deactivated trainer
- **WHEN** an `ADMIN` sends an active `USER` as `userId` and a deactivated `TRAINER` as `trainerId`
- **THEN** the response is `400 Bad Request`

#### Scenario: Reactivated user can be assigned again
- **WHEN** a `TRAINER` prescribes a workout to a `USER` who was deactivated and then activated
- **THEN** the response is `201 Created`
