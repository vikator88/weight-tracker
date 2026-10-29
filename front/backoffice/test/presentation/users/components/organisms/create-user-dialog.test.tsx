import { useState } from "react";
import { act, fireEvent, screen, waitFor, within } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { describe, expect, it, vi } from "vitest";
import { DomainError, fail, ok } from "@weight-tracker/core/domain";
import type { CreatedUserResponse, CreateUserRequest, Result } from "@weight-tracker/core/domain";
import i18n from "@/infrastructure/i18n";
import CreateUserDialog from "@/presentation/users/components/organisms/create-user-dialog";
import renderWithProviders from "../../../../support/render-with-providers";

const created: CreatedUserResponse = {
  id: "7a2b3c4d-1e2f-4a5b-9c8d-0e1f2a3b4c5d",
  email: "luis@example.com",
  name: "Luis",
  surname: "Pérez",
  dateBirth: "1990-05-17",
  role: "TRAINER",
};

/** Stands in for the users screen: owns the open state and can open the dialog again. */
function Harness({ onCreated }: { onCreated: () => void }) {
  const [open, setOpen] = useState(true);

  return (
    <>
      <button type="button" onClick={() => setOpen(true)}>
        Abrir
      </button>
      <CreateUserDialog open={open} onClose={() => setOpen(false)} onCreated={onCreated} />
    </>
  );
}

function renderDialog({
  createUser = async () => ok(created),
}: {
  createUser?: (request: CreateUserRequest) => Promise<Result<CreatedUserResponse, DomainError>>;
} = {}) {
  const execute = vi.fn(createUser);
  const onCreated = vi.fn();

  renderWithProviders(<Harness onCreated={onCreated} />, {
    getSessionUseCase: () => ({
      execute: async () => ok({ accessToken: "a", refreshToken: "r" }),
    }),
    createUserUseCase: () => ({ execute }),
  });
  return { execute, onCreated };
}

const openedDialog = () => screen.findByRole("dialog", { name: "Nuevo usuario" });
const emailField = () => screen.getByRole("textbox", { name: "Email" });
const passwordField = () => screen.getByLabelText("Contraseña");
const dateField = () => screen.getByLabelText("Fecha de nacimiento");
const roleField = () => screen.getByRole("combobox", { name: "Perfil" });

async function fillRequired(user: ReturnType<typeof userEvent.setup>) {
  await user.type(emailField(), "luis@example.com");
  await user.type(passwordField(), "Str0ng!Passw0rd");
}

async function pickRole(user: ReturnType<typeof userEvent.setup>, name: string) {
  await user.click(roleField());
  await user.click(await screen.findByRole("option", { name }));
}

