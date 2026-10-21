import { screen } from "@testing-library/react";
import { describe, expect, it } from "vitest";
import HomeView from "@/presentation/common/views/home-view";
import renderWithProviders from "../../../support/render-with-providers";

describe("HomeView", () => {
  it("shows the title", async () => {
    // Arrange & Act
    await renderWithProviders(<HomeView />);

    // Assert
    expect(screen.getByRole("heading", { level: 1, name: "Weight Tracker" })).toBeInTheDocument();
  });
});
