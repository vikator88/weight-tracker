# Spec Delta

## ADDED Requirements

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

## MODIFIED Requirements

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
