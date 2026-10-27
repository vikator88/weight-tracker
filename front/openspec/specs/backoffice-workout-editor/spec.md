# backoffice-workout-editor Specification

## Purpose

Gives trainers and admins one backoffice screen per workout, showing the exercise catalog, the workout with its
exercises, and the configuration of the exercise they select, as the base of the workout editor.

## Requirements

### Requirement: The workout screen is reached by the workout id

The backoffice SHALL offer a signed-in screen at `/workout?id=<workout id>` that reads that workout and the exercise
catalog once on opening. A visitor without a session SHALL end on `/login`.

#### Scenario: Trainer opens a workout

- **WHEN** a trainer with a stored session opens `/workout?id=w-1`
- **THEN** exactly one read of workout `w-1` and one read of the exercise catalog are sent
- **AND** the screen is shown beside the navigation drawer

#### Scenario: Visitor without a session

- **WHEN** a visitor with no stored session opens `/workout?id=w-1`
- **THEN** the visitor ends on `/login` and no workout is read

### Requirement: The screen shows three columns

On wide screens the workout screen SHALL lay out three cards side by side taking about 25%, 40% and 35% of the width, titled "Catálogo de ejercicios", "Estructura del entrenamiento" and "Detalles del ejercicio". On narrow screens
they SHALL stack in that order. The page SHALL NOT scroll horizontally.

#### Scenario: Columns in order

- **WHEN** the workout and the catalog are loaded
- **THEN** the cards "Catálogo de ejercicios", "Estructura del entrenamiento" and "Detalles del ejercicio" are shown, in
  that order

### Requirement: The screen has a page header

Above the cards, the screen SHALL show the page heading "Editar entrenamiento" and a "Volver" link to
`/assigned-plans`. The "Guardar" button that saves the workout's data SHALL sit inside the workout card, between its
fields and its list of exercises.

#### Scenario: Header shown

- **WHEN** workout "Pierna" is loaded
- **THEN** the page heading reads "Editar entrenamiento"
- **AND** a "Volver" link leads to `/assigned-plans`
- **AND** the workout card shows "Guardar" after Descripción and before "Ejercicios"

### Requirement: Guardar checks the workout's data and sends nothing when invalid

On "Guardar", the screen SHALL check the card's fields structurally and, if any fails, SHALL show the message under
that field and SHALL NOT send any request. Nombre is required and at most 256 characters after trimming; Descripción
is optional and at most 1024 characters; Fecha is a required date.

#### Scenario: Blank name

- **WHEN** the user clears Nombre and clicks "Guardar"
- **THEN** Nombre shows "Este campo es obligatorio." and no request is sent

#### Scenario: Description too long

- **WHEN** the description has 1025 characters and the user clicks "Guardar"
- **THEN** Descripción shows "No puede superar los 1024 caracteres." and no request is sent

### Requirement: Guardar replaces the workout's editable data

When the fields are valid, "Guardar" SHALL send exactly one update of the workout with its id, the trimmed name, the
date, the trimmed description or none when blank, and the athlete and trainer exactly as they were read. While it is in
progress, "Guardar" SHALL be disabled.

#### Scenario: Workout saved

- **WHEN** the user changes the name to " Pierna B " and clicks "Guardar"
- **THEN** exactly one update of workout `w-1` is sent with name "Pierna B", the loaded date, description "Día 1", and the
  loaded athlete and trainer

#### Scenario: Description cleared

- **WHEN** the user empties Descripción and clicks "Guardar"
- **THEN** the update carries no description

#### Scenario: Save in progress

- **WHEN** the update has been sent and has not answered yet
- **THEN** "Guardar" is disabled

### Requirement: The save outcome is shown

When the update succeeds, the screen SHALL show "Entrenamiento guardado.". Editing a field again SHALL hide the
confirmation. When it fails, the screen SHALL show the message for the error code and keep the typed values.

#### Scenario: Saved

- **WHEN** the API accepts the update with the name "Pierna B"
- **THEN** "Entrenamiento guardado." is shown and the Nombre field holds "Pierna B"

#### Scenario: Rejected

- **WHEN** the API answers 403
- **THEN** "No tienes permiso para realizar esta acción." is shown
- **AND** the fields keep the typed values

### Requirement: The catalog column lists and filters the exercises

The catalog column SHALL show a "Buscar ejercicio" text field, then one pill per distinct body part of the catalog, then
every catalog exercise. Pills that do not fit SHALL wrap onto the next line, never scroll horizontally. The list of
exercises SHALL have a maximum height and scroll inside its card, so a long catalog does not lengthen the page.

#### Scenario: Catalog shown

