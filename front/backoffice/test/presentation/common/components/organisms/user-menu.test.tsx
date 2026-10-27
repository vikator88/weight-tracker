import { screen, waitFor, within } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { beforeEach, describe, expect, it, vi } from "vitest";
import { DomainError, fail, ok } from "@weight-tracker/core/domain";
import type { Result, UserProfile } from "@weight-tracker/core/domain";
import UserMenu from "@/presentation/common/components/organisms/user-menu";
import LoadProfile from "../../../../support/load-profile";
import renderWithProviders, { completeProfile } from "../../../../support/render-with-providers";

const replace = vi.fn();
const push = vi.fn();

vi.mock("next/navigation", () => ({ useRouter: () => ({ replace, push }) }));

function renderUserMenu(
  logout: () => Promise<Result<void, DomainError>> = async () => ok(undefined),
  getProfile: () => Promise<Result<UserProfile, DomainError>> = async () => ok(completeProfile),
) {
  const execute = vi.fn(logout);

  return {
    execute,
    rendered: renderWithProviders(
      <LoadProfile>
        <UserMenu />
      </LoadProfile>,
      {
        logoutUseCase: () => ({ execute }),
        getMyProfileUseCase: () => ({ execute: getProfile }),
      },
    ),
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
    push.mockClear();
  });

  it("shows the user's name, surname, role and initials", async () => {
    // Arrange
    await renderUserMenu().rendered;

    // Act
    const name = await screen.findByText("Ana García");

    // Assert
    expect(name).toBeInTheDocument();
    expect(screen.getByText("Entrenador")).toBeInTheDocument();
    expect(screen.getByText("AG")).toBeInTheDocument();
  });

  it("shows the administrator role", async () => {
    // Arrange
    await renderUserMenu(undefined, async () => ok({ ...completeProfile, role: "ADMIN" })).rendered;

    // Act
    const role = await screen.findByText("Administrador");

    // Assert
    expect(role).toBeInTheDocument();
  });

  it("shows skeletons and no name while the profile is loading", async () => {
    // Arrange & Act
    await renderUserMenu(undefined, () => new Promise(() => {})).rendered;

    // Assert
    expect(screen.getByLabelText("Cargando")).toBeInTheDocument();
    expect(screen.queryByText("Ana García")).not.toBeInTheDocument();
    expect(screen.queryByText("Usuario de prueba")).not.toBeInTheDocument();
  });

  it("shows skeletons and no name when the profile failed to load", async () => {
    // Arrange & Act
    await renderUserMenu(undefined, async () => fail(new DomainError("NETWORK", ""))).rendered;

    // Assert
    expect(await screen.findByLabelText("Cargando")).toBeInTheDocument();
    expect(screen.queryByText("Entrenador")).not.toBeInTheDocument();
  });

  it.each([
    ["name", { name: "", surname: "García" }],
    ["surname", { name: "Ana", surname: "   " }],
  ])(
    "shows skeletons and no name while onboarding is pending (empty %s)",
    async (_field, names) => {
      // Arrange & Act
      await renderUserMenu(undefined, async () => ok({ ...completeProfile, ...names })).rendered;

      // Assert
      expect(await screen.findByLabelText("Cargando")).toBeInTheDocument();
      expect(screen.queryByText("Entrenador")).not.toBeInTheDocument();
    },
  );

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

  it("closes the menu and goes to the profile screen when Mi perfil is chosen", async () => {
    // Arrange
    await renderUserMenu().rendered;
    const { user, menu } = await openMenu();

    // Act
    await user.click(within(menu).getByRole("menuitem", { name: "Mi perfil" }));

    // Assert
    await waitFor(() => expect(screen.queryByRole("menu")).not.toBeInTheDocument());
    expect(push).toHaveBeenCalledWith("/profile");
  });

  it("closes the menu and does nothing when Preferencias is chosen", async () => {
    // Arrange
    const { execute, rendered } = renderUserMenu();
    await rendered;
    const { user, menu } = await openMenu();

    // Act
    await user.click(within(menu).getByRole("menuitem", { name: "Preferencias" }));

    // Assert
    await waitFor(() => expect(screen.queryByRole("menu")).not.toBeInTheDocument());
    expect(execute).not.toHaveBeenCalled();
    expect(replace).not.toHaveBeenCalled();
    expect(push).not.toHaveBeenCalled();
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
