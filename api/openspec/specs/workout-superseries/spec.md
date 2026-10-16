# workout-superseries Specification

## Purpose

Lets a workout group exercises into superseries, performed set by set without rest between exercises, and ungroup them again without losing the exercises.

## Requirements

### Requirement: Group exercises into a superseries
A workout SHALL allow two or more of its exercises to be grouped into a superseries. The workout assigns the superseries identifier, which is a positive integer unique within that workout.

#### Scenario: Group two exercises
- **WHEN** two exercises of a workout with the same total number of sets are grouped
- **THEN** both exercises carry the same superseries identifier
- **AND** exercises outside the group carry no superseries identifier

#### Scenario: Identifiers are unique per workout
- **WHEN** a second superseries is created in a workout that already has one
- **THEN** the new superseries gets a different identifier from the existing one

#### Scenario: Same identifier in different workouts
- **WHEN** a superseries is created in each of two different workouts
- **THEN** both may carry the same identifier and each groups only the exercises of its own workout

### Requirement: Superseries members must have the same number of sets
A superseries SHALL only be created when every grouped exercise has the same total number of sets. The total is the sum of the counts of the exercise's set prescriptions.

#### Scenario: Different totals are rejected
- **WHEN** an exercise with 3 sets and an exercise with 4 sets are grouped
- **THEN** the operation fails with a domain error
- **AND** neither exercise is grouped

#### Scenario: Same total from different prescriptions
- **WHEN** an exercise prescribed as 3 sets of one target and an exercise prescribed as 2 sets plus 1 set of another are grouped
- **THEN** the superseries is created, because both total 3 sets

### Requirement: Superseries needs at least two existing exercises
A superseries SHALL contain at least two exercises, each of which is already in the workout and not already part of another superseries.

#### Scenario: Single exercise is rejected
- **WHEN** only one exercise is given to group
- **THEN** the operation fails with a domain error and nothing is grouped

#### Scenario: Exercise not in the workout is rejected
- **WHEN** one of the given exercises is not in the workout
- **THEN** the operation fails with a domain error and nothing is grouped

#### Scenario: Exercise already in a superseries is rejected
- **WHEN** an exercise that already belongs to a superseries is grouped again
- **THEN** the operation fails with a domain error and the existing superseries is unchanged

#### Scenario: Duplicate exercise in the request is rejected
- **WHEN** the same exercise is given twice in the list to group
- **THEN** the operation fails with a domain error and nothing is grouped

### Requirement: Break a superseries
A workout SHALL allow a superseries to be broken by its identifier. Breaking it ungroups its exercises and never removes them from the workout.

#### Scenario: Break an existing superseries
- **WHEN** a superseries is broken
- **THEN** its exercises remain in the workout, in the same order and with the same sets and effort
- **AND** none of them carries a superseries identifier

#### Scenario: Other superseries are untouched
- **WHEN** one of two superseries in a workout is broken
- **THEN** the exercises of the other superseries keep their identifier

#### Scenario: Unknown superseries is rejected
- **WHEN** a superseries identifier that no exercise of the workout carries is broken
- **THEN** the operation fails with a domain error

### Requirement: A superseries never has fewer than two exercises
A workout SHALL NOT hold a superseries with fewer than two exercises. Removing an exercise that leaves a superseries with one member SHALL ungroup that member.

#### Scenario: Removal dissolves a pair
- **WHEN** one exercise of a two-exercise superseries is removed from the workout
- **THEN** the remaining exercise stays in the workout without a superseries identifier

#### Scenario: Removal from a larger superseries
- **WHEN** one exercise of a three-exercise superseries is removed
- **THEN** the other two stay grouped under the same identifier

### Requirement: Stored superseries data is validated on restore
Restoring a workout from storage SHALL fail when its stored superseries break the rules above.

#### Scenario: Group with a single member
- **WHEN** stored data has only one exercise with a given superseries identifier
- **THEN** restoring the workout fails with a domain error

#### Scenario: Group with different set totals
- **WHEN** stored data groups exercises whose set totals differ
- **THEN** restoring the workout fails with a domain error

### Requirement: Superseries membership is persisted with the workout
Saving and loading a workout SHALL preserve which exercises belong to which superseries. Exercises without a superseries SHALL be stored and loaded as having none, and workouts saved before this capability existed SHALL load with no superseries.

#### Scenario: Round trip
- **WHEN** a workout with a superseries is saved and loaded again
- **THEN** the same exercises are grouped under the same identifier

#### Scenario: Existing workouts
- **WHEN** a workout stored before this change is loaded
- **THEN** none of its exercises belongs to a superseries

#### Scenario: Migration rollback
- **WHEN** the schema is rolled back
- **THEN** workouts load as before, without superseries information
