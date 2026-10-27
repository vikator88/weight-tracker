import { useState } from "react";
import { act, screen, waitFor } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { beforeEach, describe, expect, it, vi } from "vitest";
import { DomainError, fail, ok } from "@weight-tracker/core/domain";
import type {
  CreateWorkoutRequest,
  Result,
  UserProfile,
  Workout,
} from "@weight-tracker/core/domain";
import i18n from "@/infrastructure/i18n";
import CreateWorkoutDialog from "@/presentation/workouts/components/organisms/create-workout-dialog";
import LoadProfile from "../../../../support/load-profile";
import renderWithProviders, { completeProfile } from "../../../../support/render-with-providers";

const push = vi.fn();

vi.mock("next/navigation", () => ({
  useRouter: () => ({ push, replace: vi.fn() }),
  usePathname: () => "/assigned-plans",
}));

const athleteId = "5f1c2f0e-9d5b-4a53-8c7e-0d6f7c1a2b3c";
const trainerId = "6a2d3f1e-0e6c-4b64-9d8f-1e7a8d2b3c4d";
const adminProfile: UserProfile = { ...completeProfile, role: "ADMIN" };

const created: Workout = {
  id: "w-1",
  name: "Pierna",
  description: null,
  workoutDate: "2026-11-02",
  userId: athleteId,
  trainerId: null,
  status: "DRAFT",
  exercises: [],
};

/** Stands in for the assigned plans screen: owns the open state and can open the dialog again. */
function Harness() {
  const [open, setOpen] = useState(true);

  return (
    <LoadProfile>
      <button type="button" onClick={() => setOpen(true)}>
        Abrir
      </button>
      <CreateWorkoutDialog open={open} onClose={() => setOpen(false)} />
    </LoadProfile>
  );
}

function renderDialog({
  profile = completeProfile,
  createWorkout = async () => ok(created),
}: {
  profile?: UserProfile;
  createWorkout?: (request: CreateWorkoutRequest) => Promise<Result<Workout, DomainError>>;
} = {}) {
  const execute = vi.fn(createWorkout);

  return {
    execute,
    rendered: renderWithProviders(<Harness />, {
      getSessionUseCase: () => ({
        execute: async () => ok({ accessToken: "a", refreshToken: "r" }),
      }),
      getMyProfileUseCase: () => ({ execute: async () => ok(profile) }),
      createWorkoutUseCase: () => ({ execute }),
    }),
  };
}

/** Waits until the profile is read, so the fields that depend on the role are settled. */
async function openedDialog() {
  const dialog = await screen.findByRole("dialog", { name: "Nuevo entrenamiento" });
  await waitFor(() => expect(screen.getByRole("textbox", { name: "Nombre" })).toBeEnabled());
  return dialog;
}

async function fill(
  user: ReturnType<typeof userEvent.setup>,
  values: Partial<Record<string, string>>,
) {
  for (const [label, value = ""] of Object.entries(values)) {
    const field =
      label === "Fecha"
        ? screen.getByLabelText("Fecha")
        : screen.getByRole("textbox", { name: label });
    await user.clear(field);
    // Pasting keeps long values fast; the field still receives a real change event.
    await user.click(field);
    await user.paste(value);
  }
}

const trainerValues = { Nombre: "  Pierna  ", Fecha: "2026-11-02", "ID del atleta": athleteId };

