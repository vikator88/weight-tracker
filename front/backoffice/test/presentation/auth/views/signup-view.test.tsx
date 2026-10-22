import { screen, waitFor } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { beforeEach, describe, expect, it, vi } from "vitest";
import {
  type AuthResponse,
  DomainError,
  type DomainErrorCode,
  fail,
  ok,
} from "@weight-tracker/core/domain";
import type { Result } from "@weight-tracker/core/domain";
import SignupView from "@/presentation/auth/views/signup-view";
import renderWithProviders from "../../../support/render-with-providers";

const replace = vi.fn();

vi.mock("next/navigation", () => ({ useRouter: () => ({ replace }) }));

const tokens: AuthResponse = { accessToken: "access", refreshToken: "refresh", expiresIn: 900 };

function renderSignup(result: Result<AuthResponse, DomainError> | Promise<never> = ok(tokens)) {
  const execute = vi.fn().mockResolvedValue(result);

  return {
    execute,
    rendered: renderWithProviders(<SignupView />, { signupUseCase: () => ({ execute }) }),
  };
}

async function fillAndSubmit(email: string, password: string) {
  const user = userEvent.setup();
  if (email) await user.type(screen.getByLabelText("Email"), email);
  if (password) await user.type(screen.getByLabelText("Contraseña"), password);
  await user.click(screen.getByRole("button", { name: "Crear cuenta" }));
}

describe("SignupView", () => {
  beforeEach(() => {
    replace.mockClear();
  });

  it("shows field errors and sends nothing when the form is empty", async () => {
    // Arrange
    const { execute, rendered } = renderSignup();
    await rendered;

    // Act
    await fillAndSubmit("", "");

    // Assert
    expect(screen.getByText("Introduce tu email.")).toBeInTheDocument();
    expect(screen.getByText("Introduce una contraseña.")).toBeInTheDocument();
    expect(execute).not.toHaveBeenCalled();
  });

  it("shows a format error and sends nothing when the email is malformed", async () => {
    // Arrange
    const { execute, rendered } = renderSignup();
    await rendered;

    // Act
    await fillAndSubmit("not-an-email", "secret");

    // Assert
    expect(screen.getByText("Introduce un email válido.")).toBeInTheDocument();
    expect(execute).not.toHaveBeenCalled();
  });

  it("sends a short password as typed", async () => {
    // Arrange
    const { execute, rendered } = renderSignup();
    await rendered;

    // Act
    await fillAndSubmit("trainer@example.com", "a");

    // Assert
    expect(execute).toHaveBeenCalledWith({ email: "trainer@example.com", password: "a" });
  });

  it("replaces the route with home on success", async () => {
    // Arrange
    const { rendered } = renderSignup();
    await rendered;

    // Act
    await fillAndSubmit("trainer@example.com", "Str0ng!pass");

    // Assert
    await waitFor(() => expect(replace).toHaveBeenCalledWith("/"));
  });

  it.each([
    ["VALIDATION", "La contraseña no cumple los requisitos de seguridad."],
    ["UNAUTHORIZED", "El email no es válido."],
    ["CONFLICT", "Ya existe una cuenta con este email."],
    ["NETWORK", "No hay conexión con el servidor."],
  ] as [DomainErrorCode, string][])(
    "shows the %s message and keeps the email",
    async (code, message) => {
      // Arrange
      const { rendered } = renderSignup(fail(new DomainError(code, "")));
      await rendered;

      // Act
      await fillAndSubmit("trainer@example.com", "Str0ng!pass");

      // Assert
      expect(await screen.findByText(message)).toBeInTheDocument();
      expect(screen.getByLabelText("Email")).toHaveValue("trainer@example.com");
      expect(replace).not.toHaveBeenCalled();
    },
  );

  it("disables the button while the request is pending", async () => {
    // Arrange
    const { rendered } = renderSignup(new Promise<never>(() => {}));
    await rendered;

    // Act
    await fillAndSubmit("trainer@example.com", "Str0ng!pass");

    // Assert
    expect(screen.getByRole("button", { name: "Crear cuenta" })).toBeDisabled();
  });

  it("links to the login screen", async () => {
    // Arrange
    const { rendered } = renderSignup();

    // Act
    await rendered;

    // Assert
    expect(screen.getByRole("link", { name: "¿Ya tienes cuenta? Inicia sesión" })).toHaveAttribute(
      "href",
      "/login",
    );
  });
});