- **WHEN** the catalog has three exercises over two body parts
- **THEN** two body-part pills and three exercises are shown

#### Scenario: Long catalog scrolls inside its card

- **WHEN** the catalog is shown
- **THEN** its list of exercises has a maximum height and scrolls vertically on its own

### Requirement: Catalog filters combine and are local

Typing SHALL keep only exercises whose name in the active language contains the text, ignoring case and accents.
Selecting a pill SHALL keep only exercises of that body part; selecting the selected pill again SHALL clear it. Both
filters SHALL apply together, and filtering SHALL NOT send any request. No match SHALL show "Ningún ejercicio coincide
con el filtro.".

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

### Requirement: Each exercise shows its name, body part and muscle

Each exercise, in the catalog and in the workout, SHALL show its name in the active language and, below it, labels with
its body part and muscle translated. An unknown code SHALL be shown as it comes. A workout exercise SHALL take these
from the catalog exercise with the same id; with no match it SHALL show its exercise id as the name and no labels.

#### Scenario: Labels in Spanish

- **WHEN** an exercise of body part `LEGS` and muscle `QUADRICEPS` is shown in Spanish
- **THEN** its labels read "Piernas" and "Cuádriceps"

#### Scenario: Name in the active language

- **WHEN** an exercise has the names "Sentadilla" (es) and "Squat" (en) and the language is English
- **THEN** it is shown as "Squat"

#### Scenario: Workout exercise missing from the catalog

- **WHEN** a workout exercise has an exercise id the catalog does not contain
- **THEN** it is shown with that id as its name and without labels

### Requirement: Body-part labels are coloured by body part

The body-part label of an exercise SHALL use a colour of its own for each body part, the same everywhere on the screen;
an unknown body part SHALL use the neutral colour. The muscle label SHALL stay neutral.

#### Scenario: Chest and legs differ

- **WHEN** a chest exercise and a legs exercise are shown
- **THEN** their body-part labels have different colours
- **AND** their muscle labels have the neutral colour

### Requirement: The plus adds a catalog exercise to the workout locally

Each catalog exercise SHALL show an "Añadir ejercicio" plus icon button on its right. Activating it SHALL append the
exercise to the end of the workout's exercises as pending, select it, and open it in the configuration card. It SHALL
NOT send any request: a pending exercise is only kept on screen until its sets can be saved.

#### Scenario: Add pressed

- **WHEN** the user clicks "Añadir ejercicio" on "Prensa de piernas" in a workout with two exercises
- **THEN** "Prensa de piernas" is listed third, marked "Pendiente" and selected
- **AND** the configuration card shows "Prensa de piernas"
- **AND** no request is sent

#### Scenario: Pending exercise starts without sets

- **WHEN** a pending exercise is shown in the configuration card
- **THEN** its effort is empty, its list of sets is empty, and the set form is empty and ready to be defined

### Requirement: The catalog hides exercises already in the workout

The catalog SHALL NOT list an exercise that is already in the workout, whether saved or pending, so the same exercise
cannot be added twice.

#### Scenario: Saved exercise hidden

- **WHEN** the workout already holds "Sentadilla"
- **THEN** the catalog does not list "Sentadilla"

#### Scenario: Added exercise leaves the catalog

- **WHEN** the user adds "Prensa de piernas" with the plus
- **THEN** the catalog no longer lists "Prensa de piernas"

### Requirement: The workout card shows the workout's data

The "Estructura del entrenamiento" card SHALL start with the workout's Nombre, Fecha and Descripción in editable
fields, filled with the loaded values. Editing them SHALL change nothing until "Guardar" is pressed.

#### Scenario: Card filled

- **WHEN** workout "Pierna" dated 2026-11-02 with description "Día 1" is loaded
- **THEN** the card's fields hold "Pierna", 2026-11-02 and "Día 1"

### Requirement: The workout card lists the workout's exercises

Inside the same card, below the fields, the workout's exercises SHALL be listed in the order the API returns them,
numbered from 1 and laid out like the catalog exercises, each with a "Quitar ejercicio" trash icon button and an
"Editar ejercicio" pencil icon button. With no exercises it SHALL show "Este entrenamiento aún no tiene ejercicios.".

#### Scenario: Exercises listed

- **WHEN** the workout has two exercises
- **THEN** both are listed in that order, numbered 1 and 2, each with "Quitar ejercicio" and "Editar ejercicio"

### Requirement: Each workout exercise shows its sets and effort

Under its labels, each exercise of the workout SHALL show its sets on one line, as the configuration card summarises
them and separated by commas, and its effort on the next line. A line SHALL NOT be shown while its property is empty,
as for a pending exercise before it is defined.

