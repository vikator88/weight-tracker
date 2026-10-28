import { act, screen, waitFor, within } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { afterEach, beforeEach, describe, expect, it, vi } from "vitest";
import { DomainError, fail, ok } from "@weight-tracker/core/domain";
import type {
  AddExerciseToWorkoutInput,
  Exercise,
  RemoveExerciseFromWorkoutInput,
  ReorderWorkoutExercisesInput,
  Result,
  Session,
  UpdateWorkoutExerciseInput,
  UpdateWorkoutInput,
  Workout,
  WorkoutDetail,
  WorkoutExerciseDetail,
} from "@weight-tracker/core/domain";
import i18n from "@/infrastructure/i18n";
import WorkoutView from "@/presentation/workouts/views/workout-view";
import renderWithProviders from "../../../support/render-with-providers";

const replace = vi.fn();
const push = vi.fn();
const navigation = vi.hoisted(() => ({ search: "id=w-1" }));

vi.mock("next/navigation", () => ({
  useRouter: () => ({ replace, push }),
  usePathname: () => "/workout",
  useSearchParams: () => new URLSearchParams(navigation.search),
}));

function catalogExercise(
  id: string,
  muscle: string,
  bodyPart: string,
  [es, en]: [string, string],
  videoUrl: string | null,
): Exercise {
  return {
    id,
    freeExerciseDbId: null,
    videoUrl,
    muscle,
    bodyPart,
    translations: [
      { languageCode: "es", name: es },
      { languageCode: "en", name: en },
    ],
  };
}

const squat = catalogExercise(
  "ex-squat",
  "QUADRICEPS",
  "LEGS",
  ["Sentadilla", "Squat"],
  "https://www.youtube.com/watch?v=h_yZV27H684",
);
const legPress = catalogExercise(
  "ex-press",
  "QUADRICEPS",
  "LEGS",
  ["Prensa de piernas", "Leg press"],
  "https://example.com/leg-press.mp4",
);
const bench = catalogExercise(
  "ex-bench",
  "CHEST",
  "CHEST",
  ["Press de banca", "Bench press"],
  null,
);
const catalog = [squat, legPress, bench];

/** Two muscles for arms and a second one for legs, so the muscle pills have something to refine. */
const curl = catalogExercise("ex-curl", "BICEPS", "ARMS", ["Curl de bíceps", "Biceps curl"], null);
const pushdown = catalogExercise(
  "ex-pushdown",
  "TRICEPS",
  "ARMS",
  ["Extensión de tríceps", "Triceps pushdown"],
  null,
);
const hammerCurl = catalogExercise(
  "ex-hammer",
  "BICEPS",
  "ARMS",
  ["Curl martillo", "Hammer curl"],
  null,
);
const legCurl = catalogExercise(
  "ex-leg-curl",
  "HAMSTRINGS",
  "LEGS",
  ["Curl femoral", "Leg curl"],
  null,
);
const muscleCatalog = async () => ok([...catalog, curl, pushdown, hammerCurl, legCurl]);

function workoutExercise(
  id: string,
  exerciseId: string,
  effort: string,
  sets: WorkoutExerciseDetail["sets"],
): WorkoutExerciseDetail {
  return { id, exerciseId, effort, sets, superSeriesId: null };
}

const squatInWorkout = workoutExercise("we-1", squat.id, "HIGH", [
  { count: 3, target: { type: "Reps", value: 10, maxValue: null } },
]);
const benchInWorkout = workoutExercise("we-2", bench.id, "MID", [
  { count: 4, target: { type: "RepsRange", value: 8, maxValue: 12 } },
]);
const pressInWorkout = workoutExercise("we-3", legPress.id, "LOW", []);

const workout: WorkoutDetail = {
  id: "w-1",
  name: "Pierna",
  description: "Día 1",
  workoutDate: "2026-11-02",
  userId: "5f1c2f0e-9d5b-4a53-8c7e-0d6f7c1a2b3c",
  trainerId: null,
  status: "DRAFT",
  exercises: [squatInWorkout, benchInWorkout, pressInWorkout],
};

/** What the API answers to an update: the saved data, with the list response's shape. */
function savedWorkout({ id, ...request }: UpdateWorkoutInput): Workout {
  return {
    ...workout,
    id,
    name: request.name ?? workout.name,
    description: request.description ?? null,
    workoutDate: request.workoutDate ?? workout.workoutDate,
    exercises: [],
  };
}

/** What the API answers to an exercise save: the entry with the sets it received. */
function entryOf(
  { exerciseId, effort, sets }: AddExerciseToWorkoutInput,
  id: string,
): WorkoutExerciseDetail {
  return {
    id,
    exerciseId: exerciseId ?? "",
    effort: effort ?? "",
    superSeriesId: null,
    sets: (sets ?? []).map(({ count, targetType, target, maxTarget }) => ({
      count: count ?? 0,
      target: { type: targetType ?? "", value: target ?? null, maxValue: maxTarget ?? null },
    })),
  };
}

/** Fills the set form of the configuration dialog and adds the set to its list. */
async function addSet(
  user: ReturnType<typeof userEvent.setup>,
  { count, type, value, max }: { count: string; type?: string; value?: string; max?: string },
) {
  const form = within(configurationDialog()).getByRole("group", { name: "Añadir serie" });
  if (type) {
    await user.click(within(form).getByRole("combobox", { name: "Tipo" }));
    await user.click(screen.getByRole("option", { name: type }));
  }
  await user.type(within(form).getByRole("spinbutton", { name: "Series" }), count);
  if (value) {
    await user.type(within(form).getByRole("spinbutton", { name: "Valor" }), value);
  }
  if (max) {
    await user.type(within(form).getByRole("spinbutton", { name: "Máximo" }), max);
  }
  await user.click(within(configurationDialog()).getByRole("button", { name: "Añadir serie" }));
}

async function chooseEffort(user: ReturnType<typeof userEvent.setup>, effort: string) {
  await user.click(within(configurationDialog()).getByRole("combobox", { name: "Esfuerzo" }));
  await user.click(screen.getByRole("option", { name: effort }));
}

const setSummaries = () =>
  within(within(configurationDialog()).getByRole("list", { name: "Series" }))
    .getAllByRole("listitem")
    .map((item) => item.textContent);
/** A workout without exercises, so the catalog lists all of its exercises. */
const emptyWorkout: WorkoutDetail = { ...workout, exercises: [] };
const withoutExercises = async () => ok(emptyWorkout);

function renderWorkout({
  getWorkout = async () => ok(workout),
  getExercises = async () => ok(catalog),
  session = async () => ok({ accessToken: "a", refreshToken: "r" }),
  updateWorkout = async (input) => ok(savedWorkout(input)),
  addExercise = async (input) => ok(entryOf(input, "we-new")),
  updateExercise = async (input) => ok(entryOf(input, "we-1")),
  removeExercise = async () => ok(undefined),
  reorderExercises = async () => ok(workout),
}: {
  getWorkout?: () => Promise<Result<WorkoutDetail, DomainError>>;
  getExercises?: () => Promise<Result<Exercise[], DomainError>>;
  session?: () => Promise<Result<Session | null, DomainError>>;
  updateWorkout?: (input: UpdateWorkoutInput) => Promise<Result<Workout, DomainError>>;
  addExercise?: (
    input: AddExerciseToWorkoutInput,
  ) => Promise<Result<WorkoutExerciseDetail, DomainError>>;
  updateExercise?: (
    input: UpdateWorkoutExerciseInput,
  ) => Promise<Result<WorkoutExerciseDetail, DomainError>>;
  removeExercise?: (input: RemoveExerciseFromWorkoutInput) => Promise<Result<void, DomainError>>;
  reorderExercises?: (
    input: ReorderWorkoutExercisesInput,
  ) => Promise<Result<WorkoutDetail, DomainError>>;
} = {}) {
  const readWorkout = vi.fn(getWorkout);
  const readCatalog = vi.fn(getExercises);
  const mutations = {
    create: vi.fn(async () => fail(new DomainError("UNEXPECTED", ""))),
    update: vi.fn(updateWorkout),
    addExercise: vi.fn(addExercise),
    updateExercise: vi.fn(updateExercise),
    removeExercise: vi.fn(removeExercise),
    reorderExercises: vi.fn(reorderExercises),
  };

  return {
    readWorkout,
    readCatalog,
    mutations,
    rendered: renderWithProviders(<WorkoutView />, {
      getSessionUseCase: () => ({ execute: session }),
      getWorkoutByIdUseCase: () => ({ execute: readWorkout }),
      getExercisesUseCase: () => ({ execute: readCatalog }),
      createWorkoutUseCase: () => ({ execute: mutations.create }),
      updateWorkoutUseCase: () => ({ execute: mutations.update }),
      addExerciseToWorkoutUseCase: () => ({ execute: mutations.addExercise }),
      updateWorkoutExerciseUseCase: () => ({ execute: mutations.updateExercise }),
      removeExerciseFromWorkoutUseCase: () => ({ execute: mutations.removeExercise }),
      reorderWorkoutExercisesUseCase: () => ({ execute: mutations.reorderExercises }),
    }),
  };
}

