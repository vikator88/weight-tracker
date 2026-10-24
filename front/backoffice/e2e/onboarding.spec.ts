import { expect, type Page, test } from "@playwright/test";
import { completeProfile, mockUsersMe } from "./support/users-me";

const loginUrl = "http://localhost:5199/auth/login";
const cors = { "Access-Control-Allow-Origin": "*", "Access-Control-Allow-Headers": "*" };

async function signIn(page: Page) {
  await page.route(loginUrl, (route) => {
    if (route.request().method() === "OPTIONS") {
      return route.fulfill({ status: 204, headers: cors });
    }
    return route.fulfill({
      status: 200,
      headers: cors,
      contentType: "application/json",
      body: JSON.stringify({ accessToken: "access", refreshToken: "refresh", expiresIn: 900 }),
    });
  });
  await page.goto("/login");
  await page.getByLabel("Email").fill("trainer@example.com");
  await page.getByLabel("Contraseña").fill("Str0ng!pass");
  await page.getByRole("button", { name: "Entrar" }).click();
}

test("asks a new user for their name, then shows home and the name in the header", async ({
  page,
}) => {
  // Arrange
  await mockUsersMe(page, { ...completeProfile, name: "", surname: "" });
  await signIn(page);
  const dialog = page.getByRole("dialog", { name: "Paso 1 de 1 · Dinos cómo te llamas" });
  await expect(dialog).toBeVisible();
  await expect(page.getByRole("heading", { level: 1, name: "Weight Tracker" })).toHaveCount(0);
  await dialog.getByLabel("Nombre").fill("Lu");
  await dialog.getByRole("button", { name: "Finalizar" }).click();
  await expect(dialog.getByText("Debe tener al menos 3 caracteres.")).toBeVisible();
  await dialog.getByLabel("Nombre").fill("Lucía");
  await dialog.getByLabel("Apellidos").fill("Pérez");

  // Act
  await dialog.getByRole("button", { name: "Finalizar" }).click();

  // Assert
  await expect(dialog).toHaveCount(0);
  await expect(page.getByRole("heading", { level: 1, name: "Weight Tracker" })).toBeVisible();
  await expect(page.getByText("Lucía Pérez")).toBeVisible();
});

test("keeps a user in onboarding on home even when they open the profile URL", async ({ page }) => {
  // Arrange
  await mockUsersMe(page, { ...completeProfile, name: "", surname: "" });
  await signIn(page);
  await expect(page.getByRole("dialog")).toBeVisible();

  // Act
  await page.goto("/profile");

  // Assert
  await expect(page).toHaveURL("/");
  await expect(page.getByRole("dialog", { name: /Dinos cómo te llamas/ })).toBeVisible();
});