#### Scenario: Sets and effort shown

- **WHEN** an exercise has effort `HIGH` and the sets 2 × 15, 1 × 10–12 and 1 × max reps
- **THEN** its item shows "Series: 2 × 15 repeticiones, 1 × 10–12 repeticiones, 1 × máximo" on one line
- **AND** "Esfuerzo: Alto" on the next line

#### Scenario: Pending exercise without sets nor effort

- **WHEN** a pending exercise has no sets nor effort yet
- **THEN** its item shows neither line

#### Scenario: No exercises

- **WHEN** the workout has no exercises
- **THEN** "Este entrenamiento aún no tiene ejercicios." is shown

### Requirement: The exercises list has a header and scrolls in its card

The workout's exercises SHALL be preceded by an "Ejercicios" header, without any add button (exercises are added from
the catalog), and their list SHALL have a maximum height and scroll inside the card.

#### Scenario: No add button in the workout card

- **WHEN** the workout card is shown
- **THEN** it shows the "Ejercicios" header and no "Añadir ejercicio" button

#### Scenario: Long workout scrolls inside its card

- **WHEN** the workout's exercises are shown
- **THEN** their list has a maximum height and scrolls vertically on its own

### Requirement: Quitar ejercicio removes the exercise from the workout

"Quitar ejercicio" on a saved exercise SHALL send exactly one removal of that catalog exercise from the workout, and
on success SHALL take it out of the list and show it again in the catalog. On a pending exercise it SHALL only take it
out of the list, without a request. A removed exercise that was selected SHALL leave the configuration card empty.

#### Scenario: Saved exercise removed

- **WHEN** the user clicks "Quitar ejercicio" on the saved "Sentadilla"
- **THEN** exactly one removal of "Sentadilla" from the workout is sent
- **AND** "Sentadilla" leaves the workout's list and is listed again in the catalog

#### Scenario: Pending exercise removed

- **WHEN** the user clicks "Quitar ejercicio" on a pending exercise
- **THEN** it leaves the list and is listed again in the catalog, and no request is sent

#### Scenario: Selected exercise removed

- **WHEN** the removed exercise was selected
- **THEN** the configuration card shows "Selecciona un ejercicio para configurarlo."

### Requirement: A removal in progress or refused is shown

While a removal is in progress, its "Quitar ejercicio" button SHALL be disabled. A failed removal SHALL show the
message for the error code in the workout card and keep the exercise listed.

#### Scenario: Removal rejected

- **WHEN** the API answers 409 to the removal
- **THEN** "La operación entra en conflicto con el estado actual." is shown and the exercise stays listed

### Requirement: The pencil selects a workout exercise to configure

Clicking "Editar ejercicio" SHALL select that workout exercise, mark it as selected in the list, and show it in the
configuration column. It SHALL NOT send any request.

#### Scenario: Select an exercise

- **WHEN** the user clicks "Editar ejercicio" on the second exercise
- **THEN** the second exercise is marked as selected
- **AND** the configuration column shows that exercise
- **AND** no request is sent

### Requirement: The configuration column shows the selected exercise

With nothing selected, the configuration column SHALL show "Selecciona un ejercicio para configurarlo.". With an
exercise selected, it SHALL show its video when the catalog gives one, then its name, body part and muscle, then its
effort, then the list of its sets, then a form to define a new set with an "Añadir serie" button, then "Guardar".
Selecting another exercise SHALL show that exercise's stored values, discarding any unsaved edit.

#### Scenario: Nothing selected

- **WHEN** the screen opens
- **THEN** the configuration column shows "Selecciona un ejercicio para configurarlo."

#### Scenario: Selected exercise shown

- **WHEN** a squat with effort `HIGH` and one set of 3 × 10 repetitions is selected
- **THEN** the column shows "Sentadilla", "Piernas" and "Cuádriceps"
- **AND** the effort field shows "Alto" and the list of sets shows "3 × 10 repeticiones"

#### Scenario: Edits discarded on another selection

- **WHEN** the user changes the effort and then selects another exercise
- **THEN** the column shows the other exercise's stored effort

### Requirement: The sets of an exercise are listed with a delete action

The configuration column SHALL list the selected exercise's sets in order, each as a summary — "2 × 15 repeticiones",
"1 × 10–12 repeticiones", "3 × 40 s" or "1 × máximo" by target type — with a "Quitar serie" trash button on its right.
Removing a set SHALL take it out of the list only; nothing is sent until "Guardar".

#### Scenario: Several sets listed

