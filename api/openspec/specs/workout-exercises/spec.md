# workout-exercises Specification

## Purpose

Lets an authorized caller put catalog exercises into an existing workout, replace their effort and set prescriptions, and remove them, so a workout can be filled after it is created.

## Requirements

### Requirement: Add an exercise to a workout
The system SHALL let a caller who may edit the workout add a catalog exercise with an effort and an ordered, non-empty list of set prescriptions. The new entry SHALL be placed after every existing entry, belong to no superseries, and be returned with its own entry identifier.

#### Scenario: Prescribing trainer adds an exercise
- **WHEN** the prescribing trainer adds an exercise with effort `HIGH` and sets `3 x 15 Reps` to a prescribed workout
- **THEN** the response is `201 Created` with the entry, its catalog exercise id, effort `HIGH` and the sets as given
- **AND** reading the workout returns the entry last, with no superseries

#### Scenario: Several prescriptions keep their order
- **WHEN** an exercise is added with sets `2 x 12 Reps` then `2 x 10 Reps`
- **THEN** the stored entry has both prescriptions in that order and a total of 4 sets

#### Scenario: Owner of a self-created workout adds an exercise
- **WHEN** the owner of a workout with no prescribing trainer adds an exercise
- **THEN** the response is `201 Created`

#### Scenario: Admin adds an exercise
- **WHEN** an admin adds an exercise to any workout
- **THEN** the response is `201 Created`

### Requirement: A catalog exercise appears once per workout
Adding a catalog exercise that is already in the workout SHALL fail with `409 Conflict` and leave the workout unchanged.

#### Scenario: Duplicate exercise
- **WHEN** an exercise already present in the workout is added again
- **THEN** the response is `409 Conflict`
- **AND** the workout still has a single entry for that exercise

### Requirement: The added exercise must exist in the catalog
Adding an exercise whose identifier is not in the exercise catalog SHALL fail with `404 Not Found` and leave the workout unchanged.

#### Scenario: Unknown catalog exercise
- **WHEN** an exercise id that is not in the catalog is added
- **THEN** the response is `404 Not Found`
- **AND** the workout is unchanged

### Requirement: Effort accepts its name or its number
The request effort SHALL be accepted as a defined effort name (`LOW`, `MIDLOW`, `MID`, `MIDHIGH`, `HIGH`) or its number (1 to 5). Responses SHALL report effort by name. Any other value SHALL fail with `400 Bad Request`.

#### Scenario: Effort as a name
- **WHEN** an exercise is added with effort `"MID"`
- **THEN** the entry's effort is `MID`

#### Scenario: Effort as a number
- **WHEN** an exercise is added with effort `5`
- **THEN** the entry's effort is `HIGH`

#### Scenario: Undefined effort
- **WHEN** an exercise is added with effort `9`
- **THEN** the response is `400 Bad Request` and the workout is unchanged

### Requirement: Set prescriptions are validated
Every set prescription SHALL have a count of at least 1 and a target type of `Reps`, `Duration` or `MaxReps`. `Reps` and `Duration` SHALL have a target of at least 1; `Duration` is in seconds. `MaxReps` SHALL ignore the target. An empty or missing list, or any invalid prescription, SHALL fail with `400 Bad Request` and change nothing.

#### Scenario: Max reps ignores the target
- **WHEN** an exercise is added with sets `2 x MaxReps` and target `0`
- **THEN** the entry has 2 sets to failure and no target value

#### Scenario: Duration in seconds
- **WHEN** an exercise is added with sets `3 x Duration` and target `40`
- **THEN** the entry has 3 sets of 40 seconds

#### Scenario: Empty sets
- **WHEN** an exercise is added or updated with an empty sets list
- **THEN** the response is `400 Bad Request`

#### Scenario: Non-positive count or target
- **WHEN** a prescription has count `0`, or a `Reps` or `Duration` target of `0`
- **THEN** the response is `400 Bad Request`

#### Scenario: Unknown target type
- **WHEN** a prescription has target type `Weight`
- **THEN** the response is `400 Bad Request`

### Requirement: Update replaces effort and all sets of an entry
The system SHALL let a caller who may edit the workout replace the effort and the whole set prescription list of an exercise in the workout, identified by its catalog exercise id. The entry SHALL keep its entry identifier, position and superseries. The previous set prescriptions SHALL be discarded and the new ones stored, even when they are equal in value.

