# Spec Delta

## MODIFIED Requirements

### Requirement: Set prescriptions are validated
Every set prescription SHALL have a count of at least 1 and a target type of `Reps`, `Duration`, `MaxReps` or `RepsRange`. `Reps` and `Duration` SHALL have a target of at least 1; `Duration` is in seconds. `MaxReps` SHALL ignore the target. `RepsRange` SHALL have a target (minimum reps) of at least 1 and a `maxTarget` (maximum reps) strictly greater than the target. Every type other than `RepsRange` SHALL ignore `maxTarget`. An empty or missing list, or any invalid prescription, SHALL fail with `400 Bad Request` and change nothing.

#### Scenario: Max reps ignores the target
- **WHEN** an exercise is added with sets `2 x MaxReps` and target `0`
- **THEN** the entry has 2 sets to failure and no target value

#### Scenario: Duration in seconds
- **WHEN** an exercise is added with sets `3 x Duration` and target `40`
- **THEN** the entry has 3 sets of 40 seconds

#### Scenario: Reps range
- **WHEN** an exercise is added or updated with sets `3 x RepsRange`, target `8` and maxTarget `12`
- **THEN** the entry has 3 sets of 8 to 12 reps
- **AND** the set target in the response has type `RepsRange`, value `8` and maxValue `12`

#### Scenario: Range maximum not above the minimum
- **WHEN** a `RepsRange` prescription has target `10` and maxTarget `10`, or target `10` and maxTarget `8`
- **THEN** the response is `400 Bad Request` and the workout is unchanged

#### Scenario: Range without a maximum
- **WHEN** a `RepsRange` prescription has target `8` and no maxTarget
- **THEN** the response is `400 Bad Request` and the workout is unchanged

#### Scenario: Range minimum below one
- **WHEN** a `RepsRange` prescription has target `0` and maxTarget `12`
- **THEN** the response is `400 Bad Request` and the workout is unchanged

#### Scenario: Other types ignore the maximum
- **WHEN** an exercise is added with sets `3 x Reps`, target `10` and maxTarget `12`
- **THEN** the entry has 3 sets of 10 reps
- **AND** the set target in the response has no maxValue

#### Scenario: Empty sets
- **WHEN** an exercise is added or updated with an empty sets list
- **THEN** the response is `400 Bad Request`

#### Scenario: Non-positive count or target
- **WHEN** a prescription has count `0`, or a `Reps` or `Duration` target of `0`
- **THEN** the response is `400 Bad Request`

#### Scenario: Unknown target type
- **WHEN** a prescription has target type `Weight`
- **THEN** the response is `400 Bad Request`
