# Spec Delta

## MODIFIED Requirements

### Requirement: Superseries needs at least two existing exercises
A superseries SHALL contain at least two exercise entries, each identified by its own workout exercise identifier, each already in the workout and not already part of another superseries.

#### Scenario: Single exercise is rejected
- **WHEN** only one exercise entry is given to group
- **THEN** the operation fails with a domain error and nothing is grouped

#### Scenario: Exercise not in the workout is rejected
- **WHEN** one of the given workout exercise identifiers does not belong to an entry of the workout
- **THEN** the operation fails with a domain error and nothing is grouped

#### Scenario: Exercise already in a superseries is rejected
- **WHEN** an exercise entry that already belongs to a superseries is grouped again
- **THEN** the operation fails with a domain error and the existing superseries is unchanged

#### Scenario: Duplicate exercise in the request is rejected
- **WHEN** the same workout exercise identifier is given twice in the list to group
- **THEN** the operation fails with a domain error and nothing is grouped

#### Scenario: Catalog exercise identifiers are not entry identifiers
- **WHEN** the catalog exercise identifier of an entry is given instead of the entry's own identifier
- **THEN** the operation fails because no entry of the workout has that identifier

## ADDED Requirements

### Requirement: Create a superseries over HTTP
`POST /workouts/{workoutId}/superseries` with body `{ "exerciseIds": [<workout exercise id>, ...] }` SHALL group those entries of the workout into a new superseries and respond `201 Created` with `{ "id": <superseries id> }`. Only callers allowed by the workout edit rule may do it.

#### Scenario: Prescribing trainer creates a superseries
- **WHEN** the prescribing trainer posts two ungrouped entries of the workout with the same set total
- **THEN** the response is `201 Created` with the new superseries id
- **AND** reading the workout shows both entries carrying that id and every other entry with none

#### Scenario: Owner of a self-created workout creates a superseries
- **WHEN** the owner of a workout with no prescribing trainer posts two ungrouped entries with the same set total
- **THEN** the response is `201 Created`

#### Scenario: Admin creates a superseries
- **WHEN** an admin posts two ungrouped entries with the same set total of any workout
- **THEN** the response is `201 Created`

#### Scenario: Caller not allowed by the edit rule
- **WHEN** a caller rejected by the workout edit rule posts a valid request
- **THEN** the response is `403 Forbidden` and no entry is grouped

#### Scenario: Unknown workout
- **WHEN** the workout id does not exist
- **THEN** the response is `404 Not Found`

#### Scenario: Entry not in the workout
- **WHEN** one of the given ids is not an entry of the workout
- **THEN** the response is `404 Not Found` and nothing is grouped

#### Scenario: Entry already grouped
- **WHEN** one of the given entries already belongs to a superseries
- **THEN** the response is `409 Conflict` and the existing superseries is unchanged

#### Scenario: Business rule breach
- **WHEN** fewer than two ids, a repeated id, or entries with different set totals are given
- **THEN** the response is `400 Bad Request` and nothing is grouped

#### Scenario: Structurally invalid body
- **WHEN** the body is missing, `exerciseIds` is missing, or an id is not a valid identifier
- **THEN** the response is `400 Bad Request`

#### Scenario: Unauthenticated caller
- **WHEN** the request carries no token or an invalid token
- **THEN** the response is `401 Unauthorized`

### Requirement: Break a superseries over HTTP
`DELETE /workouts/{workoutId}/superseries/{superSeriesId}` SHALL break that superseries of the workout and respond `204 No Content`, keeping every exercise entry in the workout. Only callers allowed by the workout edit rule may do it.

#### Scenario: Prescribing trainer breaks a superseries
- **WHEN** the prescribing trainer deletes an existing superseries of the workout
- **THEN** the response is `204 No Content`
- **AND** reading the workout shows the same entries, in the same order, none carrying that superseries id

#### Scenario: Owner of a self-created workout breaks a superseries
- **WHEN** the owner of a workout with no prescribing trainer deletes one of its superseries
- **THEN** the response is `204 No Content`

#### Scenario: Admin breaks a superseries
- **WHEN** an admin deletes an existing superseries of any workout
- **THEN** the response is `204 No Content`

#### Scenario: Caller not allowed by the edit rule
- **WHEN** a caller rejected by the workout edit rule deletes an existing superseries
- **THEN** the response is `403 Forbidden` and the superseries is unchanged

#### Scenario: Unknown workout
- **WHEN** the workout id does not exist
- **THEN** the response is `404 Not Found`

#### Scenario: Unknown superseries
- **WHEN** no entry of the workout carries the given superseries id
- **THEN** the response is `404 Not Found`

#### Scenario: Invalid superseries id
- **WHEN** the superseries id is zero or negative
- **THEN** the response is `400 Bad Request`

#### Scenario: Unauthenticated caller
- **WHEN** the request carries no token or an invalid token
- **THEN** the response is `401 Unauthorized`

### Requirement: Superseries membership is visible when reading workouts
Every exercise entry returned by `GET /workouts` SHALL expose its own identifier as `id` and its superseries as `superSeriesId`, which is `null` when the entry is not grouped. Existing fields are unchanged.

#### Scenario: Grouped entry
- **WHEN** a workout with a superseries is read
- **THEN** each grouped entry shows its `id` and the shared `superSeriesId`

#### Scenario: Ungrouped entry
- **WHEN** a workout without superseries is read
- **THEN** each entry shows its `id` and `superSeriesId` is `null`
