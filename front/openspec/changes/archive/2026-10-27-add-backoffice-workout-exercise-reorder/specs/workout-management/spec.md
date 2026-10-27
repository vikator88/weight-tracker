# Spec Delta

## ADDED Requirements

### Requirement: Reordering a workout's exercises sends the full order

Reordering SHALL send `PUT /workouts/{workoutId}/exercises/order` with the workout id in the path and a body
`{ exerciseIds }` holding the catalog exercise ids in the given order, unchanged, and SHALL return the workout detail
the API answers with 200.

#### Scenario: Exercises reordered

- **WHEN** a workout's exercises are reordered to `[B, A]` and the API answers 200 with the workout detail
- **THEN** exactly one `PUT /workouts/{workoutId}/exercises/order` request is sent with body `{ "exerciseIds": ["B", "A"] }`
- **AND** the result is successful and holds the returned workout detail

#### Scenario: Reorder rejected

- **WHEN** the API answers 400, 403, 404 or 409
- **THEN** the result is a failure with code `VALIDATION`, `FORBIDDEN`, `NOT_FOUND` or `CONFLICT`

#### Scenario: Reorder fails without a response

- **WHEN** the reorder request is rejected without a response
- **THEN** the result is a failure with code `NETWORK`