- **WHEN** an exercise has the sets 2 × 15, 1 × 10–12 and 1 × max reps
- **THEN** the list shows "2 × 15 repeticiones", "1 × 10–12 repeticiones" and "1 × máximo", in that order

#### Scenario: Set removed

- **WHEN** the user clicks "Quitar serie" on the second set
- **THEN** that set leaves the list and no request is sent

### Requirement: The set form adds a set to the list

Below the list, a form SHALL ask for Series, Tipo, and the target fields the type needs: Valor for repetitions and
seconds, Valor and Máximo for a repetition range, none for max reps. "Añadir serie" SHALL check the form structurally,
show the message under each invalid field, and otherwise append the set to the end of the list and empty the form.

#### Scenario: Set added

- **WHEN** the user enters Series 2, Tipo "Repeticiones", Valor 15 and clicks "Añadir serie"
- **THEN** "2 × 15 repeticiones" is appended to the list and the form is empty again
- **AND** no request is sent

#### Scenario: Max reps needs no value

- **WHEN** the user picks Tipo "Máximas repeticiones"
- **THEN** the form shows no Valor nor Máximo field

#### Scenario: Invalid set

- **WHEN** the user clicks "Añadir serie" with Series empty or below 1, or a needed Valor or Máximo empty or below 1
- **THEN** each of those fields shows "Introduce un número entero mayor que 0." and the list is unchanged

### Requirement: Guardar saves the exercise's effort and sets

"Guardar" in the configuration column SHALL require an effort ("Elige un esfuerzo.") and at least one set ("Añade al
menos una serie."), sending nothing otherwise. When both are present it SHALL send exactly one request: an addition to
the workout for a pending exercise, or a replacement of the exercise's effort and sets for a saved one, with the
catalog exercise id, the effort and the listed sets. While it is in progress, "Guardar" SHALL be disabled.

#### Scenario: Pending exercise saved

- **WHEN** a pending "Prensa de piernas" has effort "Alto" and the sets 2 × 15 and 1 × max reps and the user clicks
  "Guardar"
- **THEN** exactly one addition of that exercise to the workout is sent with effort `HIGH` and those two sets

#### Scenario: Saved exercise updated

- **WHEN** the user removes a set of the saved "Sentadilla" and clicks "Guardar"
- **THEN** exactly one replacement of the effort and sets of "Sentadilla" in the workout is sent with the remaining sets

#### Scenario: Missing effort or sets

- **WHEN** a pending exercise has no effort and no sets and the user clicks "Guardar"
- **THEN** "Elige un esfuerzo." and "Añade al menos una serie." are shown and no request is sent

### Requirement: The exercise save outcome is shown

When the save succeeds, the exercise SHALL stay selected and show "Ejercicio guardado.", lose its "Pendiente" mark,
and show the sets as the API answered them. When it fails, the column SHALL show the message for the error code and
keep the effort and sets as edited.

#### Scenario: Pending exercise becomes saved

- **WHEN** the API accepts the addition of the pending "Prensa de piernas"
- **THEN** "Ejercicio guardado." is shown, "Prensa de piernas" is no longer marked "Pendiente" and stays selected

#### Scenario: Exercise rejected

- **WHEN** the API answers 409 to the addition
- **THEN** "La operación entra en conflicto con el estado actual." is shown and the listed sets are kept

### Requirement: The exercise video is embedded when it is a YouTube link

When the selected exercise's video is a YouTube watch link, the configuration column SHALL embed it as a player from
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

### Requirement: The screen shows loading, not found and failure states

While the workout or the catalog loads, the screen SHALL show a loading placeholder. A missing `id`, or a workout the
API does not find, SHALL show "No hemos encontrado este entrenamiento." with a "Volver a planes asignados" link to
`/assigned-plans`. Any other failure SHALL show the error for its code with a retry that reads again what failed.

#### Scenario: Missing id

- **WHEN** the visitor opens `/workout` without `id`
- **THEN** "No hemos encontrado este entrenamiento." and the link back are shown
- **AND** no workout read is sent

#### Scenario: Unknown workout

- **WHEN** the API answers 404 for the workout
- **THEN** "No hemos encontrado este entrenamiento." and the link back are shown

#### Scenario: Catalog fails and the user retries

- **WHEN** the catalog read fails with no response and the user clicks "Reintentar"
- **THEN** the catalog is read again and shown when it succeeds

### Requirement: The workout screen is translated

Every text of the screen, including muscle, body-part and effort labels, SHALL be available in Spanish and English and
SHALL follow the active language.

#### Scenario: Screen in English

- **WHEN** the active language is English
- **THEN** the screen shows "Search exercise", "Save", "Select an exercise to configure it." and the label "Quadriceps"
