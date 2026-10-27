# Design

## Context

See proposal.md for the motivation and the specs for the behaviour. Current state that shapes the approach:

- `core` already has, and both containers register, `createWorkoutUseCase` (`CreateWorkoutRequest` → `Workout`),
  `getWorkoutByIdUseCase` (`{ id }` → `WorkoutDetail`) and `getExercisesUseCase` (`void` → `Exercise[]`).
- `WorkoutDetail.exercises[]` carry `id`, `exerciseId`, `effort` and `sets[{ count, target: { type, value, maxValue } }]`,
  no name, muscle or body part. `Exercise` carries `id`, `videoUrl`, `muscle`, `bodyPart` and
  `translations[{ languageCode, name }]`.
- API values used for labels: muscles `ABDOMINALS … TRICEPS` (17), body parts `ARMS, LEGS, CHEST, BACK, SHOULDERS, CORE,
NECK`, efforts `LOW, MIDLOW, MID, MIDHIGH, HIGH`, set target types `Reps`, `Duration` (seconds), `MaxReps` (no value),
  `RepsRange` (`value`…`maxValue`). The API's workout limits are `WorkoutName.MaxLength = 256` (trimmed, not blank) and
  `WorkoutDescription.MaxLength = 1024`.
- The backoffice is a static export (`next build` → `out/`), so routes cannot depend on runtime path segments.
- Existing patterns: `profile-name-rules.ts` (form rules in `core`), `profile-form.tsx` (validate on submit, field
  messages, `Alert` for the API error), `use-update-profile.ts` (action hook with `runWithSession`), `use-workouts.ts`
  (read hook with generation guard), `assigned-plans-view.tsx` (view states). `onboarding-dialog.tsx` imports MUI dialog
  widgets directly; this change wraps them as atoms instead.
- `IconButton` atom is typed `ComponentProps<typeof MuiIconButton>`, which does not accept `component`/`href`.

## Goals / Non-Goals

**Goals:**

- Wire the single create mutation end to end and land on the new screen.
- Lay out the editor with real data, so later changes only add mutations behind the inert controls.
- Keep catalog lookup, filtering and labels in one place, reusable by the future add/edit flows.

**Non-Goals:**

- No caching of the catalog or the workout across screens, and no shared provider.
- No dirty-state tracking or "unsaved changes" prompt for the card or the configuration fields.
- No change to the navigation drawer, the session handling or `core` use cases.

## Decisions

### Route: `/workout?id=`

`backoffice/src/app/workout/page.tsx` default-exports `WorkoutView`. The view renders `AppShell` and, inside it, a
`Suspense` (fallback `HomeSkeleton`) around `WorkoutScreen`, the component that calls `useSearchParams().get("id")`, as
the Next 16 docs require for a prerendered route. Rejected: `/workouts/[id]` with `generateStaticParams`, impossible for
runtime ids; `/assigned-plans/workout?id=`, longer with no benefit. Navigation uses
`` `/workout?id=${encodeURIComponent(id)}` `` everywhere.

### Core: `workout-rules.ts`

In `core/src/application/validation/`, re-exported from its barrel, shaped like `profile-name-rules.ts`:

```ts
export const WORKOUT_NAME_MAX_LENGTH = 256;
export const WORKOUT_DESCRIPTION_MAX_LENGTH = 1024;
export type WorkoutFieldViolation = "required" | "maxLength" | "date" | "uuid";

export interface CreateWorkoutFormValues {
  name: string;
  description: string;
  workoutDate: string;
  userId: string;
  trainerId: string;
}

/** `withTrainer` mirrors who the API requires a trainer from; it is passed in, never derived here. */
export function validateCreateWorkoutForm(
  values: CreateWorkoutFormValues,
  options: { withTrainer: boolean },
): Partial<Record<keyof CreateWorkoutFormValues, WorkoutFieldViolation>>;
```

- `name`: trimmed empty → `required`; trimmed length > 256 → `maxLength`.
- `description`: length > 1024 → `maxLength`; empty is fine.
- `workoutDate`: empty → `required`; not `YYYY-MM-DD` or not a real calendar day → `date`.
- `userId`, and `trainerId` when `withTrainer`: trimmed empty → `required`; not a UUID
  (`/^[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}$/i`) → `uuid`. `trainerId` is ignored otherwise.

