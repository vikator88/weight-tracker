# Spec Delta

## ADDED Requirements

### Requirement: The screen shows two stacked cards at full width

The workout screen SHALL use the full width of the page beside the navigation drawer. It SHALL show two cards, one
above the other and each taking the full width, on every screen size: "Propiedades del entrenamiento" on top and
"Ejercicios" below. The page SHALL NOT scroll horizontally.

#### Scenario: Cards in order

- **WHEN** the workout and the catalog are loaded
- **THEN** the card "Propiedades del entrenamiento" is shown above the card "Ejercicios", both at full width
- **AND** neither the catalog nor the exercise configuration is shown on the page

### Requirement: Añadir ejercicio opens the catalog dialog

The "Ejercicios" card SHALL show an "Añadir ejercicio" button in its header. Activating it SHALL open the "Catálogo de
ejercicios" dialog, at least 75% of the viewport width on wide screens, without sending any request.

#### Scenario: Catalog opened

- **WHEN** the user clicks "Añadir ejercicio"
- **THEN** the "Catálogo de ejercicios" dialog is shown
- **AND** no request is sent

### Requirement: The catalog dialog can be dismissed

The catalog dialog SHALL close with its "Cerrar" button, with Escape and with a click outside it. Closing it SHALL add
nothing to the workout and send no request.

#### Scenario: Catalog dismissed with Escape

- **WHEN** the catalog dialog is open and the user presses Escape
- **THEN** the dialog closes, the workout's list is unchanged and no request is sent

#### Scenario: Catalog dismissed with Cerrar

- **WHEN** the catalog dialog is open and the user clicks "Cerrar"
- **THEN** the dialog closes and no request is sent

### Requirement: Choosing a catalog exercise opens its configuration

Each exercise in the catalog dialog SHALL show an "Elegir ejercicio" plus icon button on its right. Activating it SHALL
close the catalog dialog and open the configuration dialog for that exercise, with no effort, no sets and an empty set
form. It SHALL NOT send any request and SHALL NOT add the exercise to the list.

#### Scenario: Exercise chosen

- **WHEN** the user clicks "Elegir ejercicio" on "Prensa de piernas"
- **THEN** the catalog dialog closes and the configuration dialog shows "Prensa de piernas"
- **AND** its effort is empty, its list of sets is empty and the set form is empty
- **AND** the workout's list is unchanged and no request is sent

### Requirement: The configuration dialog can be dismissed

The configuration dialog SHALL close with its "Cancelar" button, with Escape and with a click outside it, except while
its save is in progress. Closing it SHALL discard the edits and send no request. Closing it while adding SHALL leave the
exercise out of the workout and still listed in the catalog.

#### Scenario: Add dismissed

- **WHEN** the configuration dialog is open for "Prensa de piernas" being added, the user adds a set and clicks
  "Cancelar"
- **THEN** the dialog closes, "Prensa de piernas" is not in the workout's list and no request is sent
- **AND** reopening the catalog lists "Prensa de piernas"

#### Scenario: Edit dismissed

- **WHEN** the user opens "Sentadilla" with the pencil, changes its effort and presses Escape
- **THEN** the dialog closes and no request is sent
- **AND** opening "Sentadilla" again shows its stored effort

#### Scenario: Save in progress

- **WHEN** the save has been sent and has not answered yet
- **THEN** "Cancelar" is disabled and Escape does not close the dialog

### Requirement: Each listed exercise shows its sets and effort

Under its labels, each exercise of the workout SHALL show its sets on one line, without a label, each as its count
and target only ("2 × 15", "1 × 10–12", "3 × 40 s", "1 × máx") and separated by commas, and its effort on the next
line. A line SHALL NOT be shown while its property is empty.

#### Scenario: Sets and effort shown

- **WHEN** an exercise has effort `HIGH` and the sets 2 × 15, 1 × 10–12 and 1 × max reps
- **THEN** its item shows "2 × 15, 1 × 10–12, 1 × máx" on one line
- **AND** "Esfuerzo: Alto" on the next line

#### Scenario: No exercises

- **WHEN** the workout has no exercises
- **THEN** "Este entrenamiento aún no tiene ejercicios." is shown

### Requirement: The exercises card has an add button and grows with its list

