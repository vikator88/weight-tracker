import { screen, waitFor } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { describe, expect, it, vi } from "vitest";
import { DomainError, fail, ok } from "@weight-tracker/core/domain";
import type { Result, UpdateProfileRequest, UserProfile } from "@weight-tracker/core/domain";
import { nameStep } from "@/presentation/onboarding/components/name-step";
import OnboardingDialog from "@/presentation/onboarding/components/onboarding-dialog";
import {
  validateNameFields,
  type OnboardingStep,
} from "@/presentation/onboarding/hooks/use-onboarding";
import renderWithProviders, { completeProfile } from "../../../support/render-with-providers";

function renderDialog(
  update: (request: UpdateProfileRequest) => Promise<Result<UserProfile, DomainError>> = async (
    request,
  ) => ok({ ...completeProfile, ...request }),
  steps?: OnboardingStep[],
) {
  const execute = vi.fn(update);

  return {
    execute,
    rendered: renderWithProviders(<OnboardingDialog steps={steps} />, {
      updateMyProfileUseCase: () => ({ execute }),
    }),
  };
}

/** Two steps over the same fields: the first checks only the name, the second checks both. */
const twoSteps: OnboardingStep[] = [
  {
    ...nameStep,
    id: "first",
    validate: (values) => {
      const { name } = validateNameFields(values);
      return name ? { name } : {};
    },
  },
  { ...nameStep, id: "second" },
];