describe("CreateWorkoutDialog", () => {
  beforeEach(() => {
    push.mockClear();
  });

  it("asks for the workout's data with empty fields", async () => {
    // Arrange & Act
    await renderDialog().rendered;

    // Assert
    await openedDialog();
    for (const name of ["Nombre", "Descripción", "ID del atleta"]) {
      expect(screen.getByRole("textbox", { name })).toHaveValue("");
    }
    expect(screen.getByLabelText("Fecha")).toHaveValue("");
    expect(screen.getByRole("button", { name: "Cancelar" })).toBeInTheDocument();
    expect(screen.getByRole("button", { name: "Crear" })).toBeInTheDocument();
  });

  it("asks an admin for the trainer", async () => {
    // Arrange & Act
    await renderDialog({ profile: adminProfile }).rendered;

    // Assert
    await openedDialog();
    expect(await screen.findByRole("textbox", { name: "ID del entrenador" })).toBeInTheDocument();
  });

  it("does not ask a trainer for the trainer", async () => {
    // Arrange & Act
    await renderDialog().rendered;

    // Assert
    await openedDialog();
    expect(screen.queryByRole("textbox", { name: "ID del entrenador" })).not.toBeInTheDocument();
  });

  it("reports every empty required field and sends nothing", async () => {
    // Arrange
    const { execute, rendered } = renderDialog();
    await rendered;
    await openedDialog();
    const user = userEvent.setup();

    // Act
    await user.click(screen.getByRole("button", { name: "Crear" }));

    // Assert
    expect(await screen.findAllByText("Este campo es obligatorio.")).toHaveLength(3);
    expect(screen.getByRole("textbox", { name: "Descripción" })).not.toHaveAccessibleDescription();
    expect(execute).not.toHaveBeenCalled();
  });

  it.each([
    {
      case: "a blank name",
      values: { Nombre: "   " },
      label: "Nombre",
      message: "Este campo es obligatorio.",
    },
    {
      case: "a 257-character name",
      values: { Nombre: "a".repeat(257) },
      label: "Nombre",
      message: "No puede superar los 256 caracteres.",
    },
    {
      case: "a 1025-character description",
      values: { Descripción: "a".repeat(1025) },
      label: "Descripción",
      message: "No puede superar los 1024 caracteres.",
    },
    {
      case: "an athlete id that is not a UUID",
      values: { "ID del atleta": "ana" },
      label: "ID del atleta",
      message: "Introduce un identificador válido.",
    },
  ])("reports $case and sends nothing", async ({ values, label, message }) => {
    // Arrange
    const { execute, rendered } = renderDialog();
    await rendered;
    await openedDialog();
    const user = userEvent.setup();
    await fill(user, { ...trainerValues, ...values });

    // Act
    await user.click(screen.getByRole("button", { name: "Crear" }));

    // Assert
    expect(await screen.findByRole("textbox", { name: label })).toHaveAccessibleDescription(
      message,
    );
    expect(execute).not.toHaveBeenCalled();
  });

  it("asks an admin to fill the trainer and sends nothing", async () => {
    // Arrange
    const { execute, rendered } = renderDialog({ profile: adminProfile });
    await rendered;
    await openedDialog();
    const user = userEvent.setup();
    await screen.findByRole("textbox", { name: "ID del entrenador" });
    await fill(user, trainerValues);

    // Act
    await user.click(screen.getByRole("button", { name: "Crear" }));

    // Assert
    expect(
      await screen.findByRole("textbox", { name: "ID del entrenador" }),
    ).toHaveAccessibleDescription("Este campo es obligatorio.");
    expect(execute).not.toHaveBeenCalled();
  });

  it("sends a trainer's workout trimmed, without description nor trainer, once", async () => {
    // Arrange
    const { execute, rendered } = renderDialog();
    await rendered;
    await openedDialog();
    const user = userEvent.setup();
    await fill(user, trainerValues);

    // Act
    await user.click(screen.getByRole("button", { name: "Crear" }));

    // Assert
    await waitFor(() => expect(execute).toHaveBeenCalledTimes(1));
    expect(execute).toHaveBeenCalledWith({
      name: "Pierna",
      workoutDate: "2026-11-02",
      userId: athleteId,
    });
  });

  it("sends an admin's workout with its description and trainer", async () => {
    // Arrange
    const { execute, rendered } = renderDialog({ profile: adminProfile });
    await rendered;
    await openedDialog();
    const user = userEvent.setup();
    await screen.findByRole("textbox", { name: "ID del entrenador" });
    await fill(user, { ...trainerValues, Descripción: "Día 1", "ID del entrenador": trainerId });

    // Act
    await user.click(screen.getByRole("button", { name: "Crear" }));

    // Assert
    await waitFor(() =>
      expect(execute).toHaveBeenCalledWith({
        name: "Pierna",
        description: "Día 1",
        workoutDate: "2026-11-02",
        userId: athleteId,
        trainerId,
      }),
    );
  });

  it("disables Crear while the workout is being created", async () => {
    // Arrange
    const { rendered } = renderDialog({ createWorkout: () => new Promise(() => {}) });
    await rendered;
    await openedDialog();
    const user = userEvent.setup();
    await fill(user, trainerValues);

    // Act
    await user.click(screen.getByRole("button", { name: "Crear" }));

    // Assert
    expect(await screen.findByRole("button", { name: "Crear" })).toBeDisabled();
  });

  it("closes and opens the created workout's screen", async () => {
    // Arrange
    const { rendered } = renderDialog();
    await rendered;
    await openedDialog();
    const user = userEvent.setup();
    await fill(user, trainerValues);

    // Act
    await user.click(screen.getByRole("button", { name: "Crear" }));

    // Assert
    await waitFor(() => expect(push).toHaveBeenCalledWith("/workout?id=w-1"));
    await waitFor(() => expect(screen.queryByRole("dialog")).not.toBeInTheDocument());
  });

  it.each([
    ["NOT_FOUND", "No hemos encontrado lo que buscas."],
    ["VALIDATION", "Revisa los datos introducidos."],
  ] as const)("keeps the dialog and the values when the API answers %s", async (code, message) => {
    // Arrange
    const { rendered } = renderDialog({
      createWorkout: async () => fail(new DomainError(code, "")),
    });
    await rendered;
    await openedDialog();
    const user = userEvent.setup();
    await fill(user, trainerValues);

    // Act
    await user.click(screen.getByRole("button", { name: "Crear" }));

    // Assert
    expect(await screen.findByText(message)).toBeInTheDocument();
    expect(screen.getByRole("dialog", { name: "Nuevo entrenamiento" })).toBeInTheDocument();
    expect(screen.getByRole("textbox", { name: "Nombre" })).toHaveValue("  Pierna  ");
    expect(push).not.toHaveBeenCalled();
  });

  it("closes on Cancelar without a request and opens again empty", async () => {
    // Arrange
    const { execute, rendered } = renderDialog();
    await rendered;
    await openedDialog();
    const user = userEvent.setup();
    await fill(user, { Nombre: "Pierna" });
    await user.click(screen.getByRole("button", { name: "Cancelar" }));
    await waitFor(() => expect(screen.queryByRole("dialog")).not.toBeInTheDocument());

    // Act
    await user.click(screen.getByRole("button", { name: "Abrir" }));

    // Assert
    await openedDialog();
    expect(screen.getByRole("textbox", { name: "Nombre" })).toHaveValue("");
    expect(execute).not.toHaveBeenCalled();
  });

  it("follows the active language", async () => {
    // Arrange
    await renderDialog({ profile: adminProfile }).rendered;
    await openedDialog();
    await screen.findByRole("textbox", { name: "ID del entrenador" });

    // Act
    await act(() => i18n.changeLanguage("en"));

    // Assert
    expect(screen.getByRole("dialog", { name: "New workout" })).toBeInTheDocument();
    for (const name of ["Name", "Description", "Athlete ID", "Trainer ID"]) {
      expect(screen.getByRole("textbox", { name })).toBeInTheDocument();
    }
    expect(screen.getByLabelText("Date")).toBeInTheDocument();
    expect(screen.getByRole("button", { name: "Cancel" })).toBeInTheDocument();
    expect(screen.getByRole("button", { name: "Create" })).toBeInTheDocument();
  });
});