The workout's exercises SHALL be preceded by the "Ejercicios" card title with the "Añadir ejercicio" button beside it,
and their list SHALL have no maximum height and no scroll of its own: the card SHALL grow with its rows and the page
SHALL scroll instead.

#### Scenario: Add button in the exercises card

- **WHEN** the exercises card is shown
- **THEN** it shows the "Ejercicios" title and an "Añadir ejercicio" button

#### Scenario: Long workout grows the card

- **WHEN** the workout's exercises are shown
- **THEN** their list has no maximum height and does not scroll on its own

### Requirement: Quitar ejercicio removes a saved exercise from the workout

"Quitar ejercicio" SHALL send exactly one removal of that catalog exercise from the workout, and on success SHALL take
it out of the list and list it again in the catalog.

#### Scenario: Saved exercise removed

- **WHEN** the user clicks "Quitar ejercicio" on "Sentadilla"
- **THEN** exactly one removal of "Sentadilla" from the workout is sent
- **AND** "Sentadilla" leaves the workout's list and is listed again in the catalog

### Requirement: Guardar in the configuration dialog saves the effort and sets

"Guardar" in the configuration dialog SHALL require an effort ("Elige un esfuerzo.") and at least one set ("Añade al
menos una serie."), sending nothing otherwise. When both are present it SHALL send exactly one request: an addition to
the workout for an exercise chosen from the catalog, or a replacement of the exercise's effort and sets for one opened
with the pencil, with the catalog exercise id, the effort and the listed sets. While it is in progress, "Guardar" SHALL
be disabled.

#### Scenario: Chosen exercise added

- **WHEN** "Prensa de piernas", chosen from the catalog, has effort "Alto" and the sets 2 × 15 and 1 × max reps and the
  user clicks "Guardar"
- **THEN** exactly one addition of that exercise to the workout is sent with effort `HIGH` and those two sets

#### Scenario: Saved exercise updated

- **WHEN** the user removes a set of "Sentadilla", opened with the pencil, and clicks "Guardar"
- **THEN** exactly one replacement of the effort and sets of "Sentadilla" in the workout is sent with the remaining sets

#### Scenario: Missing effort or sets

- **WHEN** a chosen exercise has no effort and no sets and the user clicks "Guardar"
- **THEN** "Elige un esfuerzo." and "Añade al menos una serie." are shown and no request is sent

### Requirement: The exercise save closes the dialog or shows the error

When the save succeeds, the configuration dialog SHALL close. An added exercise SHALL be appended to the end of the
workout's list, and an edited one SHALL show the sets and effort as the API answered them. When the save fails, the
dialog SHALL stay open, show the message for the error code and keep the effort and sets as edited.

#### Scenario: Chosen exercise added to the list

- **WHEN** the API accepts the addition of "Prensa de piernas" in a workout with two exercises
- **THEN** the dialog closes and "Prensa de piernas" is listed third with the sets the API answered

#### Scenario: Edited exercise updated in the list

- **WHEN** the API accepts the replacement of the sets of "Sentadilla"
- **THEN** the dialog closes and the "Sentadilla" item shows the sets the API answered

#### Scenario: Exercise rejected

- **WHEN** the API answers 409 to the addition
- **THEN** the dialog stays open, "La operación entra en conflicto con el estado actual." is shown and the listed sets
  are kept
- **AND** the exercise is not in the workout's list

### Requirement: The properties card shows the workout's data

The "Propiedades del entrenamiento" card SHALL hold the workout's Nombre, Fecha and Descripción in editable fields,
filled with the loaded values, and no exercises. Editing them SHALL change nothing until "Guardar" is pressed.

#### Scenario: Card filled

- **WHEN** workout "Pierna" dated 2026-11-02 with description "Día 1" is loaded
- **THEN** the card's fields hold "Pierna", 2026-11-02 and "Día 1"
- **AND** the card lists no exercises

### Requirement: The exercises card lists the workout's exercises

The "Ejercicios" card SHALL list the workout's saved exercises in their current order (the order the API returns
them until the user reorders them), numbered from 1. Each SHALL have a "Reordenar ejercicio" drag handle on its left,
a "Quitar ejercicio" trash icon button and an "Editar ejercicio" pencil icon button. With no exercises it SHALL show
"Este entrenamiento aún no tiene ejercicios.".

#### Scenario: Exercises listed

