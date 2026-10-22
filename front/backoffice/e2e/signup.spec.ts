import { expect, type Page, test } from "@playwright/test";

const signupUrl = "http://localhost:5199/auth/signup";
const cors = { "Access-Control-Allow-Origin": "*", "Access-Control-Allow-Headers": "*" };

/** Intercepts the signup call, including the cross-origin preflight, since the site and API differ in origin. */
async function mockSignup(page: Page, status: number, body: object) {
  await page.route(signupUrl, (route) => {
    if (route.request().method() === "OPTIONS") {
      return route.fulfill({ status: 204, headers: cors });
    }
    return route.fulfill({
      status,
      headers: cors,
      contentType: "application/json",
      body: JSON.stringify(body),
    });
  });
}

test("signs up, lands on home and stays there after a reload", async ({ page }) => {
  // Arrange
  await mockSignup(page, 200, { accessToken: "access", refreshToken: "refresh", expiresIn: 900 });
  await page.goto("/signup");

  // Act
  await page.getByLabel("Email").fill("trainer@example.com");
  await page.getByLabel("Contraseña").fill("Str0ng!pass");
  await page.getByRole("button", { name: "Crear cuenta" }).click();
  await expect(page).toHaveURL("/");
  await page.reload();

  // Assert
  await expect(page.getByRole("heading", { level: 1, name: "Weight Tracker" })).toBeVisible();
  await expect(page).toHaveURL("/");
});

test("sends a visitor without tokens from home to signup", async ({ page }) => {
  // Arrange & Act
  await page.goto("/");

  // Assert
  await expect(page).toHaveURL(/\/signup\/?$/);
  await expect(page.getByRole("heading", { level: 1, name: "Crear cuenta" })).toBeVisible();
  await expect(page.getByRole("heading", { name: "Weight Tracker" })).toHaveCount(0);
});

test("shows the conflict message when the email is already registered", async ({ page }) => {
  // Arrange
  await mockSignup(page, 409, { title: "Conflict", status: 409 });
  await page.goto("/signup");

  // Act
  await page.getByLabel("Email").fill("trainer@example.com");
  await page.getByLabel("Contraseña").fill("Str0ng!pass");
  await page.getByRole("button", { name: "Crear cuenta" }).click();

  // Assert
  await expect(page.getByText("Ya existe una cuenta con este email.")).toBeVisible();
});
