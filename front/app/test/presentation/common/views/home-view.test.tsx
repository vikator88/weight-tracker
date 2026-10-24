import { fireEvent, screen, waitFor } from "@testing-library/react-native";
import { DomainError, fail, ok } from "@weight-tracker/core/domain";
import type { Result } from "@weight-tracker/core/domain";
import HomeView from "@/presentation/common/views/home-view";
import renderWithProviders from "../../../support/render-with-providers";

function setup(logoutResult: Promise<Result<void, DomainError>> = Promise.resolve(ok(undefined))) {
  const logout = jest.fn(() => logoutResult);
  const getSession = jest.fn(async () => ok(null));
  const dependencies = {
    logoutUseCase: () => ({ execute: logout }),
    getSessionUseCase: () => ({ execute: getSession }),
  };

  return { logout, getSession, dependencies };
}

describe("HomeView", () => {
  it("shows the title and the signed-in message", async () => {
    // Arrange & Act
    await renderWithProviders(<HomeView />);

    // Assert
    expect(screen.getByRole("heading", { name: "Weight Tracker" })).toBeOnTheScreen();
    expect(screen.getByText("Has iniciado sesión.")).toBeOnTheScreen();
  });

  it("shows the logout button", async () => {
    // Arrange & Act
    await renderWithProviders(<HomeView />);

    // Assert
    expect(screen.getByRole("button", { name: "Cerrar sesión" })).toBeOnTheScreen();
  });

  it("logs out and reads the session again when the button is pressed", async () => {
    // Arrange
    const { logout, getSession, dependencies } = setup();
    await renderWithProviders(<HomeView />, dependencies);
    await waitFor(() => expect(getSession).toHaveBeenCalledTimes(1));

    // Act
    await fireEvent.press(screen.getByRole("button", { name: "Cerrar sesión" }));

    // Assert
    await waitFor(() => expect(getSession).toHaveBeenCalledTimes(2));
    expect(logout).toHaveBeenCalledTimes(1);
  });

  it("disables the logout button while logging out", async () => {
    // Arrange
    const { dependencies } = setup(new Promise(() => {}));
    await renderWithProviders(<HomeView />, dependencies);

    // Act
    await fireEvent.press(screen.getByRole("button", { name: "Cerrar sesión" }));

    // Assert
    await waitFor(() =>
      expect(screen.getByRole("button", { name: "Cerrar sesión" })).toBeDisabled(),
    );
  });

  it("shows the error and does not read the session again when logging out fails", async () => {
    // Arrange
    const { getSession, dependencies } = setup(
      Promise.resolve(fail(new DomainError("UNEXPECTED", ""))),
    );
    await renderWithProviders(<HomeView />, dependencies);
    await waitFor(() => expect(getSession).toHaveBeenCalledTimes(1));

    // Act
    await fireEvent.press(screen.getByRole("button", { name: "Cerrar sesión" }));

    // Assert
    expect(await screen.findByText("Ha ocurrido un error inesperado.")).toBeOnTheScreen();
    expect(getSession).toHaveBeenCalledTimes(1);
  });
});