- **WHEN** the workout has two exercises
- **THEN** both are listed in that order, numbered 1 and 2, each in its own bordered card with "Reordenar ejercicio",
  "Quitar ejercicio" and "Editar ejercicio"

### Requirement: Listed exercises are cards in rows of up to three

Each exercise of the workout SHALL be shown in its own bordered card. The cards SHALL flow left to right in rows of at
most three, wrapping onto the next row, with fewer per row on narrower screens. Inside a card, the first row SHALL hold
the drag handle, the position, the name and the action icons; the second the body-part and muscle labels; the third
the sets.

#### Scenario: Rows inside a card

- **WHEN** "Sentadilla", listed first, with the sets 3 × 10, is shown and the catalog has more than one legs muscle
- **THEN** its card's first row holds "Reordenar ejercicio", "1", "Sentadilla", "Quitar ejercicio" and "Editar
  ejercicio"
- **AND** the labels "Piernas" and "Cuádriceps" come below that row, and "3 × 10" below the labels

#### Scenario: Exercises in a row

- **WHEN** the workout has two exercises on a wide screen
- **THEN** their cards sit side by side in the same row, the first on the left

#### Scenario: At most three per row

- **WHEN** the workout has four exercises on a wide screen
- **THEN** the first three share a row and the fourth starts the next one

### Requirement: The pencil opens the exercise's configuration

Clicking "Editar ejercicio" SHALL open the configuration dialog for that workout exercise, the same dialog used to add
one, filled with its stored effort and sets. It SHALL NOT send any request.

#### Scenario: Edit an exercise

- **WHEN** the user clicks "Editar ejercicio" on the second exercise
- **THEN** the configuration dialog shows that exercise with its stored effort and sets
- **AND** no request is sent

### Requirement: The configuration dialog shows the exercise

The configuration dialog SHALL be titled "Configuración del ejercicio" and SHALL show the exercise's video when the catalog
gives one, then its name, body part and muscle; then its effort, the list of its sets and a form to define a new set
with an "Añadir serie" button; and SHALL end with "Cancelar" and "Guardar". Each opening SHALL start from the stored
values, discarding any edit left unsaved.

#### Scenario: Exercise shown

- **WHEN** a squat with effort `HIGH` and one set of 3 × 10 repetitions is opened with the pencil
- **THEN** the dialog shows "Sentadilla", "Piernas" and "Cuádriceps"
- **AND** the effort field shows "Alto" and the list of sets shows "3 × 10 repeticiones"

#### Scenario: Another exercise opened

- **WHEN** the user changes the effort of one exercise, cancels, and opens another exercise
- **THEN** the dialog shows the other exercise's stored effort

### Requirement: Catalog exercises are cards in rows of up to three

In the catalog dialog, each exercise SHALL be shown in its own bordered card, with its name and the "Elegir
ejercicio" button on the first row and its body-part and muscle labels below. The cards SHALL flow left to right in
rows of at most three, wrapping onto the next row, with fewer per row on narrower screens. The list SHALL keep its
maximum height and scroll inside the dialog.

#### Scenario: Catalog as cards

- **WHEN** the catalog dialog lists three exercises on a wide screen
- **THEN** each one is in its own bordered card, and the three cards share one row from left to right

### Requirement: Each row of filter pills has a label

In the catalog, the body-part pills SHALL be preceded by the label "Partes del cuerpo", and the muscle pills by the
label "Músculos". Each label SHALL name its group of pills, and the muscle label SHALL be shown and hidden together
with its row.

#### Scenario: Labels shown

- **WHEN** the user selects the pill "Piernas" in the catalog
- **THEN** the label "Partes del cuerpo" precedes the body-part pills and "Músculos" precedes the muscle pills

### Requirement: Muscle pills refine a selected body part

While a body part is selected in the catalog, a second row of pills SHALL show one pill per distinct muscle of the
catalog exercises of that body part, sorted by name. Selecting a muscle SHALL keep only the exercises of that body
part and muscle; selecting it again SHALL clear it. Changing or clearing the body part SHALL clear the muscle. With no
body part selected, or when the listed exercises of that body part work a single muscle, the row SHALL NOT be shown.

#### Scenario: No body part, no muscle row

- **WHEN** the catalog dialog opens with no body part selected
- **THEN** no muscle pills and no "Músculos" label are shown

#### Scenario: Muscles of the body part

