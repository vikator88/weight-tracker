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

/** Fills the set form of the configuration card and adds the set to its list. */
async function addSet(
  user: ReturnType<typeof userEvent.setup>,
  { count, type, value, max }: { count: string; type?: string; value?: string; max?: string },
) {
  const form = within(configurationSection()).getByRole("group", { name: "Añadir serie" });
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
  await user.click(within(configurationSection()).getByRole("button", { name: "Añadir serie" }));
}

async function chooseEffort(user: ReturnType<typeof userEvent.setup>, effort: string) {
  await user.click(within(configurationSection()).getByRole("combobox", { name: "Esfuerzo" }));
  await user.click(screen.getByRole("option", { name: effort }));
}

const setSummaries = () =>
  within(within(configurationSection()).getByRole("list", { name: "Series" }))
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

const catalogSection = () => screen.getByRole("region", { name: "Catálogo de ejercicios" });
const workoutSection = () => screen.getByRole("region", { name: "Estructura del entrenamiento" });
const configurationSection = () => screen.getByRole("region", { name: "Detalles del ejercicio" });

function catalogNames() {
  return within(catalogSection())
    .queryAllByRole("listitem")
    .map((item) => within(item).getAllByText(/./)[0]?.textContent);
}

async function loaded() {
  await screen.findByRole("region", { name: "Catálogo de ejercicios" });
}

/** Each workout exercise as its position and name, in the order listed. */
function workoutRows() {
  return within(workoutSection())
    .getAllByRole("listitem")
    .map((item) =>
      within(item)
        .getAllByText(/./)
        .slice(0, 2)
        .map((text) => text.textContent),
    );
}

function workoutButtons(name: string) {
  return within(workoutSection()).getAllByRole("button", { name });
}

/**
 * jsdom has no layout, so every element would sit at the same place and dnd-kit's keyboard sensor could not move
 * between rows. Each list item gets its own row instead, by its place among its siblings.
 */
function stubListItemRects() {
  return vi.spyOn(HTMLElement.prototype, "getBoundingClientRect").mockImplementation(function (
    this: HTMLElement,
  ) {
    const index =
      this.tagName === "LI" && this.parentElement
        ? Array.from(this.parentElement.children).indexOf(this)
        : 0;
    const top = index * 60;
    return {
      x: 0,
      y: top,
      top,
      left: 0,
      right: 300,
      bottom: top + 50,
      width: 300,
      height: 50,
      toJSON: () => ({}),
    };
  });
}