describe("OnboardingDialog", () => {
  it("shows the step title and only the Finalizar action when there is one step", async () => {
    // Arrange & Act
    await renderDialog().rendered;

    // Assert
    expect(
      await screen.findByRole("dialog", { name: "Paso 1 de 1 · Dinos cómo te llamas" }),
    ).toBeInTheDocument();
    expect(screen.getByRole("textbox", { name: "Nombre" })).toBeInTheDocument();
    expect(screen.getByRole("textbox", { name: "Apellidos" })).toBeInTheDocument();
    expect(screen.getByRole("button", { name: "Finalizar" })).toBeInTheDocument();
    expect(screen.queryByRole("button", { name: "Atrás" })).not.toBeInTheDocument();
    expect(screen.queryByRole("button", { name: "Siguiente" })).not.toBeInTheDocument();
  });

  it("shows the required message under each empty field and sends nothing", async () => {
    // Arrange
    const { execute, rendered } = renderDialog();
    await rendered;
    const user = userEvent.setup();

    // Act
    await user.click(await screen.findByRole("button", { name: "Finalizar" }));

    // Assert
    expect(await screen.findAllByText("Este campo es obligatorio.")).toHaveLength(2);
    expect(execute).not.toHaveBeenCalled();
  });

  it("shows the minimum message when a value has fewer than 3 characters", async () => {
    // Arrange
    const { execute, rendered } = renderDialog();
    await rendered;
    const user = userEvent.setup();
    await user.type(await screen.findByRole("textbox", { name: "Nombre" }), "Al");
    await user.type(screen.getByRole("textbox", { name: "Apellidos" }), "García");

    // Act
    await user.click(screen.getByRole("button", { name: "Finalizar" }));

    // Assert
    expect(await screen.findByText("Debe tener al menos 3 caracteres.")).toBeInTheDocument();
    expect(execute).not.toHaveBeenCalled();
  });

  it("shows the maximum message when a value has more than 100 characters", async () => {
    // Arrange
    const { execute, rendered } = renderDialog();
    await rendered;
    const user = userEvent.setup();
    await user.type(await screen.findByRole("textbox", { name: "Nombre" }), "Ana");
    await user.click(screen.getByRole("textbox", { name: "Apellidos" }));
    await user.paste("a".repeat(101));

    // Act
    await user.click(screen.getByRole("button", { name: "Finalizar" }));

    // Assert
    expect(await screen.findByText("No puede superar los 100 caracteres.")).toBeInTheDocument();
    expect(execute).not.toHaveBeenCalled();
  });

  it("sends the trimmed values", async () => {
    // Arrange
    const { execute, rendered } = renderDialog();
    await rendered;
    const user = userEvent.setup();
    await user.type(await screen.findByRole("textbox", { name: "Nombre" }), "  Ana ");
    await user.type(screen.getByRole("textbox", { name: "Apellidos" }), "García");

    // Act
    await user.click(screen.getByRole("button", { name: "Finalizar" }));

    // Assert
    await waitFor(() => expect(execute).toHaveBeenCalledTimes(1));
    expect(execute).toHaveBeenCalledWith({ name: "Ana", surname: "García" });
  });

  it("disables Finalizar while saving", async () => {
    // Arrange
    await renderDialog(() => new Promise(() => {})).rendered;
    const user = userEvent.setup();
    await user.type(await screen.findByRole("textbox", { name: "Nombre" }), "Ana");
    await user.type(screen.getByRole("textbox", { name: "Apellidos" }), "García");
    const finish = screen.getByRole("button", { name: "Finalizar" });

    // Act
    await user.click(finish);

    // Assert
    await waitFor(() => expect(finish).toBeDisabled());
  });

  it("stays open with the error and the typed values when saving fails", async () => {
    // Arrange
    await renderDialog(async () => fail(new DomainError("NETWORK", ""))).rendered;
    const user = userEvent.setup();
    await user.type(await screen.findByRole("textbox", { name: "Nombre" }), "Ana");
    await user.type(screen.getByRole("textbox", { name: "Apellidos" }), "García");

    // Act
    await user.click(screen.getByRole("button", { name: "Finalizar" }));

    // Assert
    expect(await screen.findByText("No hay conexión con el servidor.")).toBeInTheDocument();
    expect(screen.getByRole("dialog")).toBeInTheDocument();
    expect(screen.getByRole("textbox", { name: "Nombre" })).toHaveValue("Ana");
  });

  it("has no close control and ignores Escape and a click outside", async () => {
    // Arrange
    await renderDialog().rendered;
    const user = userEvent.setup();
    const dialog = await screen.findByRole("dialog");

    // Act
    await user.keyboard("{Escape}");
    await user.click(dialog.parentElement as HTMLElement);

    // Assert
    expect(screen.getByRole("dialog")).toBeInTheDocument();
    expect(screen.queryByRole("button", { name: /cerrar/i })).not.toBeInTheDocument();
  });

  it("moves between steps only when the step is valid, keeping the typed values", async () => {
    // Arrange
    await renderDialog(undefined, twoSteps).rendered;
    const user = userEvent.setup();
    await screen.findByRole("dialog", { name: "Paso 1 de 2 · Dinos cómo te llamas" });
    await user.click(screen.getByRole("button", { name: "Siguiente" }));
    const blocked = screen.getByRole("dialog", { name: "Paso 1 de 2 · Dinos cómo te llamas" });
    await user.type(screen.getByRole("textbox", { name: "Nombre" }), "Ana");

    // Act
    await user.click(screen.getByRole("button", { name: "Siguiente" }));

    // Assert
    expect(blocked).toBeInTheDocument();
    expect(
      await screen.findByRole("dialog", { name: "Paso 2 de 2 · Dinos cómo te llamas" }),
    ).toBeInTheDocument();
    expect(screen.getByRole("button", { name: "Atrás" })).toBeInTheDocument();
    expect(screen.getByRole("button", { name: "Finalizar" })).toBeInTheDocument();
    expect(screen.queryByRole("button", { name: "Siguiente" })).not.toBeInTheDocument();
  });

  it("goes back to the first step with the typed values", async () => {
    // Arrange
    await renderDialog(undefined, twoSteps).rendered;
    const user = userEvent.setup();
    await user.type(await screen.findByRole("textbox", { name: "Nombre" }), "Ana");
    await user.click(screen.getByRole("button", { name: "Siguiente" }));
    await screen.findByRole("dialog", { name: "Paso 2 de 2 · Dinos cómo te llamas" });

    // Act
    await user.click(screen.getByRole("button", { name: "Atrás" }));

    // Assert
    expect(
      await screen.findByRole("dialog", { name: "Paso 1 de 2 · Dinos cómo te llamas" }),
    ).toBeInTheDocument();
    expect(screen.getByRole("textbox", { name: "Nombre" })).toHaveValue("Ana");
    expect(screen.queryByRole("button", { name: "Atrás" })).not.toBeInTheDocument();
  });
});