- **WHEN** the user selects the pill "Brazos" and the catalog's arms exercises work biceps and triceps
- **THEN** a second row shows the pills "Bíceps" and "Tríceps"

#### Scenario: Filter by muscle

- **WHEN** the user selects "Brazos" and then "Bíceps"
- **THEN** only arms exercises that work the biceps are listed

#### Scenario: Single muscle, no muscle row

- **WHEN** the user selects the pill "Pecho" and every listed chest exercise works the chest
- **THEN** no muscle pills and no "Músculos" label are shown

#### Scenario: Muscle cleared with its body part

- **WHEN** a muscle is selected and the user selects another body part
- **THEN** no muscle is selected and the new body part's muscles are shown

### Requirement: The configuration dialog shows the exercise beside its configuration

On wide screens the configuration dialog SHALL show the exercise (video, name and labels) in a left column of about
30% of its width, and the configuration (effort, sets and set form) in a right column of about 70%. On narrow screens
the columns SHALL stack, the exercise first.

#### Scenario: Exercise beside its configuration

- **WHEN** the dialog is open on a wide screen
- **THEN** the exercise's name is in the left column and the effort field in the right one, side by side

### Requirement: A single muscle is left out of the exercise lists

In the catalog and the workout list, an exercise's muscle label SHALL be left out when every catalog exercise of the
same body part works that one muscle. The configuration dialog SHALL always show it.

#### Scenario: Single-muscle body part

- **WHEN** every chest exercise of the catalog works the chest
- **THEN** a chest exercise in the catalog and in the workout list shows only the label "Pecho"
- **AND** its configuration dialog shows both "Pecho" labels

## MODIFIED Requirements

### Requirement: Each exercise shows its name, body part and muscle

Each exercise, in the catalog and in the workout, SHALL show its name in the active language and, below it, labels with
its body part and muscle translated. An unknown code SHALL be shown as it comes. A workout exercise SHALL take these
from the catalog exercise with the same id; with no match it SHALL show its exercise id as the name and no labels.

#### Scenario: Labels in Spanish

- **WHEN** an exercise of body part `LEGS` and muscle `QUADRICEPS` is shown in Spanish, and the catalog has legs
  exercises for another muscle too
- **THEN** its labels read "Piernas" and "Cuádriceps"

#### Scenario: Name in the active language

- **WHEN** an exercise has the names "Sentadilla" (es) and "Squat" (en) and the language is English
- **THEN** it is shown as "Squat"

#### Scenario: Workout exercise missing from the catalog

- **WHEN** a workout exercise has an exercise id the catalog does not contain
- **THEN** it is shown with that id as its name and without labels

### Requirement: Body-part labels are coloured by body part

The body-part label of an exercise SHALL use a colour of its own for each body part, the same everywhere on the
screen, filled with that colour; an unknown body part SHALL use the neutral colour. The muscle label SHALL use the
colour of the exercise's body part as its outline only. The body-part and muscle filter pills SHALL use the same
colours, outlined while unselected and filled while selected.

#### Scenario: Chest and legs differ

- **WHEN** a chest exercise and a legs exercise are shown
- **THEN** their body-part labels are filled with different colours
- **AND** each muscle label shown is outlined in its body part's colour

#### Scenario: Pills follow the labels

- **WHEN** the pill "Piernas" is selected in the catalog
- **THEN** it is filled with the legs colour, the other body-part pills are outlined in their colours, and the muscle
  pills are outlined in the legs colour

### Requirement: The screen has a page header

Above the cards, the screen SHALL show the page heading "Editar entrenamiento" and a "Volver" link to
`/assigned-plans`. The "Guardar" button that saves the workout's data SHALL sit inside the properties card, after its
fields.

#### Scenario: Header shown

- **WHEN** workout "Pierna" is loaded
- **THEN** the page heading reads "Editar entrenamiento"
- **AND** a "Volver" link leads to `/assigned-plans`
- **AND** the properties card shows "Guardar" after Descripción

### Requirement: Catalog filters combine and are local

Typing SHALL keep only exercises whose name in the active language contains the text, ignoring case and accents.
Selecting a pill SHALL keep only exercises of that body part; selecting the selected pill again SHALL clear it. Both
filters SHALL apply together, and filtering SHALL NOT send any request. No match SHALL show "Ningún ejercicio coincide
con el filtro.". The filters SHALL be kept when the catalog dialog is closed and opened again on the same screen.

