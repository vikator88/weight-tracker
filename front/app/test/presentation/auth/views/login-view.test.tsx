import type { ReactNode } from "react";
import { fireEvent, screen, waitFor } from "@testing-library/react-native";
import {
  DomainError,
  fail,
  ok,
  type AuthResponse,
  type DomainErrorCode,
} from "@weight-tracker/core/domain";
import LoginView from "@/presentation/auth/views/login-view";
import renderWithProviders from "../../../support/render-with-providers";

const mockLinks: { href: string; replace?: boolean }[] = [];

jest.mock("expo-router", () => ({
  Link: ({ children, href, replace }: { children: ReactNode; href: string; replace?: boolean }) => {
    mockLinks.push({ href, replace });
    return children;
  },
}));

const authResponse: AuthResponse = {
  accessToken: "access",
  refreshToken: "refresh",
  expiresIn: 3600,
};

function setup(
  loginResult: ReturnType<typeof ok<AuthResponse>> | ReturnType<typeof fail<DomainError>> = ok(
    authResponse,
  ),
) {
  const login = jest.fn(async (_credentials: { email: string; password: string }) => loginResult);
  const getSession = jest.fn(async () => ok(null));
  const dependencies = {
    loginUseCase: () => ({ execute: login }),
    getSessionUseCase: () => ({ execute: getSession }),
  };

  return { login, getSession, dependencies };
}

async function fillAndSubmit(email: string, password: string) {
  await fireEvent.changeText(screen.getByLabelText("Email"), email);
  await fireEvent.changeText(screen.getByLabelText("Contraseña"), password);
  await fireEvent.press(screen.getByRole("button", { name: "Entrar" }));
}

describe("LoginView", () => {
  it("shows the title, the fields and the submit button", async () => {
    // Arrange
    const { dependencies } = setup();

    // Act
    await renderWithProviders(<LoginView />, dependencies);

    // Assert
    expect(screen.getByRole("heading", { name: "Iniciar sesión" })).toBeOnTheScreen();
    expect(screen.getByLabelText("Email")).toBeOnTheScreen();
    expect(screen.getByLabelText("Contraseña")).toBeOnTheScreen();
    expect(screen.getByRole("button", { name: "Entrar" })).toBeOnTheScreen();
  });

  it("shows both field errors and sends nothing when the form is empty", async () => {
    // Arrange
    const { login, dependencies } = setup();
    await renderWithProviders(<LoginView />, dependencies);

    // Act
    await fireEvent.press(screen.getByRole("button", { name: "Entrar" }));

    // Assert
    expect(await screen.findByText("Introduce tu email.")).toBeOnTheScreen();
    expect(screen.getByText("Introduce tu contraseña.")).toBeOnTheScreen();
    expect(login).not.toHaveBeenCalled();
  });

  it("shows the format error and sends nothing when the email is malformed", async () => {
    // Arrange
    const { login, dependencies } = setup();
    await renderWithProviders(<LoginView />, dependencies);

    // Act
    await fillAndSubmit("not-an-email", "secret");

    // Assert
    expect(await screen.findByText("Introduce un email válido.")).toBeOnTheScreen();
    expect(login).not.toHaveBeenCalled();
  });

  it("trims the values before sending", async () => {
    // Arrange
    const { login, dependencies } = setup();
    await renderWithProviders(<LoginView />, dependencies);

    // Act
    await fillAndSubmit(" athlete@example.com ", " Str0ng!pass ");

    // Assert
    await waitFor(() =>
      expect(login).toHaveBeenCalledWith({
        email: "athlete@example.com",
        password: "Str0ng!pass",
      }),
    );
  });

  it("disables the submit button while the request is in flight", async () => {
    // Arrange
    const login = jest.fn(() => new Promise<never>(() => {}));
    await renderWithProviders(<LoginView />, { loginUseCase: () => ({ execute: login }) });

    // Act
    await fillAndSubmit("athlete@example.com", "secret");

    // Assert
    await waitFor(() => expect(screen.getByRole("button", { name: "Entrar" })).toBeDisabled());
  });

  it("reads the session again after a successful login", async () => {
    // Arrange
    const { getSession, dependencies } = setup();
    await renderWithProviders(<LoginView />, dependencies);
    await waitFor(() => expect(getSession).toHaveBeenCalledTimes(1));

    // Act
    await fillAndSubmit("athlete@example.com", "secret");

    // Assert
    await waitFor(() => expect(getSession).toHaveBeenCalledTimes(2));
  });

  it.each([
    ["UNAUTHORIZED", "Email o contraseña incorrectos."],
    ["NETWORK", "No hay conexión con el servidor."],
  ] as [DomainErrorCode, string][])(
    "shows the message for %s and keeps the email",
    async (code, message) => {
      // Arrange
      const { dependencies } = setup(fail(new DomainError(code, "failed")));
      await renderWithProviders(<LoginView />, dependencies);

      // Act
      await fillAndSubmit("athlete@example.com", "secret");

      // Assert
      expect(await screen.findByText(message)).toBeOnTheScreen();
      expect(screen.getByLabelText("Email").props.value).toBe("athlete@example.com");
    },
  );
});
