# Spec Delta

## MODIFIED Requirements

### Requirement: The screen offers a new workout button

The screen SHALL show a "Nuevo entrenamiento" button with a plus icon above the table, for both admins and trainers.
Activating it SHALL open the create workout dialog and SHALL NOT send any request by itself.

#### Scenario: Button shown and inert

- **WHEN** a trainer clicks "Nuevo entrenamiento"
- **THEN** the create workout dialog titled "Nuevo entrenamiento" is shown
- **AND** the trainer stays on `/assigned-plans`
- **AND** no request is sent

## ADDED Requirements

### Requirement: Each row offers an edit action

Each workout row SHALL end with an edit icon button labelled "Editar <workout name>" that navigates to that workout's
screen at `/workout?id=<workout id>`, for both admins and trainers.

#### Scenario: Edit a workout from the table

- **WHEN** a trainer clicks "Editar Pierna" on the row of the workout "Pierna"
- **THEN** the trainer ends on `/workout?id=<id of Pierna>`

#### Scenario: Edit action in English

- **WHEN** the active language is English
- **THEN** the edit action of the workout "Pierna" is labelled "Edit Pierna"
