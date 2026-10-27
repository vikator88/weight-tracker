# Spec Delta

## ADDED Requirements

### Requirement: Planes asignados goes to the assigned plans screen

The Planes asignados entry SHALL link to `/assigned-plans` and SHALL be shown as selected while the visitor is on that
screen. It SHALL NOT be selected on any other screen.

#### Scenario: Planes asignados clicked

- **WHEN** the visitor clicks "Planes asignados" from home
- **THEN** the visitor ends on `/assigned-plans`

#### Scenario: Planes asignados on its screen

- **WHEN** the visitor is on `/assigned-plans`
- **THEN** Planes asignados is shown as selected
- **AND** Inicio is not shown as selected

## MODIFIED Requirements

### Requirement: Items without a screen do nothing

Every item other than Inicio, Planes asignados and Cerrar sesión SHALL be shown but SHALL NOT navigate or trigger any
operation until its screen exists.

#### Scenario: Placeholder item clicked

- **WHEN** the visitor clicks "Todos los atletas"
- **THEN** the visitor stays on the current screen
- **AND** no request or operation is triggered