The values are form strings, not `CreateWorkoutRequest`, because the request type allows `null` everywhere; the dialog
builds the request after validating. Alternative rejected: validating inside the component — the invariants keep form
rules once in `core`.

### State hooks

| Hook                 | Holds                                                                                                | Changed by                   | Resets                       |
| -------------------- | ---------------------------------------------------------------------------------------------------- | ---------------------------- | ---------------------------- |
| `useWorkout(id)`     | `status: "loading" \| "loaded" \| "failed"`, `workout: WorkoutDetail \| null`, `errorCode`, `reload` | mount, `id` change, `reload` | screen exit                  |
| `useExercises()`     | same shape with `exercises: Exercise[]`                                                              | mount, `reload`              | screen exit                  |
| `useCreateWorkout()` | `creating`, `errorCode`, `create(request) → Promise<Workout \| null>`, `reset()`                     | the dialog                   | `reset` on dialog close/open |

`useWorkout` and `useExercises` copy `use-workouts.ts` (request-only effect, generation guard, `runWithSession`). With
a `null` id, `useWorkout` sends nothing and reports `failed` with `NOT_FOUND`, so the view has a single not-found path.
`useCreateWorkout` copies `use-update-profile.ts` and returns the created workout so the caller navigates.

`useExerciseLabels()` is a small presentation hook in `workouts/hooks/` that reads `i18n.language` and returns
`nameOf(exercise)` (translation whose `languageCode` matches the active language, else the first translation, else
the id), `muscleLabel(code)`, `bodyPartLabel(code)`, `effortLabel(code)` and `targetTypeLabel(type)`, each falling back
to the raw code via i18next `defaultValue`. Catalog, filter and configuration all use it, so the lookup lives once.

View-local state in `WorkoutScreen`: `filterText`, `selectedBodyPart: string | null`, `selectedWorkoutExerciseId: string
| null`. The catalog is indexed once with `useMemo` into `Map<exerciseId, Exercise>`.

### Layout

`WorkoutScreen` uses a `Box` grid: `gridTemplateColumns: { xs: "1fr", md: "1fr 2fr 1fr" }`, `gap: 3`, `minWidth: 0`
on each column so long names wrap instead of widening the page. Loading: `HomeSkeleton` until both reads finish.
Failure of either read: `LoadError` for that read with its own `reload` (workout failure takes precedence). `NOT_FOUND`
on the workout: a not-found block (`Typography` + `Link component={NextLink} href="/assigned-plans"`).

### Visual pass (after `crear_entrenamiento.png`)

The layout follows the mockup `front/crear_entrenamiento.png` with the data the API has. Not reproduced, because the API
has no data for them: thumbnails, exercise descriptions, rest time, supersets, drag handles; nor the "Todos" pill.

- **Page header** (only once the workout has loaded): a "Volver" `Link` (`ArrowBackOutlined`, `NextLink` to
  `/assigned-plans`), an `h1` ("Editar entrenamiento" since the title change below) and, originally, the "Guardar" `Button` on the right. Guardar
  leaves the workout card. Loading, not-found and error states keep their look.
- **Cards**: each column is a `Card` (`variant="outlined"`) whose `CardContent` starts with an `h2` title; the column
  `section` is labelled by that heading (`aria-labelledby`). Titles: "Catálogo de ejercicios", "Estructura del
  entrenamiento", "Detalles del ejercicio".
- **Middle card**: `WorkoutDetailsCard` renders the card, its fields, and its `children`; the view passes
  `WorkoutExercisesList` as children, so the exercises sit inside the same card. The list starts with an "Ejercicios"
  header row and an inert `Button` "Añadir ejercicio" (`AddOutlined`).
- **Scroll**: the catalog `List` and the workout exercises `List` use `maxHeight: "min(60vh, 640px)"` and
  `overflowY: "auto"`, so long lists scroll inside their cards.
- **Numbering**: `ExerciseListItem` takes an optional `position`, drawn as a 24 px `primary.light` circle before the
  name. Only the workout list passes it.
- **Chip colours**: `useExerciseLabels` gains `bodyPartColor(code)`, the single map from body part to MUI chip colour
  (`CHEST` error, `BACK` info, `LEGS` success, `ARMS` warning, `SHOULDERS` secondary, `CORE` primary, `NECK` and
  unknown `default`). `ExerciseListItem` takes `labels: { text, color? }[]`; the body-part chip uses the colour and the
  muscle chip stays `default`. Chips stay `variant="outlined"`.
