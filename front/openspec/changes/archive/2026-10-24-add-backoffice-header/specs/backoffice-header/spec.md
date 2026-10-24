# Spec Delta

## Purpose

Gives every signed-in backoffice screen a common header with search, alerts and the user's account menu, placed
beside the navigation drawer.

## ADDED Requirements

### Requirement: Signed-in screens show the header beside the drawer

Every signed-in backoffice screen SHALL show a header at the top of the content area, to the right of the drawer. The
drawer SHALL keep the full height of the window. The screen's content SHALL appear below the header. Trainers and
admins SHALL see the same header.

#### Scenario: Signed-in visitor opens home

- **WHEN** a trainer or an admin with a stored session opens `/`
- **THEN** the drawer is shown on the left at full height
- **AND** the header is shown to its right, above the home heading "Weight Tracker"

### Requirement: The header is hidden without a session

The header SHALL NOT be rendered for a visitor without a stored session, nor while the session is being read. Login
and signup SHALL NOT show the header.

#### Scenario: Visitor without a session opens home

- **WHEN** a visitor with no stored session opens `/`
- **THEN** the visitor ends on `/login` and the header is never rendered

#### Scenario: Session not read yet

- **WHEN** a signed-in screen has not finished reading the session
- **THEN** only a progress indicator is shown and the header is not rendered

### Requirement: The header has a search box that does not act yet

The header SHALL show, on its left, a search box with a search icon at its start and the placeholder "Escribe para
buscar...". It SHALL take half of the header's width. Typing SHALL change its text and SHALL trigger nothing else.

#### Scenario: Search box shown

- **WHEN** the header is shown in Spanish
- **THEN** a search box labelled "Buscar" shows the placeholder "Escribe para buscar..." and a search icon

#### Scenario: Typing in the search box

- **WHEN** the visitor types "Juan" in the search box
- **THEN** the box shows "Juan"
- **AND** no request or operation is triggered and the visitor stays on the current screen

### Requirement: The header controls are right-aligned in a fixed order

The header SHALL show, aligned to its right and in this order from left to right, the language selector, the alerts
control and the user menu.

#### Scenario: Control order

- **WHEN** the header is shown
- **THEN** the controls appear as "Cambiar idioma", then "Alertas", then the user menu, the user menu being rightmost

### Requirement: The user menu shows the current user

The user menu SHALL show the user's name, the user's role below it, a circular avatar with the name's initials and a
toggle that opens the menu. Until the API provides the user, the user SHALL be the placeholder "Usuario de prueba"
with the role trainer.

#### Scenario: Placeholder user in Spanish

- **WHEN** the header is shown in Spanish
- **THEN** the user menu shows "Usuario de prueba", "Entrenador" below it, and an avatar with "UP"

### Requirement: The user menu lists the account entries

Opening the user menu SHALL show, in this order, "Mi perfil", "Preferencias", a separator and "Cerrar sesión". "Mi
perfil" and "Preferencias" SHALL close the menu and do nothing else until their screens exist.

#### Scenario: Open the user menu

- **WHEN** the visitor clicks the user menu toggle
- **THEN** the menu shows "Mi perfil", "Preferencias", a separator and "Cerrar sesión", in that order

#### Scenario: Placeholder entry clicked

- **WHEN** the visitor clicks "Mi perfil"
- **THEN** the menu closes, the visitor stays on the current screen and no operation is triggered

### Requirement: The alerts control lists placeholder alerts

The alerts control SHALL show a bell with a badge holding the number of alerts. Clicking it SHALL open a dropdown with
the title "Alertas" and the alerts. Until alerts come from the API, there SHALL be three placeholder alerts. Clicking
an alert SHALL close the dropdown and do nothing else.

#### Scenario: Open the alerts

- **WHEN** the visitor clicks "Alertas"
- **THEN** the badge shows 3
- **AND** the dropdown shows "Juan Pérez ha completado su entrenamiento.", "María López ha aceptado tu invitación."
  and "Revisión semanal pendiente de Carlos Ruiz."

#### Scenario: Alert clicked

- **WHEN** the visitor clicks an alert
- **THEN** the dropdown closes and the visitor stays on the current screen
