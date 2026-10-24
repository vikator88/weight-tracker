import { screen, waitFor } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { beforeEach, describe, expect, it, vi } from "vitest";
import { DomainError, fail, ok } from "@weight-tracker/core/domain";
import type { Result, UpdateProfileRequest, UserProfile } from "@weight-tracker/core/domain";
import ProfileView from "@/presentation/profile/views/profile-view";
import renderWithProviders, { completeProfile } from "../../../support/render-with-providers";

const replace = vi.fn();
const push = vi.fn();

vi.mock("next/navigation", () => ({
  useRouter: () => ({ replace, push }),
  usePathname: () => "/profile",
}));

function renderProfileView({
  profile = completeProfile,
  update = async (request: UpdateProfileRequest) => ok({ ...completeProfile, ...request }),
}: {
  profile?: UserProfile;
  update?: (request: UpdateProfileRequest) => Promise<Result<UserProfile, DomainError>>;
} = {}) {
  const execute = vi.fn(update);

  return {
    execute,
    rendered: renderWithProviders(<ProfileView />, {
      getSessionUseCase: () => ({
        execute: async () => ok({ accessToken: "a", refreshToken: "r" }),
      }),
      getMyProfileUseCase: () => ({ execute: async () => ok(profile) }),
      updateMyProfileUseCase: () => ({ execute }),
    }),
  };
}

describe("ProfileView", () => {
  beforeEach(() => {
    replace.mockClear();
    push.mockClear();
  });

  it("shows the title and the fields filled with the current name", async () => {
    // Arrange & Act
    await renderProfileView().rendered;

    // Assert
    expect(await screen.findByRole("heading", { level: 1, name: "Mi perfil" })).toBeInTheDocument();
    expect(screen.getByRole("textbox", { name: "Nombre" })).toHaveValue("Ana");
    expect(screen.getByRole("textbox", { name: "Apellidos" })).toHaveValue("García");
    expect(screen.getByRole("button", { name: "Guardar" })).toBeInTheDocument();
  });

  it("shows the length message and sends nothing when the name is too short", async () => {
    // Arrange
    const { execute, rendered } = renderProfileView();
    await rendered;
    const user = userEvent.setup();
    const name = await screen.findByRole("textbox", { name: "Nombre" });
    await user.clear(name);
    await user.type(name, "Al");

    // Act
    await user.click(screen.getByRole("button", { name: "Guardar" }));

    // Assert
    expect(await screen.findByText("Debe tener al menos 3 caracteres.")).toBeInTheDocument();
    expect(execute).not.toHaveBeenCalled();
  });

  it("shows the required message when the surname is empty", async () => {
    // Arrange
    const { execute, rendered } = renderProfileView();
    await rendered;
    const user = userEvent.setup();
    await user.clear(await screen.findByRole("textbox", { name: "Apellidos" }));

    // Act
    await user.click(screen.getByRole("button", { name: "Guardar" }));

    // Assert
    expect(await screen.findByText("Este campo es obligatorio.")).toBeInTheDocument();
    expect(execute).not.toHaveBeenCalled();
  });

  it("shows the maximum message when the surname is longer than 100 characters", async () => {
    // Arrange
    const { execute, rendered } = renderProfileView();
    await rendered;
    const user = userEvent.setup();
    const surname = await screen.findByRole("textbox", { name: "Apellidos" });
    await user.clear(surname);
    await user.click(surname);
    await user.paste("a".repeat(101));

    // Act
    await user.click(screen.getByRole("button", { name: "Guardar" }));

    // Assert
    expect(await screen.findByText("No puede superar los 100 caracteres.")).toBeInTheDocument();
    expect(execute).not.toHaveBeenCalled();
  });

  it("saves the trimmed values, confirms, and updates the header", async () => {
    // Arrange
    const { execute, rendered } = renderProfileView();
    await rendered;
    const user = userEvent.setup();
    const name = await screen.findByRole("textbox", { name: "Nombre" });
    await user.clear(name);
    await user.type(name, " Lucía ");

    // Act
    await user.click(screen.getByRole("button", { name: "Guardar" }));

    // Assert
    expect(await screen.findByText("Perfil actualizado.")).toBeInTheDocument();
    expect(execute).toHaveBeenCalledWith({ name: "Lucía", surname: "García" });
    expect(screen.getByText("Lucía García")).toBeInTheDocument();
  });

  it("hides the confirmation when a field changes", async () => {
    // Arrange
    await renderProfileView().rendered;
    const user = userEvent.setup();
    await user.click(await screen.findByRole("button", { name: "Guardar" }));
    await screen.findByText("Perfil actualizado.");

    // Act
    await user.type(screen.getByRole("textbox", { name: "Nombre" }), "x");

    // Assert
    expect(screen.queryByText("Perfil actualizado.")).not.toBeInTheDocument();
  });

  it.each([
    ["VALIDATION", "Revisa los datos introducidos."],
    ["NETWORK", "No hay conexión con el servidor."],
  ] as const)(
    "shows the %s message and keeps the typed values when saving fails",
    async (code, message) => {
      // Arrange
      await renderProfileView({ update: async () => fail(new DomainError(code, "")) }).rendered;
      const user = userEvent.setup();
      const name = await screen.findByRole("textbox", { name: "Nombre" });
      await user.clear(name);
      await user.type(name, "Lucía");

      // Act
      await user.click(screen.getByRole("button", { name: "Guardar" }));

      // Assert
      expect(await screen.findByText(message)).toBeInTheDocument();
      expect(screen.getByRole("textbox", { name: "Nombre" })).toHaveValue("Lucía");
      expect(screen.queryByText("Perfil actualizado.")).not.toBeInTheDocument();
    },
  );

  it("disables Guardar while saving", async () => {
    // Arrange
    await renderProfileView({ update: () => new Promise(() => {}) }).rendered;
    const user = userEvent.setup();
    const save = await screen.findByRole("button", { name: "Guardar" });

    // Act
    await user.click(save);

    // Assert
    await waitFor(() => expect(save).toBeDisabled());
  });

  it("sends a user in onboarding to the home screen and hides the form", async () => {
    // Arrange & Act
    await renderProfileView({ profile: { ...completeProfile, name: "" } }).rendered;

    // Assert
    await waitFor(() => expect(replace).toHaveBeenCalledWith("/"));
    expect(screen.queryByRole("textbox", { name: "Nombre" })).not.toBeInTheDocument();
  });
});
