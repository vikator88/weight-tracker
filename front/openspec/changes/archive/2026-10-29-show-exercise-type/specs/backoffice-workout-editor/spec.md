# Spec Delta

## ADDED Requirements

### Requirement: Type pills filter the catalog by exercise type

The catalog dialog SHALL show, before the body-part row, a row of pills with one pill per exercise type, in the order
"Fuerza", "Cardio" and "Estiramiento". The row SHALL always be shown, and selecting a type SHALL NOT show or hide any
other row. Each pill SHALL use its type's colour, outlined while unselected and filled while selected.

#### Scenario: Type row first

- **WHEN** the catalog dialog opens
- **THEN** the type pills "Fuerza", "Cardio" and "Estiramiento" are shown, outlined in blue, red and green, before the
  body-part pills

#### Scenario: Type selected

- **WHEN** the user selects the pill "Cardio"
- **THEN** only cardio exercises are listed
- **AND** "Cardio" is filled in red, and the body-part row is shown as before

#### Scenario: Type cleared

- **WHEN** the user selects the already selected pill "Cardio"
- **THEN** no type is selected and the type no longer filters the list

## MODIFIED Requirements

### Requirement: Catalog filters combine and are local

Typing SHALL keep only exercises whose name in the active language contains the text, ignoring case and accents.
Selecting a pill SHALL keep only exercises of that type, body part or muscle; selecting the selected pill again SHALL
clear it. All the filters SHALL apply together, and filtering SHALL NOT send any request. No match SHALL show "Ningún
ejercicio coincide con el filtro.". The filters SHALL be kept when the catalog dialog is closed and opened again on the
same screen.

#### Scenario: Filter by text

- **WHEN** the user types "sentadi"
- **THEN** only exercises whose name contains "sentadi" (such as "Sentadilla") are shown

#### Scenario: Filter by body part and text together

- **WHEN** the user selects the pill "Piernas" and types "prensa"
- **THEN** only legs exercises whose name contains "prensa" are shown

#### Scenario: Filter by type and body part together

- **WHEN** the user selects the pills "Fuerza" and "Piernas"
- **THEN** only strength exercises of the legs are shown

#### Scenario: Clear the body part

- **WHEN** the user selects the already selected pill "Piernas"
- **THEN** no pill is selected and the body part no longer filters the list

#### Scenario: Nothing matches

- **WHEN** no exercise matches the filters
- **THEN** "Ningún ejercicio coincide con el filtro." is shown

#### Scenario: Filters kept on reopening

- **WHEN** the user types "prensa", closes the catalog dialog and clicks "Añadir ejercicio" again
- **THEN** the filter field holds "prensa" and only matching exercises are shown

#### Scenario: Type kept on reopening

- **WHEN** the user selects "Cardio", closes the catalog dialog and opens it again
- **THEN** "Cardio" is still selected and only cardio exercises are shown

### Requirement: Each row of filter pills has a label

In the catalog, the type pills SHALL be preceded by the label "Tipo", the body-part pills by the label "Partes del
cuerpo", and the muscle pills by the label "Músculos". Each label SHALL name its group of pills, and the muscle label
SHALL be shown and hidden together with its row.

#### Scenario: Labels shown

- **WHEN** the user selects the pill "Piernas" in the catalog
- **THEN** the label "Partes del cuerpo" precedes the body-part pills and "Músculos" precedes the muscle pills

#### Scenario: Type label first

- **WHEN** the catalog dialog opens
- **THEN** the label "Tipo" precedes the type pills, and comes before "Partes del cuerpo"
