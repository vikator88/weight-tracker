import { screen } from "@testing-library/react-native";
import HomeView from "@/presentation/common/views/home-view";
import renderWithProviders from "../../../support/render-with-providers";

describe("HomeView", () => {
  it("shows the title", async () => {
    // Arrange & Act
    await renderWithProviders(<HomeView />);

    // Assert
    expect(screen.getByRole("heading", { name: "Weight Tracker" })).toBeOnTheScreen();
  });
});