#### Scenario: Filter by text

- **WHEN** the user types "sentadi"
- **THEN** only exercises whose name contains "sentadi" (such as "Sentadilla") are shown

#### Scenario: Filter by body part and text together

- **WHEN** the user selects the pill "Piernas" and types "prensa"
- **THEN** only legs exercises whose name contains "prensa" are shown

#### Scenario: Clear the body part

- **WHEN** the user selects the already selected pill "Piernas"
- **THEN** no pill is selected and the body part no longer filters the list

#### Scenario: Nothing matches

- **WHEN** no exercise matches the filters
- **THEN** "Ningún ejercicio coincide con el filtro." is shown

#### Scenario: Filters kept on reopening

- **WHEN** the user types "prensa", closes the catalog dialog and clicks "Añadir ejercicio" again
- **THEN** the filter field holds "prensa" and only matching exercises are shown

### Requirement: The catalog hides exercises already in the workout

The catalog SHALL NOT list an exercise that is already saved in the workout, so the same exercise cannot be added
twice.

#### Scenario: Saved exercise hidden

- **WHEN** the workout already holds "Sentadilla"
- **THEN** the catalog does not list "Sentadilla"

#### Scenario: Added exercise leaves the catalog

- **WHEN** the API accepts the addition of "Prensa de piernas" and the user opens the catalog again
- **THEN** the catalog no longer lists "Prensa de piernas"

### Requirement: A removal in progress or refused is shown

While a removal is in progress, its "Quitar ejercicio" button SHALL be disabled. A failed removal SHALL show the
message for the error code in the exercises card and keep the exercise listed.

#### Scenario: Removal rejected

- **WHEN** the API answers 409 to the removal
- **THEN** "La operación entra en conflicto con el estado actual." is shown and the exercise stays listed

### Requirement: A refused reorder restores the previous order

When the reorder fails, the list SHALL go back to the order it had before the drop, and the message for the error code
SHALL be shown in the exercises card.

#### Scenario: Reorder rejected

- **WHEN** the API answers 409 to the reorder
- **THEN** the exercises are listed in the order they had before the drop
- **AND** "La operación entra en conflicto con el estado actual." is shown

### Requirement: The sets of an exercise are listed with a delete action

The configuration dialog SHALL list the exercise's sets in order, each as a summary — "2 × 15 repeticiones",
"1 × 10–12 repeticiones", "3 × 40 s" or "1 × máximo" by target type — with a "Quitar serie" trash button on its right.
Removing a set SHALL take it out of the list only; nothing is sent until "Guardar".

#### Scenario: Several sets listed

- **WHEN** an exercise has the sets 2 × 15, 1 × 10–12 and 1 × max reps
- **THEN** the list shows "2 × 15 repeticiones", "1 × 10–12 repeticiones" and "1 × máximo", in that order

#### Scenario: Set removed

- **WHEN** the user clicks "Quitar serie" on the second set
- **THEN** that set leaves the list and no request is sent

### Requirement: The exercise video is embedded when it is a YouTube link

When the exercise's video is a YouTube watch link, the configuration dialog SHALL embed it as a player from
`https://www.youtube-nocookie.com/embed/<video id>`, titled with the exercise name. Any other link SHALL be shown as a
"Ver vídeo" link opening in a new tab. An exercise without video SHALL show no video section.

#### Scenario: YouTube video

- **WHEN** the video is `https://www.youtube.com/watch?v=h_yZV27H684`
- **THEN** a player from `https://www.youtube-nocookie.com/embed/h_yZV27H684` is shown

#### Scenario: Other link

- **WHEN** the video is `https://example.com/squat.mp4`
- **THEN** a "Ver vídeo" link to that address opening in a new tab is shown and no player

#### Scenario: No video

- **WHEN** the exercise has no video
- **THEN** no video section is shown

### Requirement: The workout screen is translated

Every text of the screen and its dialogs, including muscle, body-part and effort labels, SHALL be available in Spanish
and English and SHALL follow the active language.

#### Scenario: Screen in English

- **WHEN** the active language is English
- **THEN** the screen shows "Workout properties", "Add exercise", "Save" and, in the catalog dialog, "Search exercise"
  and the label "Quadriceps"

