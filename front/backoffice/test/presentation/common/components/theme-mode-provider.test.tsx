import { screen, waitFor } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { useTheme } from "@mui/material/styles";
import { describe, expect, it, vi } from "vitest";
import { DomainError, fail, ok } from "@weight-tracker/core/domain";
import type { Result } from "@weight-tracker/core/domain";
import ThemeModeProvider from "@/presentation/common/components/theme-mode-provider";
import ThemeModeSwitch from "@/presentation/common/components/organisms/theme-mode-switch";
import renderWithProviders from "../../../support/render-with-providers";

/** Shows the palette mode of the theme it renders in. */
function PaletteMode() {
  return <span data-testid="palette-mode">{useTheme().palette.mode}</span>;
}

async function renderThemeModeProvider(
  stored: Result<string | null, DomainError>,
  store: () => Promise<Result<void, DomainError>> = async () => ok(undefined),
) {
  const execute = vi.fn(store);
  await renderWithProviders(
    <ThemeModeProvider>
      <ThemeModeSwitch />
      <PaletteMode />
    </ThemeModeProvider>,
    {
      getPreferredThemeModeUseCase: () => ({ execute: async () => stored }),
      setPreferredThemeModeUseCase: () => ({ execute }),
    },
  );

  return { execute };
}

describe("ThemeModeProvider", () => {
  it("starts light when no mode is stored", async () => {
    // Arrange & Act
    await renderThemeModeProvider(ok(null));

    // Assert
    expect(await screen.findByRole("switch", { name: "Modo oscuro" })).not.toBeChecked();
    expect(screen.getByTestId("palette-mode")).toHaveTextContent("light");
  });

  it("starts in the stored dark mode", async () => {
    // Arrange & Act
    await renderThemeModeProvider(ok("dark"));

    // Assert
    const darkMode = await screen.findByRole("switch", { name: "Modo oscuro" });
    await waitFor(() => expect(darkMode).toBeChecked());
    expect(screen.getByTestId("palette-mode")).toHaveTextContent("dark");
  });

  it("ignores a stored mode the backoffice does not support", async () => {
    // Arrange & Act
    await renderThemeModeProvider(ok("sepia"));

    // Assert
    expect(await screen.findByRole("switch", { name: "Modo oscuro" })).not.toBeChecked();
    expect(screen.getByTestId("palette-mode")).toHaveTextContent("light");
  });

  it("starts light when the stored mode cannot be read", async () => {
    // Arrange & Act
    await renderThemeModeProvider(fail(new DomainError("UNEXPECTED", "")));

    // Assert
    expect(await screen.findByRole("switch", { name: "Modo oscuro" })).not.toBeChecked();
    expect(screen.getByTestId("palette-mode")).toHaveTextContent("light");
  });

  it("switches to dark and stores the choice", async () => {
    // Arrange
    const { execute } = await renderThemeModeProvider(ok(null));
    const darkMode = await screen.findByRole("switch", { name: "Modo oscuro" });

    // Act
    await userEvent.setup().click(darkMode);

    // Assert
    expect(darkMode).toBeChecked();
    expect(screen.getByTestId("palette-mode")).toHaveTextContent("dark");
    expect(execute).toHaveBeenCalledWith("dark");
  });

  it("switches back to light and stores the choice", async () => {
    // Arrange
    const { execute } = await renderThemeModeProvider(ok("dark"));
    const darkMode = await screen.findByRole("switch", { name: "Modo oscuro" });
    await waitFor(() => expect(darkMode).toBeChecked());

    // Act
    await userEvent.setup().click(darkMode);

    // Assert
    expect(darkMode).not.toBeChecked();
    expect(screen.getByTestId("palette-mode")).toHaveTextContent("light");
    expect(execute).toHaveBeenCalledWith("light");
  });

  it("stays dark when the choice cannot be stored", async () => {
    // Arrange
    await renderThemeModeProvider(ok(null), async () => fail(new DomainError("UNEXPECTED", "")));
    const darkMode = await screen.findByRole("switch", { name: "Modo oscuro" });

    // Act
    await userEvent.setup().click(darkMode);

    // Assert
    expect(darkMode).toBeChecked();
    expect(screen.getByTestId("palette-mode")).toHaveTextContent("dark");
  });
});
