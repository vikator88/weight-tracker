# Spec Delta

## Purpose

Gives every signed-in backoffice screen a common navigation drawer, so trainers and admins reach each area of the
backoffice and log out from one stable place.

## ADDED Requirements

### Requirement: Signed-in screens show the navigation drawer

Every signed-in backoffice screen SHALL show a navigation drawer on its left, always visible, with the screen's content
beside it. Trainers and admins SHALL see the same drawer.

#### Scenario: Signed-in visitor opens home

- **WHEN** a trainer or an admin with a stored session opens `/`
- **THEN** the navigation drawer is shown
- **AND** the home heading "Weight Tracker" is shown beside it

### Requirement: The drawer is hidden without a session

The drawer SHALL NOT be rendered for a visitor without a stored session, nor while the session is being read.

#### Scenario: Visitor without a session opens home

- **WHEN** a visitor with no stored session opens `/`
- **THEN** the visitor ends on `/login`
- **AND** neither the drawer nor any of its items is rendered

#### Scenario: Session not read yet

- **WHEN** a signed-in screen has not finished reading the session
- **THEN** only a progress indicator is shown and the drawer is not rendered

### Requirement: The drawer lists every area in a fixed order

The drawer SHALL show the logo at the top, then these entries in this order. Section titles SHALL be headers, not
actions, with their items below them.

- Inicio
- Atletas: Todos los atletas, Invitaciones pendientes
- Programación: Planes asignados, Calendario
- Biblioteca: Plantillas de entrenamiento, Catálogo de ejercicios
- Seguimiento: Feedback de entrenamientos, Revisiones semanales, Pendientes de revisión
- Configuración: Mi perfil, Preferencias
- Cerrar sesión

#### Scenario: Drawer content in Spanish

- **WHEN** the drawer is shown in Spanish
- **THEN** it shows the logo with the text alternative "Weight Tracker"
- **AND** it shows every section title and item above, in that order

#### Scenario: Section titles do not act

- **WHEN** the visitor clicks the section title "Atletas"
- **THEN** nothing happens and the visitor stays on the current screen

### Requirement: Inicio goes to home and shows where the visitor is

The Inicio entry SHALL link to `/`. The entry of the current screen SHALL be shown as selected.

#### Scenario: Inicio on home

- **WHEN** the visitor is on `/`
- **THEN** Inicio is shown as selected
- **AND** Inicio links to `/`

### Requirement: Items without a screen do nothing

Every item other than Inicio and Cerrar sesión SHALL be shown but SHALL NOT navigate or trigger any operation until its
screen exists.

#### Scenario: Placeholder item clicked

- **WHEN** the visitor clicks "Todos los atletas"
- **THEN** the visitor stays on the current screen
- **AND** no request or operation is triggered
