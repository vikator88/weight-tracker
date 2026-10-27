import { expect, test } from "@playwright/test";
import { mockUsersMe } from "./support/users-me";

const workoutsUrl = "http://localhost:5199/workouts";
const cors = {
  "Access-Control-Allow-Origin": "*",
  "Access-Control-Allow-Headers": "authorization, content-type",
  "Access-Control-Allow-Methods": "GET, OPTIONS",
};

const workouts = [
  {
    id: "7b0c1f7e-3a54-4d4e-9d0e-1a2b3c4d5e6f",
    name: "Pierna",
    description: null,
    workoutDate: "2026-10-27",
    userId: "5f1c2f0e-9d5b-4a53-8c7e-0d6f7c1a2b3c",
    trainerId: null,
    status: "DRAFT",
    exercises: [],
  },
  {
    id: "8c1d2e3f-4a5b-4c6d-8e7f-9a0b1c2d3e4f",
    name: "Empuje",
    description: null,
    workoutDate: "2026-11-03",
    userId: "6a2d3f1e-0e6c-4b64-9d8f-1e7a8d2b3c4d",
    trainerId: null,
    status: "DRAFT",
    exercises: [],
  },
];

test("opens Planes asignados from the drawer and lists the workouts", async ({ page }) => {
  // Arrange
  await mockUsersMe(page);
  await page.route(workoutsUrl, (route) =>
    route.request().method() === "OPTIONS"
      ? route.fulfill({ status: 204, headers: cors })
      : route.fulfill({
          status: 200,
          headers: cors,
          contentType: "application/json",
          body: JSON.stringify(workouts),
        }),
  );
  await page.addInitScript(() => {
    window.localStorage.setItem("auth.accessToken", "access");
    window.localStorage.setItem("auth.refreshToken", "refresh");
  });
  await page.goto("/");
  await expect(page.getByText("Ana García")).toBeVisible();

  // Act
  await page.getByRole("link", { name: "Planes asignados" }).click();

  // Assert
  await expect(page).toHaveURL(/\/assigned-plans\/?$/);
  await expect(page.getByRole("heading", { level: 1, name: "Planes asignados" })).toBeVisible();
  await expect(page.getByRole("row", { name: /Pierna/ })).toBeVisible();
  await expect(page.getByRole("row", { name: /Empuje/ })).toBeVisible();
  await expect(page.getByRole("columnheader", { name: "Entrenador" })).toHaveCount(0);
});