const propertiesSection = () =>
  screen.getByRole("region", { name: "Propiedades del entrenamiento" });
/** While a dialog is open the page behind it is hidden from assistive technology; `hidden` still reaches it. */
const exercisesSection = ({ hidden = false } = {}) =>
  screen.getByRole("region", { name: "Ejercicios", hidden });
const catalogDialog = () => screen.getByRole("dialog", { name: "Catálogo de ejercicios" });
const configurationDialog = () =>
  screen.getByRole("dialog", { name: "Configuración del ejercicio" });

function catalogNames() {
  return within(catalogDialog())
    .queryAllByRole("listitem")
    .map((item) => within(item).getAllByText(/./)[0]?.textContent);
}

async function loaded() {
  await screen.findByRole("region", { name: "Ejercicios" });
}

async function openCatalog(user: ReturnType<typeof userEvent.setup>) {
  await user.click(within(exercisesSection()).getByRole("button", { name: "Añadir ejercicio" }));
  await screen.findByRole("dialog", { name: "Catálogo de ejercicios" });
}

/** Opens the catalog and chooses the exercise named `name`, which opens its configuration. */
async function chooseFromCatalog(user: ReturnType<typeof userEvent.setup>, name: string) {
  await openCatalog(user);
  const item = within(catalogDialog())
    .getAllByRole("listitem")
    .find((candidate) => within(candidate).queryByText(name) !== null);
  await user.click(within(item!).getByRole("button", { name: "Elegir ejercicio" }));
  await screen.findByRole("dialog", { name: "Configuración del ejercicio" });
}

/** Opens the configuration of the listed exercise at `index` with its pencil. */
async function editExercise(user: ReturnType<typeof userEvent.setup>, index: number) {
  await user.click(workoutButtons("Editar ejercicio")[index]!);
  await screen.findByRole("dialog", { name: "Configuración del ejercicio" });
}

async function dialogsClosed() {
  await waitFor(() => expect(screen.queryByRole("dialog")).not.toBeInTheDocument());
}

function workoutItems({ hidden = false } = {}) {
  return within(exercisesSection({ hidden })).queryAllByRole("listitem", { hidden });
}

/** Each workout exercise as its position and name, in the order listed. */
function workoutRows({ hidden = false } = {}) {
  return workoutItems({ hidden }).map((item) =>
    within(item)
      .getAllByText(/./)
      .slice(0, 2)
      .map((text) => text.textContent),
  );
}

function workoutButtons(name: string) {
  return within(exercisesSection()).getAllByRole("button", { name });
}

/**
 * jsdom has no layout, so every element would sit at the same place and dnd-kit's keyboard sensor could not move
 * between cards. Each list item gets its own cell instead, in rows of three as on a wide screen, by its place among its
 * siblings.
 */
function stubListItemRects() {
  return vi.spyOn(HTMLElement.prototype, "getBoundingClientRect").mockImplementation(function (
    this: HTMLElement,
  ) {
    const index =
      this.tagName === "LI" && this.parentElement
        ? Array.from(this.parentElement.children).indexOf(this)
        : 0;
    const left = (index % 3) * 310;
    const top = Math.floor(index / 3) * 160;
    return {
      x: left,
      y: top,
      top,
      left,
      right: left + 300,
      bottom: top + 150,
      width: 300,
      height: 150,
      toJSON: () => ({}),
    };
  });
}

/** Picks up the workout exercise at `from` by its handle, moves it `steps` cards to the right and drops it. */
async function moveRight(user: ReturnType<typeof userEvent.setup>, from: number, steps: number) {
  workoutButtons("Reordenar ejercicio")[from]!.focus();
  await user.keyboard("[Space]");
  for (let step = 0; step < steps; step += 1) {
    await user.keyboard("[ArrowRight]");
  }
  await user.keyboard("[Space]");
}

function mutationCalls(mutations: ReturnType<typeof renderWorkout>["mutations"]) {
  return Object.values(mutations).reduce((total, mock) => total + mock.mock.calls.length, 0);
}

