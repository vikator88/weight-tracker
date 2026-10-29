import { useState } from "react";
import { act, fireEvent, screen, waitFor, within } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { afterEach, beforeEach, describe, expect, it, vi } from "vitest";
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
    const field = screen.getByRole("textbox", { name: label });
    await user.clear(field);
    // Pasting keeps long values fast; the field still receives a real change event.
    await user.click(field);
    await user.paste(value);
  }
}

const trainerValues = { Nombre: "  Pierna  ", "ID del atleta": athleteId };

const weekField = () => screen.getByRole("group", { name: "Semana" });

/** Thursday 29 October 2026, local noon: the current week is 26 Oct – 1 Nov 2026. */
const today = new Date(2026, 9, 29, 12);

describe("CreateWorkoutDialog", () => {
  beforeEach(() => {
    push.mockClear();
    // Only `Date` is faked, so user events and promises keep running.
    vi.useFakeTimers({ toFake: ["Date"] });
    vi.setSystemTime(today);
  });

  afterEach(() => {
    vi.useRealTimers();
  });

  it("asks for the workout's data with empty fields and the current week", async () => {
    // Arrange & Act
    await renderDialog().rendered;

    // Assert
    await openedDialog();
    for (const name of ["Nombre", "Descripción", "ID del atleta"]) {
      expect(screen.getByRole("textbox", { name })).toHaveValue("");
    }
    expect(within(weekField()).getByText("Semana del 26 oct – 1 nov")).toBeInTheDocument();
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
    expect(await screen.findAllByText("Este campo es obligatorio.")).toHaveLength(2);
    expect(screen.getByRole("textbox", { name: "Nombre" })).toHaveAccessibleDescription(
      "Este campo es obligatorio.",
    );
    expect(screen.getByRole("textbox", { name: "ID del atleta" })).toHaveAccessibleDescription(
      "Este campo es obligatorio.",
    );
    expect(screen.getByRole("textbox", { name: "Descripción" })).not.toHaveAccessibleDescription();
    expect(within(weekField()).queryByText("Este campo es obligatorio.")).not.toBeInTheDocument();
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
    await user.click(within(weekField()).getByRole("button", { name: "Semana siguiente" }));

    // Act
    await user.click(screen.getByRole("button", { name: "Crear" }));

    // Assert
    await waitFor(() => expect(execute).toHaveBeenCalledTimes(1));
    expect(execute).toHaveBeenCalledWith({
      name: "Pierna",
      workoutDate: "2026-11-04",
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
        workoutDate: "2026-10-28",
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
    const week = screen.getByRole("group", { name: "Week" });
    expect(within(week).getByText("Week of Oct 26 – Nov 1")).toBeInTheDocument();
    for (const name of ["Previous week", "Next week", "Pick a week"]) {
      expect(within(week).getByRole("button", { name })).toBeInTheDocument();
    }
    expect(screen.getByRole("button", { name: "Cancel" })).toBeInTheDocument();
    expect(screen.getByRole("button", { name: "Create" })).toBeInTheDocument();
  });

  describe("week field", () => {
    it("shows the selected week with its buttons", async () => {
      // Arrange & Act
      await renderDialog().rendered;

      // Assert
      await openedDialog();
      const field = weekField();
      expect(within(field).getByText("Semana")).toBeInTheDocument();
      expect(within(field).getByText("Semana del 26 oct – 1 nov")).toBeInTheDocument();
      for (const name of ["Semana anterior", "Semana siguiente", "Elegir semana"]) {
        expect(within(field).getByRole("button", { name })).toBeInTheDocument();
      }
    });

    it("moves to the previous week without a request", async () => {
      // Arrange
      const { execute, rendered } = renderDialog();
      await rendered;
      await openedDialog();
      const user = userEvent.setup();

      // Act
      await user.click(within(weekField()).getByRole("button", { name: "Semana anterior" }));

      // Assert
      expect(within(weekField()).getByText("Semana del 19 oct – 25 oct")).toBeInTheDocument();
      expect(execute).not.toHaveBeenCalled();
    });

    it("moves to the next week across a month", async () => {
      // Arrange
      await renderDialog().rendered;
      await openedDialog();
      const user = userEvent.setup();

      // Act
      await user.click(within(weekField()).getByRole("button", { name: "Semana siguiente" }));

      // Assert
      expect(within(weekField()).getByText("Semana del 2 nov – 8 nov")).toBeInTheDocument();
    });

    it("disables the week field while the workout is being created", async () => {
      // Arrange
      const { rendered } = renderDialog({ createWorkout: () => new Promise(() => {}) });
      await rendered;
      await openedDialog();
      const user = userEvent.setup();
      await fill(user, trainerValues);

      // Act
      await user.click(screen.getByRole("button", { name: "Crear" }));

      // Assert
      await waitFor(() =>
        expect(within(weekField()).getByRole("button", { name: "Semana anterior" })).toBeDisabled(),
      );
      expect(within(weekField()).getByRole("button", { name: "Semana siguiente" })).toBeDisabled();
      expect(within(weekField()).getByRole("button", { name: "Elegir semana" })).toBeDisabled();
    });

    it("selects the week of the day picked in the calendar", async () => {
      // Arrange
      await renderDialog().rendered;
      await openedDialog();
      const picker = within(weekField()).getByLabelText("Elegir semana", { selector: "input" });

      // Act
      fireEvent.change(picker, { target: { value: "2026-11-13" } });

      // Assert
      expect(within(weekField()).getByText("Semana del 9 nov – 15 nov")).toBeInTheDocument();
    });

    it("shows both years and sends the Wednesday of a week spanning two years", async () => {
      // Arrange
      vi.setSystemTime(new Date(2026, 11, 29, 12));
      const { execute, rendered } = renderDialog();
      await rendered;
      await openedDialog();
      const user = userEvent.setup();
      await fill(user, trainerValues);
      expect(
        within(weekField()).getByText("Semana del 28 dic 2026 – 3 ene 2027"),
      ).toBeInTheDocument();

      // Act
      await user.click(screen.getByRole("button", { name: "Crear" }));

      // Assert
      await waitFor(() =>
        expect(execute).toHaveBeenCalledWith(
          expect.objectContaining({ workoutDate: "2026-12-30" }),
        ),
      );
    });
  });
});
