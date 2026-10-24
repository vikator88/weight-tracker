import { expect, test } from "@playwright/test";
import { mockUsersMe } from "./support/users-me";

test("changes the name from Mi perfil and updates the header", async ({ page }) => {
  // Arrange
  await mockUsersMe(page);
  await page.addInitScript(() => {
    window.localStorage.setItem("auth.accessToken", "access");
    window.localStorage.setItem("auth.refreshToken", "refresh");
  });
  await page.goto("/");
  await expect(page.getByText("Ana García")).toBeVisible();
  await page.getByRole("button", { name: "Menú de usuario" }).click();
  await page.getByRole("menuitem", { name: "Mi perfil" }).click();
  await expect(page.getByRole("heading", { level: 1, name: "Mi perfil" })).toBeVisible();
  await page.getByLabel("Nombre").fill("Lucía");

  // Act
  await page.getByRole("button", { name: "Guardar" }).click();

  // Assert
  await expect(page.getByText("Perfil actualizado.")).toBeVisible();
  await expect(page.getByText("Lucía García")).toBeVisible();
});
