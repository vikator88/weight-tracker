# Spec Delta

## MODIFIED Requirements

### Requirement: The workout card lists the workout's exercises

Inside the same card, below the fields, the workout's exercises SHALL be listed in their current order (the order the
API returns them until the user reorders them), numbered from 1 and laid out like the catalog exercises. Each SHALL
have a "Reordenar ejercicio" drag handle on its left, a "Quitar ejercicio" trash icon button and an "Editar ejercicio"
pencil icon button. With no exercises it SHALL show "Este entrenamiento aún no tiene ejercicios.".

#### Scenario: Exercises listed

- **WHEN** the workout has two exercises
- **THEN** both are listed in that order, numbered 1 and 2, each with "Reordenar ejercicio", "Quitar ejercicio" and "Editar ejercicio"

## ADDED Requirements

### Requirement: Dropping a saved exercise reorders the list and saves the order

Dragging a saved exercise by its handle and dropping it at another position SHALL reorder the list on screen at once,
renumbering it. It SHALL then send exactly one reorder of the workout with the catalog ids of every saved exercise in
the new order. On success the list SHALL keep the new order.

#### Scenario: Exercise moved down

- **WHEN** the workout lists "Sentadilla" then "Press banca" and the user drops "Sentadilla" below "Press banca"
- **THEN** "Press banca" is listed as 1 and "Sentadilla" as 2 before the API answers
- **AND** exactly one reorder is sent with the ids of "Press banca" and "Sentadilla" in that order

#### Scenario: Reorder accepted

- **WHEN** the API accepts the reorder
- **THEN** the list keeps the new order and no error is shown

### Requirement: Dropping an exercise at its own position sends nothing

Dropping an exercise where it was, or cancelling the drag, SHALL leave the order unchanged and SHALL NOT send a request.

#### Scenario: Dropped in place

- **WHEN** the user picks up "Sentadilla" and drops it at its own position
- **THEN** the order is unchanged and no reorder is sent

#### Scenario: Drag cancelled

- **WHEN** the user picks up "Sentadilla", moves it and presses Escape
- **THEN** the order is unchanged and no reorder is sent

### Requirement: A refused reorder restores the previous order

When the reorder fails, the list SHALL go back to the order it had before the drop, and the message for the error code
SHALL be shown in the workout card.

#### Scenario: Reorder rejected

- **WHEN** the API answers 409 to the reorder
- **THEN** the exercises are listed in the order they had before the drop
- **AND** "La operación entra en conflicto con el estado actual." is shown

### Requirement: Dragging is disabled while a reorder is in progress

While a reorder request is in progress, every drag handle SHALL be disabled, so a second reorder cannot start before
the first one is answered.

#### Scenario: Reorder in progress

- **WHEN** a reorder has been sent and the API has not answered yet
- **THEN** every "Reordenar ejercicio" handle is disabled

### Requirement: Pending exercises cannot be reordered

A pending exercise (added from the catalog and not saved yet) SHALL keep its place after the saved ones, with its
handle disabled. Pending exercises SHALL never be included in a reorder request.

#### Scenario: Pending exercise handle

- **WHEN** the workout lists a saved exercise and a pending one
- **THEN** the pending exercise's "Reordenar ejercicio" handle is disabled

#### Scenario: Reorder with a pending exercise listed

- **WHEN** the user reorders two saved exercises while a pending exercise is listed
- **THEN** the reorder sent holds only the two saved exercises' ids

### Requirement: Exercises can be reordered from the keyboard

A focused drag handle SHALL pick up its exercise with Space or Enter. The arrow keys SHALL move it, Space or Enter SHALL
drop it, and Escape SHALL cancel. Screen readers SHALL get the pick-up, move, drop and cancel announcements in the
active language.

#### Scenario: Keyboard reorder

- **WHEN** the user focuses the handle of exercise 1, presses Space, ArrowDown and Space
- **THEN** that exercise is listed as 2 and a reorder is sent

### Requirement: Reordering keeps the selection

Reordering SHALL NOT change which exercise is selected for configuration.

#### Scenario: Selected exercise moved

- **WHEN** "Sentadilla" is selected and the user moves it to another position
- **THEN** "Sentadilla" stays selected and the configuration card still shows it