- **Configuration card**: the video comes first, then the name and chips, then effort and sets.
- **Copy**: `workout.structure.title` (Estructura del entrenamiento / Workout structure), `workout.exercises.title`
  becomes "Ejercicios" / "Exercises", `workout.exercises.add` (Añadir ejercicio / Add exercise),
  `workout.configuration.title` becomes "Detalles del ejercicio" / "Exercise details", `workout.back` (Volver / Back).

### Saving the workout (Guardar)

- **Core**: `workout-rules.ts` gains `WorkoutDetailsFormValues` (`name`, `description`, `workoutDate`) and
  `validateWorkoutDetailsForm(values)`, with the same name, description and date rules as the create form;
  `validateCreateWorkoutForm` reuses it for those three fields. No new use case: `updateWorkoutUseCase`
  (`UpdateWorkoutInput` → `Workout`) is already registered in both containers.
- **State hook** `useUpdateWorkout()` in `workouts/hooks/`, modelled on `use-create-workout.ts`: `saving`, `errorCode`,
  `saved`, `save(input) → Promise<Workout | null>`, `clearSaved()`; it calls `updateWorkoutUseCase` through
  `runWithSession`. Held by the screen; resets on exit.
- **Form state is lifted**: the card becomes controlled (`values`, `errors`, `disabled`, `onChange`), and a
  `LoadedWorkout` component inside the view, rendered once the workout is loaded, owns the values (seeded from the
  workout), the field errors, the heading name and the save. Guardar lives in the header, so the view is the common
  owner of the button and the fields.
- **Request**: `{ id, name: trimmed, workoutDate, description: trimmed or omitted when blank, userId: as read,
trainerId: as read }`. The API replaces every editable property, so athlete and trainer are sent back unchanged.
- **Outcome**: success shows an `Alert` "Entrenamiento guardado." (`workout.details.saved`) above the cards and sets
  the heading to the response's `name`; editing a field hides it. Failure shows `errors:<code>` in an `Alert` above
  the cards and keeps values and heading. Field messages reuse `createWorkout.validation.*`.

### Adding exercises from the catalog (local only)

- The middle card's "Añadir ejercicio" button and its `workout.exercises.add` key are removed; the catalog plus is the
  only way to add.
- `LoadedWorkout` holds `pending: WorkoutExerciseItem[]`. The plus appends
  `{ workoutExercise: { id: "pending-<exerciseId>", exerciseId, effort: "", sets: [{ count: "", target: { type: "Reps",
value: null, maxValue: null } }], superSeriesId: null }, exercise, pending: true }` and selects it, so the
  configuration card opens with one empty set row to define. `WorkoutExerciseItem` gains `pending?: boolean`.
- The list shows saved exercises then pending ones, numbered together; a pending one shows a "Pendiente" caption
  (`workout.exercises.pending`). `ExerciseListItem` gains an optional `note` for it.
- The catalog receives the ids already in the workout (saved and pending) and leaves them out before filtering, so an
  exercise cannot be added twice (the API would answer 409).
- Nothing is sent: pending items live in screen state and are lost on exit or reload. Saving a pending exercise (at least
  one valid set) comes with the change that wires `addExerciseToWorkoutUseCase`.

### Sets and saving an exercise (configuration card)

- **Core**: `WorkoutRepositoryInterface.updateExercise(workoutId, exerciseId, request)` → `PUT
/workouts/{workoutId}/exercises/{exerciseId}` (`exerciseId` is the catalog id; the body repeats it), returning
  `WorkoutExerciseDetail`. `UpdateWorkoutExerciseUseCase` takes `UpdateWorkoutExerciseInput = AddExerciseRequest &
{ workoutId: string; exerciseId: string }` and forwards it, like `AddExerciseToWorkoutUseCase`. New `Dependencies` key
  `updateWorkoutExerciseUseCase`, registered in both containers and given a placeholder in both test helpers.