describe("CreateUserDialog", () => {
  it("asks for the user's data with empty fields and the Usuario profile", async () => {
    // Arrange & Act
    renderDialog();

    // Assert
    await openedDialog();
    expect(emailField()).toHaveValue("");
    expect(passwordField()).toHaveValue("");
    expect(roleField()).toHaveTextContent("Usuario");
    expect(screen.getByRole("textbox", { name: "Nombre" })).toHaveValue("");
    expect(screen.getByRole("textbox", { name: "Apellidos" })).toHaveValue("");
    expect(dateField()).toHaveValue("");
    expect(screen.getByRole("button", { name: "Cancelar" })).toBeInTheDocument();
    expect(screen.getByRole("button", { name: "Crear usuario" })).toBeInTheDocument();
  });

  it("offers the three profiles", async () => {
    // Arrange
    renderDialog();
    await openedDialog();
    const user = userEvent.setup();

    // Act
    await user.click(roleField());

    // Assert
    const options = within(await screen.findByRole("listbox")).getAllByRole("option");
    expect(options.map((option) => option.textContent)).toEqual([
      "Usuario",
      "Entrenador",
      "Administrador",
    ]);
  });

  it("reports empty email and password and sends nothing", async () => {
    // Arrange
    const { execute } = renderDialog();
    await openedDialog();
    const user = userEvent.setup();

    // Act
    await user.click(screen.getByRole("button", { name: "Crear usuario" }));

    // Assert
    expect(await screen.findAllByText("Este campo es obligatorio.")).toHaveLength(2);
    expect(emailField()).toHaveAccessibleDescription("Este campo es obligatorio.");
    expect(passwordField()).toHaveAccessibleDescription("Este campo es obligatorio.");
    expect(execute).not.toHaveBeenCalled();
  });

  it("reports a malformed email and sends nothing", async () => {
    // Arrange
    const { execute } = renderDialog();
    await openedDialog();
    const user = userEvent.setup();
    await user.type(emailField(), "luis.example.com");
    await user.type(passwordField(), "Str0ng!Passw0rd");

    // Act
    await user.click(screen.getByRole("button", { name: "Crear usuario" }));

    // Assert
    expect(await screen.findByText("Introduce un email válido.")).toBeInTheDocument();
    expect(execute).not.toHaveBeenCalled();
  });

  it("reports a name longer than 100 characters and sends nothing", async () => {
    // Arrange
    const { execute } = renderDialog();
    await openedDialog();
    const user = userEvent.setup();
    await fillRequired(user);
    await user.click(screen.getByRole("textbox", { name: "Nombre" }));
    await user.paste("a".repeat(101));

    // Act
    await user.click(screen.getByRole("button", { name: "Crear usuario" }));

    // Assert
    expect(await screen.findByRole("textbox", { name: "Nombre" })).toHaveAccessibleDescription(
      "No puede superar los 100 caracteres.",
    );
    expect(execute).not.toHaveBeenCalled();
  });

  it("sends only the required fields when the optional ones are empty", async () => {
    // Arrange
    const { execute } = renderDialog();
    await openedDialog();
    const user = userEvent.setup();
    await fillRequired(user);
    await pickRole(user, "Entrenador");

    // Act
    await user.click(screen.getByRole("button", { name: "Crear usuario" }));

    // Assert
    await waitFor(() => expect(execute).toHaveBeenCalledTimes(1));
    expect(execute).toHaveBeenCalledWith({
      email: "luis@example.com",
      password: "Str0ng!Passw0rd",
      role: 2,
    });
  });

  it("sends every field, with name and surname trimmed", async () => {
    // Arrange
    const { execute } = renderDialog();
    await openedDialog();
    const user = userEvent.setup();
    await fillRequired(user);
    await pickRole(user, "Administrador");
    await user.type(screen.getByRole("textbox", { name: "Nombre" }), "  Luis  ");
    await user.type(screen.getByRole("textbox", { name: "Apellidos" }), " Pérez ");
    fireEvent.change(dateField(), { target: { value: "1990-05-17" } });

    // Act
    await user.click(screen.getByRole("button", { name: "Crear usuario" }));

    // Assert
    await waitFor(() =>
      expect(execute).toHaveBeenCalledWith({
        email: "luis@example.com",
        password: "Str0ng!Passw0rd",
        role: 3,
        name: "Luis",
        surname: "Pérez",
        dateBirth: "1990-05-17",
      }),
    );
  });

  it("disables Crear usuario while the user is being created", async () => {
    // Arrange
    renderDialog({ createUser: () => new Promise(() => {}) });
    await openedDialog();
    const user = userEvent.setup();
    await fillRequired(user);

    // Act
    await user.click(screen.getByRole("button", { name: "Crear usuario" }));

    // Assert
    expect(await screen.findByRole("button", { name: "Crear usuario" })).toBeDisabled();
  });

  it("reports the creation and closes", async () => {
    // Arrange
    const { onCreated } = renderDialog();
    await openedDialog();
    const user = userEvent.setup();
    await fillRequired(user);

    // Act
    await user.click(screen.getByRole("button", { name: "Crear usuario" }));

    // Assert
    await waitFor(() => expect(onCreated).toHaveBeenCalledTimes(1));
    await waitFor(() => expect(screen.queryByRole("dialog")).not.toBeInTheDocument());
  });

  it.each([
    ["CONFLICT", "Ya existe un usuario con este email."],
    ["VALIDATION", "Revisa los datos. La contraseña debe cumplir los requisitos de seguridad."],
    ["FORBIDDEN", "No tienes permiso para realizar esta acción."],
  ] as const)("keeps the dialog and the values when the API answers %s", async (code, message) => {
    // Arrange
    const { onCreated } = renderDialog({
      createUser: async () => fail(new DomainError(code, "")),
    });
    await openedDialog();
    const user = userEvent.setup();
    await fillRequired(user);

    // Act
    await user.click(screen.getByRole("button", { name: "Crear usuario" }));

    // Assert
    expect(await screen.findByText(message)).toBeInTheDocument();
    expect(screen.getByRole("dialog", { name: "Nuevo usuario" })).toBeInTheDocument();
    expect(emailField()).toHaveValue("luis@example.com");
    expect(onCreated).not.toHaveBeenCalled();
  });

  it("closes on Cancelar without a request and opens again empty", async () => {
    // Arrange
    const { execute } = renderDialog();
    await openedDialog();
    const user = userEvent.setup();
    await user.type(emailField(), "luis@example.com");
    await user.click(screen.getByRole("button", { name: "Cancelar" }));
    await waitFor(() => expect(screen.queryByRole("dialog")).not.toBeInTheDocument());

    // Act
    await user.click(screen.getByRole("button", { name: "Abrir" }));

    // Assert
    await openedDialog();
    expect(emailField()).toHaveValue("");
    expect(execute).not.toHaveBeenCalled();
  });

  it("follows the active language", async () => {
    // Arrange
    renderDialog();
    await openedDialog();

    // Act
    await act(() => i18n.changeLanguage("en"));

    // Assert
    expect(screen.getByRole("dialog", { name: "New user" })).toBeInTheDocument();
    expect(screen.getByLabelText("Password")).toBeInTheDocument();
    expect(screen.getByLabelText("Date of birth")).toBeInTheDocument();
    expect(screen.getByRole("combobox", { name: "Profile" })).toHaveTextContent("User");
    expect(screen.getByRole("button", { name: "Create user" })).toBeInTheDocument();
  });
});
