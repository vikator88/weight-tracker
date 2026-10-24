# Spec Delta

## MODIFIED Requirements

### Requirement: The user menu shows the current user

The user menu SHALL show the signed-in user's name and surname, the user's translated role below it, a circular avatar
with the initials of the name and of the surname, and a toggle that opens the menu. While the profile is loading,
after it failed to load, or while onboarding is pending, the name, role and avatar SHALL show as skeletons and no
name text SHALL be shown.

#### Scenario: Trainer in Spanish

- **WHEN** the profile is name "Ana", surname "García", role `TRAINER`, and the header is shown in Spanish
- **THEN** the user menu shows "Ana García", "Entrenador" below it, and an avatar with "AG"

#### Scenario: Administrator in Spanish

- **WHEN** the profile has role `ADMIN`
- **THEN** the role below the name reads "Administrador"

#### Scenario: Placeholder user in Spanish

- **WHEN** the header is shown in Spanish before the profile has loaded
- **THEN** the name, role and avatar show as skeletons
- **AND** "Usuario de prueba" and "UP" are not shown

#### Scenario: Onboarding pending

- **WHEN** the profile loads with an empty surname
- **THEN** the name, role and avatar show as skeletons and no name text is shown

### Requirement: The user menu lists the account entries

Opening the user menu SHALL show, in this order, "Mi perfil", "Preferencias", a separator and "Cerrar sesión". "Mi
perfil" SHALL close the menu and navigate to `/profile`. "Preferencias" SHALL close the menu and do nothing else until
its screen exists.

#### Scenario: Open the user menu

- **WHEN** the visitor clicks the user menu toggle
- **THEN** the menu shows "Mi perfil", "Preferencias", a separator and "Cerrar sesión", in that order

#### Scenario: Mi perfil clicked

- **WHEN** the visitor clicks "Mi perfil"
- **THEN** the menu closes and the visitor is taken to `/profile`

#### Scenario: Placeholder entry clicked

- **WHEN** the visitor clicks "Preferencias"
- **THEN** the menu closes, the visitor stays on the current screen and no operation is triggered
