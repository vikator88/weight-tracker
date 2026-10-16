# Spec Delta

## Purpose

Decides who may change an existing workout, so every workout-editing operation applies the same permission rule.

## ADDED Requirements

### Requirement: Admins may edit any workout
An authenticated caller with the `ADMIN` role SHALL be allowed to edit any workout, whether or not it has a prescribing trainer.

#### Scenario: Admin edits a prescribed workout
- **WHEN** an admin edits a workout that has a prescribing trainer
- **THEN** the edit is applied

#### Scenario: Admin edits a self-created workout
- **WHEN** an admin edits a workout that has no prescribing trainer
- **THEN** the edit is applied

### Requirement: Only the prescribing trainer may edit a prescribed workout
When a workout has a prescribing trainer, the system SHALL allow that trainer to edit it and SHALL reject every other non-admin caller, including the workout's owner, with `403 Forbidden`.

#### Scenario: Prescribing trainer edits
- **WHEN** the workout's prescribing trainer edits the workout
- **THEN** the edit is applied

#### Scenario: Owner of a prescribed workout is rejected
- **WHEN** the owner of a workout that has a prescribing trainer tries to edit it
- **THEN** the response is `403 Forbidden`
- **AND** the workout is unchanged

#### Scenario: Another trainer is rejected
- **WHEN** a trainer who is neither the prescribing trainer nor the owner tries to edit a prescribed workout
- **THEN** the response is `403 Forbidden`
- **AND** the workout is unchanged

### Requirement: The owner may edit a workout without a prescribing trainer
When a workout has no prescribing trainer, the system SHALL allow its owner to edit it, whatever the owner's role, and SHALL reject every other non-admin caller with `403 Forbidden`.

#### Scenario: Regular user edits their own workout
- **WHEN** a user with the `USER` role edits a workout they own that has no prescribing trainer
- **THEN** the edit is applied

#### Scenario: Trainer edits their own workout
- **WHEN** a trainer edits a workout they own that has no prescribing trainer
- **THEN** the edit is applied

#### Scenario: A trainer who is not the owner is rejected
- **WHEN** a trainer who does not own a workout with no prescribing trainer tries to edit it
- **THEN** the response is `403 Forbidden`
- **AND** the workout is unchanged

### Requirement: Missing workouts are reported before permissions
An edit aimed at a workout that does not exist SHALL fail with `404 Not Found` for every authenticated caller.

#### Scenario: Unknown workout
- **WHEN** an authenticated caller edits a workout id that does not exist
- **THEN** the response is `404 Not Found`