### Requirement: Dropping a saved exercise reorders the list and saves the order

Dragging a saved exercise by its handle and dropping it at another position SHALL reorder the list on screen at once,
renumbering it. It SHALL then send exactly one reorder of the workout with the catalog ids of every saved exercise in
the new order. On success the list SHALL keep the new order.

#### Scenario: Exercise moved down

- **WHEN** the workout lists "Sentadilla" then "Press banca" and the user drops "Sentadilla" after "Press banca"
- **THEN** "Press banca" is listed as 1 and "Sentadilla" as 2 before the API answers
- **AND** exactly one reorder is sent with the ids of "Press banca" and "Sentadilla" in that order

#### Scenario: Reorder accepted

- **WHEN** the API accepts the reorder
- **THEN** the list keeps the new order and no error is shown

### Requirement: Exercises can be reordered from the keyboard

A focused drag handle SHALL pick up its exercise with Space or Enter. The arrow keys SHALL move it across the rows of
cards, Space or Enter SHALL drop it, and Escape SHALL cancel. Screen readers SHALL get the pick-up, move, drop and
cancel announcements in the active language.

#### Scenario: Keyboard reorder

- **WHEN** the user focuses the handle of exercise 1, presses Space, ArrowRight and Space
- **THEN** that exercise is listed as 2 and a reorder is sent

## REMOVED Requirements

### Requirement: The screen shows three columns

**Reason**: The catalog and the configuration move into dialogs; the screen shows two cards at full width.
**Migration**: See "The screen shows two stacked cards at full width".

### Requirement: The catalog column lists and filters the exercises

**Reason**: The catalog is no longer a column. Its content (filter field, wrapping body-part pills, list scrolling
inside a maximum height) moves unchanged into the catalog dialog.
**Migration**: See "Añadir ejercicio opens the catalog dialog" and "Catalog filters combine and are local".

### Requirement: The plus adds a catalog exercise to the workout locally

**Reason**: Pending exercises are removed. Choosing a catalog exercise now opens its configuration, and the exercise
is only added when the API accepts the configuration.
**Migration**: See "Choosing a catalog exercise opens its configuration" and "Guardar saves the exercise's effort and
sets".

### Requirement: The workout card shows the workout's data

**Reason**: The workout card is split; its data lives in the properties card.
**Migration**: See "The properties card shows the workout's data".

### Requirement: The workout card lists the workout's exercises

**Reason**: The workout card is split; its exercises live in the exercises card.
**Migration**: See "The exercises card lists the workout's exercises".

### Requirement: Pending exercises cannot be reordered

**Reason**: Pending exercises no longer exist; every listed exercise is saved and can be reordered.
**Migration**: None.

### Requirement: Reordering keeps the selection

**Reason**: There is no selection any more; the pencil opens a dialog instead of selecting an exercise.
**Migration**: None.

### Requirement: The pencil selects a workout exercise to configure

**Reason**: The pencil now opens the configuration dialog.
**Migration**: See "The pencil opens the exercise's configuration".

### Requirement: The configuration column shows the selected exercise

**Reason**: The configuration is now a dialog, opened only for an exercise, so the "Selecciona un ejercicio para
configurarlo." placeholder is gone.
**Migration**: See "The configuration dialog shows the exercise".

### Requirement: Each workout exercise shows its sets and effort

**Reason**: Pending exercises and the configuration column are gone; the requirement is restated for the dialogs.
**Migration**: See "Each listed exercise shows its sets and effort".

### Requirement: The exercises list has a header and scrolls in its card

**Reason**: Pending exercises and the configuration column are gone; the requirement is restated for the dialogs.
**Migration**: See "The exercises card has an add button and grows with its list".

### Requirement: Quitar ejercicio removes the exercise from the workout

**Reason**: Pending exercises and the configuration column are gone; the requirement is restated for the dialogs.
**Migration**: See "Quitar ejercicio removes a saved exercise from the workout".

### Requirement: Guardar saves the exercise's effort and sets

**Reason**: Pending exercises and the configuration column are gone; the requirement is restated for the dialogs.
**Migration**: See "Guardar in the configuration dialog saves the effort and sets".

### Requirement: The exercise save outcome is shown

**Reason**: Pending exercises and the configuration column are gone; the requirement is restated for the dialogs.
**Migration**: See "The exercise save closes the dialog or shows the error".