describe("WorkoutView", () => {
  beforeEach(() => {
    replace.mockClear();
    push.mockClear();
    navigation.search = "id=w-1";
  });

  it("reads the workout and the catalog once", async () => {
    // Arrange & Act
    const { readWorkout, readCatalog, rendered } = renderWorkout();
    await rendered;

    // Assert
    await loaded();
    expect(readWorkout).toHaveBeenCalledTimes(1);
    expect(readWorkout).toHaveBeenCalledWith({ id: "w-1" });
    expect(readCatalog).toHaveBeenCalledTimes(1);
    expect(screen.getByRole("navigation", { name: "Menú principal" })).toBeInTheDocument();
  });

  it("sends a visitor without a session to login and reads no workout", async () => {
    // Arrange & Act
    const { readWorkout, rendered } = renderWorkout({ session: async () => ok(null) });
    await rendered;

    // Assert
    await waitFor(() => expect(replace).toHaveBeenCalledWith("/login"));
    expect(readWorkout).not.toHaveBeenCalled();
  });

  it("shows the properties and the exercises cards in that order, and no dialog", async () => {
    // Arrange & Act
    await renderWorkout().rendered;

    // Assert
    await loaded();
    const regions = screen
      .getAllByRole("region")
      .map((region) => within(region).queryByRole("heading", { level: 2 })?.textContent)
      .filter(Boolean);
    expect(regions).toEqual(["Propiedades del entrenamiento", "Ejercicios"]);
    expect(screen.queryByRole("dialog")).not.toBeInTheDocument();
  });

  it("opens the catalog from Añadir ejercicio without a request", async () => {
    // Arrange
    const { mutations, rendered } = renderWorkout();
    await rendered;
    await loaded();
    const user = userEvent.setup();

    // Act
    await user.click(within(exercisesSection()).getByRole("button", { name: "Añadir ejercicio" }));

    // Assert
    expect(
      await screen.findByRole("dialog", { name: "Catálogo de ejercicios" }),
    ).toBeInTheDocument();
    expect(mutationCalls(mutations)).toBe(0);
  });

  it("closes the catalog with Escape without changing the workout or sending a request", async () => {
    // Arrange
    const { mutations, rendered } = renderWorkout();
    await rendered;
    await loaded();
    const user = userEvent.setup();
    await openCatalog(user);

    // Act
    await user.keyboard("{Escape}");

    // Assert
    await dialogsClosed();
    expect(workoutRows().map(([, name]) => name)).toEqual([
      "Sentadilla",
      "Press de banca",
      "Prensa de piernas",
    ]);
    expect(mutationCalls(mutations)).toBe(0);
  });

  it("closes the catalog with Cerrar without a request", async () => {
    // Arrange
    const { mutations, rendered } = renderWorkout();
    await rendered;
    await loaded();
    const user = userEvent.setup();
    await openCatalog(user);

    // Act
    await user.click(within(catalogDialog()).getByRole("button", { name: "Cerrar" }));

    // Assert
    await dialogsClosed();
    expect(mutationCalls(mutations)).toBe(0);
  });

  it("shows one pill per body part and every catalog exercise", async () => {
    // Arrange
    await renderWorkout({ getWorkout: withoutExercises }).rendered;
    await loaded();
    const user = userEvent.setup();

    // Act
    await openCatalog(user);

    // Assert
    const pills = within(catalogDialog()).getAllByRole("button", { pressed: false });
    expect(pills.map((pill) => pill.textContent)).toEqual(["Pecho", "Piernas"]);
    expect(catalogNames()).toEqual(["Sentadilla", "Prensa de piernas", "Press de banca"]);
  });

  it("filters the catalog by name, ignoring case and accents", async () => {
    // Arrange
    await renderWorkout({ getWorkout: withoutExercises }).rendered;
    await loaded();
    const user = userEvent.setup();
    await openCatalog(user);

    // Act
    await user.type(
      within(catalogDialog()).getByRole("searchbox", { name: "Buscar ejercicio" }),
      "SENTADÍ",
    );

    // Assert
    expect(catalogNames()).toEqual(["Sentadilla"]);
  });

  it("combines the body-part pill and the text", async () => {
    // Arrange
    await renderWorkout({ getWorkout: withoutExercises }).rendered;
    await loaded();
    const user = userEvent.setup();
    await openCatalog(user);
    await user.click(within(catalogDialog()).getByRole("button", { name: "Piernas" }));

    // Act
    await user.type(
      within(catalogDialog()).getByRole("searchbox", { name: "Buscar ejercicio" }),
      "prensa",
    );

    // Assert
    expect(catalogNames()).toEqual(["Prensa de piernas"]);
  });

  it("clears the body part when its pill is selected again", async () => {
    // Arrange
    await renderWorkout({ getWorkout: withoutExercises }).rendered;
    await loaded();
    const user = userEvent.setup();
    await openCatalog(user);
    const pill = within(catalogDialog()).getByRole("button", { name: "Pecho" });
    await user.click(pill);
    expect(catalogNames()).toEqual(["Press de banca"]);

    // Act
    await user.click(pill);

    // Assert
    expect(pill).toHaveAttribute("aria-pressed", "false");
    expect(catalogNames()).toHaveLength(3);
  });

  it("says so when no exercise matches the filters", async () => {
    // Arrange
    await renderWorkout().rendered;
    await loaded();
    const user = userEvent.setup();
    await openCatalog(user);

    // Act
    await user.type(
      within(catalogDialog()).getByRole("searchbox", { name: "Buscar ejercicio" }),
      "remo",
    );

    // Assert
    expect(
      within(catalogDialog()).getByText("Ningún ejercicio coincide con el filtro."),
    ).toBeInTheDocument();
    expect(catalogNames()).toEqual([]);
  });

  it("keeps the filters when the catalog is opened again", async () => {
    // Arrange
    await renderWorkout({ getWorkout: withoutExercises }).rendered;
    await loaded();
    const user = userEvent.setup();
    await openCatalog(user);
    await user.type(
      within(catalogDialog()).getByRole("searchbox", { name: "Buscar ejercicio" }),
      "prensa",
    );
    await user.keyboard("{Escape}");
    await dialogsClosed();

    // Act
    await openCatalog(user);

    // Assert
    expect(
      within(catalogDialog()).getByRole("searchbox", { name: "Buscar ejercicio" }),
    ).toHaveValue("prensa");
    expect(catalogNames()).toEqual(["Prensa de piernas"]);
  });

  it("shows each catalog exercise in its own outlined card with Elegir ejercicio", async () => {
    // Arrange
    await renderWorkout({ getWorkout: withoutExercises }).rendered;
    await loaded();
    const user = userEvent.setup();

    // Act
    await openCatalog(user);

    // Assert
    const items = within(catalogDialog()).getAllByRole("listitem");
    expect(items).toHaveLength(3);
    for (const item of items) {
      expect(item.querySelectorAll(".MuiCard-root.MuiPaper-outlined")).toHaveLength(1);
      expect(within(item).getByRole("button", { name: "Elegir ejercicio" })).toBeInTheDocument();
    }
  });

  it("labels each exercise with its body part and muscle", async () => {
    // Arrange
    await renderWorkout({ getWorkout: withoutExercises, getExercises: muscleCatalog }).rendered;
    await loaded();
    const user = userEvent.setup();

    // Act
    await openCatalog(user);

    // Assert
    const [squatItem] = within(catalogDialog()).getAllByRole("listitem");
    expect(within(squatItem!).getByText("Piernas")).toBeInTheDocument();
    expect(within(squatItem!).getByText("Cuádriceps")).toBeInTheDocument();
  });

  it("shows a workout exercise missing from the catalog by its id, without labels", async () => {
    // Arrange
    const orphan = workoutExercise("we-9", "ex-unknown", "MID", []);
    const getWorkout = async () => ok({ ...workout, exercises: [orphan] });

    // Act
    await renderWorkout({ getWorkout }).rendered;

    // Assert
    await loaded();
    const [item] = workoutItems();
    expect(item).toHaveTextContent("ex-unknown");
    expect(within(item!).queryByText("Cuádriceps")).not.toBeInTheDocument();
  });

  it("opens a chosen exercise's configuration empty, without listing it or sending a request", async () => {
    // Arrange
    const withTwo = { ...workout, exercises: [squatInWorkout, benchInWorkout] };
    const { mutations, readWorkout, rendered } = renderWorkout({
      getWorkout: async () => ok(withTwo),
    });
    await rendered;
    await loaded();
    const user = userEvent.setup();
    await openCatalog(user);

    // Act
    await user.click(within(catalogDialog()).getByRole("button", { name: "Elegir ejercicio" }));

    // Assert
    const dialog = await screen.findByRole("dialog", { name: "Configuración del ejercicio" });
    await waitFor(() =>
      expect(
        screen.queryByRole("dialog", { name: "Catálogo de ejercicios" }),
      ).not.toBeInTheDocument(),
    );
    expect(within(dialog).getByRole("heading", { name: "Prensa de piernas" })).toBeInTheDocument();
    expect(within(dialog).getByRole("combobox", { name: "Esfuerzo" })).not.toHaveTextContent(/\w/);
    expect(within(dialog).getByText("Todavía no hay series.")).toBeInTheDocument();
    const form = within(dialog).getByRole("group", { name: "Añadir serie" });
    expect(within(form).getByRole("spinbutton", { name: "Series" })).toHaveValue(null);
    expect(within(form).getByRole("spinbutton", { name: "Valor" })).toHaveValue(null);
    expect(workoutItems({ hidden: true })).toHaveLength(2);
    expect(mutationCalls(mutations)).toBe(0);
    expect(readWorkout).toHaveBeenCalledTimes(1);
  });

  it("hides from the catalog the exercises already in the workout", async () => {
    // Arrange
    await renderWorkout({ getWorkout: async () => ok({ ...workout, exercises: [squatInWorkout] }) })
      .rendered;
    await loaded();
    const user = userEvent.setup();

    // Act
    await openCatalog(user);

    // Assert
    expect(catalogNames()).toEqual(["Prensa de piernas", "Press de banca"]);
  });

  it("leaves an added exercise out of the catalog", async () => {
    // Arrange
    await renderWorkout({ getWorkout: withoutExercises }).rendered;
    await loaded();
    const user = userEvent.setup();
    await chooseFromCatalog(user, "Prensa de piernas");
    await chooseEffort(user, "Alto");
    await addSet(user, { count: "2", value: "15" });
    await user.click(within(configurationDialog()).getByRole("button", { name: "Guardar" }));
    await dialogsClosed();

    // Act
    await openCatalog(user);

    // Assert
    expect(catalogNames()).toEqual(["Sentadilla", "Press de banca"]);
  });

  it("closes a chosen exercise with Cancelar without adding it or sending a request", async () => {
    // Arrange
    const withTwo = { ...workout, exercises: [squatInWorkout, benchInWorkout] };
    const { mutations, rendered } = renderWorkout({ getWorkout: async () => ok(withTwo) });
    await rendered;
    await loaded();
    const user = userEvent.setup();
    await chooseFromCatalog(user, "Prensa de piernas");
    await addSet(user, { count: "2", value: "15" });

    // Act
    await user.click(within(configurationDialog()).getByRole("button", { name: "Cancelar" }));

    // Assert
    await dialogsClosed();
    expect(workoutRows().map(([, name]) => name)).toEqual(["Sentadilla", "Press de banca"]);
    expect(mutationCalls(mutations)).toBe(0);
  });

  it("lists a dismissed exercise in the catalog again", async () => {
    // Arrange
    await renderWorkout({ getWorkout: withoutExercises }).rendered;
    await loaded();
    const user = userEvent.setup();
    await chooseFromCatalog(user, "Prensa de piernas");
    await user.click(within(configurationDialog()).getByRole("button", { name: "Cancelar" }));
    await dialogsClosed();

    // Act
    await openCatalog(user);

    // Assert
    expect(catalogNames()).toEqual(["Sentadilla", "Prensa de piernas", "Press de banca"]);
  });

  it("returns the focus to Añadir ejercicio when an add flow ends", async () => {
    // Arrange
    await renderWorkout({ getWorkout: withoutExercises }).rendered;
    await loaded();
    const user = userEvent.setup();
    await chooseFromCatalog(user, "Sentadilla");

    // Act
    await user.click(within(configurationDialog()).getByRole("button", { name: "Cancelar" }));

    // Assert
    await dialogsClosed();
    await waitFor(() =>
      expect(
        within(exercisesSection()).getByRole("button", { name: "Añadir ejercicio" }),
      ).toHaveFocus(),
    );
  });

  it("closes an edited exercise with Escape without a request", async () => {
    // Arrange
    const { mutations, rendered } = renderWorkout();
    await rendered;
    await loaded();
    const user = userEvent.setup();
    await editExercise(user, 0);
    await chooseEffort(user, "Bajo");

    // Act
    await user.keyboard("{Escape}");

    // Assert
    await dialogsClosed();
    expect(mutationCalls(mutations)).toBe(0);
  });

  it("shows the stored values again when a dismissed exercise is opened again", async () => {
    // Arrange
    await renderWorkout().rendered;
    await loaded();
    const user = userEvent.setup();
    await editExercise(user, 0);
    await chooseEffort(user, "Bajo");
    await user.click(within(configurationDialog()).getByRole("button", { name: "Quitar serie" }));
    await user.click(within(configurationDialog()).getByRole("button", { name: "Cancelar" }));
    await dialogsClosed();

    // Act
    await editExercise(user, 0);

    // Assert
    expect(
      within(configurationDialog()).getByRole("combobox", { name: "Esfuerzo" }),
    ).toHaveTextContent("Alto");
    expect(setSummaries()).toEqual(["3 × 10 repeticiones"]);
  });

  it("shows another exercise's stored values after one is cancelled", async () => {
    // Arrange
    await renderWorkout().rendered;
    await loaded();
    const user = userEvent.setup();
    await editExercise(user, 0);
    await chooseEffort(user, "Bajo");
    await user.click(within(configurationDialog()).getByRole("button", { name: "Cancelar" }));
    await dialogsClosed();

    // Act
    await editExercise(user, 1);

    // Assert
    expect(
      within(configurationDialog()).getByRole("combobox", { name: "Esfuerzo" }),
    ).toHaveTextContent("Medio");
  });

  it("disables Cancelar while the exercise is being saved", async () => {
    // Arrange
    await renderWorkout({ getWorkout: withoutExercises, addExercise: () => new Promise(() => {}) })
      .rendered;
    await loaded();
    const user = userEvent.setup();
    await chooseFromCatalog(user, "Sentadilla");
    await chooseEffort(user, "Alto");
    await addSet(user, { count: "3", value: "10" });

    // Act
    await user.click(within(configurationDialog()).getByRole("button", { name: "Guardar" }));

    // Assert
    expect(within(configurationDialog()).getByRole("button", { name: "Cancelar" })).toBeDisabled();
    expect(within(configurationDialog()).getByRole("button", { name: "Guardar" })).toBeDisabled();
  });

  it("keeps the dialog open on Escape while the exercise is being saved", async () => {
    // Arrange
    await renderWorkout({ getWorkout: withoutExercises, addExercise: () => new Promise(() => {}) })
      .rendered;
    await loaded();
    const user = userEvent.setup();
    await chooseFromCatalog(user, "Sentadilla");
    await chooseEffort(user, "Alto");
    await addSet(user, { count: "3", value: "10" });
    await user.click(within(configurationDialog()).getByRole("button", { name: "Guardar" }));

    // Act
    await user.keyboard("{Escape}");

    // Assert
    expect(configurationDialog()).toBeInTheDocument();
  });

  it("fills the properties card with the workout's data and no exercises", async () => {
    // Arrange & Act
    await renderWorkout().rendered;

    // Assert
    await loaded();
    const card = propertiesSection();
    expect(within(card).getByRole("textbox", { name: "Nombre" })).toHaveValue("Pierna");
    expect(within(card).getByLabelText("Fecha")).toHaveValue("2026-11-02");
    expect(within(card).getByRole("textbox", { name: "Descripción" })).toHaveValue("Día 1");
    expect(within(card).queryByRole("list")).not.toBeInTheDocument();
  });

  it("shows the edit heading, a way back to the list and Guardar inside the properties card", async () => {
    // Arrange & Act
    await renderWorkout().rendered;

    // Assert
    await loaded();
    expect(
      screen.getByRole("heading", { level: 1, name: "Editar entrenamiento" }),
    ).toBeInTheDocument();
    expect(screen.queryByRole("heading", { level: 1, name: "Pierna" })).not.toBeInTheDocument();
    expect(screen.getByRole("link", { name: "Volver" })).toHaveAttribute("href", "/assigned-plans");
    expect(
      within(propertiesSection()).getByRole("button", { name: "Guardar" }),
    ).toBeInTheDocument();
  });

  it("places Guardar after the description", async () => {
    // Arrange & Act
    await renderWorkout().rendered;

    // Assert
    await loaded();
    const card = propertiesSection();
    const description = within(card).getByRole("textbox", { name: "Descripción" });
    const save = within(card).getByRole("button", { name: "Guardar" });
    expect(
      description.compareDocumentPosition(save) & Node.DOCUMENT_POSITION_FOLLOWING,
    ).toBeTruthy();
  });

  it("shows the Ejercicios title and Añadir ejercicio in the exercises card", async () => {
    // Arrange & Act
    await renderWorkout().rendered;

    // Assert
    await loaded();
    const card = exercisesSection();
    expect(within(card).getByRole("heading", { level: 2, name: "Ejercicios" })).toBeInTheDocument();
    expect(within(card).getByRole("button", { name: "Añadir ejercicio" })).toBeInTheDocument();
  });

  it("scrolls the catalog inside its dialog", async () => {
    // Arrange
    await renderWorkout({ getWorkout: withoutExercises }).rendered;
    await loaded();
    const user = userEvent.setup();

    // Act
    await openCatalog(user);

    // Assert
    expect(
      within(catalogDialog()).getByRole("list", { name: "Catálogo de ejercicios" }),
    ).toHaveStyle({ maxHeight: "min(60vh, 640px)", overflowY: "auto" });
  });

  it("lets the exercises card grow with its list instead of scrolling it", async () => {
    // Arrange & Act
    await renderWorkout().rendered;

    // Assert
    await loaded();
    const list = within(exercisesSection()).getByRole("list", { name: "Ejercicios" });
    expect(list).not.toHaveStyle({ maxHeight: "min(60vh, 640px)" });
    expect(list).not.toHaveStyle({ overflowY: "auto" });
  });

  it("fills body-part labels, outlines muscle labels in the same colour and leaves out a single muscle", async () => {
    // Arrange
    await renderWorkout({ getWorkout: withoutExercises, getExercises: muscleCatalog }).rendered;
    await loaded();
    const user = userEvent.setup();

    // Act
    await openCatalog(user);

    // Assert
    const [squatItem, , benchItem] = within(catalogDialog()).getAllByRole("listitem");
    const chip = (item: HTMLElement, text: string) =>
      within(item).getByText(text).closest(".MuiChip-root");
    const benchLabels = within(benchItem!).getAllByText("Pecho");
    expect(benchLabels).toHaveLength(1);
    expect(benchLabels[0]!.closest(".MuiChip-root")).toHaveClass(
      "MuiChip-colorError",
      "MuiChip-filled",
    );
    expect(chip(squatItem!, "Piernas")).toHaveClass("MuiChip-colorSuccess", "MuiChip-filled");
    expect(chip(squatItem!, "Cuádriceps")).toHaveClass("MuiChip-colorSuccess", "MuiChip-outlined");
  });

  it("colours the labels of the workout's exercises and of the configuration the same way", async () => {
    // Arrange
    await renderWorkout({ getExercises: muscleCatalog }).rendered;
    await loaded();
    const user = userEvent.setup();
    const [squatItem] = workoutItems();
    expect(within(squatItem!).getByText("Piernas").closest(".MuiChip-root")).toHaveClass(
      "MuiChip-colorSuccess",
      "MuiChip-filled",
    );
    expect(within(squatItem!).getByText("Cuádriceps").closest(".MuiChip-root")).toHaveClass(
      "MuiChip-colorSuccess",
      "MuiChip-outlined",
    );

    // Act
    await editExercise(user, 0);

    // Assert
    const dialog = configurationDialog();
    expect(within(dialog).getByText("Piernas").closest(".MuiChip-root")).toHaveClass(
      "MuiChip-colorSuccess",
      "MuiChip-filled",
    );
    expect(within(dialog).getByText("Cuádriceps").closest(".MuiChip-root")).toHaveClass(
      "MuiChip-colorSuccess",
      "MuiChip-outlined",
    );
  });

  it("shows the body-part pills under their label and no muscle row before a body part is chosen", async () => {
    // Arrange
    await renderWorkout({ getWorkout: withoutExercises, getExercises: muscleCatalog }).rendered;
    await loaded();
    const user = userEvent.setup();

    // Act
    await openCatalog(user);

    // Assert
    const bodyParts = within(catalogDialog()).getByRole("group", { name: "Partes del cuerpo" });
    expect(within(bodyParts).getByText("Partes del cuerpo")).toBeInTheDocument();
    expect(
      within(bodyParts)
        .getAllByRole("button")
        .map((pill) => pill.textContent),
    ).toEqual(["Brazos", "Pecho", "Piernas"]);
    expect(
      within(catalogDialog()).queryByRole("group", { name: "Músculos" }),
    ).not.toBeInTheDocument();
    expect(within(catalogDialog()).queryByText("Músculos")).not.toBeInTheDocument();
  });

  it("shows no muscle row for a body part whose listed exercises work one muscle", async () => {
    // Arrange
    await renderWorkout({ getWorkout: withoutExercises, getExercises: muscleCatalog }).rendered;
    await loaded();
    const user = userEvent.setup();
    await openCatalog(user);

    // Act
    await user.click(
      within(within(catalogDialog()).getByRole("group", { name: "Partes del cuerpo" })).getByRole(
        "button",
        { name: "Pecho" },
      ),
    );

    // Assert
    expect(catalogNames()).toEqual(["Press de banca"]);
    expect(
      within(catalogDialog()).queryByRole("group", { name: "Músculos" }),
    ).not.toBeInTheDocument();
    expect(within(catalogDialog()).queryByText("Músculos")).not.toBeInTheDocument();
  });

  it("leaves out a single muscle in the workout list but keeps it in the configuration", async () => {
    // Arrange
    await renderWorkout().rendered;
    await loaded();
    const user = userEvent.setup();
    const benchItem = workoutItems()[1]!;
    expect(within(benchItem).getAllByText("Pecho")).toHaveLength(1);

    // Act
    await editExercise(user, 1);

    // Assert
    expect(within(configurationDialog()).getAllByText("Pecho")).toHaveLength(2);
  });

  it("shows the muscles of the selected body part under their label", async () => {
    // Arrange
    await renderWorkout({ getWorkout: withoutExercises, getExercises: muscleCatalog }).rendered;
    await loaded();
    const user = userEvent.setup();
    await openCatalog(user);

    // Act
    await user.click(
      within(within(catalogDialog()).getByRole("group", { name: "Partes del cuerpo" })).getByRole(
        "button",
        { name: "Brazos" },
      ),
    );

    // Assert
    const muscles = within(catalogDialog()).getByRole("group", { name: "Músculos" });
    expect(within(muscles).getByText("Músculos")).toBeInTheDocument();
    expect(
      within(muscles)
        .getAllByRole("button")
        .map((pill) => pill.textContent),
    ).toEqual(["Bíceps", "Tríceps"]);
  });

  it("fills a selected pill with its colour and outlines the others in theirs", async () => {
    // Arrange
    await renderWorkout({ getWorkout: withoutExercises, getExercises: muscleCatalog }).rendered;
    await loaded();
    const user = userEvent.setup();
    await openCatalog(user);
    const bodyParts = within(catalogDialog()).getByRole("group", { name: "Partes del cuerpo" });

    // Act
    await user.click(within(bodyParts).getByRole("button", { name: "Piernas" }));

    // Assert
    expect(within(bodyParts).getByRole("button", { name: "Piernas" })).toHaveClass(
      "MuiChip-colorSuccess",
      "MuiChip-filled",
    );
    expect(within(bodyParts).getByRole("button", { name: "Brazos" })).toHaveClass(
      "MuiChip-colorWarning",
      "MuiChip-outlined",
    );
    const muscles = within(catalogDialog()).getByRole("group", { name: "Músculos" });
    for (const pill of within(muscles).getAllByRole("button")) {
      expect(pill).toHaveClass("MuiChip-colorSuccess", "MuiChip-outlined");
    }
  });

  it("filters the catalog by the selected body part and muscle", async () => {
    // Arrange
    await renderWorkout({ getWorkout: withoutExercises, getExercises: muscleCatalog }).rendered;
    await loaded();
    const user = userEvent.setup();
    await openCatalog(user);
    await user.click(
      within(within(catalogDialog()).getByRole("group", { name: "Partes del cuerpo" })).getByRole(
        "button",
        { name: "Brazos" },
      ),
    );

    // Act
    await user.click(
      within(within(catalogDialog()).getByRole("group", { name: "Músculos" })).getByRole("button", {
        name: "Bíceps",
      }),
    );

    // Assert
    expect(catalogNames()).toEqual(["Curl de bíceps", "Curl martillo"]);
  });

  it("clears the muscle when another body part is selected", async () => {
    // Arrange
    await renderWorkout({ getWorkout: withoutExercises, getExercises: muscleCatalog }).rendered;
    await loaded();
    const user = userEvent.setup();
    await openCatalog(user);
    const bodyParts = within(catalogDialog()).getByRole("group", { name: "Partes del cuerpo" });
    await user.click(within(bodyParts).getByRole("button", { name: "Brazos" }));
    await user.click(
      within(within(catalogDialog()).getByRole("group", { name: "Músculos" })).getByRole("button", {
        name: "Bíceps",
      }),
    );

    // Act
    await user.click(within(bodyParts).getByRole("button", { name: "Piernas" }));

    // Assert
    const muscles = within(catalogDialog()).getByRole("group", { name: "Músculos" });
    expect(
      within(muscles)
        .getAllByRole("button")
        .map((pill) => pill.textContent),
    ).toEqual(["Cuádriceps", "Isquiotibiales"]);
    expect(within(muscles).queryByRole("button", { pressed: true })).not.toBeInTheDocument();
    expect(catalogNames()).toEqual(["Sentadilla", "Prensa de piernas", "Curl femoral"]);
  });

  it("clears the muscle and hides its row when the body part is cleared", async () => {
    // Arrange
    await renderWorkout({ getWorkout: withoutExercises, getExercises: muscleCatalog }).rendered;
    await loaded();
    const user = userEvent.setup();
    await openCatalog(user);
    const bodyParts = within(catalogDialog()).getByRole("group", { name: "Partes del cuerpo" });
    await user.click(within(bodyParts).getByRole("button", { name: "Brazos" }));
    await user.click(
      within(within(catalogDialog()).getByRole("group", { name: "Músculos" })).getByRole("button", {
        name: "Bíceps",
      }),
    );

    // Act
    await user.click(within(bodyParts).getByRole("button", { name: "Brazos" }));

    // Assert
    expect(
      within(catalogDialog()).queryByRole("group", { name: "Músculos" }),
    ).not.toBeInTheDocument();
    expect(catalogNames()).toHaveLength(7);
  });

  it("shows the message under a blank name and sends nothing", async () => {
    // Arrange
    const { mutations, rendered } = renderWorkout();
    await rendered;
    await loaded();
    const user = userEvent.setup();
    await user.clear(within(propertiesSection()).getByRole("textbox", { name: "Nombre" }));

    // Act
    await user.click(screen.getByRole("button", { name: "Guardar" }));

    // Assert
    expect(
      within(propertiesSection()).getByRole("textbox", { name: "Nombre" }),
    ).toHaveAccessibleDescription("Este campo es obligatorio.");
    expect(mutationCalls(mutations)).toBe(0);
  });

  it("shows the message under a too long description and sends nothing", async () => {
    // Arrange
    const { mutations, rendered } = renderWorkout();
    await rendered;
    await loaded();
    const user = userEvent.setup();
    const description = within(propertiesSection()).getByRole("textbox", { name: "Descripción" });
    await user.clear(description);
    await user.click(description);
    await user.paste("a".repeat(1025));

    // Act
    await user.click(screen.getByRole("button", { name: "Guardar" }));

    // Assert
    expect(description).toHaveAccessibleDescription("No puede superar los 1024 caracteres.");
    expect(mutationCalls(mutations)).toBe(0);
  });

  it("saves the trimmed data with the athlete and trainer as read, once", async () => {
    // Arrange
    const withTrainer = { ...workout, trainerId: "6a2d3f1e-0e6c-4b64-9d8f-1e7a8d2b3c4d" };
    const { mutations, rendered } = renderWorkout({ getWorkout: async () => ok(withTrainer) });
    await rendered;
    await loaded();
    const user = userEvent.setup();
    const name = within(propertiesSection()).getByRole("textbox", { name: "Nombre" });
    await user.clear(name);
    await user.type(name, "  Pierna B  ");

    // Act
    await user.click(screen.getByRole("button", { name: "Guardar" }));

    // Assert
    await waitFor(() => expect(mutations.update).toHaveBeenCalledTimes(1));
    expect(mutations.update).toHaveBeenCalledWith({
      id: "w-1",
      name: "Pierna B",
      workoutDate: "2026-11-02",
      description: "Día 1",
      userId: withTrainer.userId,
      trainerId: withTrainer.trainerId,
    });
    expect(mutations.create).not.toHaveBeenCalled();
    expect(mutations.addExercise).not.toHaveBeenCalled();
  });

  it("sends no description when it is left blank", async () => {
    // Arrange
    const { mutations, rendered } = renderWorkout();
    await rendered;
    await loaded();
    const user = userEvent.setup();
    await user.clear(within(propertiesSection()).getByRole("textbox", { name: "Descripción" }));

    // Act
    await user.click(screen.getByRole("button", { name: "Guardar" }));

    // Assert
    await waitFor(() => expect(mutations.update).toHaveBeenCalledTimes(1));
    expect(mutations.update.mock.calls[0]?.[0]).not.toHaveProperty("description");
  });

  it("disables Guardar while the workout is being saved", async () => {
    // Arrange
    const { rendered } = renderWorkout({ updateWorkout: () => new Promise(() => {}) });
    await rendered;
    await loaded();
    const user = userEvent.setup();

    // Act
    await user.click(screen.getByRole("button", { name: "Guardar" }));

    // Assert
    expect(await screen.findByRole("button", { name: "Guardar" })).toBeDisabled();
  });

  it("confirms the save and keeps the saved name in its field", async () => {
    // Arrange
    await renderWorkout().rendered;
    await loaded();
    const user = userEvent.setup();
    const name = within(propertiesSection()).getByRole("textbox", { name: "Nombre" });
    await user.clear(name);
    await user.type(name, "Pierna B");

    // Act
    await user.click(screen.getByRole("button", { name: "Guardar" }));

    // Assert
    expect(await screen.findByText("Entrenamiento guardado.")).toBeInTheDocument();
    expect(within(propertiesSection()).getByRole("textbox", { name: "Nombre" })).toHaveValue(
      "Pierna B",
    );
  });

  it("hides the confirmation when a field changes again", async () => {
    // Arrange
    await renderWorkout().rendered;
    await loaded();
    const user = userEvent.setup();
    await user.click(screen.getByRole("button", { name: "Guardar" }));
    await screen.findByText("Entrenamiento guardado.");

    // Act
    await user.type(within(propertiesSection()).getByRole("textbox", { name: "Nombre" }), "!");

    // Assert
    expect(screen.queryByText("Entrenamiento guardado.")).not.toBeInTheDocument();
  });

  it("shows the error and keeps the values when the API refuses", async () => {
    // Arrange
    const { rendered } = renderWorkout({
      updateWorkout: async () => fail(new DomainError("FORBIDDEN", "")),
    });
    await rendered;
    await loaded();
    const user = userEvent.setup();
    const name = within(propertiesSection()).getByRole("textbox", { name: "Nombre" });
    await user.clear(name);
    await user.type(name, "Pierna B");

    // Act
    await user.click(screen.getByRole("button", { name: "Guardar" }));

    // Assert
    expect(
      await screen.findByText("No tienes permiso para realizar esta acción."),
    ).toBeInTheDocument();
    expect(name).toHaveValue("Pierna B");
    expect(screen.queryByText("Entrenamiento guardado.")).not.toBeInTheDocument();
  });

  it("shows each workout exercise's sets comma separated and its effort, on their own lines", async () => {
    // Arrange
    const sets: WorkoutExerciseDetail["sets"] = [
      { count: 2, target: { type: "Reps", value: 15, maxValue: null } },
      { count: 1, target: { type: "RepsRange", value: 10, maxValue: 12 } },
      { count: 1, target: { type: "MaxReps", value: null, maxValue: null } },
    ];
    const getWorkout = async () =>
      ok({ ...workout, exercises: [workoutExercise("we-1", squat.id, "HIGH", sets)] });

    // Act
    await renderWorkout({ getWorkout }).rendered;

    // Assert
    await loaded();
    const [item] = workoutItems();
    const setsLine = within(item!).getByText("2 × 15, 1 × 10–12, 1 × máx");
    const effortLine = within(item!).getByText("Esfuerzo: Alto");
    expect(setsLine).not.toBe(effortLine);
    expect(
      setsLine.compareDocumentPosition(effortLine) & Node.DOCUMENT_POSITION_FOLLOWING,
    ).toBeTruthy();
  });

  it("closes the dialog and lists an added exercise last, as the API answered it", async () => {
    // Arrange
    const withTwo = { ...workout, exercises: [squatInWorkout, benchInWorkout] };
    await renderWorkout({ getWorkout: async () => ok(withTwo) }).rendered;
    await loaded();
    const user = userEvent.setup();
    await chooseFromCatalog(user, "Prensa de piernas");
    await chooseEffort(user, "Medio");
    await addSet(user, { count: "3", value: "10" });

    // Act
    await user.click(within(configurationDialog()).getByRole("button", { name: "Guardar" }));

    // Assert
    await dialogsClosed();
    expect(workoutRows()).toEqual([
      ["1", "Sentadilla"],
      ["2", "Press de banca"],
      ["3", "Prensa de piernas"],
    ]);
    const item = workoutItems()[2]!;
    expect(within(item).getByText("3 × 10", { exact: true })).toBeInTheDocument();
    expect(within(item).getByText("Esfuerzo: Medio")).toBeInTheDocument();
  });

  it("closes the dialog and updates an edited exercise as the API answered it", async () => {
    // Arrange
    await renderWorkout().rendered;
    await loaded();
    const user = userEvent.setup();
    await editExercise(user, 0);
    await addSet(user, { count: "2", value: "15" });

    // Act
    await user.click(within(configurationDialog()).getByRole("button", { name: "Guardar" }));

    // Assert
    await dialogsClosed();
    const [item] = workoutItems();
    expect(within(item!).getByText("3 × 10, 2 × 15")).toBeInTheDocument();
  });

  it("shows each listed exercise in its own outlined card", async () => {
    // Arrange & Act
    await renderWorkout().rendered;

    // Assert
    await loaded();
    const items = workoutItems();
    expect(items).toHaveLength(3);
    for (const item of items) {
      expect(item.querySelectorAll(".MuiCard-root.MuiPaper-outlined")).toHaveLength(1);
    }
  });

  it("puts the handle, position, name and actions first, then the labels, then the sets", async () => {
    // Arrange & Act
    await renderWorkout({ getExercises: muscleCatalog }).rendered;

    // Assert
    await loaded();
    const [item] = workoutItems();
    const inOrder = [
      within(item!).getByRole("button", { name: "Reordenar ejercicio" }),
      within(item!).getByText("1", { exact: true }),
      within(item!).getByText("Sentadilla"),
      within(item!).getByRole("button", { name: "Quitar ejercicio" }),
      within(item!).getByRole("button", { name: "Editar ejercicio" }),
      within(item!).getByText("Piernas"),
      within(item!).getByText("Cuádriceps"),
      within(item!).getByText("3 × 10", { exact: true }),
    ];
    for (const [index, node] of inOrder.slice(1).entries()) {
      expect(
        inOrder[index]!.compareDocumentPosition(node) & Node.DOCUMENT_POSITION_FOLLOWING,
      ).toBeTruthy();
    }
    const firstRow = within(item!).getByText("Sentadilla").closest("p")!.parentElement!
      .parentElement!;
    expect(firstRow).toContainElement(
      within(item!).getByRole("button", { name: "Editar ejercicio" }),
    );
    expect(firstRow).not.toContainElement(within(item!).getByText("Piernas"));
  });

  it("lists the workout's exercises in order with their actions", async () => {
    // Arrange & Act
    await renderWorkout().rendered;

    // Assert
    await loaded();
    expect(workoutRows()).toEqual([
      ["1", "Sentadilla"],
      ["2", "Press de banca"],
      ["3", "Prensa de piernas"],
    ]);
    for (const item of workoutItems()) {
      expect(within(item).getByRole("button", { name: "Reordenar ejercicio" })).toBeEnabled();
      expect(within(item).getByRole("button", { name: "Quitar ejercicio" })).toBeInTheDocument();
      expect(within(item).getByRole("button", { name: "Editar ejercicio" })).toBeInTheDocument();
    }
  });

  describe("reordering", () => {
    let rects: ReturnType<typeof stubListItemRects>;

    beforeEach(() => {
      rects = stubListItemRects();
    });

    afterEach(() => {
      rects.mockRestore();
    });

    it("moves a dropped exercise at once and sends the full saved order, once", async () => {
      // Arrange
      const { mutations, rendered } = renderWorkout({
        reorderExercises: () => new Promise(() => {}),
      });
      await rendered;
      await loaded();
      const user = userEvent.setup();

      // Act
      await moveRight(user, 0, 1);

      // Assert
      expect(workoutRows()).toEqual([
        ["1", "Press de banca"],
        ["2", "Sentadilla"],
        ["3", "Prensa de piernas"],
      ]);
      expect(mutations.reorderExercises).toHaveBeenCalledTimes(1);
      expect(mutations.reorderExercises).toHaveBeenCalledWith({
        workoutId: "w-1",
        exerciseIds: ["ex-bench", "ex-squat", "ex-press"],
      });
    });

    it("keeps the new order and shows no error when the API accepts it", async () => {
      // Arrange
      await renderWorkout().rendered;
      await loaded();
      const user = userEvent.setup();

      // Act
      await moveRight(user, 0, 2);

      // Assert
      await waitFor(() =>
        expect(
          workoutButtons("Reordenar ejercicio").every((handle) => !handle.hasAttribute("disabled")),
        ).toBe(true),
      );
      expect(workoutRows()).toEqual([
        ["1", "Press de banca"],
        ["2", "Prensa de piernas"],
        ["3", "Sentadilla"],
      ]);
      expect(within(exercisesSection()).queryByRole("alert")).not.toBeInTheDocument();
    });

    it("puts the previous order back and shows the error when the API refuses it", async () => {
      // Arrange
      await renderWorkout({ reorderExercises: async () => fail(new DomainError("CONFLICT", "")) })
        .rendered;
      await loaded();
      const user = userEvent.setup();

      // Act
      await moveRight(user, 0, 1);

      // Assert
      expect(
        await within(exercisesSection()).findByText(
          "La operación entra en conflicto con el estado actual.",
        ),
      ).toBeInTheDocument();
      expect(workoutRows()).toEqual([
        ["1", "Sentadilla"],
        ["2", "Press de banca"],
        ["3", "Prensa de piernas"],
      ]);
    });

    it("sends nothing when the exercise is dropped where it was", async () => {
      // Arrange
      const { mutations, rendered } = renderWorkout();
      await rendered;
      await loaded();
      const user = userEvent.setup();

      // Act
      await moveRight(user, 0, 0);

      // Assert
      expect(workoutRows().map(([, name]) => name)).toEqual([
        "Sentadilla",
        "Press de banca",
        "Prensa de piernas",
      ]);
      expect(mutationCalls(mutations)).toBe(0);
    });

    it("sends nothing when the drag is cancelled with Escape", async () => {
      // Arrange
      const { mutations, rendered } = renderWorkout();
      await rendered;
      await loaded();
      const user = userEvent.setup();
      workoutButtons("Reordenar ejercicio")[0]!.focus();
      await user.keyboard("[Space]");
      await user.keyboard("[ArrowRight]");

      // Act
      await user.keyboard("[Escape]");

      // Assert
      expect(workoutRows().map(([, name]) => name)).toEqual([
        "Sentadilla",
        "Press de banca",
        "Prensa de piernas",
      ]);
      expect(mutationCalls(mutations)).toBe(0);
    });

    it("disables every handle while the reorder is in progress", async () => {
      // Arrange
      await renderWorkout({ reorderExercises: () => new Promise(() => {}) }).rendered;
      await loaded();
      const user = userEvent.setup();

      // Act
      await moveRight(user, 0, 1);

      // Assert
      for (const handle of workoutButtons("Reordenar ejercicio")) {
        expect(handle).toBeDisabled();
      }
    });
  });

  it("removes an exercise through the API", async () => {
    // Arrange
    const { mutations, rendered } = renderWorkout({
      getWorkout: async () => ok({ ...workout, exercises: [squatInWorkout] }),
    });
    await rendered;
    await loaded();
    const user = userEvent.setup();

    // Act
    await user.click(workoutButtons("Quitar ejercicio")[0]!);

    // Assert
    await waitFor(() => expect(workoutItems()).toHaveLength(0));
    expect(mutations.removeExercise).toHaveBeenCalledTimes(1);
    expect(mutations.removeExercise).toHaveBeenCalledWith({
      workoutId: "w-1",
      exerciseId: "ex-squat",
    });
  });

  it("lists a removed exercise in the catalog again", async () => {
    // Arrange
    await renderWorkout({ getWorkout: async () => ok({ ...workout, exercises: [squatInWorkout] }) })
      .rendered;
    await loaded();
    const user = userEvent.setup();
    await user.click(workoutButtons("Quitar ejercicio")[0]!);
    await waitFor(() => expect(workoutItems()).toHaveLength(0));

    // Act
    await openCatalog(user);

    // Assert
    expect(catalogNames()).toEqual(["Sentadilla", "Prensa de piernas", "Press de banca"]);
  });

  it("disables Quitar ejercicio while the removal is in progress", async () => {
    // Arrange
    await renderWorkout({ removeExercise: () => new Promise(() => {}) }).rendered;
    await loaded();
    const user = userEvent.setup();
    const [squatItem] = workoutItems();

    // Act
    await user.click(within(squatItem!).getByRole("button", { name: "Quitar ejercicio" }));

    // Assert
    expect(within(squatItem!).getByRole("button", { name: "Quitar ejercicio" })).toBeDisabled();
  });

  it("shows the error and keeps the exercise when the API refuses the removal", async () => {
    // Arrange
    await renderWorkout({ removeExercise: async () => fail(new DomainError("CONFLICT", "")) })
      .rendered;
    await loaded();
    const user = userEvent.setup();

    // Act
    await user.click(workoutButtons("Quitar ejercicio")[0]!);

    // Assert
    expect(
      await within(exercisesSection()).findByText(
        "La operación entra en conflicto con el estado actual.",
      ),
    ).toBeInTheDocument();
    expect(workoutItems()).toHaveLength(3);
  });

  it("says so when the workout has no exercises", async () => {
    // Arrange & Act
    await renderWorkout({ getWorkout: async () => ok({ ...workout, exercises: [] }) }).rendered;

    // Assert
    expect(
      await screen.findByText("Este entrenamiento aún no tiene ejercicios."),
    ).toBeInTheDocument();
  });

  it("opens a listed exercise's configuration with the pencil, without a request", async () => {
    // Arrange
    const { mutations, rendered } = renderWorkout();
    await rendered;
    await loaded();
    const user = userEvent.setup();

    // Act
    await user.click(workoutButtons("Editar ejercicio")[1]!);

    // Assert
    const dialog = await screen.findByRole("dialog", { name: "Configuración del ejercicio" });
    expect(within(dialog).getByRole("heading", { name: "Press de banca" })).toBeInTheDocument();
    expect(within(dialog).getAllByText("Pecho")).toHaveLength(2);
    expect(within(dialog).getByRole("combobox", { name: "Esfuerzo" })).toHaveTextContent("Medio");
    expect(setSummaries()).toEqual(["4 × 8–12 repeticiones"]);
    expect(within(dialog).getByRole("button", { name: "Cancelar" })).toBeEnabled();
    expect(within(dialog).getByRole("button", { name: "Guardar" })).toBeEnabled();
    expect(mutationCalls(mutations)).toBe(0);
  });

  it("shows a squat's name, labels, effort and sets in the dialog", async () => {
    // Arrange
    await renderWorkout().rendered;
    await loaded();
    const user = userEvent.setup();

    // Act
    await editExercise(user, 0);

    // Assert
    const dialog = configurationDialog();
    expect(within(dialog).getByRole("heading", { name: "Sentadilla" })).toBeInTheDocument();
    expect(within(dialog).getByText("Piernas")).toBeInTheDocument();
    expect(within(dialog).getByText("Cuádriceps")).toBeInTheDocument();
    expect(within(dialog).getByRole("combobox", { name: "Esfuerzo" })).toHaveTextContent("Alto");
    expect(setSummaries()).toEqual(["3 × 10 repeticiones"]);
  });

  it("lists several sets in order with a summary each", async () => {
    // Arrange
    const sets: WorkoutExerciseDetail["sets"] = [
      { count: 2, target: { type: "Reps", value: 15, maxValue: null } },
      { count: 1, target: { type: "RepsRange", value: 10, maxValue: 12 } },
      { count: 3, target: { type: "Duration", value: 40, maxValue: null } },
      { count: 1, target: { type: "MaxReps", value: null, maxValue: null } },
    ];
    const getWorkout = async () =>
      ok({ ...workout, exercises: [workoutExercise("we-1", squat.id, "HIGH", sets)] });
    await renderWorkout({ getWorkout }).rendered;
    await loaded();
    const user = userEvent.setup();

    // Act
    await editExercise(user, 0);

    // Assert
    expect(setSummaries()).toEqual([
      "2 × 15 repeticiones",
      "1 × 10–12 repeticiones",
      "3 × 40 s",
      "1 × máximo",
    ]);
  });

  it("removes a set from the list without a request", async () => {
    // Arrange
    const { mutations, rendered } = renderWorkout();
    await rendered;
    await loaded();
    const user = userEvent.setup();
    await editExercise(user, 0);
    await addSet(user, { count: "1", type: "Máximas repeticiones" });

    // Act
    await user.click(
      within(configurationDialog()).getAllByRole("button", { name: "Quitar serie" })[0]!,
    );

    // Assert
    expect(setSummaries()).toEqual(["1 × máximo"]);
    expect(mutationCalls(mutations)).toBe(0);
  });

  it("adds a set from the form to the end of the list and empties the form", async () => {
    // Arrange
    const { mutations, rendered } = renderWorkout();
    await rendered;
    await loaded();
    const user = userEvent.setup();
    await editExercise(user, 0);

    // Act
    await addSet(user, { count: "2", value: "15" });

    // Assert
    expect(setSummaries()).toEqual(["3 × 10 repeticiones", "2 × 15 repeticiones"]);
    const form = within(configurationDialog()).getByRole("group", { name: "Añadir serie" });
    expect(within(form).getByRole("spinbutton", { name: "Series" })).toHaveValue(null);
    expect(within(form).getByRole("spinbutton", { name: "Valor" })).toHaveValue(null);
    expect(mutationCalls(mutations)).toBe(0);
  });

  it("asks for no value when the set is max reps", async () => {
    // Arrange
    await renderWorkout().rendered;
    await loaded();
    const user = userEvent.setup();
    await editExercise(user, 0);
    const form = within(configurationDialog()).getByRole("group", { name: "Añadir serie" });

    // Act
    await user.click(within(form).getByRole("combobox", { name: "Tipo" }));
    await user.click(screen.getByRole("option", { name: "Máximas repeticiones" }));

    // Assert
    expect(within(form).queryByRole("spinbutton", { name: "Valor" })).not.toBeInTheDocument();
    expect(within(form).queryByRole("spinbutton", { name: "Máximo" })).not.toBeInTheDocument();
  });

  it("reports an invalid set and keeps the list unchanged", async () => {
    // Arrange
    await renderWorkout().rendered;
    await loaded();
    const user = userEvent.setup();
    await editExercise(user, 0);
    const form = within(configurationDialog()).getByRole("group", { name: "Añadir serie" });
    await user.type(within(form).getByRole("spinbutton", { name: "Series" }), "0");

    // Act
    await user.click(within(configurationDialog()).getByRole("button", { name: "Añadir serie" }));

    // Assert
    expect(within(form).getByRole("spinbutton", { name: "Series" })).toHaveAccessibleDescription(
      "Introduce un número entero mayor que 0.",
    );
    expect(within(form).getByRole("spinbutton", { name: "Valor" })).toHaveAccessibleDescription(
      "Introduce un número entero mayor que 0.",
    );
    expect(setSummaries()).toEqual(["3 × 10 repeticiones"]);
  });

  it("asks for an effort and a set before saving a chosen exercise", async () => {
    // Arrange
    const { mutations, rendered } = renderWorkout({ getWorkout: withoutExercises });
    await rendered;
    await loaded();
    const user = userEvent.setup();
    await chooseFromCatalog(user, "Sentadilla");

    // Act
    await user.click(within(configurationDialog()).getByRole("button", { name: "Guardar" }));

    // Assert
    expect(within(configurationDialog()).getByText("Elige un esfuerzo.")).toBeInTheDocument();
    expect(
      within(configurationDialog()).getByText("Añade al menos una serie."),
    ).toBeInTheDocument();
    expect(mutationCalls(mutations)).toBe(0);
  });

  it("adds a chosen exercise to the workout with its effort and sets, once", async () => {
    // Arrange
    const { mutations, rendered } = renderWorkout({ getWorkout: withoutExercises });
    await rendered;
    await loaded();
    const user = userEvent.setup();
    await chooseFromCatalog(user, "Prensa de piernas");
    await chooseEffort(user, "Alto");
    await addSet(user, { count: "2", value: "15" });
    await addSet(user, { count: "1", type: "Máximas repeticiones" });

    // Act
    await user.click(within(configurationDialog()).getByRole("button", { name: "Guardar" }));

    // Assert
    await waitFor(() => expect(mutations.addExercise).toHaveBeenCalledTimes(1));
    expect(mutations.addExercise).toHaveBeenCalledWith({
      workoutId: "w-1",
      exerciseId: "ex-press",
      effort: "HIGH",
      sets: [
        { count: 2, targetType: "Reps", target: 15 },
        { count: 1, targetType: "MaxReps" },
      ],
    });
    expect(mutations.updateExercise).not.toHaveBeenCalled();
  });

  it("replaces a listed exercise's effort and sets, once", async () => {
    // Arrange
    const { mutations, rendered } = renderWorkout();
    await rendered;
    await loaded();
    const user = userEvent.setup();
    await editExercise(user, 0);
    await addSet(user, { count: "1", type: "Rango de repeticiones", value: "8", max: "12" });
    await user.click(
      within(configurationDialog()).getAllByRole("button", { name: "Quitar serie" })[0]!,
    );

    // Act
    await user.click(within(configurationDialog()).getByRole("button", { name: "Guardar" }));

    // Assert
    await waitFor(() => expect(mutations.updateExercise).toHaveBeenCalledTimes(1));
    expect(mutations.updateExercise).toHaveBeenCalledWith({
      workoutId: "w-1",
      exerciseId: "ex-squat",
      effort: "HIGH",
      sets: [{ count: 1, targetType: "RepsRange", target: 8, maxTarget: 12 }],
    });
    expect(mutations.addExercise).not.toHaveBeenCalled();
  });

  it("keeps the dialog open with the error and the sets when the API refuses the exercise", async () => {
    // Arrange
    const { rendered } = renderWorkout({
      getWorkout: withoutExercises,
      addExercise: async () => fail(new DomainError("CONFLICT", "")),
    });
    await rendered;
    await loaded();
    const user = userEvent.setup();
    await chooseFromCatalog(user, "Sentadilla");
    await chooseEffort(user, "Medio");
    await addSet(user, { count: "3", value: "10" });

    // Act
    await user.click(within(configurationDialog()).getByRole("button", { name: "Guardar" }));

    // Assert
    expect(
      await within(configurationDialog()).findByText(
        "La operación entra en conflicto con el estado actual.",
      ),
    ).toBeInTheDocument();
    expect(setSummaries()).toEqual(["3 × 10 repeticiones"]);
    expect(workoutItems({ hidden: true })).toHaveLength(0);
  });

  it("embeds a YouTube video without tracking cookies", async () => {
    // Arrange
    await renderWorkout().rendered;
    await loaded();
    const user = userEvent.setup();

    // Act
    await editExercise(user, 0);

    // Assert
    expect(within(configurationDialog()).getByTitle("Sentadilla")).toHaveAttribute(
      "src",
      "https://www.youtube-nocookie.com/embed/h_yZV27H684",
    );
  });

  it("shows the video before the exercise's name", async () => {
    // Arrange
    await renderWorkout().rendered;
    await loaded();
    const user = userEvent.setup();

    // Act
    await editExercise(user, 0);

    // Assert
    const dialog = configurationDialog();
    const video = within(dialog).getByTitle("Sentadilla");
    const name = within(dialog).getByRole("heading", { level: 3, name: "Sentadilla" });
    expect(video.compareDocumentPosition(name) & Node.DOCUMENT_POSITION_FOLLOWING).toBeTruthy();
  });

  it("links any other video in a new tab", async () => {
    // Arrange
    await renderWorkout().rendered;
    await loaded();
    const user = userEvent.setup();

    // Act
    await editExercise(user, 2);

    // Assert
    const link = within(configurationDialog()).getByRole("link", { name: "Ver vídeo" });
    expect(link).toHaveAttribute("href", "https://example.com/leg-press.mp4");
    expect(link).toHaveAttribute("target", "_blank");
    expect(within(configurationDialog()).queryByTitle("Prensa de piernas")).not.toBeInTheDocument();
  });

  it("shows no video section for an exercise without video", async () => {
    // Arrange
    await renderWorkout().rendered;
    await loaded();
    const user = userEvent.setup();

    // Act
    await editExercise(user, 1);

    // Assert
    expect(
      within(configurationDialog()).queryByRole("region", { name: "Vídeo" }),
    ).not.toBeInTheDocument();
  });

  it("shows the skeleton while the workout loads", async () => {
    // Arrange & Act
    await renderWorkout({ getWorkout: () => new Promise(() => {}) }).rendered;

    // Assert
    expect(await screen.findByLabelText("Cargando")).toBeInTheDocument();
    expect(screen.queryByRole("region", { name: "Ejercicios" })).not.toBeInTheDocument();
  });

  it("shows not found without reading when there is no id", async () => {
    // Arrange
    navigation.search = "";

    // Act
    const { readWorkout, rendered } = renderWorkout();
    await rendered;

    // Assert
    expect(await screen.findByText("No hemos encontrado este entrenamiento.")).toBeInTheDocument();
    expect(screen.getByRole("link", { name: "Volver a planes asignados" })).toHaveAttribute(
      "href",
      "/assigned-plans",
    );
    expect(readWorkout).not.toHaveBeenCalled();
  });

  it("shows not found when the API does not find the workout", async () => {
    // Arrange & Act
    await renderWorkout({ getWorkout: async () => fail(new DomainError("NOT_FOUND", "")) })
      .rendered;

    // Assert
    expect(await screen.findByText("No hemos encontrado este entrenamiento.")).toBeInTheDocument();
    expect(screen.getByRole("link", { name: "Volver a planes asignados" })).toBeInTheDocument();
  });

  it("reads the catalog again when Reintentar is pressed after it failed", async () => {
    // Arrange
    const getExercises = vi
      .fn<() => Promise<Result<Exercise[], DomainError>>>()
      .mockResolvedValueOnce(fail(new DomainError("NETWORK", "")))
      .mockResolvedValueOnce(ok(catalog));
    const { readCatalog, rendered } = renderWorkout({ getExercises, getWorkout: withoutExercises });
    await rendered;
    const user = userEvent.setup();
    await screen.findByText("No hay conexión con el servidor.");

    // Act
    await user.click(screen.getByRole("button", { name: "Reintentar" }));

    // Assert
    await loaded();
    expect(propertiesSection()).toBeInTheDocument();
    expect(readCatalog).toHaveBeenCalledTimes(2);
  });

  it("follows the active language", async () => {
    // Arrange
    await renderWorkout({ getWorkout: withoutExercises, getExercises: muscleCatalog }).rendered;
    await loaded();
    const user = userEvent.setup();
    await openCatalog(user);

    // Act
    await act(() => i18n.changeLanguage("en"));

    // Assert
    const dialog = screen.getByRole("dialog", { name: "Exercise catalog" });
    expect(within(dialog).getByRole("searchbox", { name: "Search exercise" })).toBeInTheDocument();
    expect(within(dialog).getByRole("button", { name: "Legs" })).toBeInTheDocument();
    expect(within(dialog).getAllByText("Quadriceps").length).toBeGreaterThan(0);
    expect(within(dialog).getByText("Squat")).toBeInTheDocument();
    expect(
      screen.getByRole("heading", { name: "Workout properties", hidden: true }),
    ).toBeInTheDocument();
    expect(screen.getByRole("button", { name: "Add exercise", hidden: true })).toBeInTheDocument();
    expect(screen.getByRole("button", { name: "Save", hidden: true })).toBeInTheDocument();
  });
});
