import { expect, type Page, test } from "@playwright/test";
import { mockUsersMe } from "./support/users-me";

const api = "http://localhost:5199";
const cors = {
  "Access-Control-Allow-Origin": "*",
  "Access-Control-Allow-Headers": "authorization, content-type",
  "Access-Control-Allow-Methods": "GET, POST, PUT, DELETE, OPTIONS",
};

const athleteId = "5f1c2f0e-9d5b-4a53-8c7e-0d6f7c1a2b3c";

interface StoredWorkout {
  id: string;
  name: string;
  description: string | null;
  workoutDate: string;
  userId: string;
  trainerId: string | null;
  status: string;
  exercises: unknown[];
}

const existing: StoredWorkout = {
  id: "7b0c1f7e-3a54-4d4e-9d0e-1a2b3c4d5e6f",
  name: "Empuje",
  description: "Día 2",
  workoutDate: "2026-11-03",
  userId: athleteId,
  trainerId: null,
  status: "DRAFT",
  exercises: [],
};

const catalog = [
  {
    id: "1f2e3d4c-5b6a-4978-8695-a4b3c2d1e0f9",
    freeExerciseDbId: null,
    videoUrl: null,
    muscle: "QUADRICEPS",
    bodyPart: "LEGS",
    translations: [
      { languageCode: "es", name: "Sentadilla" },
      { languageCode: "en", name: "Squat" },
    ],
  },
];

function json(body: unknown, status = 200) {
  return { status, headers: cors, contentType: "application/json", body: JSON.stringify(body) };
}

/**
 * Intercepts the workout and catalog endpoints. A `POST /workouts` stores what it receives as a new workout, and a
 * `PUT /workouts/{id}` replaces that workout's data.
 */
async function mockWorkoutsApi(page: Page) {
  const workouts = new Map<string, StoredWorkout>([[existing.id, existing]]);

  await page.route(`${api}/workouts`, (route) => {
    const request = route.request();

    if (request.method() === "OPTIONS") {
      return route.fulfill({ status: 204, headers: cors });
    }
    if (request.method() === "POST") {
      const body = request.postDataJSON() as Partial<StoredWorkout>;
      const created = {
        ...existing,
        ...body,
        id: "8c1d2e3f-4a5b-4c6d-8e7f-9a0b1c2d3e4f",
        description: body.description ?? null,
      };
      workouts.set(created.id, created);
      return route.fulfill(json(created, 201));
    }
    return route.fulfill(json([...workouts.values()]));
  });
  await page.route(`${api}/workouts/*`, (route) => {
    if (route.request().method() === "OPTIONS") {
      return route.fulfill({ status: 204, headers: cors });
    }
    const id = new URL(route.request().url()).pathname.split("/").pop() ?? "";
    const workout = workouts.get(id);
    if (!workout) {
      return route.fulfill(json({ status: 404 }, 404));
    }
    if (route.request().method() === "PUT") {
      const body = route.request().postDataJSON() as Partial<StoredWorkout>;
      const updated = { ...workout, ...body, description: body.description ?? null };
      workouts.set(id, updated);
      return route.fulfill(json(updated));
    }
    return route.fulfill(json(workout));
  });
  await page.route(`${api}/workouts/*/exercises`, (route) => {
    const request = route.request();
    if (request.method() === "OPTIONS") {
      return route.fulfill({ status: 204, headers: cors });
    }
    const workoutId = new URL(request.url()).pathname.split("/")[2] ?? "";
    const workout = workouts.get(workoutId);
    if (!workout) {
      return route.fulfill(json({ status: 404 }, 404));
    }
    const body = request.postDataJSON() as {
      exerciseId: string;
      effort: string;
      sets: { count: number; targetType: string; target?: number; maxTarget?: number }[];
    };
    const entry = {
      id: "9d8c7b6a-5f4e-4d3c-8b2a-1f0e9d8c7b6a",
      exerciseId: body.exerciseId,
      effort: body.effort,
      superSeriesId: null,
      sets: body.sets.map((set) => ({
        count: set.count,
        target: {
          type: set.targetType,
          value: set.target ?? null,
          maxValue: set.maxTarget ?? null,
        },
      })),
    };
    workouts.set(workoutId, { ...workout, exercises: [...workout.exercises, entry] });
    return route.fulfill(json(entry, 201));
  });
  await page.route(`${api}/workouts/*/exercises/*`, (route) => {
    const request = route.request();
    if (request.method() === "OPTIONS") {
      return route.fulfill({ status: 204, headers: cors });
    }
    const [, , workoutId = "", , exerciseId = ""] = new URL(request.url()).pathname.split("/");
    const workout = workouts.get(workoutId);
    if (!workout || request.method() !== "DELETE") {
      return route.fulfill(json({ status: 404 }, 404));
    }
    workouts.set(workoutId, {
      ...workout,
      exercises: workout.exercises.filter(
        (entry) => (entry as { exerciseId: string }).exerciseId !== exerciseId,
      ),
    });
    return route.fulfill({ status: 204, headers: cors });
  });
  await page.route(`${api}/exercises`, (route) =>
    route.request().method() === "OPTIONS"
      ? route.fulfill({ status: 204, headers: cors })
      : route.fulfill(json(catalog)),
  );
}

