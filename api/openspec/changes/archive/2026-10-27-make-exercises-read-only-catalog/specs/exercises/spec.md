# Spec Delta

## ADDED Requirements

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

## REMOVED Requirements

### Requirement: Body part is derived from the muscle
**Reason**: Two of its scenarios change the muscle or send a body part through API writes, which no longer exist.
**Migration**: Replaced by "Body part is derived from the stored muscle", which keeps the same mapping.

### Requirement: Admins create exercises with muscle, translations and dataset id
**Reason**: Exercises are a read-only catalog, imported by migration and maintained in the database.
**Migration**: Add exercises with a FluentMigrator migration or a direct database edit.

### Requirement: Admins replace an exercise's muscle, video and translations
**Reason**: Exercises are a read-only catalog, maintained in the database.
**Migration**: Change the muscle, video url or translations with a migration or a direct database edit.

### Requirement: The dataset id is immutable after creation
**Reason**: No API flow can create or change an exercise, so there is no update to guard. The application never changes the dataset id.
**Migration**: None. Edits to the dataset id outside the application are the operator's responsibility.

### Requirement: Dataset ids are unique among exercises
**Reason**: Uniqueness was enforced on API creates, which no longer exist. The database constraint `UQ_Exercises_FreeExerciseDbId` still enforces it on every write.
**Migration**: None. Database edits that break uniqueness are rejected by the constraint.

### Requirement: Exercise writes validate their input
**Reason**: There are no exercise writes left in the API.
**Migration**: Migrations and database edits must store a defined muscle, two-letter lowercase language codes, names of 3 to 150 characters and dataset ids of up to 128 characters. Stored data that breaks these rules fails when it is read.

### Requirement: Language codes and dataset ids are normalized
**Reason**: Normalization happened on API input, which no longer exists.
**Migration**: Store values already normalized: lowercase language codes, and trimmed names and dataset ids.
