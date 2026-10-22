import type { ReactNode } from "react";
import { screen } from "@testing-library/react-native";
import NotFoundView from "@/presentation/common/views/not-found-view";
import renderWithProviders from "../../../support/render-with-providers";

jest.mock("expo-router", () => ({
  Link: ({ children }: { children: ReactNode }) => children,
}));

describe("NotFoundView", () => {
  it("shows the message and the link to the home", async () => {
    // Arrange & Act
    await renderWithProviders(<NotFoundView />);

    // Assert
    expect(screen.getByRole("heading", { name: "Esta pantalla no existe." })).toBeOnTheScreen();
    expect(screen.getByRole("button", { name: "Ir al inicio" })).toBeOnTheScreen();
  });
});