/** Picks up the workout exercise at `from` by its handle, moves it down `steps` rows and drops it. */
async function moveDown(user: ReturnType<typeof userEvent.setup>, from: number, steps: number) {
  workoutButtons("Reordenar ejercicio")[from]!.focus();
  await user.keyboard("[Space]");
  for (let step = 0; step < steps; step += 1) {
    await user.keyboard("[ArrowDown]");
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

  it("shows the catalog, the workout and the configuration columns in that order", async () => {
    // Arrange & Act
    await renderWorkout().rendered;

    // Assert
    await loaded();
    const regions = screen
      .getAllByRole("region")
      .map((region) => within(region).queryByRole("heading", { level: 2 })?.textContent)
      .filter(Boolean);
    expect(regions).toEqual([
      "Catálogo de ejercicios",
      "Estructura del entrenamiento",
      "Detalles del ejercicio",
    ]);
  });

  it("shows one pill per body part and every catalog exercise", async () => {
    // Arrange & Act
    await renderWorkout({ getWorkout: withoutExercises }).rendered;

    // Assert
    await loaded();
    const pills = within(catalogSection()).getAllByRole("button", { pressed: false });
    expect(pills.map((pill) => pill.textContent)).toEqual(["Pecho", "Piernas"]);
    expect(catalogNames()).toEqual(["Sentadilla", "Prensa de piernas", "Press de banca"]);
  });

  it("filters the catalog by name, ignoring case and accents", async () => {
    // Arrange
    await renderWorkout({ getWorkout: withoutExercises }).rendered;
    await loaded();
    const user = userEvent.setup();

    // Act
    await user.type(
      within(catalogSection()).getByRole("searchbox", { name: "Buscar ejercicio" }),
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
    await user.click(within(catalogSection()).getByRole("button", { name: "Piernas" }));

    // Act
    await user.type(
      within(catalogSection()).getByRole("searchbox", { name: "Buscar ejercicio" }),
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
    const pill = within(catalogSection()).getByRole("button", { name: "Pecho" });
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

    // Act
    await user.type(
      within(catalogSection()).getByRole("searchbox", { name: "Buscar ejercicio" }),
      "remo",
    );

    // Assert
    expect(
      within(catalogSection()).getByText("Ningún ejercicio coincide con el filtro."),
    ).toBeInTheDocument();
    expect(catalogNames()).toEqual([]);
  });

  it("labels each exercise with its body part and muscle", async () => {
    // Arrange & Act
    await renderWorkout({ getWorkout: withoutExercises }).rendered;

    // Assert
    await loaded();
    const [squatItem] = within(catalogSection()).getAllByRole("listitem");
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
    const [item] = within(workoutSection()).getAllByRole("listitem");
    expect(item).toHaveTextContent("ex-unknown");
    expect(within(item!).queryByText("Cuádriceps")).not.toBeInTheDocument();
  });

  it("adds a catalog exercise as pending, selects it and opens it, without a request", async () => {
    // Arrange
    const withTwo = { ...workout, exercises: [squatInWorkout, benchInWorkout] };
    const { mutations, readWorkout, rendered } = renderWorkout({
      getWorkout: async () => ok(withTwo),
    });
    await rendered;
    await loaded();
    const user = userEvent.setup();

    // Act
    await user.click(within(catalogSection()).getByRole("button", { name: "Añadir ejercicio" }));

    // Assert
    const items = within(workoutSection()).getAllByRole("listitem");
    expect(items).toHaveLength(3);
    expect(items[2]).toHaveTextContent("3");
    expect(items[2]).toHaveTextContent("Prensa de piernas");
    expect(items[2]).toHaveTextContent("Pendiente");
    expect(items[2]).toHaveAttribute("aria-current", "true");
    expect(
      within(configurationSection()).getByRole("heading", { name: "Prensa de piernas" }),
    ).toBeInTheDocument();
    expect(mutationCalls(mutations)).toBe(0);
    expect(readWorkout).toHaveBeenCalledTimes(1);
  });

  it("opens a pending exercise with no effort, no sets and an empty set form", async () => {
    // Arrange
    await renderWorkout({ getWorkout: withoutExercises }).rendered;
    await loaded();
    const user = userEvent.setup();
    const [squatItem] = within(catalogSection()).getAllByRole("listitem");

    // Act
    await user.click(within(squatItem!).getByRole("button", { name: "Añadir ejercicio" }));

    // Assert
    const panel = configurationSection();
    expect(within(panel).getByRole("combobox", { name: "Esfuerzo" })).not.toHaveTextContent(/\w/);
    expect(within(panel).getByText("Todavía no hay series.")).toBeInTheDocument();
    const form = within(panel).getByRole("group", { name: "Añadir serie" });
    expect(within(form).getByRole("spinbutton", { name: "Series" })).toHaveValue(null);
    expect(within(form).getByRole("spinbutton", { name: "Valor" })).toHaveValue(null);
  });

  it("hides from the catalog the exercises already in the workout", async () => {
    // Arrange & Act
    await renderWorkout({ getWorkout: async () => ok({ ...workout, exercises: [squatInWorkout] }) })
      .rendered;

    // Assert
    await loaded();
    expect(catalogNames()).toEqual(["Prensa de piernas", "Press de banca"]);
  });

  it("removes an added exercise from the catalog", async () => {
    // Arrange
    await renderWorkout({ getWorkout: withoutExercises }).rendered;
    await loaded();
    const user = userEvent.setup();
    const [, pressItem] = within(catalogSection()).getAllByRole("listitem");

    // Act
    await user.click(within(pressItem!).getByRole("button", { name: "Añadir ejercicio" }));

    // Assert
    expect(catalogNames()).toEqual(["Sentadilla", "Press de banca"]);
  });

  it("fills the card with the workout's data", async () => {
    // Arrange & Act
    await renderWorkout().rendered;

    // Assert
    await loaded();
    expect(within(workoutSection()).getByRole("textbox", { name: "Nombre" })).toHaveValue("Pierna");
    expect(within(workoutSection()).getByLabelText("Fecha")).toHaveValue("2026-11-02");
    expect(within(workoutSection()).getByRole("textbox", { name: "Descripción" })).toHaveValue(
      "Día 1",
    );
  });

  it("shows the edit heading, a way back to the list and Guardar inside the workout card", async () => {
    // Arrange & Act
    await renderWorkout().rendered;

    // Assert
    await loaded();
    expect(
      screen.getByRole("heading", { level: 1, name: "Editar entrenamiento" }),
    ).toBeInTheDocument();
    expect(screen.queryByRole("heading", { level: 1, name: "Pierna" })).not.toBeInTheDocument();
    expect(screen.getByRole("link", { name: "Volver" })).toHaveAttribute("href", "/assigned-plans");
    expect(within(workoutSection()).getByRole("button", { name: "Guardar" })).toBeInTheDocument();
  });

  it("places Guardar after the description and before the exercises", async () => {
    // Arrange & Act
    await renderWorkout().rendered;

    // Assert
    await loaded();
    const card = workoutSection();
    const description = within(card).getByRole("textbox", { name: "Descripción" });
    const save = within(card).getByRole("button", { name: "Guardar" });
    const exercises = within(card).getByRole("heading", { level: 3, name: "Ejercicios" });
    expect(
      description.compareDocumentPosition(save) & Node.DOCUMENT_POSITION_FOLLOWING,
    ).toBeTruthy();
    expect(save.compareDocumentPosition(exercises) & Node.DOCUMENT_POSITION_FOLLOWING).toBeTruthy();
  });

  it("keeps the workout's exercises inside the workout card", async () => {
    // Arrange & Act
    await renderWorkout().rendered;

    // Assert
    await loaded();
    const card = workoutSection();
    expect(within(card).getByRole("textbox", { name: "Nombre" })).toBeInTheDocument();
    expect(within(card).getByRole("heading", { level: 3, name: "Ejercicios" })).toBeInTheDocument();
    expect(within(card).getByRole("list", { name: "Ejercicios" })).toBeInTheDocument();
  });

  it("scrolls the catalog and the workout's exercises inside their cards", async () => {
    // Arrange & Act
    await renderWorkout({ getWorkout: async () => ok({ ...workout, exercises: [benchInWorkout] }) })
      .rendered;

    // Assert
    await loaded();
    for (const list of [
      within(catalogSection()).getByRole("list", { name: "Catálogo de ejercicios" }),
      within(workoutSection()).getByRole("list", { name: "Ejercicios" }),
    ]) {
      expect(list).toHaveStyle({ maxHeight: "min(60vh, 640px)", overflowY: "auto" });
    }
  });

  it("shows no add button in the workout card", async () => {
    // Arrange & Act
    await renderWorkout().rendered;

    // Assert
    await loaded();
    expect(
      within(workoutSection()).getByRole("heading", { level: 3, name: "Ejercicios" }),
    ).toBeInTheDocument();
    expect(
      within(workoutSection()).queryByRole("button", { name: "Añadir ejercicio" }),
    ).not.toBeInTheDocument();
  });

  it("colours body-part labels by body part and keeps muscle labels neutral", async () => {
    // Arrange & Act
    await renderWorkout({ getWorkout: withoutExercises }).rendered;

    // Assert
    await loaded();
    const [squatItem, , benchItem] = within(catalogSection()).getAllByRole("listitem");
    const chip = (item: HTMLElement, text: string) =>
      within(item).getByText(text).closest(".MuiChip-root");
    const [benchBodyPart, benchMuscle] = within(benchItem!)
      .getAllByText("Pecho")
      .map((text) => text.closest(".MuiChip-root"));
    expect(benchBodyPart).toHaveClass("MuiChip-colorError");
    expect(benchMuscle).toHaveClass("MuiChip-colorDefault");
    expect(chip(squatItem!, "Piernas")).toHaveClass("MuiChip-colorSuccess");
    expect(chip(squatItem!, "Cuádriceps")).toHaveClass("MuiChip-colorDefault");
  });

  it("shows the message under a blank name and sends nothing", async () => {
    // Arrange
    const { mutations, rendered } = renderWorkout();
    await rendered;
    await loaded();
    const user = userEvent.setup();
    await user.clear(within(workoutSection()).getByRole("textbox", { name: "Nombre" }));

    // Act
    await user.click(screen.getByRole("button", { name: "Guardar" }));

    // Assert
    expect(
      within(workoutSection()).getByRole("textbox", { name: "Nombre" }),
    ).toHaveAccessibleDescription("Este campo es obligatorio.");
    expect(mutationCalls(mutations)).toBe(0);
  });

  it("shows the message under a too long description and sends nothing", async () => {
    // Arrange
    const { mutations, rendered } = renderWorkout();
    await rendered;
    await loaded();
    const user = userEvent.setup();
    const description = within(workoutSection()).getByRole("textbox", { name: "Descripción" });
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
    const name = within(workoutSection()).getByRole("textbox", { name: "Nombre" });
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
    await user.clear(within(workoutSection()).getByRole("textbox", { name: "Descripción" }));

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
    const name = within(workoutSection()).getByRole("textbox", { name: "Nombre" });
    await user.clear(name);
    await user.type(name, "Pierna B");

    // Act
    await user.click(screen.getByRole("button", { name: "Guardar" }));

    // Assert
    expect(await screen.findByText("Entrenamiento guardado.")).toBeInTheDocument();
    expect(within(workoutSection()).getByRole("textbox", { name: "Nombre" })).toHaveValue(
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
    await user.type(within(workoutSection()).getByRole("textbox", { name: "Nombre" }), "!");

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
    const name = within(workoutSection()).getByRole("textbox", { name: "Nombre" });
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
    const [item] = within(workoutSection()).getAllByRole("listitem");
    const setsLine = within(item!).getByText(
      "Series: 2 × 15 repeticiones, 1 × 10–12 repeticiones, 1 × máximo",
    );
    const effortLine = within(item!).getByText("Esfuerzo: Alto");
    expect(setsLine).not.toBe(effortLine);
    expect(
      setsLine.compareDocumentPosition(effortLine) & Node.DOCUMENT_POSITION_FOLLOWING,
    ).toBeTruthy();
  });

  it("shows neither line for a pending exercise not defined yet", async () => {
    // Arrange
    await renderWorkout({ getWorkout: withoutExercises }).rendered;
    await loaded();
    const user = userEvent.setup();

    // Act
    await user.click(
      within(catalogSection()).getAllByRole("button", { name: "Añadir ejercicio" })[0]!,
    );

    // Assert
    const [item] = within(workoutSection()).getAllByRole("listitem");
    expect(within(item!).queryByText(/^Series:/)).not.toBeInTheDocument();
    expect(within(item!).queryByText(/^Esfuerzo:/)).not.toBeInTheDocument();
  });

  it("updates the lines with what the API answers after an exercise save", async () => {
    // Arrange
    await renderWorkout({ getWorkout: withoutExercises }).rendered;
    await loaded();
    const user = userEvent.setup();
    await user.click(
      within(catalogSection()).getAllByRole("button", { name: "Añadir ejercicio" })[0]!,
    );
    await chooseEffort(user, "Medio");
    await addSet(user, { count: "3", value: "10" });

    // Act
    await user.click(within(configurationSection()).getByRole("button", { name: "Guardar" }));

    // Assert
    await screen.findByText("Ejercicio guardado.");
    const [item] = within(workoutSection()).getAllByRole("listitem");
    expect(within(item!).getByText("Series: 3 × 10 repeticiones")).toBeInTheDocument();
    expect(within(item!).getByText("Esfuerzo: Medio")).toBeInTheDocument();
  });

  it("lists the workout's exercises in order with their actions", async () => {
    // Arrange & Act
    await renderWorkout().rendered;

    // Assert
    await loaded();
    const items = within(workoutSection()).getAllByRole("listitem");
    expect(
      items.map((item) =>
        within(item)
          .getAllByText(/./)
          .slice(0, 2)
          .map((text) => text.textContent),
      ),
    ).toEqual([
      ["1", "Sentadilla"],
      ["2", "Press de banca"],
      ["3", "Prensa de piernas"],
    ]);
    for (const item of items) {
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
      await moveDown(user, 0, 1);

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
      await moveDown(user, 0, 2);

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
      expect(within(workoutSection()).queryByRole("alert")).not.toBeInTheDocument();
    });

    it("puts the previous order back and shows the error when the API refuses it", async () => {
      // Arrange
      await renderWorkout({ reorderExercises: async () => fail(new DomainError("CONFLICT", "")) })
        .rendered;
      await loaded();
      const user = userEvent.setup();

      // Act
      await moveDown(user, 0, 1);

      // Assert
      expect(
        await within(workoutSection()).findByText(
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
      await moveDown(user, 0, 0);

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
      await user.keyboard("[ArrowDown]");

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
      await moveDown(user, 0, 1);

      // Assert
      for (const handle of workoutButtons("Reordenar ejercicio")) {
        expect(handle).toBeDisabled();
      }
    });

    it("disables a pending exercise's handle and never sends pending exercises", async () => {
      // Arrange
      const { mutations, rendered } = renderWorkout({
        getWorkout: async () => ok({ ...workout, exercises: [squatInWorkout, benchInWorkout] }),
      });
      await rendered;
      await loaded();
      const user = userEvent.setup();
      await user.click(
        within(catalogSection()).getAllByRole("button", { name: "Añadir ejercicio" })[0]!,
      );

      // Act
      await moveDown(user, 0, 1);

      // Assert
      const handles = workoutButtons("Reordenar ejercicio");
      expect(handles).toHaveLength(3);
      await waitFor(() => expect(handles[0]).toBeEnabled());
      expect(handles[2]).toBeDisabled();
      expect(mutations.reorderExercises).toHaveBeenCalledWith({
        workoutId: "w-1",
        exerciseIds: ["ex-bench", "ex-squat"],
      });
    });

    it("keeps the moved exercise selected", async () => {
      // Arrange
      await renderWorkout().rendered;
      await loaded();
      const user = userEvent.setup();
      await user.click(workoutButtons("Editar ejercicio")[0]!);

      // Act
      await moveDown(user, 0, 1);

      // Assert
      const squatItem = within(workoutSection()).getAllByRole("listitem")[1]!;
      expect(within(squatItem).getByText("Sentadilla")).toBeInTheDocument();
      expect(squatItem).toHaveAttribute("aria-current", "true");
      expect(within(configurationSection()).getByText("Sentadilla")).toBeInTheDocument();
    });
  });

  it("removes a saved exercise through the API and lists it in the catalog again", async () => {
    // Arrange
    const { mutations, rendered } = renderWorkout({
      getWorkout: async () => ok({ ...workout, exercises: [squatInWorkout] }),
    });
    await rendered;
    await loaded();
    const user = userEvent.setup();
    expect(catalogNames()).toEqual(["Prensa de piernas", "Press de banca"]);

    // Act
    await user.click(within(workoutSection()).getByRole("button", { name: "Quitar ejercicio" }));

    // Assert
    await waitFor(() =>
      expect(within(workoutSection()).queryAllByRole("listitem")).toHaveLength(0),
    );
    expect(mutations.removeExercise).toHaveBeenCalledTimes(1);
    expect(mutations.removeExercise).toHaveBeenCalledWith({
      workoutId: "w-1",
      exerciseId: "ex-squat",
    });
    expect(catalogNames()).toEqual(["Sentadilla", "Prensa de piernas", "Press de banca"]);
  });

  it("drops a pending exercise without a request and lists it in the catalog again", async () => {
    // Arrange
    const { mutations, rendered } = renderWorkout({ getWorkout: withoutExercises });
    await rendered;
    await loaded();
    const user = userEvent.setup();
    await user.click(
      within(catalogSection()).getAllByRole("button", { name: "Añadir ejercicio" })[0]!,
    );

    // Act
    await user.click(within(workoutSection()).getByRole("button", { name: "Quitar ejercicio" }));

    // Assert
    expect(within(workoutSection()).queryAllByRole("listitem")).toHaveLength(0);
    expect(catalogNames()).toEqual(["Sentadilla", "Prensa de piernas", "Press de banca"]);
    expect(mutationCalls(mutations)).toBe(0);
  });

  it("empties the configuration card when the selected exercise is removed", async () => {
    // Arrange
    await renderWorkout().rendered;
    await loaded();
    const user = userEvent.setup();
    const [squatItem] = within(workoutSection()).getAllByRole("listitem");
    await user.click(within(squatItem!).getByRole("button", { name: "Editar ejercicio" }));

    // Act
    await user.click(within(squatItem!).getByRole("button", { name: "Quitar ejercicio" }));

    // Assert
    expect(
      await within(configurationSection()).findByText("Selecciona un ejercicio para configurarlo."),
    ).toBeInTheDocument();
  });

  it("disables Quitar ejercicio while the removal is in progress", async () => {
    // Arrange
    await renderWorkout({ removeExercise: () => new Promise(() => {}) }).rendered;
    await loaded();
    const user = userEvent.setup();
    const [squatItem] = within(workoutSection()).getAllByRole("listitem");

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
    await user.click(
      within(workoutSection()).getAllByRole("button", { name: "Quitar ejercicio" })[0]!,
    );

    // Assert
    expect(
      await within(workoutSection()).findByText(
        "La operación entra en conflicto con el estado actual.",
      ),
    ).toBeInTheDocument();
    expect(within(workoutSection()).getAllByRole("listitem")).toHaveLength(3);
  });

  it("says so when the workout has no exercises", async () => {
    // Arrange & Act
    await renderWorkout({ getWorkout: async () => ok({ ...workout, exercises: [] }) }).rendered;

    // Assert
    expect(
      await screen.findByText("Este entrenamiento aún no tiene ejercicios."),
    ).toBeInTheDocument();
  });

  it("asks to select an exercise before any is chosen", async () => {
    // Arrange & Act
    await renderWorkout().rendered;

    // Assert
    await loaded();
    expect(
      within(configurationSection()).getByText("Selecciona un ejercicio para configurarlo."),
    ).toBeInTheDocument();
  });

  it("selects an exercise with the pencil and shows it in the configuration", async () => {
    // Arrange
    const { mutations, rendered } = renderWorkout();
    await rendered;
    await loaded();
    const user = userEvent.setup();
    const [, benchItem] = within(workoutSection()).getAllByRole("listitem");

    // Act
    await user.click(within(benchItem!).getByRole("button", { name: "Editar ejercicio" }));

    // Assert
    expect(benchItem).toHaveAttribute("aria-current", "true");
    const panel = configurationSection();
    expect(within(panel).getByRole("heading", { name: "Press de banca" })).toBeInTheDocument();
    expect(within(panel).getAllByText("Pecho")).toHaveLength(2);
    expect(within(panel).getByRole("combobox", { name: "Esfuerzo" })).toHaveTextContent("Medio");
    expect(setSummaries()).toEqual(["4 × 8–12 repeticiones"]);
    expect(mutationCalls(mutations)).toBe(0);
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
    await user.click(within(workoutSection()).getByRole("button", { name: "Editar ejercicio" }));

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
    const [squatItem] = within(workoutSection()).getAllByRole("listitem");
    await user.click(within(squatItem!).getByRole("button", { name: "Editar ejercicio" }));
    await addSet(user, { count: "1", type: "Máximas repeticiones" });

    // Act
    await user.click(
      within(configurationSection()).getAllByRole("button", { name: "Quitar serie" })[0]!,
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
    const [squatItem] = within(workoutSection()).getAllByRole("listitem");
    await user.click(within(squatItem!).getByRole("button", { name: "Editar ejercicio" }));

    // Act
    await addSet(user, { count: "2", value: "15" });

    // Assert
    expect(setSummaries()).toEqual(["3 × 10 repeticiones", "2 × 15 repeticiones"]);
    const form = within(configurationSection()).getByRole("group", { name: "Añadir serie" });
    expect(within(form).getByRole("spinbutton", { name: "Series" })).toHaveValue(null);
    expect(within(form).getByRole("spinbutton", { name: "Valor" })).toHaveValue(null);
    expect(mutationCalls(mutations)).toBe(0);
  });

  it("asks for no value when the set is max reps", async () => {
    // Arrange
    await renderWorkout().rendered;
    await loaded();
    const user = userEvent.setup();
    await user.click(
      within(workoutSection()).getAllByRole("button", { name: "Editar ejercicio" })[0]!,
    );
    const form = within(configurationSection()).getByRole("group", { name: "Añadir serie" });

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
    await user.click(
      within(workoutSection()).getAllByRole("button", { name: "Editar ejercicio" })[0]!,
    );
    const form = within(configurationSection()).getByRole("group", { name: "Añadir serie" });
    await user.type(within(form).getByRole("spinbutton", { name: "Series" }), "0");

    // Act
    await user.click(within(configurationSection()).getByRole("button", { name: "Añadir serie" }));

    // Assert
    expect(within(form).getByRole("spinbutton", { name: "Series" })).toHaveAccessibleDescription(
      "Introduce un número entero mayor que 0.",
    );
    expect(within(form).getByRole("spinbutton", { name: "Valor" })).toHaveAccessibleDescription(
      "Introduce un número entero mayor que 0.",
    );
    expect(setSummaries()).toEqual(["3 × 10 repeticiones"]);
  });

  it("asks for an effort and a set before saving a pending exercise", async () => {
    // Arrange
    const { mutations, rendered } = renderWorkout({ getWorkout: withoutExercises });
    await rendered;
    await loaded();
    const user = userEvent.setup();
    await user.click(
      within(catalogSection()).getAllByRole("button", { name: "Añadir ejercicio" })[0]!,
    );

    // Act
    await user.click(within(configurationSection()).getByRole("button", { name: "Guardar" }));

    // Assert
    expect(within(configurationSection()).getByText("Elige un esfuerzo.")).toBeInTheDocument();
    expect(
      within(configurationSection()).getByText("Añade al menos una serie."),
    ).toBeInTheDocument();
    expect(mutationCalls(mutations)).toBe(0);
  });

  it("adds a pending exercise to the workout with its effort and sets, once", async () => {
    // Arrange
    const { mutations, rendered } = renderWorkout({ getWorkout: withoutExercises });
    await rendered;
    await loaded();
    const user = userEvent.setup();
    const [, pressItem] = within(catalogSection()).getAllByRole("listitem");
    await user.click(within(pressItem!).getByRole("button", { name: "Añadir ejercicio" }));
    await chooseEffort(user, "Alto");
    await addSet(user, { count: "2", value: "15" });
    await addSet(user, { count: "1", type: "Máximas repeticiones" });

    // Act
    await user.click(within(configurationSection()).getByRole("button", { name: "Guardar" }));

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

  it("shows a saved pending exercise as saved, still selected, with the confirmation", async () => {
    // Arrange
    await renderWorkout({ getWorkout: withoutExercises }).rendered;
    await loaded();
    const user = userEvent.setup();
    const [, pressItem] = within(catalogSection()).getAllByRole("listitem");
    await user.click(within(pressItem!).getByRole("button", { name: "Añadir ejercicio" }));
    await chooseEffort(user, "Alto");
    await addSet(user, { count: "2", value: "15" });

    // Act
    await user.click(within(configurationSection()).getByRole("button", { name: "Guardar" }));

    // Assert
    expect(await screen.findByText("Ejercicio guardado.")).toBeInTheDocument();
    const [item] = within(workoutSection()).getAllByRole("listitem");
    expect(item).not.toHaveTextContent("Pendiente");
    expect(item).toHaveAttribute("aria-current", "true");
    expect(setSummaries()).toEqual(["2 × 15 repeticiones"]);
  });

  it("replaces a saved exercise's effort and sets, once", async () => {
    // Arrange
    const { mutations, rendered } = renderWorkout();
    await rendered;
    await loaded();
    const user = userEvent.setup();
    const [squatItem] = within(workoutSection()).getAllByRole("listitem");
    await user.click(within(squatItem!).getByRole("button", { name: "Editar ejercicio" }));
    await addSet(user, { count: "1", type: "Rango de repeticiones", value: "8", max: "12" });
    await user.click(
      within(configurationSection()).getAllByRole("button", { name: "Quitar serie" })[0]!,
    );

    // Act
    await user.click(within(configurationSection()).getByRole("button", { name: "Guardar" }));

    // Assert
    await waitFor(() => expect(mutations.updateExercise).toHaveBeenCalledTimes(1));
    expect(mutations.updateExercise).toHaveBeenCalledWith({
      workoutId: "w-1",
      exerciseId: "ex-squat",
      effort: "HIGH",
      sets: [{ count: 1, targetType: "RepsRange", target: 8, maxTarget: 12 }],
    });
    expect(mutations.addExercise).not.toHaveBeenCalled();
    expect(await screen.findByText("Ejercicio guardado.")).toBeInTheDocument();
  });

  it("shows the error and keeps the sets when the API refuses the exercise", async () => {
    // Arrange
    const { rendered } = renderWorkout({
      getWorkout: withoutExercises,
      addExercise: async () => fail(new DomainError("CONFLICT", "")),
    });
    await rendered;
    await loaded();
    const user = userEvent.setup();
    await user.click(
      within(catalogSection()).getAllByRole("button", { name: "Añadir ejercicio" })[0]!,
    );
    await chooseEffort(user, "Medio");
    await addSet(user, { count: "3", value: "10" });

    // Act
    await user.click(within(configurationSection()).getByRole("button", { name: "Guardar" }));

    // Assert
    expect(
      await screen.findByText("La operación entra en conflicto con el estado actual."),
    ).toBeInTheDocument();
    expect(setSummaries()).toEqual(["3 × 10 repeticiones"]);
    expect(within(workoutSection()).getAllByRole("listitem")[0]).toHaveTextContent("Pendiente");
  });

  it("shows the stored values again after another exercise is selected", async () => {
    // Arrange
    await renderWorkout().rendered;
    await loaded();
    const user = userEvent.setup();
    const [squatItem, benchItem] = within(workoutSection()).getAllByRole("listitem");
    await user.click(within(squatItem!).getByRole("button", { name: "Editar ejercicio" }));
    await user.click(within(configurationSection()).getByRole("button", { name: "Quitar serie" }));
    await user.click(within(benchItem!).getByRole("button", { name: "Editar ejercicio" }));

    // Act
    await user.click(within(squatItem!).getByRole("button", { name: "Editar ejercicio" }));

    // Assert
    const panel = configurationSection();
    expect(within(panel).getByRole("combobox", { name: "Esfuerzo" })).toHaveTextContent("Alto");
    expect(setSummaries()).toEqual(["3 × 10 repeticiones"]);
  });

  it("embeds a YouTube video without tracking cookies", async () => {
    // Arrange
    await renderWorkout().rendered;
    await loaded();
    const user = userEvent.setup();
    const [squatItem] = within(workoutSection()).getAllByRole("listitem");

    // Act
    await user.click(within(squatItem!).getByRole("button", { name: "Editar ejercicio" }));

    // Assert
    expect(within(configurationSection()).getByTitle("Sentadilla")).toHaveAttribute(
      "src",
      "https://www.youtube-nocookie.com/embed/h_yZV27H684",
    );
  });

  it("shows the video before the exercise's name", async () => {
    // Arrange
    await renderWorkout().rendered;
    await loaded();
    const user = userEvent.setup();
    const [squatItem] = within(workoutSection()).getAllByRole("listitem");

    // Act
    await user.click(within(squatItem!).getByRole("button", { name: "Editar ejercicio" }));

    // Assert
    const panel = configurationSection();
    const video = within(panel).getByTitle("Sentadilla");
    const name = within(panel).getByRole("heading", { level: 3, name: "Sentadilla" });
    expect(video.compareDocumentPosition(name) & Node.DOCUMENT_POSITION_FOLLOWING).toBeTruthy();
  });

  it("links any other video in a new tab", async () => {
    // Arrange
    await renderWorkout().rendered;
    await loaded();
    const user = userEvent.setup();
    const [, , pressItem] = within(workoutSection()).getAllByRole("listitem");

    // Act
    await user.click(within(pressItem!).getByRole("button", { name: "Editar ejercicio" }));

    // Assert
    const link = within(configurationSection()).getByRole("link", { name: "Ver vídeo" });
    expect(link).toHaveAttribute("href", "https://example.com/leg-press.mp4");
    expect(link).toHaveAttribute("target", "_blank");
    expect(
      within(configurationSection()).queryByTitle("Prensa de piernas"),
    ).not.toBeInTheDocument();
  });

  it("shows no video section for an exercise without video", async () => {
    // Arrange
    await renderWorkout().rendered;
    await loaded();
    const user = userEvent.setup();
    const [, benchItem] = within(workoutSection()).getAllByRole("listitem");

    // Act
    await user.click(within(benchItem!).getByRole("button", { name: "Editar ejercicio" }));

    // Assert
    expect(
      within(configurationSection()).queryByRole("region", { name: "Vídeo" }),
    ).not.toBeInTheDocument();
  });

  it("shows the skeleton while the workout loads", async () => {
    // Arrange & Act
    await renderWorkout({ getWorkout: () => new Promise(() => {}) }).rendered;

    // Assert
    expect(await screen.findByLabelText("Cargando")).toBeInTheDocument();
    expect(
      screen.queryByRole("region", { name: "Catálogo de ejercicios" }),
    ).not.toBeInTheDocument();
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
    expect(catalogNames()).toHaveLength(3);
    expect(readCatalog).toHaveBeenCalledTimes(2);
  });

  it("follows the active language", async () => {
    // Arrange
    await renderWorkout({ getWorkout: withoutExercises }).rendered;
    await loaded();

    // Act
    await act(() => i18n.changeLanguage("en"));

    // Assert
    expect(screen.getByRole("searchbox", { name: "Search exercise" })).toBeInTheDocument();
    expect(screen.getByRole("button", { name: "Save" })).toBeInTheDocument();
    expect(screen.getByText("Select an exercise to configure it.")).toBeInTheDocument();
    expect(screen.getByRole("button", { name: "Legs" })).toBeInTheDocument();
    expect(screen.getAllByText("Quadriceps").length).toBeGreaterThan(0);
    expect(
      within(screen.getByRole("region", { name: "Exercise catalog" })).getByText("Squat"),
    ).toBeInTheDocument();
  });
});
