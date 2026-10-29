import { expect, type Page, test } from "@playwright/test";
import { mockUsersMe } from "./support/users-me";

const loginUrl = "http://localhost:5199/auth/login";
const refreshUrl = "http://localhost:5199/auth/refresh";
const usersMeUrl = "http://localhost:5199/users/me";
const cors = {
  "Access-Control-Allow-Origin": "*",
  "Access-Control-Allow-Headers": "authorization, content-type",
  "Access-Control-Allow-Methods": "GET, POST, PUT, OPTIONS",
};

/** Intercepts a `POST` and its cross-origin preflight, and keeps the bodies it receives. */
async function mockPost(page: Page, url: string, status: number, body: object) {
  const received: unknown[] = [];

  await page.route(url, (route) => {
    if (route.request().method() === "OPTIONS") {
      return route.fulfill({ status: 204, headers: cors });
    }
    received.push(route.request().postDataJSON());
    return route.fulfill({
      status,
      headers: cors,
      contentType: "application/json",
      body: JSON.stringify(body),
    });
  });

  return received;
}

/** Answers the first `GET /users/me` with `401`, as an expired access token does; later calls fall through. */
async function expireAccessTokenOnce(page: Page) {
  const authorizations: (string | null)[] = [];
  let expired = false;

  await page.route(usersMeUrl, async (route) => {
    const request = route.request();
    if (request.method() !== "GET") {
      return route.fallback();
    }
    authorizations.push(await request.headerValue("authorization"));
    if (expired) {
      return route.fallback();
    }
    expired = true;
    return route.fulfill({
      status: 401,
      headers: cors,
      contentType: "application/json",
      body: JSON.stringify({ title: "Unauthorized", status: 401 }),
    });
  });

  return authorizations;
}

async function logIn(page: Page) {
  await mockPost(page, loginUrl, 200, {
    accessToken: "access",
    refreshToken: "refresh",
    expiresIn: 900,
  });
  await page.goto("/login");
  await page.getByLabel("Email").fill("trainer@example.com");
  await page.getByLabel("Contraseña").fill("Str0ng!pass");
  await page.getByRole("button", { name: "Entrar" }).click();
}

test("renews an expired access token and shows the profile", async ({ page }) => {
  // Arrange
  await mockUsersMe(page);
  const authorizations = await expireAccessTokenOnce(page);
  const refreshes = await mockPost(page, refreshUrl, 200, {
    accessToken: "new-access",
    refreshToken: "new-refresh",
    expiresIn: 900,
  });

  // Act
  await logIn(page);

  // Assert
  await expect(page.getByRole("heading", { level: 1, name: "Trainera" })).toBeVisible();
  await expect(page.getByText("Ana García")).toBeVisible();
  await expect(page).toHaveURL("/");
  expect(refreshes).toEqual([{ refreshToken: "refresh" }]);
  expect(authorizations).toEqual(["Bearer access", "Bearer new-access"]);
});

test("sends the user to login when the refresh token is rejected", async ({ page }) => {
  // Arrange
  await mockUsersMe(page);
  await expireAccessTokenOnce(page);
  await mockPost(page, refreshUrl, 401, { title: "Unauthorized", status: 401 });

  // Act
  await logIn(page);

  // Assert
  await expect(page).toHaveURL(/\/login\/?$/);
  await expect(page.getByRole("heading", { level: 1, name: "Iniciar sesión" })).toBeVisible();
  await expect(page.getByRole("heading", { name: "Trainera" })).toHaveCount(0);
  await expect(page.getByText("Ana García")).toHaveCount(0);
});
