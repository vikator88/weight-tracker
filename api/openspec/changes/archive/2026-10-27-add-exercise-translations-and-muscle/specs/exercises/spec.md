# Spec Delta

## Purpose

Defines the exercises that workouts are built from: their names per language, the muscle they work and the body part derived from it, and their link to the free exercise dataset, as read anonymously and written by admins through `/exercises`.

## ADDED Requirements

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

### Requirement: Body part is derived from the muscle
The `bodyPart` of an exercise SHALL always be the body part mapped from its `muscle`. Every muscle SHALL map to exactly one body part. Requests SHALL NOT set the body part, and only the muscle SHALL be stored.

#### Scenario: Muscle mapping
- **WHEN** an exercise has a given muscle
- **THEN** its `bodyPart` is `CORE` for `ABDOMINALS`; `LEGS` for `ABDUCTORS`, `ADDUCTORS`, `CALVES`, `GLUTES`, `HAMSTRINGS` and `QUADRICEPS`; `ARMS` for `BICEPS`, `FOREARMS` and `TRICEPS`; `CHEST` for `CHEST`; `BACK` for `LATS`, `LOWER_BACK`, `MIDDLE_BACK` and `TRAPS`; `NECK` for `NECK`; `SHOULDERS` for `SHOULDERS`

#### Scenario: Changing the muscle changes the body part
- **WHEN** an admin updates an exercise from muscle `BICEPS` to `HAMSTRINGS`
- **THEN** the response and later reads report `bodyPart` `LEGS`

#### Scenario: Body part in the request is ignored
- **WHEN** an admin sends a valid `POST /exercises` that also contains `bodyPart` `CHEST` and has `muscle` `LATS`
- **THEN** the created exercise has `bodyPart` `BACK`

### Requirement: Admins create exercises with muscle, translations and dataset id
`POST /exercises` SHALL accept `muscle` (required), `videoUrl` (optional), `freeExerciseDbId` (optional) and `translations` (optional list of `{ languageCode, name }`). It SHALL respond `201 Created` with the exercise in the read shape. The existing authorization of the route is unchanged.

#### Scenario: Create with translations
- **WHEN** an admin sends `POST /exercises` with `muscle` `CHEST`, `freeExerciseDbId` `Dumbbell_Bench_Press` and translations `es` "Press con mancuernas" and `en` "Dumbbell bench press"
- **THEN** the response is `201 Created` with both translations, `muscle` `CHEST`, `bodyPart` `CHEST` and that `freeExerciseDbId`
- **AND** `GET /exercises/{id}` returns the same data

#### Scenario: Create without translations or dataset id
- **WHEN** an admin sends `POST /exercises` with only `muscle` `NECK`
- **THEN** the response is `201 Created` with an empty `translations` list and `freeExerciseDbId` `null`

### Requirement: Admins replace an exercise's muscle, video and translations
`PUT /exercises/{id}` SHALL accept `muscle`, `videoUrl` and `translations`, and SHALL leave the exercise with exactly the translations in the request. A language already stored SHALL keep its translation with the new name, a new language SHALL be added and a language missing from the request SHALL be removed. It SHALL respond `200 OK` with the exercise in the read shape.

#### Scenario: Rename, add and remove translations
- **WHEN** an exercise has translations `es` and `en`, and an admin sends `PUT /exercises/{id}` with `es` under a new name and `fr`
- **THEN** the response is `200 OK` with exactly `es` (new name) and `fr`
- **AND** `en` is no longer returned by later reads

#### Scenario: Empty translations clear every name
- **WHEN** an admin sends `PUT /exercises/{id}` with an empty `translations` list
- **THEN** the exercise is returned with no translations

#### Scenario: Unknown exercise
- **WHEN** an admin sends `PUT /exercises/{id}` for an id that does not exist
- **THEN** the response is `404 Not Found`

### Requirement: The dataset id is immutable after creation
`freeExerciseDbId` SHALL only be set on creation. `PUT /exercises/{id}` SHALL ignore a `freeExerciseDbId` in the body and keep the stored value.

#### Scenario: Update does not change the dataset id
- **WHEN** an admin sends `PUT /exercises/{id}` for the seeded Sentadilla with `freeExerciseDbId` `Plank`
- **THEN** the response is `200 OK` and `freeExerciseDbId` is still `Barbell_Squat`

### Requirement: Dataset ids are unique among exercises
No two exercises SHALL share a `freeExerciseDbId`. Creating an exercise with a `freeExerciseDbId` already in use SHALL respond `409 Conflict` and SHALL NOT create it. Exercises without a dataset id SHALL NOT conflict with each other.

#### Scenario: Duplicate dataset id
- **WHEN** an admin sends `POST /exercises` with `freeExerciseDbId` `Barbell_Squat`, already used by the seeded Sentadilla
- **THEN** the response is `409 Conflict` and no exercise is created

#### Scenario: Several exercises without dataset id
- **WHEN** an admin creates two exercises without `freeExerciseDbId`
- **THEN** both are created

### Requirement: Exercise writes validate their input
`POST` and `PUT /exercises` SHALL respond `400 Bad Request` and change nothing when `muscle` is missing or not a defined muscle, when a `languageCode` is not a two-letter ISO 639-1 code, when a translation `name` is empty or outside 3 to 150 characters after trimming, when the request repeats a language code, or when `freeExerciseDbId` on `POST` is empty, whitespace only or longer than 128 characters.

#### Scenario: Unknown muscle
- **WHEN** an admin sends `POST /exercises` with `muscle` `WINGS`
- **THEN** the response is `400 Bad Request`

#### Scenario: Invalid language code
- **WHEN** an admin sends `POST /exercises` with a translation whose `languageCode` is `spa`
- **THEN** the response is `400 Bad Request`

#### Scenario: Duplicate language in one request
- **WHEN** an admin sends `PUT /exercises/{id}` with two translations for `es`
- **THEN** the response is `400 Bad Request` and the stored translations are unchanged

#### Scenario: Dataset id too long
- **WHEN** an admin sends `POST /exercises` with a `freeExerciseDbId` of 129 characters
- **THEN** the response is `400 Bad Request`

### Requirement: Language codes and dataset ids are normalized
Language codes SHALL be trimmed and lowercased, and translation names and `freeExerciseDbId` SHALL be trimmed, before they are stored and returned.

#### Scenario: Upper-case language code
- **WHEN** an admin creates an exercise with a translation for `" ES "`
- **THEN** the stored and returned `languageCode` is `es`

#### Scenario: Padded dataset id
- **WHEN** an admin creates an exercise with `freeExerciseDbId` `"  Barbell_Curl_Lying  "`
- **THEN** the stored and returned value is `Barbell_Curl_Lying`

### Requirement: Workout listings embed translations and muscle
`GET /workouts` SHALL return, for every workout entry, the referenced exercise's `translations`, `muscle` and `bodyPart` in place of the former `name`. Entries SHALL NOT contain `name`. `GET /workouts/{id}` is unchanged and carries no exercise data.

#### Scenario: Listing a workout with exercises
- **WHEN** the owner sends `GET /workouts` and has a workout with the seeded Press Banca
- **THEN** that entry carries the translations of Press Banca, `muscle` `CHEST` and `bodyPart` `CHEST`
- **AND** the entry has no `name` field