async function signIn(page: Page) {
  await mockUsersMe(page);
  await mockWorkoutsApi(page);
  await page.addInitScript(() => {
    window.localStorage.setItem("auth.accessToken", "access");
    window.localStorage.setItem("auth.refreshToken", "refresh");
  });
}

test("creates a workout from the dialog and lands on its screen", async ({ page }) => {
  // Arrange
  await signIn(page);
  await page.goto("/assigned-plans");
  await page.getByRole("button", { name: "Nuevo entrenamiento" }).click();
  const dialog = page.getByRole("dialog", { name: "Nuevo entrenamiento" });
  await dialog.getByLabel("Nombre").fill("Pierna");
  await dialog.getByLabel("Fecha").fill("2026-11-02");
  await dialog.getByLabel("ID del atleta").fill(athleteId);

  // Act
  await dialog.getByRole("button", { name: "Crear" }).click();

  // Assert
  await expect(page).toHaveURL(/\/workout\/?\?id=8c1d2e3f-4a5b-4c6d-8e7f-9a0b1c2d3e4f$/);
  await expect(page.getByRole("heading", { level: 1, name: "Editar entrenamiento" })).toBeVisible();
  await expect(page.getByRole("textbox", { name: "Nombre" })).toHaveValue("Pierna");
  await expect(page.getByText("Sentadilla")).toBeVisible();
  await expect(page.getByText("Selecciona un ejercicio para configurarlo.")).toBeVisible();
});

test("opens a workout from the table's edit action", async ({ page }) => {
  // Arrange
  await signIn(page);
  await page.goto("/assigned-plans");

  // Act
  await page.getByRole("link", { name: "Editar Empuje" }).click();

  // Assert
  await expect(page).toHaveURL(/\/workout\/?\?id=7b0c1f7e-3a54-4d4e-9d0e-1a2b3c4d5e6f$/);
  await expect(page.getByRole("heading", { level: 1, name: "Editar entrenamiento" })).toBeVisible();
  await expect(page.getByRole("textbox", { name: "Nombre" })).toHaveValue("Empuje");
  await expect(page.getByRole("textbox", { name: "Descripción" })).toHaveValue("Día 2");
});

test("saves a workout's data and shows it after a reload", async ({ page }) => {
  // Arrange
  await signIn(page);
  await page.goto(`/workout?id=${existing.id}`);
  const name = page.getByRole("textbox", { name: "Nombre" });
  await expect(name).toHaveValue("Empuje");
  await name.fill("Empuje B");

  // Act
  await page.getByRole("button", { name: "Guardar" }).click();

  // Assert
  await expect(page.getByText("Entrenamiento guardado.")).toBeVisible();
  await page.reload();
  await expect(page.getByRole("textbox", { name: "Nombre" })).toHaveValue("Empuje B");
});

