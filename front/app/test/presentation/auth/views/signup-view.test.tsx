import type { ReactNode } from "react";
import { fireEvent, screen, waitFor } from "@testing-library/react-native";
import {
  DomainError,
  fail,
  ok,
  type AuthResponse,
  type DomainErrorCode,
} from "@weight-tracker/core/domain";
import SignupView from "@/presentation/auth/views/signup-view";
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
  signupResult: ReturnType<typeof ok<AuthResponse>> | ReturnType<typeof fail<DomainError>> = ok(
    authResponse,
  ),
) {
  const signup = jest.fn(async (_credentials: { email: string; password: string }) => signupResult);
  const getSession = jest.fn(async () => ok(null));
  const dependencies = {
    signupUseCase: () => ({ execute: signup }),
    getSessionUseCase: () => ({ execute: getSession }),
  };

  return { signup, getSession, dependencies };
}

async function fillAndSubmit(email: string, password: string) {
  await fireEvent.changeText(screen.getByLabelText("Email"), email);
  await fireEvent.changeText(screen.getByLabelText("Contraseña"), password);
  await fireEvent.press(screen.getByRole("button", { name: "Crear cuenta" }));
}

describe("SignupView", () => {
  it("shows the title, the fields and the submit button", async () => {
    // Arrange
    const { dependencies } = setup();

    // Act
    await renderWithProviders(<SignupView />, dependencies);

    // Assert
    expect(screen.getByRole("heading", { name: "Crear cuenta" })).toBeOnTheScreen();
    expect(screen.getByLabelText("Email")).toBeOnTheScreen();
    expect(screen.getByLabelText("Contraseña")).toBeOnTheScreen();
    expect(screen.getByRole("button", { name: "Crear cuenta" })).toBeOnTheScreen();
  });

  it("shows both field errors and sends nothing when the form is empty", async () => {
    // Arrange
    const { signup, dependencies } = setup();
    await renderWithProviders(<SignupView />, dependencies);

    // Act
    await fireEvent.press(screen.getByRole("button", { name: "Crear cuenta" }));

    // Assert
    expect(await screen.findByText("Introduce tu email.")).toBeOnTheScreen();
    expect(screen.getByText("Introduce una contraseña.")).toBeOnTheScreen();
    expect(signup).not.toHaveBeenCalled();
  });

  it("shows the format error and sends nothing when the email is malformed", async () => {
    // Arrange
    const { signup, dependencies } = setup();
    await renderWithProviders(<SignupView />, dependencies);

    // Act
    await fillAndSubmit("not-an-email", "secret");

    // Assert
    expect(await screen.findByText("Introduce un email válido.")).toBeOnTheScreen();
    expect(signup).not.toHaveBeenCalled();
  });

  it("trims the values before sending", async () => {
    // Arrange
    const { signup, dependencies } = setup();
    await renderWithProviders(<SignupView />, dependencies);

    // Act
    await fillAndSubmit(" athlete@example.com ", " Str0ng!pass ");

    // Assert
    await waitFor(() =>
      expect(signup).toHaveBeenCalledWith({
        email: "athlete@example.com",
        password: "Str0ng!pass",
      }),
    );
  });

  it("sends a short password anyway because strength is the API's rule", async () => {
    // Arrange
    const { signup, dependencies } = setup();
    await renderWithProviders(<SignupView />, dependencies);

    // Act
    await fillAndSubmit("athlete@example.com", "a");

    // Assert
    await waitFor(() =>
      expect(signup).toHaveBeenCalledWith({ email: "athlete@example.com", password: "a" }),
    );
  });

  it("disables the submit button while the request is in flight", async () => {
    // Arrange
    const signup = jest.fn(() => new Promise<never>(() => {}));
    await renderWithProviders(<SignupView />, { signupUseCase: () => ({ execute: signup }) });

    // Act
    await fillAndSubmit("athlete@example.com", "secret");

    // Assert
    await waitFor(() =>
      expect(screen.getByRole("button", { name: "Crear cuenta" })).toBeDisabled(),
    );
  });

  it("reads the session again after a successful signup", async () => {
    // Arrange
    const { getSession, dependencies } = setup();
    await renderWithProviders(<SignupView />, dependencies);
    await waitFor(() => expect(getSession).toHaveBeenCalledTimes(1));

    // Act
    await fillAndSubmit("athlete@example.com", "secret");

    // Assert
    await waitFor(() => expect(getSession).toHaveBeenCalledTimes(2));
  });

  it.each([
    ["VALIDATION", "La contraseña no cumple los requisitos de seguridad."],
    ["UNAUTHORIZED", "El email no es válido."],
    ["CONFLICT", "Ya existe una cuenta con este email."],
    ["NETWORK", "No hay conexión con el servidor."],
  ] as [DomainErrorCode, string][])(
    "shows the message for %s and keeps the email",
    async (code, message) => {
      // Arrange
      const { dependencies } = setup(fail(new DomainError(code, "failed")));
      await renderWithProviders(<SignupView />, dependencies);

      // Act
      await fillAndSubmit("athlete@example.com", "secret");

      // Assert
      expect(await screen.findByText(message)).toBeOnTheScreen();
      expect(screen.getByLabelText("Email").props.value).toBe("athlete@example.com");
    },
  );

  it("links to the login screen, replacing signup in the history", async () => {
    // Arrange
    const { dependencies } = setup();
    mockLinks.length = 0;

    // Act
    await renderWithProviders(<SignupView />, dependencies);

    // Assert
    expect(
      screen.getByRole("button", { name: "¿Ya tienes cuenta? Inicia sesión" }),
    ).toBeOnTheScreen();
    expect(mockLinks).toContainEqual({ href: "/login", replace: true });
  });
});
