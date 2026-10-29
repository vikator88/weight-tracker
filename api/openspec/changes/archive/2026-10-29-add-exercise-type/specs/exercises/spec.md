# Spec Delta

## MODIFIED Requirements

### Requirement: Exercise reads expose translations, muscle, body part and dataset id
`GET /exercises` and `GET /exercises/{id}` SHALL return each exercise with `id`, `freeExerciseDbId` (or `null`), `videoUrl` (or `null`), `muscle`, `bodyPart`, `type` and `translations`, a list of `{ languageCode, name }`. `muscle`, `bodyPart` and `type` SHALL be returned by name. The response SHALL NOT contain a top-level `name`.

#### Scenario: Read a seeded exercise
- **WHEN** an anonymous caller sends `GET /exercises/{id}` for the seeded Sentadilla
- **THEN** the response is `200 OK` with `freeExerciseDbId` `Barbell_Squat`, `muscle` `QUADRICEPS`, `bodyPart` `LEGS`, `type` `STRENGTH` and translations for `es` and `en`
- **AND** the response has no `name` field

#### Scenario: Exercise without translations
- **WHEN** a caller reads an exercise that has no translations
- **THEN** the response is `200 OK` with an empty `translations` list

#### Scenario: List exercises
- **WHEN** an anonymous caller sends `GET /exercises`
- **THEN** every exercise in the response carries its `translations`, `muscle`, `bodyPart` and `type`

### Requirement: Workout listings embed translations and muscle
`GET /workouts` SHALL return, for every workout entry, the referenced exercise's `translations`, `muscle`, `bodyPart` and `type` in place of the former `name`. Entries SHALL NOT contain `name`. `GET /workouts/{id}` is unchanged and carries no exercise data.

#### Scenario: Listing a workout with exercises
- **WHEN** the owner sends `GET /workouts` and has a workout with the seeded Press Banca
- **THEN** that entry carries the translations of Press Banca, `muscle` `CHEST`, `bodyPart` `CHEST` and `type` `STRENGTH`
- **AND** the entry has no `name` field

## ADDED Requirements

### Requirement: Every exercise has one mandatory type
Every exercise SHALL have exactly one type out of `STRENGTH`, `CARDIO` or `STRETCHING`, stored with the exercise. An exercise without a type, or with any other value, SHALL NOT exist in the catalog.

#### Scenario: Cardio exercise
- **WHEN** a caller reads the catalog exercise `Running_Treadmill`
- **THEN** its `type` is `CARDIO`

#### Scenario: Stretching exercise
- **WHEN** a caller reads a catalog exercise that the source dataset classifies as stretching
- **THEN** its `type` is `STRETCHING`

#### Scenario: Whole catalog is typed
- **WHEN** the database has been migrated
- **THEN** every catalog exercise has a type, with 739 `STRENGTH`, 14 `CARDIO` and 123 `STRETCHING`

### Requirement: Type is independent of muscle and body part
The type of an exercise SHALL NOT be derived from, nor change, its `muscle` or `bodyPart`. Exercises of any type SHALL keep the muscle and body part they had before types existed.

#### Scenario: Cardio keeps its muscle
- **WHEN** a caller reads `Running_Treadmill`
- **THEN** it reports `muscle` `QUADRICEPS`, `bodyPart` `LEGS` and `type` `CARDIO`

### Requirement: Type comes from the catalog source category
Each catalog exercise's type SHALL follow its source dataset category: `cardio` is `CARDIO`; `stretching` is `STRETCHING`; `strength`, `powerlifting`, `olympic weightlifting`, `strongman` and `plyometrics` are `STRENGTH`.

#### Scenario: Plyometrics is strength
- **WHEN** a caller reads a catalog exercise whose source category is `plyometrics`
- **THEN** its `type` is `STRENGTH`
