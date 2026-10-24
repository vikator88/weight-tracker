import { expect, type Page, test } from "@playwright/test";

const loginUrl = "http://localhost:5199/auth/login";
const cors = { "Access-Control-Allow-Origin": "*", "Access-Control-Allow-Headers": "*" };

/** Intercepts the login call, including the cross-origin preflight, since the site and API differ in origin. */
async function mockLogin(page: Page, status: number, body: object) {
  await page.route(loginUrl, (route) => {
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

test("logs in, lands on home and stays there after a reload", async ({ page }) => {
  // Arrange
  await mockLogin(page, 200, { accessToken: "access", refreshToken: "refresh", expiresIn: 900 });
  await page.goto("/login");

  // Act
  await page.getByLabel("Email").fill("trainer@example.com");
  await page.getByLabel("Contraseña").fill("Str0ng!pass");
  await page.getByRole("button", { name: "Entrar" }).click();
  await expect(page).toHaveURL("/");
  await page.reload();

  // Assert
  await expect(page.getByRole("heading", { level: 1, name: "Weight Tracker" })).toBeVisible();
  await expect(page).toHaveURL("/");
});

/** Logs in through the intercepted API and waits for home, with its navigation drawer. */
async function logIn(page: Page) {
  await mockLogin(page, 200, { accessToken: "access", refreshToken: "refresh", expiresIn: 900 });
  await page.goto("/login");
  await page.getByLabel("Email").fill("trainer@example.com");
  await page.getByLabel("Contraseña").fill("Str0ng!pass");
  await page.getByRole("button", { name: "Entrar" }).click();
  await expect(page.getByRole("navigation", { name: "Menú principal" })).toBeVisible();
}

test("logs out from the user menu and lands on login", async ({ page }) => {
  // Arrange
  await logIn(page);

  // Act
  await page.getByRole("button", { name: "Menú de usuario" }).click();
  await page.getByRole("menuitem", { name: "Cerrar sesión" }).click();

  // Assert
  await expect(page).toHaveURL(/\/login\/?$/);
  await expect(page.getByRole("heading", { level: 1, name: "Iniciar sesión" })).toBeVisible();
  await expect(page.getByRole("navigation", { name: "Menú principal" })).toHaveCount(0);
});

test("sends a visitor who logged out from home back to login", async ({ page }) => {
  // Arrange
  await logIn(page);
  await page.getByRole("button", { name: "Menú de usuario" }).click();
  await page.getByRole("menuitem", { name: "Cerrar sesión" }).click();
  await expect(page).toHaveURL(/\/login\/?$/);

  // Act
  await page.goto("/");

  // Assert
  await expect(page).toHaveURL(/\/login\/?$/);
  await expect(page.getByRole("heading", { name: "Weight Tracker" })).toHaveCount(0);
  await expect(page.getByRole("navigation", { name: "Menú principal" })).toHaveCount(0);
});

test("keeps the chosen language after a reload", async ({ page }) => {
  // Arrange
  await logIn(page);

  // Act
  await page.getByRole("button", { name: "Cambiar idioma" }).click();
  await page.getByRole("menuitem", { name: "English" }).click();
  await expect(page.getByRole("textbox", { name: "Search" })).toBeVisible();
  await page.reload();

  // Assert
  await expect(page.getByPlaceholder("Type for search...")).toBeVisible();
  await expect(page.locator("html")).toHaveAttribute("lang", "en");
});

test("sends a visitor without tokens from home to login", async ({ page }) => {
  // Arrange & Act
  await page.goto("/");

  // Assert
  await expect(page).toHaveURL(/\/login\/?$/);
  await expect(page.getByRole("heading", { level: 1, name: "Iniciar sesión" })).toBeVisible();
  await expect(page.getByRole("heading", { name: "Weight Tracker" })).toHaveCount(0);
});

test("shows the invalid-credentials message when the API answers 401", async ({ page }) => {
  // Arrange
  await mockLogin(page, 401, { title: "Unauthorized", status: 401 });
  await page.goto("/login");

  // Act
  await page.getByLabel("Email").fill("trainer@example.com");
  await page.getByLabel("Contraseña").fill("wrong-password");
  await page.getByRole("button", { name: "Entrar" }).click();

  // Assert
  await expect(page.getByText("Email o contraseña incorrectos.")).toBeVisible();
});

test("moves between login and signup through the links", async ({ page }) => {
  // Arrange
  await page.goto("/login");

  // Act
  await page.getByRole("link", { name: "¿No tienes cuenta? Crea una" }).click();
  await expect(page).toHaveURL(/\/signup\/?$/);
  await page.getByRole("link", { name: "¿Ya tienes cuenta? Inicia sesión" }).click();

  // Assert
  await expect(page).toHaveURL(/\/login\/?$/);
  await expect(page.getByRole("heading", { level: 1, name: "Iniciar sesión" })).toBeVisible();
});
