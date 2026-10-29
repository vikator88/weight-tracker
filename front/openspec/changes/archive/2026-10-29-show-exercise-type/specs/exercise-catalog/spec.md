# Spec Delta

## ADDED Requirements

### Requirement: Listed exercises carry their type

Each exercise returned by listing the catalog SHALL carry its `type` as the API sends it (`STRENGTH`, `CARDIO` or
`STRETCHING`), unchanged, beside its muscle and body part.

#### Scenario: Type kept

- **WHEN** the API answers with an exercise whose `type` is `CARDIO`
- **THEN** the exercise in the successful result has `type` `CARDIO`

#### Scenario: Unknown type kept as it comes

- **WHEN** the API answers with an exercise whose `type` is a value the front does not know
- **THEN** the exercise in the result carries that value unchanged
