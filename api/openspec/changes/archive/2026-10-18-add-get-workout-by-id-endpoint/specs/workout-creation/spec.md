# Spec Delta

## MODIFIED Requirements

### Requirement: Creation returns the new workout
A successful creation SHALL respond `201 Created` with the new workout: a server-generated `id`, `workoutDate`, `userId`, `trainerId` (or `null`) and an empty `exercises` list. The response SHALL carry a `Location` header with the URL of `GET /workouts/{id}` for the new workout. The new workout SHALL be visible through `GET /workouts` to its owner, its trainer when set, and admins.

#### Scenario: Created workout is readable
- **WHEN** a caller creates a workout successfully
- **THEN** the response is `201 Created` with the workout and an empty `exercises` list
- **AND** `GET /workouts` by the owner includes a workout with that `id`

#### Scenario: Location points at the new workout
- **WHEN** a caller creates a workout successfully
- **THEN** the `Location` header ends with `/workouts/{id}` of the created workout
- **AND** following it as the same caller returns `200 OK` with that workout