#### Scenario: Change effort and sets
- **WHEN** an entry with `3 x 10 Reps` at effort `MID` is updated to `2 x 12 Reps` then `2 x 10 Reps` at effort `HIGH`
- **THEN** the response is `200 OK` with the updated entry
- **AND** reading the workout shows only the new prescriptions, at the same position and with the same entry id

#### Scenario: Effort-only change still replaces the stored sets
- **WHEN** an entry is updated with a new effort and the same set values
- **THEN** the stored set prescriptions are new records equal in value to the old ones
- **AND** none of the previous set records remain

### Requirement: Update cannot change the exercise
The catalog exercise id in the update body SHALL match the one in the route. A mismatch SHALL fail with `400 Bad Request` and change nothing. Swapping the exercise needs a removal and an addition.

#### Scenario: Body and route disagree
- **WHEN** an update targets one exercise in the route and names another in the body
- **THEN** the response is `400 Bad Request` and the workout is unchanged

### Requirement: Update keeps superseries set totals equal
When the updated entry belongs to a superseries and its new total set count differs from the other members', the update SHALL fail with `400 Bad Request` and leave the entry unchanged.

#### Scenario: Total changes inside a superseries
- **WHEN** a 3-set member of a superseries is updated to 4 sets
- **THEN** the response is `400 Bad Request`
- **AND** the entry keeps its previous effort and sets and stays in the superseries

#### Scenario: Same total inside a superseries
- **WHEN** a 3-set member of a superseries is updated to `2 x 8 Reps` then `1 x MaxReps`
- **THEN** the update is applied and the entry stays in the superseries

### Requirement: Remove an exercise from a workout
The system SHALL let a caller who may edit the workout remove an exercise from it by catalog exercise id. Its set prescriptions SHALL be removed with it. When the removed entry leaves a superseries with a single member, that member SHALL leave the superseries too.

#### Scenario: Remove an entry and its sets
- **WHEN** an exercise is removed from a workout
- **THEN** the response is `204 No Content`
- **AND** the workout no longer contains it and no set prescription of that entry remains stored

#### Scenario: Remove a member of a two-exercise superseries
- **WHEN** one exercise of a two-member superseries is removed
- **THEN** the remaining exercise belongs to no superseries

### Requirement: Exercise must be in the workout to update or remove it
Updating or removing a catalog exercise that is not in the workout SHALL fail with `404 Not Found`.

#### Scenario: Update an absent exercise
- **WHEN** an update targets a catalog exercise that the workout does not contain
- **THEN** the response is `404 Not Found`

#### Scenario: Remove an absent exercise
- **WHEN** a removal targets a catalog exercise that the workout does not contain
- **THEN** the response is `404 Not Found`

### Requirement: Exercise routes apply the workout edit rule
Adding, updating and removing exercises SHALL follow the workout editing permission rule: an unknown workout is `404 Not Found`, and a caller who may not edit the workout gets `403 Forbidden` with the workout unchanged.

#### Scenario: Owner of a prescribed workout is rejected
- **WHEN** the owner of a workout with a prescribing trainer adds, updates or removes an exercise
- **THEN** the response is `403 Forbidden` and the workout is unchanged

#### Scenario: Trainer who does not own a self-created workout is rejected
- **WHEN** a trainer adds, updates or removes an exercise in a workout with no prescribing trainer that they do not own
- **THEN** the response is `403 Forbidden` and the workout is unchanged

#### Scenario: Unknown workout
- **WHEN** an authenticated caller targets a workout id that does not exist
- **THEN** the response is `404 Not Found`

### Requirement: Exercise routes require authentication
Each of the three exercise routes SHALL reject a request with no token, an invalid token, or a token whose user no longer exists, with `401 Unauthorized`.

#### Scenario: No token
- **WHEN** any exercise route is called without a bearer token
- **THEN** the response is `401 Unauthorized`

#### Scenario: Invalid token
- **WHEN** any exercise route is called with a malformed or unsigned token
- **THEN** the response is `401 Unauthorized`

#### Scenario: User no longer exists
- **WHEN** any exercise route is called with a valid token whose user was deleted
- **THEN** the response is `401 Unauthorized`

### Requirement: Built workout reads back as added
A workout built through the exercise routes SHALL read back, through the get-by-id route, with its entries in the order they were added and with each entry's effort and sets as last written.

#### Scenario: Create, add, read
- **WHEN** a caller creates a workout, adds two exercises, and reads the workout by id
- **THEN** the response lists both entries in insertion order with their efforts and set prescriptions
