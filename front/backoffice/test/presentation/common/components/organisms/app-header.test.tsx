import { screen } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { describe, expect, it, vi } from "vitest";
import AppHeader from "@/presentation/common/components/organisms/app-header";
import renderWithProviders from "../../../../support/render-with-providers";

const replace = vi.fn();

vi.mock("next/navigation", () => ({ useRouter: () => ({ replace }) }));

describe("AppHeader", () => {
  it("shows the search box with its icon and placeholder", async () => {
    // Arrange & Act
    await renderWithProviders(<AppHeader />);

    // Assert
    const search = screen.getByRole("textbox", { name: "Buscar" });
    expect(search).toHaveAttribute("placeholder", "Escribe para buscar...");
    expect(screen.getByTestId("SearchOutlinedIcon")).toBeInTheDocument();
  });

  it("takes the typed text and triggers nothing", async () => {
    // Arrange
    const execute = vi.fn();
    await renderWithProviders(<AppHeader />, {
      logoutUseCase: () => ({ execute }),
      setPreferredLanguageUseCase: () => ({ execute }),
    });

    // Act
    await userEvent.setup().type(screen.getByRole("textbox", { name: "Buscar" }), "Juan");

    // Assert
    expect(screen.getByRole("textbox", { name: "Buscar" })).toHaveValue("Juan");
    expect(execute).not.toHaveBeenCalled();
    expect(replace).not.toHaveBeenCalled();
  });

  it("shows the controls in the order language, alerts, user", async () => {
    // Arrange & Act
    await renderWithProviders(<AppHeader />);

    // Assert
    const labels = screen.getAllByRole("button").map((button) => button.getAttribute("aria-label"));
    expect(labels).toEqual(["Cambiar idioma", "Alertas", "Menú de usuario"]);
  });
});
