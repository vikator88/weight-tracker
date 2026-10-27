# Spec Delta

## ADDED Requirements

### Requirement: Reorder the exercises of a workout
The system SHALL let a caller who may edit the workout set the order of its exercises by sending the catalog exercise ids of the workout in the new order. The response SHALL be `200 OK` with the workout detail in the new order. Each entry SHALL keep its entry identifier, effort, set prescriptions and superseries.

#### Scenario: Prescribing trainer reorders a workout
- **WHEN** the prescribing trainer of a workout with exercises `A, B` sends the order `B, A`
- **THEN** the response is `200 OK` and lists the entries as `B, A`
- **AND** reading the workout by id returns `B, A` with the same entry ids, efforts and sets as before

#### Scenario: Superseries survive a reorder
- **WHEN** a workout whose exercises `A` and `B` form a superseries is reordered
- **THEN** `A` and `B` still belong to the same superseries

#### Scenario: Owner of a self-created workout reorders it
- **WHEN** the owner of a workout with no prescribing trainer sends a new order
- **THEN** the response is `200 OK` and the new order is stored

#### Scenario: Admin reorders a workout
- **WHEN** an admin sends a new order for any workout
- **THEN** the response is `200 OK` and the new order is stored

### Requirement: The new order lists exactly the exercises of the workout
The order SHALL contain every catalog exercise of the workout exactly once and nothing else. A list with fewer or more ids, an id that is not in the workout, or a repeated id SHALL fail with `409 Conflict` and leave the order unchanged. Workout entry ids are not accepted in place of catalog exercise ids.

#### Scenario: Missing exercise
- **WHEN** a workout has exercises `A, B` and the order `B` is sent
- **THEN** the response is `409 Conflict` and the order stays `A, B`

#### Scenario: Extra exercise
- **WHEN** a workout has exercises `A, B` and the order `B, A, C` is sent
- **THEN** the response is `409 Conflict` and the order stays `A, B`

#### Scenario: Exercise not in the workout
- **WHEN** a workout has exercises `A, B` and the order `B, C` is sent
- **THEN** the response is `409 Conflict` and the order stays `A, B`

#### Scenario: Repeated exercise
- **WHEN** a workout has exercises `A, B` and the order `B, B` is sent
- **THEN** the response is `409 Conflict` and the order stays `A, B`

#### Scenario: Entry ids instead of catalog ids
- **WHEN** the order is sent with workout entry ids instead of catalog exercise ids
- **THEN** the response is `409 Conflict` and the order is unchanged

### Requirement: Reorder request is structurally validated
A reorder request without the `exerciseIds` list SHALL fail with `400 Bad Request` and change nothing.

#### Scenario: Missing list
- **WHEN** the reorder route is called with an empty body
- **THEN** the response is `400 Bad Request`

### Requirement: Reorder route applies the workout edit rule
Reordering SHALL follow the workout editing permission rule. An unknown workout SHALL return `404 Not Found`. A caller who may not edit the workout SHALL get `403 Forbidden`, and the order SHALL be unchanged.

#### Scenario: Owner of a prescribed workout is rejected
- **WHEN** the owner of a workout with a prescribing trainer sends a new order
- **THEN** the response is `403 Forbidden` and the order is unchanged

#### Scenario: Trainer who does not own a self-created workout is rejected
- **WHEN** a trainer sends a new order for a workout with no prescribing trainer that they do not own
- **THEN** the response is `403 Forbidden`

#### Scenario: Another trainer on a prescribed workout is rejected
- **WHEN** a trainer other than the prescribing one sends a new order
- **THEN** the response is `403 Forbidden`

#### Scenario: Unknown workout
- **WHEN** an authenticated caller sends an order for a workout id that does not exist
- **THEN** the response is `404 Not Found`

### Requirement: Reorder route requires authentication
The reorder route SHALL reject a request with no token or with an invalid token with `401 Unauthorized`.

#### Scenario: No token
- **WHEN** the reorder route is called without a bearer token
- **THEN** the response is `401 Unauthorized`

#### Scenario: Invalid token
- **WHEN** the reorder route is called with a malformed or unsigned token
- **THEN** the response is `401 Unauthorized`
