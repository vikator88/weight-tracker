# backoffice-navigation Specification

## Purpose

Gives every signed-in backoffice screen a common navigation drawer, so trainers and admins reach each area of the
backoffice and log out from one stable place.

## Requirements

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

The drawer SHALL show the app name row at the top, then these entries in this order. Section titles SHALL be headers,
not actions, with their items below them. Account entries and logout SHALL NOT be in the drawer.

- Inicio
- Atletas: Todos los atletas, Invitaciones pendientes
- Programación: Planes asignados, Calendario
- Biblioteca: Plantillas de entrenamiento, Catálogo de ejercicios
- Seguimiento: Feedback de entrenamientos, Revisiones semanales, Pendientes de revisión

#### Scenario: Drawer content in Spanish

- **WHEN** the drawer is shown in Spanish
- **THEN** it shows the app name row, then every section title and item above, in that order
- **AND** it does not show "Configuración", "Mi perfil", "Preferencias" or "Cerrar sesión"

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

### Requirement: The drawer starts with the app name

The drawer SHALL start with a row that shows the logo and the app name "Weight Tracker". The row SHALL NOT be a menu
entry, and it SHALL have the same height as the header beside it.

#### Scenario: App name row

- **WHEN** the drawer is shown
- **THEN** its first row shows the logo and "Weight Tracker"
- **AND** clicking that row does nothing

### Requirement: Every drawer item has an icon

Every drawer item, Inicio included, SHALL show an icon before its text. Section titles SHALL NOT have an icon.

#### Scenario: Icons shown

- **WHEN** the drawer is shown
- **THEN** each of its items shows an icon before its text