- **Core rules** in `workout-rules.ts`: `SetFormValues` (`count`, `type`, `value`, `maxValue` as strings),
  `validateSetForm` (`count` an integer ≥ 1; `value` an integer ≥ 1 unless the type is `MaxReps`; `maxValue` an integer ≥
  1 for `RepsRange`; violation `"positiveInteger"`), and `validateExerciseConfiguration({ effort, setCount })` (`effort`
  `"required"`, sets `"minItems"`). Ordering inside a range is left to the API.
- **Panel** (`ExerciseConfigurationPanel`): local `effort`, `sets: SetFormValues[]` (seeded from the stored sets),
  `draft: SetFormValues` (empty, type `Reps`), field errors. Order: video, name and chips, effort select, sets list
  (`List` of summaries with a `DeleteOutlined` "Quitar serie" button each), set form, "Añadir serie" (`AddOutlined`,
  outlined), then a full-width contained "Guardar". "Añadir serie" validates the draft, appends it and resets it.
- **Summaries**: `workout.configuration.summary.<type>` with `{{series}}`, `{{value}}`, `{{max}}` ("{{series}} ×
  {{value}} repeticiones", "{{series}} × {{value}}–{{max}} repeticiones", "{{series}} × {{value}} s", "{{series}} ×
  máximo"); `series` rather than `count`, which i18next reserves for plurals. Unknown types fall back to
  "{{series}} × {{value}}".
- **Save**: `useSaveWorkoutExercise()` (`saving`, `errorCode`, `save(workoutId, item, config) → Promise<detail | null>`)
  calls `addExerciseToWorkoutUseCase` for a pending item and `updateWorkoutExerciseUseCase` otherwise, through
  `runWithSession`. Request sets map to `SetPrescriptionRequest`: `{ count, targetType: type, target?: value,
maxTarget?: maxValue }`, numbers parsed, absent keys omitted.
- **After a save**: `LoadedWorkout` keeps `saved: WorkoutExerciseDetail[]` (seeded from the workout) and `pending`. A
  saved pending item leaves `pending` and is appended to `saved`; an updated one replaces its entry by `exerciseId`.
  Selection moves to the entry's API id, and `lastSavedId` makes the remounted panel show "Ejercicio guardado." until
  the next edit. The save result's sets are what the panel shows next.
- **Copy**: `workout.configuration.removeSet` (Quitar serie), `.addSet` (Añadir serie), `.save` (Guardar), `.saved`
  (Ejercicio guardado.), `.validation.positiveInteger` (Introduce un número entero mayor que 0.), `.validation.effort`
  (Elige un esfuerzo.), `.validation.sets` (Añade al menos una serie.), `.noSets` (Todavía no hay series.), and the
  summaries; English in the copy table's style.

### Title, Guardar placement, column widths and removing exercises

- **Header**: the `h1` is the fixed "Editar entrenamiento" (`workout.editTitle`) with the "Volver" link under it; it no
  longer shows or updates with the workout's name. "Guardar" moves into `WorkoutDetailsCard`, right-aligned between
  the fields and the exercises (`onSave`, `saving` props); the save confirmation and error `Alert`s stay above the cards.
- **Columns**: `gridTemplateColumns` becomes `minmax(0, 25fr) minmax(0, 40fr) minmax(0, 35fr)` on `md` and up.
- **Core**: `WorkoutRepositoryInterface.removeExercise(workoutId, exerciseId)` → `DELETE
/workouts/{workoutId}/exercises/{exerciseId}` (catalog id, no body, `204` read as an empty result) returning
  `Result<void, DomainError>`. `RemoveExerciseFromWorkoutUseCase` takes `RemoveExerciseFromWorkoutInput = { workoutId,
exerciseId }`. New key `removeExerciseFromWorkoutUseCase`, registered in both containers, with placeholders and
  container-test entries in both fronts.
- **Presentation**: `useRemoveWorkoutExercise()` (`removingId`, `errorCode`, `remove(workoutId, exerciseId) →
Promise<boolean>`). `WorkoutExercisesList` gets `onRemove(item)` and `removingId`. `LoadedWorkout.handleRemove`: a
  pending item is dropped locally; a saved one is removed through the hook and, on success, dropped from
  `savedExercises`. Either way the exercise reappears in the catalog, because `available` is derived from the items, and
  a removed selection clears `selectedId`. A failed removal shows `errors:<code>` in an `Alert` inside the workout card,
  above the list.

### Sets and effort in the workout's list

- `useExerciseLabels` gains `setSummary({ count, type, value, maxValue })`, the single formatter of set summaries
  (`workout.configuration.summary.*`), now used by both the configuration card and the list; the panel's own
  `summaryOf` goes away.
- `ExerciseListItem` gains optional `lines: string[]`, each rendered as its own `body2` secondary line under the labels.
- `WorkoutExercisesList` passes `Series: <summaries joined by ", ">` (`workout.exercises.setsLine`) and `Esfuerzo:
<label>` (`workout.exercises.effortLine`), each only when not empty. The saved list is refreshed by an exercise
  save, so the lines follow what the API answered.

### Components

Atoms (new, `common/components/atoms/`, pass-through like `button.tsx`): `dialog`, `dialog-title`, `dialog-content`,
`dialog-actions`, `card`, `card-content`, `chip`, `list`, `list-item`. The effort and set-type selects use the existing
`text-field` atom with `select`, so no `select` atom is added. `IconButton` becomes generic over its `component`
(`IconButtonProps<C, { component?: C }>`, defaulting to `button`) so it can render `component={NextLink}` while
existing callers keep their typed `onClick` events; a plain `IconButtonProps<ElementType>` would have untyped them.

Molecules (new, `workouts/components/molecules/`, no use cases, no container):

- `ExerciseListItem` — props `name`, `labels: string[]`, `selected?`, `actions: ReactNode`. `ListItem` with the name in
  `Typography`, a row of small outlined `Chip`s for the labels, and the actions on the right.
- `BodyPartFilter` — props `bodyParts: { code, label }[]`, `selected`, `onToggle(code)`. A `Box` with
  `display: flex; flexWrap: wrap; gap: 1` of `Chip`s (`color="primary"` + `variant="filled"` when selected,
  `aria-pressed`), so pills wrap and never scroll.
- `ExerciseVideo` — props `url: string`, `title: string`. Parses `youtube.com/watch?v=<id>` (and `youtu.be/<id>`) with
  `URL`; embeds `<Box component="iframe" src="https://www.youtube-nocookie.com/embed/<id>" title={title}
allow="encrypted-media; picture-in-picture" allowFullScreen sx={{ aspectRatio: "16 / 9", width: "100%", border: 0 }} />`;
  any other URL renders `Link` "Ver vídeo" with `target="_blank" rel="noopener noreferrer"`. The caller does not render
  it when `videoUrl` is `null`.

Organisms (new, `workouts/components/organisms/`):

- `CreateWorkoutDialog` — props `open`, `onClose`. Reads the role from `useCurrentUser`, `withTrainer = role ===
"ADMIN"`. Holds the form values and field errors; on submit validates with `validateCreateWorkoutForm`, then calls
  `create` with `{ name: name.trim(), workoutDate, userId: userId.trim(), description?: trimmed if not blank,
trainerId?: trimmed if withTrainer }` (omitted keys, not `null`). On a workout, closes and `router.push`es. Values,
  errors and `errorCode` reset each time it opens. Fields: `TextField` atoms, Fecha `type="date"` with
  `slotProps.inputLabel.shrink`. "Crear" is `type="submit"`, disabled while `creating`.
- `ExerciseCatalogPanel` — props `exercises`, `filterText`, `onFilterTextChange`, `selectedBodyPart`, `onToggleBodyPart`.
  Builds distinct body parts sorted by label (seven at most, a shorter list than the 17 muscles), filters by `selectedBodyPart` and by `normalize(nameOf(e)).includes(
normalize(filterText))`, where `normalize` lowercases and strips diacritics (`NFD` + remove `\p{M}`). Each item has an
  `IconButton` "Añadir ejercicio" with `AddOutlined` and no `onClick`.
- `WorkoutDetailsCard` — props `workout`. `Card` with `TextField`s seeded from the workout (local state only) and a
  "Guardar" `Button` with no `onClick`.
- `WorkoutExercisesList` — props `items: { workoutExercise, exercise?: Exercise }[]`, `selectedId`, `onSelect`. Trash
  (`DeleteOutlined`, "Quitar ejercicio", no `onClick`) and pencil (`EditOutlined`, "Editar ejercicio", `onSelect(id)`).
- `ExerciseConfigurationPanel` — props `item: { workoutExercise, exercise? } | null`. Rendered by the view with
  `key={item?.workoutExercise.id}`, so its local copy of effort and sets is re-seeded on every selection without an
  effect. Shows name, labels, `ExerciseVideo` when `exercise?.videoUrl`, then an effort `Select` over the five efforts,
  then one row per set: count, target type `Select` (four types), value (hidden for `MaxReps`) and max value (only for
  `RepsRange`), as number `TextField`s. Nothing is submitted.

`WorkoutsTable` gains a last, unlabelled column with `IconButton component={NextLink} href={"/workout?id=…"}
aria-label={t("assignedPlans.edit", { name })}` and `EditOutlined`. `AssignedPlansView` holds `dialogOpen` and renders
`CreateWorkoutDialog`; the button's `onClick` opens it.

Imports flow view → organisms → molecules → atoms; only atoms import MUI widgets. Layout primitives (`Box`,
`Container`) and icons are imported where used, as existing views and organisms do.

### Errors and copy

| Failure                     | Code                     | Shown                                              |
| --------------------------- | ------------------------ | -------------------------------------------------- |
| Rejected data / role fields | `VALIDATION`             | dialog `Alert`, existing `errors` copy             |
| Not allowed                 | `FORBIDDEN`              | dialog `Alert` / `LoadError`, existing copy        |
| Unknown athlete or trainer  | `NOT_FOUND`              | dialog `Alert`, existing copy                      |
| Unknown or missing workout  | `NOT_FOUND`              | screen not-found block (`workout.notFound`)        |
| Session expired / missing   | `UNAUTHORIZED`           | `runWithSession` (refresh, or end session → login) |
| No response / other         | `NETWORK` / `UNEXPECTED` | `Alert` / `LoadError`, existing copy               |

No new error code. New keys in the `workouts` namespace (es / en), plus the section names `workout.catalog.title`,
`workout.exercises.title` and `workout.configuration.title` (Catálogo de ejercicios / Exercise catalog, Ejercicios del
entrenamiento / Workout exercises, Configuración del ejercicio / Exercise settings) that label the three columns:

| Key                                                                                                    | es                                                                                                                                                                                     | en                                                                                                                                                                |
| ------------------------------------------------------------------------------------------------------ | -------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- | ----------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| `assignedPlans.edit`                                                                                   | Editar {{name}}                                                                                                                                                                        | Edit {{name}}                                                                                                                                                     |
| `createWorkout.title`                                                                                  | Nuevo entrenamiento                                                                                                                                                                    | New workout                                                                                                                                                       |
| `createWorkout.fields.name` / `description` / `date` / `userId` / `trainerId`                          | Nombre / Descripción / Fecha / ID del atleta / ID del entrenador                                                                                                                       | Name / Description / Date / Athlete ID / Trainer ID                                                                                                               |
| `createWorkout.actions.cancel` / `create`                                                              | Cancelar / Crear                                                                                                                                                                       | Cancel / Create                                                                                                                                                   |
| `createWorkout.validation.required`                                                                    | Este campo es obligatorio.                                                                                                                                                             | This field is required.                                                                                                                                           |
| `createWorkout.validation.maxLength`                                                                   | No puede superar los {{max}} caracteres.                                                                                                                                               | It cannot be longer than {{max}} characters.                                                                                                                      |
| `createWorkout.validation.date`                                                                        | Introduce una fecha válida.                                                                                                                                                            | Enter a valid date.                                                                                                                                               |
| `createWorkout.validation.uuid`                                                                        | Introduce un identificador válido.                                                                                                                                                     | Enter a valid identifier.                                                                                                                                         |
| `workout.catalog.filter`                                                                               | Buscar ejercicio                                                                                                                                                                       | Search exercise                                                                                                                                                   |
| `workout.catalog.empty`                                                                                | Ningún ejercicio coincide con el filtro.                                                                                                                                               | No exercise matches the filter.                                                                                                                                   |
| `workout.catalog.add`                                                                                  | Añadir ejercicio                                                                                                                                                                       | Add exercise                                                                                                                                                      |
| `workout.details.save`                                                                                 | Guardar                                                                                                                                                                                | Save                                                                                                                                                              |
| `workout.exercises.empty`                                                                              | Este entrenamiento aún no tiene ejercicios.                                                                                                                                            | This workout has no exercises yet.                                                                                                                                |
| `workout.exercises.remove` / `edit`                                                                    | Quitar ejercicio / Editar ejercicio                                                                                                                                                    | Remove exercise / Edit exercise                                                                                                                                   |
| `workout.configuration.empty`                                                                          | Selecciona un ejercicio para configurarlo.                                                                                                                                             | Select an exercise to configure it.                                                                                                                               |
| `workout.configuration.effort` / `sets` / `setCount` / `targetType` / `targetValue` / `targetMaxValue` | Esfuerzo / Series / Series / Tipo / Valor / Máximo                                                                                                                                     | Effort / Sets / Sets / Type / Value / Maximum                                                                                                                     |
| `workout.configuration.video` / `openVideo`                                                            | Vídeo / Ver vídeo                                                                                                                                                                      | Video / Watch video                                                                                                                                               |
| `workout.notFound` / `backToList`                                                                      | No hemos encontrado este entrenamiento. / Volver a planes asignados                                                                                                                    | We could not find this workout. / Back to assigned plans                                                                                                          |
| `muscles.*`                                                                                            | Abdominales, Abductores, Aductores, Bíceps, Gemelos, Pecho, Antebrazos, Glúteos, Isquiotibiales, Dorsales, Zona lumbar, Espalda media, Cuello, Cuádriceps, Hombros, Trapecios, Tríceps | Abdominals, Abductors, Adductors, Biceps, Calves, Chest, Forearms, Glutes, Hamstrings, Lats, Lower back, Middle back, Neck, Quadriceps, Shoulders, Traps, Triceps |
| `bodyParts.*`                                                                                          | Brazos, Piernas, Pecho, Espalda, Hombros, Core, Cuello                                                                                                                                 | Arms, Legs, Chest, Back, Shoulders, Core, Neck                                                                                                                    |
| `efforts.*`                                                                                            | Bajo, Medio-bajo, Medio, Medio-alto, Alto                                                                                                                                              | Low, Medium-low, Medium, Medium-high, High                                                                                                                        |
| `targetTypes.*` (`Reps`, `Duration`, `MaxReps`, `RepsRange`)                                           | Repeticiones, Segundos, Máximas repeticiones, Rango de repeticiones                                                                                                                    | Reps, Seconds, Max reps, Rep range                                                                                                                                |

`muscles.*` etc. follow the API codes in the order listed in Context. The app gets no copy.

### Tests

- core: `test/application/validation/workout-rules.test.ts` mirroring `profile-name-rules.test.ts`: every violation,
  the 256/257 and 1024/1025 boundaries, trimmed name, invalid calendar date (`2026-02-30`), UUID case-insensitivity,
  `trainerId` ignored without `withTrainer`.
- backoffice component tests (`renderWithProviders`, `next/navigation` mocked with `useRouter`, `usePathname`,
  `useSearchParams`):
  - `test/presentation/workouts/components/organisms/create-workout-dialog.test.tsx` per the creation spec, both roles.
  - `assigned-plans-view.test.tsx` updated: the button opens the dialog; the edit link `href`; English label.
  - `test/presentation/workouts/views/workout-view.test.tsx` per the editor spec, including the inert actions (use
    case fakes record no extra calls) and the video variants.
- backoffice E2E `e2e/workout.spec.ts`: (1) open the dialog, create, land on `/workout?id=…` with the name in the card;
  (2) open a workout from the table edit action. Intercepts `GET`/`POST /workouts`, `GET /workouts/{id}` and
  `GET /exercises` (with CORS preflight) like `assigned-plans.spec.ts`.
- Parity test covers the new keys. No change to test helpers: the three use cases already have placeholders.

## Risks / Trade-offs

- [Inert plus/trash may look broken] → same convention as the drawer placeholders; follow-up changes wire them.
- [Edits in the card and configuration panel are silently discarded] → accepted for this layout pass; no save exists to
  lose data against.
- [Typed UUIDs are error-prone] → structural UUID check and the API's `NOT_FOUND`; an athlete picker is a later change.
- [YouTube embed depends on a third party] → `youtube-nocookie.com`, no autoplay; non-YouTube URLs degrade to a link.
- [Large catalog filtered on every keystroke] → a few hundred items, filtered in memory with `useMemo`; acceptable.
- [Exercise without catalog match] → shown by id, so a catalog/workout mismatch is visible rather than hidden.

## Migration Plan

None. Additive change; rollback is reverting it.
