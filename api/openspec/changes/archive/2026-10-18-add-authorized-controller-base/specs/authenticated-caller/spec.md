# Spec Delta

## Purpose

Defines how protected routes establish who the caller is: a request is served only when its access token is valid and belongs to a user that still exists, while public routes stay open.

## ADDED Requirements

### Requirement: Protected routes reject requests without a valid token
Every protected route SHALL reject a request that carries no access token, or a malformed or wrongly signed one, with `401 Unauthorized`, and SHALL NOT perform the operation.

#### Scenario: No token
- **WHEN** a caller sends a request to a protected route without an access token
- **THEN** the response is `401 Unauthorized`
- **AND** no state changes

#### Scenario: Invalid token
- **WHEN** a caller sends a request to a protected route with a malformed or wrongly signed access token
- **THEN** the response is `401 Unauthorized`
- **AND** no state changes

### Requirement: Protected routes reject tokens of users that no longer exist
A protected route SHALL accept a validly signed, unexpired access token only if the user it was issued to still exists. Otherwise the response SHALL be `401 Unauthorized` and the operation SHALL NOT run. This check SHALL happen before any role requirement is evaluated. The response SHALL NOT reveal that the user was removed.

#### Scenario: User removed after login on a workout route
- **WHEN** a user logs in, the user is then removed, and the token is used on `GET /workouts`, `POST /workouts`, `POST /workouts/{workoutId}/superseries` or `DELETE /workouts/{workoutId}/superseries/{superSeriesId}`
- **THEN** the response is `401 Unauthorized`
- **AND** no workout is created or changed

#### Scenario: Admin removed after login on an exercise write route
- **WHEN** an admin logs in, the admin is then removed, and the token is used on `POST /exercises`, `PUT /exercises/{id}` or `DELETE /exercises/{id}`
- **THEN** the response is `401 Unauthorized`
- **AND** no exercise is created, changed or deleted

#### Scenario: Non-admin removed after login on an admin route
- **WHEN** a trainer logs in, the trainer is then removed, and the token is used on `POST /exercises`, `PUT /exercises/{id}` or `DELETE /exercises/{id}`
- **THEN** the response is `401 Unauthorized`, not `403 Forbidden`

#### Scenario: Existing user without the required role
- **WHEN** a user or trainer who still exists calls `POST /exercises`, `PUT /exercises/{id}` or `DELETE /exercises/{id}` with their valid access token
- **THEN** the response is `403 Forbidden`
- **AND** no exercise is created, changed or deleted

#### Scenario: Existing user is served
- **WHEN** a user who still exists calls a protected route with their valid access token
- **THEN** the request is served as before this change

### Requirement: The caller's identity and role come from the stored user
For a protected route, the identity and role used to decide what the caller may see or do, including role requirements on the route, SHALL be those of the stored user the token belongs to.

#### Scenario: Scoped read uses the stored user
- **WHEN** a seeded `USER` calls `GET /workouts` with a valid token
- **THEN** the response contains only that user's workouts
- **AND** excludes workouts owned by other users

### Requirement: Public routes are unaffected
Routes that do not require authentication SHALL keep serving anonymous callers and SHALL NOT look up a caller.

#### Scenario: Anonymous exercise read
- **WHEN** a caller sends `GET /exercises` or `GET /exercises/{id}` without an access token
- **THEN** the request is served as before this change

#### Scenario: Auth endpoints stay public
- **WHEN** a caller sends `POST /auth/login`, `POST /auth/signup` or `POST /auth/refresh` without an access token
- **THEN** the request is served as before this change