test("adds an exercise from the catalog with two sets and keeps it after a reload", async ({
  page,
}) => {
  // Arrange
  await signIn(page);
  await page.goto(`/workout?id=${existing.id}`);
  await page.getByRole("button", { name: "Añadir ejercicio" }).click();
  const panel = page.getByRole("region", { name: "Detalles del ejercicio" });
  await panel.getByRole("combobox", { name: "Esfuerzo" }).click();
  await page.getByRole("option", { name: "Alto", exact: true }).click();
  const form = panel.getByRole("group", { name: "Añadir serie" });
  await form.getByRole("spinbutton", { name: "Series" }).fill("2");
  await form.getByRole("spinbutton", { name: "Valor" }).fill("15");
  await panel.getByRole("button", { name: "Añadir serie" }).click();
  await form.getByRole("combobox", { name: "Tipo" }).click();
  await page.getByRole("option", { name: "Máximas repeticiones" }).click();
  await form.getByRole("spinbutton", { name: "Series" }).fill("1");
  await panel.getByRole("button", { name: "Añadir serie" }).click();

  // Act
  await panel.getByRole("button", { name: "Guardar" }).click();

  // Assert
  await expect(panel.getByText("Ejercicio guardado.")).toBeVisible();
  await page.reload();
  const workoutCard = page.getByRole("region", { name: "Estructura del entrenamiento" });
  await expect(workoutCard.getByText("Sentadilla")).toBeVisible();
  await expect(workoutCard.getByText("Pendiente")).toHaveCount(0);
  await expect(
    workoutCard.getByText("Series: 2 × 15 repeticiones, 1 × máximo", { exact: true }),
  ).toBeVisible();
  await expect(workoutCard.getByText("Esfuerzo: Alto", { exact: true })).toBeVisible();
  await workoutCard.getByRole("button", { name: "Editar ejercicio" }).click();
  const details = page.getByRole("region", { name: "Detalles del ejercicio" });
  await expect(details.getByText("2 × 15 repeticiones", { exact: true })).toBeVisible();
  await expect(details.getByText("1 × máximo", { exact: true })).toBeVisible();
});

test("removes a saved exercise and lists it in the catalog again", async ({ page }) => {
  // Arrange
  await signIn(page);
  await page.goto(`/workout?id=${existing.id}`);
  await page.getByRole("button", { name: "Añadir ejercicio" }).click();
  const panel = page.getByRole("region", { name: "Detalles del ejercicio" });
  await panel.getByRole("combobox", { name: "Esfuerzo" }).click();
  await page.getByRole("option", { name: "Medio", exact: true }).click();
  const form = panel.getByRole("group", { name: "Añadir serie" });
  await form.getByRole("spinbutton", { name: "Series" }).fill("3");
  await form.getByRole("spinbutton", { name: "Valor" }).fill("10");
  await panel.getByRole("button", { name: "Añadir serie" }).click();
  await panel.getByRole("button", { name: "Guardar" }).click();
  await expect(panel.getByText("Ejercicio guardado.")).toBeVisible();
  const workoutCard = page.getByRole("region", { name: "Estructura del entrenamiento" });
  const catalog = page.getByRole("region", { name: "Catálogo de ejercicios" });
  await expect(catalog.getByText("Sentadilla")).toHaveCount(0);

  // Act
  await workoutCard.getByRole("button", { name: "Quitar ejercicio" }).click();

  // Assert
  await expect(workoutCard.getByText("Este entrenamiento aún no tiene ejercicios.")).toBeVisible();
  await expect(catalog.getByText("Sentadilla")).toBeVisible();
  await page.reload();
  await expect(
    page.getByRole("region", { name: "Catálogo de ejercicios" }).getByText("Sentadilla"),
  ).toBeVisible();
});
