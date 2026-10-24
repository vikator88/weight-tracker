import { screen, waitFor, within } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { beforeEach, describe, expect, it, vi } from "vitest";
import { DomainError, fail, ok } from "@weight-tracker/core/domain";
import type { Result } from "@weight-tracker/core/domain";
import UserMenu from "@/presentation/common/components/user-menu";
import renderWithProviders from "../../../support/render-with-providers";

const replace = vi.fn();

vi.mock("next/navigation", () => ({ useRouter: () => ({ replace }) }));

function renderUserMenu(
  logout: () => Promise<Result<void, DomainError>> = async () => ok(undefined),
) {
  const execute = vi.fn(logout);

  return {
    execute,
    rendered: renderWithProviders(<UserMenu />, { logoutUseCase: () => ({ execute }) }),
  };
}

async function openMenu() {
  const user = userEvent.setup();
  await user.click(screen.getByRole("button", { name: "Menú de usuario" }));
  return { user, menu: await screen.findByRole("menu") };
}

describe("UserMenu", () => {
  beforeEach(() => {
    replace.mockClear();
  });

  it("shows the user's name, role and initials", async () => {
    // Arrange & Act
    await renderUserMenu().rendered;

    // Assert
    expect(screen.getByText("Usuario de prueba")).toBeInTheDocument();
    expect(screen.getByText("Entrenador")).toBeInTheDocument();
    expect(screen.getByText("UP")).toBeInTheDocument();
  });

  it("lists profile, preferences, a separator and logout, in that order", async () => {
    // Arrange
    await renderUserMenu().rendered;

    // Act
    const { menu } = await openMenu();

    // Assert
    expect(
      within(menu)
        .getAllByRole("menuitem")
        .map((item) => item.textContent),
    ).toEqual(["Mi perfil", "Preferencias", "Cerrar sesión"]);
    expect(within(menu).getByRole("separator")).toBeInTheDocument();
  });

  it("closes the menu and does nothing when a placeholder entry is chosen", async () => {
    // Arrange
    const { execute, rendered } = renderUserMenu();
    await rendered;
    const { user, menu } = await openMenu();

    // Act
    await user.click(within(menu).getByRole("menuitem", { name: "Mi perfil" }));

    // Assert
    await waitFor(() => expect(screen.queryByRole("menu")).not.toBeInTheDocument());
    expect(execute).not.toHaveBeenCalled();
    expect(replace).not.toHaveBeenCalled();
  });

  it("logs out and goes to login", async () => {
    // Arrange
    const { execute, rendered } = renderUserMenu();
    await rendered;
    const { user, menu } = await openMenu();

    // Act
    await user.click(within(menu).getByRole("menuitem", { name: "Cerrar sesión" }));

    // Assert
    await waitFor(() => expect(replace).toHaveBeenCalledWith("/login"));
    expect(execute).toHaveBeenCalledTimes(1);
  });

  it("disables the logout entry while logging out", async () => {
    // Arrange
    await renderUserMenu(() => new Promise(() => {})).rendered;
    const { user, menu } = await openMenu();
    const entry = within(menu).getByRole("menuitem", { name: "Cerrar sesión" });

    // Act
    await user.click(entry);

    // Assert
    await waitFor(() => expect(entry).toHaveAttribute("aria-disabled", "true"));
  });

  it("shows the error in the menu and stays when logging out fails", async () => {
    // Arrange
    await renderUserMenu(async () => fail(new DomainError("UNEXPECTED", ""))).rendered;
    const { user, menu } = await openMenu();

    // Act
    await user.click(within(menu).getByRole("menuitem", { name: "Cerrar sesión" }));

    // Assert
    expect(await within(menu).findByText("Ha ocurrido un error inesperado.")).toBeInTheDocument();
    expect(replace).not.toHaveBeenCalled();
  });
});
