import { expect, test } from "@playwright/test";

test("serves the home page from the static export", async ({ page }) => {
  // Arrange
  await page.route("http://localhost:5199/**", (route) => route.abort());

  // Act
  await page.goto("/");

  // Assert
  await expect(page.getByRole("heading", { level: 1, name: "Weight Tracker" })).toBeVisible();
});
