import { screen, waitFor, within } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { afterEach, describe, expect, it, vi } from "vitest";
import { DomainError, fail, ok } from "@weight-tracker/core/domain";
import type { Result } from "@weight-tracker/core/domain";
import i18n from "@/infrastructure/i18n";
import LanguageMenu from "@/presentation/common/components/language-menu";
import renderWithProviders from "../../../support/render-with-providers";

function renderLanguageMenu(
  store: () => Promise<Result<void, DomainError>> = async () => ok(undefined),
) {
  const execute = vi.fn(store);

  return {
    execute,
    rendered: renderWithProviders(<LanguageMenu />, {
      setPreferredLanguageUseCase: () => ({ execute }),
    }),
  };
}

describe("LanguageMenu", () => {
  afterEach(async () => {
    // The i18n instance is shared between tests.
    await i18n.changeLanguage("es");
  });

  it("shows the current language and marks it in the menu", async () => {
    // Arrange
    await renderLanguageMenu().rendered;
    const button = screen.getByRole("button", { name: "Cambiar idioma" });

    // Act
    await userEvent.setup().click(button);

    // Assert
    expect(button).toHaveTextContent("ES");
    const menu = await screen.findByRole("menu");
    expect(within(menu).getByRole("menuitem", { name: "Español" })).toHaveClass("Mui-selected");
    expect(within(menu).getByRole("menuitem", { name: "English" })).not.toHaveClass("Mui-selected");
  });

  it("switches to English and stores the choice", async () => {
    // Arrange
    const { execute, rendered } = renderLanguageMenu();
    await rendered;
    const user = userEvent.setup();
    await user.click(screen.getByRole("button", { name: "Cambiar idioma" }));

    // Act
    await user.click(await screen.findByRole("menuitem", { name: "English" }));

    // Assert
    await waitFor(() =>
      expect(screen.getByRole("button", { name: "Change language" })).toHaveTextContent("EN"),
    );
    expect(execute).toHaveBeenCalledWith("en");
  });

  it("still switches when the choice cannot be stored", async () => {
    // Arrange
    const { rendered } = renderLanguageMenu(async () => fail(new DomainError("UNEXPECTED", "")));
    await rendered;
    const user = userEvent.setup();
    await user.click(screen.getByRole("button", { name: "Cambiar idioma" }));

    // Act
    await user.click(await screen.findByRole("menuitem", { name: "English" }));

    // Assert
    await waitFor(() => expect(i18n.language).toBe("en"));
    expect(screen.queryByRole("alert")).not.toBeInTheDocument();
  });
});
