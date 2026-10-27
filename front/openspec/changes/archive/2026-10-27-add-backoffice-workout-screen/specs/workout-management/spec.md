# Spec Delta

## ADDED Requirements

### Requirement: Updating an exercise of a workout replaces its prescription

Updating an exercise of a workout SHALL send `PUT /workouts/{workoutId}/exercises/{exerciseId}`, with the workout id
and the catalog exercise id in the path and a body holding that same `exerciseId`, the `effort` and the list of set
prescriptions, unchanged, and SHALL return a successful result holding what the API answers with.

#### Scenario: Exercise updated

- **WHEN** the exercise of a workout is updated with an effort and two set prescriptions and the API accepts it
- **THEN** exactly one `PUT /workouts/{workoutId}/exercises/{exerciseId}` request is sent with the catalog exercise id in
  the path and the body unchanged
- **AND** the result is successful and its value is the API's answer

#### Scenario: Exercise not in the workout

- **WHEN** the API answers 404
- **THEN** the result is a failure with code `NOT_FOUND`

#### Scenario: Invalid prescription

- **WHEN** the API answers 400
- **THEN** the result is a failure with code `VALIDATION` and the API's detail kept

### Requirement: Removing an exercise from a workout sends its catalog id

Removing an exercise SHALL send `DELETE /workouts/{workoutId}/exercises/{exerciseId}`, with the workout id and the
catalog exercise id in the path and no body, and SHALL return a successful empty result when the API answers 204.

#### Scenario: Exercise removed

- **WHEN** an exercise is removed from a workout and the API answers 204
- **THEN** exactly one `DELETE /workouts/{workoutId}/exercises/{exerciseId}` request is sent without a body
- **AND** the result is successful

#### Scenario: Removal rejected

- **WHEN** the API answers 404 or 409
- **THEN** the result is a failure with code `NOT_FOUND` or `CONFLICT`
