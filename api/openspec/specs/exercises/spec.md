# exercises Specification

## Purpose

Defines the exercises that workouts are built from: their names per language, the muscle they work and the body part derived from it, and their link to the free exercise dataset, as read anonymously and written by admins through `/exercises`.

## Requirements

### Requirement: Exercise reads expose translations, muscle, body part and dataset id
`GET /exercises` and `GET /exercises/{id}` SHALL return each exercise with `id`, `freeExerciseDbId` (or `null`), `videoUrl` (or `null`), `muscle`, `bodyPart` and `translations`, a list of `{ languageCode, name }`. `muscle` and `bodyPart` SHALL be returned by name. The response SHALL NOT contain a top-level `name`.

#### Scenario: Read a seeded exercise
- **WHEN** an anonymous caller sends `GET /exercises/{id}` for the seeded Sentadilla
- **THEN** the response is `200 OK` with `freeExerciseDbId` `Barbell_Squat`, `muscle` `QUADRICEPS`, `bodyPart` `LEGS` and translations for `es` and `en`
- **AND** the response has no `name` field

#### Scenario: Exercise without translations
- **WHEN** a caller reads an exercise that has no translations
- **THEN** the response is `200 OK` with an empty `translations` list

#### Scenario: List exercises
- **WHEN** an anonymous caller sends `GET /exercises`
- **THEN** every exercise in the response carries its `translations`, `muscle` and `bodyPart`

### Requirement: Workout listings embed translations and muscle
`GET /workouts` SHALL return, for every workout entry, the referenced exercise's `translations`, `muscle` and `bodyPart` in place of the former `name`. Entries SHALL NOT contain `name`. `GET /workouts/{id}` is unchanged and carries no exercise data.

#### Scenario: Listing a workout with exercises
- **WHEN** the owner sends `GET /workouts` and has a workout with the seeded Press Banca
- **THEN** that entry carries the translations of Press Banca, `muscle` `CHEST` and `bodyPart` `CHEST`
- **AND** the entry has no `name` field

### Requirement: Exercises cannot be written through the API
The system SHALL NOT serve any route that creates, updates or deletes exercises or their translations, for any caller, ADMIN included. `POST /exercises`, `PUT /exercises/{id}` and `DELETE /exercises/{id}` SHALL get the framework's default response for an unmatched route and SHALL NOT change stored exercises. Exercises SHALL only change outside the application, through migrations or direct database edits.

#### Scenario: Admin tries to create an exercise
- **WHEN** an ADMIN sends `POST /exercises` with a body that used to be valid
- **THEN** the response is not a success status
- **AND** `GET /exercises` returns the same exercises as before

#### Scenario: Admin tries to update an exercise
- **WHEN** an ADMIN sends `PUT /exercises/{id}` for the seeded Sentadilla
- **THEN** the response is not a success status
- **AND** `GET /exercises/{id}` returns the seeded Sentadilla unchanged

#### Scenario: Admin tries to delete an exercise
- **WHEN** an ADMIN sends `DELETE /exercises/{id}` for the seeded Curl de Bíceps
- **THEN** the response is not a success status
- **AND** `GET /exercises/{id}` still returns it

### Requirement: Body part is derived from the stored muscle
The `bodyPart` of an exercise SHALL always be the body part mapped from its stored `muscle`. Every muscle SHALL map to exactly one body part. Only the muscle SHALL be stored.

#### Scenario: Muscle mapping
- **WHEN** an exercise has a given muscle
- **THEN** its `bodyPart` is `CORE` for `ABDOMINALS`; `LEGS` for `ABDUCTORS`, `ADDUCTORS`, `CALVES`, `GLUTES`, `HAMSTRINGS` and `QUADRICEPS`; `ARMS` for `BICEPS`, `FOREARMS` and `TRICEPS`; `CHEST` for `CHEST`; `BACK` for `LATS`, `LOWER_BACK`, `MIDDLE_BACK` and `TRAPS`; `NECK` for `NECK`; `SHOULDERS` for `SHOULDERS`

#### Scenario: Body part follows the stored muscle
- **WHEN** a caller reads an exercise whose stored muscle is `HAMSTRINGS`
- **THEN** the response reports `bodyPart` `LEGS`
