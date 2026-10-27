import { screen, within } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { beforeEach, describe, expect, it, vi } from "vitest";
import NavigationDrawer from "@/presentation/common/components/organisms/navigation-drawer";
import renderWithProviders from "../../../../support/render-with-providers";

const replace = vi.fn();
const navigation = vi.hoisted(() => ({ pathname: "/" }));

vi.mock("next/navigation", () => ({
  useRouter: () => ({ replace }),
  usePathname: () => navigation.pathname,
}));

const items = [
  "Inicio",
  "Todos los atletas",
  "Invitaciones pendientes",
  "Planes asignados",
  "Calendario",
  "Plantillas de entrenamiento",
  "Catálogo de ejercicios",
  "Feedback de entrenamientos",
  "Revisiones semanales",
  "Pendientes de revisión",
];

describe("NavigationDrawer", () => {
  beforeEach(() => {
    replace.mockClear();
    navigation.pathname = "/";
  });

  it("shows the app name row, then every section and item in order", async () => {
    // Arrange & Act
    await renderWithProviders(<NavigationDrawer />);

    // Assert
    const nav = screen.getByRole("navigation", { name: "Menú principal" });
    expect(
      within(nav)
        .getAllByText(/./)
        .map((element) => element.textContent),
    ).toEqual([
      "Weight Tracker",
      "Inicio",
      "Atletas",
      "Todos los atletas",
      "Invitaciones pendientes",
      "Programación",
      "Planes asignados",
      "Calendario",
      "Biblioteca",
      "Plantillas de entrenamiento",
      "Catálogo de ejercicios",
      "Seguimiento",
      "Feedback de entrenamientos",
      "Revisiones semanales",
      "Pendientes de revisión",
    ]);
  });

  it("shows the app name as a row that is not an entry", async () => {
    // Arrange & Act
    await renderWithProviders(<NavigationDrawer />);

    // Assert
    const appName = screen.getByText("Weight Tracker");
    expect(appName.closest("a, button, [role='button']")).toBeNull();
  });

  it("makes Inicio the first entry, linked to home and selected", async () => {
    // Arrange & Act
    await renderWithProviders(<NavigationDrawer />);

    // Assert
    const nav = screen.getByRole("navigation", { name: "Menú principal" });
    const [first] = within(nav).getAllByRole("link");
    expect(first).toHaveTextContent("Inicio");
    expect(first).toHaveAttribute("href", "/");
    expect(first).toHaveClass("Mui-selected");
  });

  it("links Planes asignados to its screen, not selected elsewhere", async () => {
    // Arrange & Act
    await renderWithProviders(<NavigationDrawer />);

    // Assert
    const entry = screen.getByRole("link", { name: "Planes asignados" });
    expect(entry).toHaveAttribute("href", "/assigned-plans");
    expect(entry).not.toHaveClass("Mui-selected");
  });

  it("selects Planes asignados and not Inicio on its screen", async () => {
    // Arrange
    navigation.pathname = "/assigned-plans";

    // Act
    await renderWithProviders(<NavigationDrawer />);

    // Assert
    expect(screen.getByRole("link", { name: "Planes asignados" })).toHaveClass("Mui-selected");
    expect(screen.getByRole("link", { name: "Inicio" })).not.toHaveClass("Mui-selected");
  });

  it("keeps every other item without a link", async () => {
    // Arrange & Act
    await renderWithProviders(<NavigationDrawer />);

    // Assert
    const nav = screen.getByRole("navigation", { name: "Menú principal" });
    expect(
      within(nav)
        .getAllByRole("link")
        .map((link) => link.textContent),
    ).toEqual(["Inicio", "Planes asignados"]);
  });

  it("shows an icon on every item", async () => {
    // Arrange & Act
    await renderWithProviders(<NavigationDrawer />);

    // Assert
    for (const item of items) {
      const entry = screen.getByText(item).closest("a, [role='button']") as HTMLElement;
      expect(within(entry).getByTestId(/Icon$/)).toBeInTheDocument();
    }
  });

  it("no longer shows the settings section or logout", async () => {
    // Arrange & Act
    await renderWithProviders(<NavigationDrawer />);

    // Assert
    for (const text of ["Configuración", "Mi perfil", "Preferencias", "Cerrar sesión"]) {
      expect(screen.queryByText(text)).not.toBeInTheDocument();
    }
  });

  it("does nothing when a section header or a placeholder item is clicked", async () => {
    // Arrange
    const execute = vi.fn();
    await renderWithProviders(<NavigationDrawer />, { logoutUseCase: () => ({ execute }) });
    const user = userEvent.setup();

    // Act
    await user.click(screen.getByText("Atletas"));
    await user.click(screen.getByRole("button", { name: "Todos los atletas" }));

    // Assert
    expect(execute).not.toHaveBeenCalled();
    expect(replace).not.toHaveBeenCalled();
  });
});
