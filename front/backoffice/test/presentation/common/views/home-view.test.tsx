import { screen, waitFor } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { beforeEach, describe, expect, it, vi } from "vitest";
import { DomainError, fail, ok } from "@weight-tracker/core/domain";
import type {
  Result,
  Session,
  UpdateProfileRequest,
  UserProfile,
} from "@weight-tracker/core/domain";
import HomeView from "@/presentation/common/views/home-view";
import renderWithProviders, { completeProfile } from "../../../support/render-with-providers";

const replace = vi.fn();

vi.mock("next/navigation", () => ({
  useRouter: () => ({ replace }),
  usePathname: () => "/",
}));

function getSession(result: Promise<Result<Session | null, DomainError>>) {
  return { getSessionUseCase: () => ({ execute: () => result }) };
}

const signedIn = {
  getSessionUseCase: () => ({ execute: async () => ok({ accessToken: "a", refreshToken: "r" }) }),
};

function renderHome(
  getProfile: () => Promise<Result<UserProfile, DomainError>>,
  update: (request: UpdateProfileRequest) => Promise<Result<UserProfile, DomainError>> = async (
    request,
  ) => ok({ ...completeProfile, ...request }),
) {
  const read = vi.fn(getProfile);

  return {
    read,
    rendered: renderWithProviders(<HomeView />, {
      ...signedIn,
      getMyProfileUseCase: () => ({ execute: read }),
      updateMyProfileUseCase: () => ({ execute: update }),
    }),
  };
}

describe("HomeView", () => {
  beforeEach(() => {
    replace.mockClear();
  });

  it("shows the home content and the navigation drawer when a session is stored", async () => {
    // Arrange & Act
    await renderWithProviders(
      <HomeView />,
      getSession(Promise.resolve(ok({ accessToken: "a", refreshToken: "r" }))),
    );

    // Assert
    expect(
      await screen.findByRole("heading", { level: 1, name: "Weight Tracker" }),
    ).toBeInTheDocument();
    expect(screen.getByText("Has iniciado sesión.")).toBeInTheDocument();
    expect(screen.getByRole("navigation", { name: "Menú principal" })).toBeInTheDocument();
    expect(screen.getByRole("textbox", { name: "Buscar" })).toBeInTheDocument();
    expect(replace).not.toHaveBeenCalled();
  });

  it("redirects to login and hides the content and the drawer when there is no session", async () => {
    // Arrange & Act
    await renderWithProviders(<HomeView />, getSession(Promise.resolve(ok(null))));

    // Assert
    await waitFor(() => expect(replace).toHaveBeenCalledWith("/login"));
    expect(screen.queryByRole("heading", { name: "Weight Tracker" })).not.toBeInTheDocument();
    expect(screen.queryByRole("navigation", { name: "Menú principal" })).not.toBeInTheDocument();
    expect(screen.queryByRole("textbox", { name: "Buscar" })).not.toBeInTheDocument();
  });

  it("redirects to login and hides the content and the drawer when the session cannot be read", async () => {
    // Arrange & Act
    await renderWithProviders(
      <HomeView />,
      getSession(Promise.resolve(fail(new DomainError("UNEXPECTED", "")))),
    );

    // Assert
    await waitFor(() => expect(replace).toHaveBeenCalledWith("/login"));
    expect(screen.queryByRole("heading", { name: "Weight Tracker" })).not.toBeInTheDocument();
    expect(screen.queryByRole("navigation", { name: "Menú principal" })).not.toBeInTheDocument();
    expect(screen.queryByRole("textbox", { name: "Buscar" })).not.toBeInTheDocument();
  });

  it("shows only a progress indicator while the session is being read", async () => {
    // Arrange & Act
    await renderWithProviders(<HomeView />, getSession(new Promise(() => {})));

    // Assert
    expect(screen.getByRole("progressbar")).toBeInTheDocument();
    expect(screen.queryByRole("heading", { name: "Weight Tracker" })).not.toBeInTheDocument();
    expect(screen.queryByRole("navigation", { name: "Menú principal" })).not.toBeInTheDocument();
    expect(screen.queryByRole("textbox", { name: "Buscar" })).not.toBeInTheDocument();
  });

  it("shows logout only inside the closed user menu", async () => {
    // Arrange & Act
    await renderWithProviders(
      <HomeView />,
      getSession(Promise.resolve(ok({ accessToken: "a", refreshToken: "r" }))),
    );

    // Assert
    await screen.findByRole("heading", { level: 1, name: "Weight Tracker" });
    expect(screen.getByRole("button", { name: "Menú de usuario" })).toBeInTheDocument();
    expect(screen.queryByText("Cerrar sesión")).not.toBeInTheDocument();
  });

  it("shows the skeleton and no content or dialog while the profile is loading", async () => {
    // Arrange & Act
    await renderHome(() => new Promise(() => {})).rendered;

    // Assert
    expect((await screen.findAllByLabelText("Cargando")).length).toBeGreaterThan(0);
    expect(
      screen.queryByRole("heading", { name: "Weight Tracker", level: 1 }),
    ).not.toBeInTheDocument();
    expect(screen.queryByRole("dialog")).not.toBeInTheDocument();
  });

  it("reads the profile once", async () => {
    // Arrange
    const { read, rendered } = renderHome(async () => ok(completeProfile));

    // Act
    await rendered;
    await screen.findByRole("heading", { level: 1, name: "Weight Tracker" });

    // Assert
    expect(read).toHaveBeenCalledTimes(1);
  });

  it("shows the regular content and no dialog when the profile is complete", async () => {
    // Arrange & Act
    await renderHome(async () => ok(completeProfile)).rendered;

    // Assert
    expect(
      await screen.findByRole("heading", { level: 1, name: "Weight Tracker" }),
    ).toBeInTheDocument();
    expect(screen.queryByRole("dialog")).not.toBeInTheDocument();
    expect(screen.getByText("Ana García")).toBeInTheDocument();
  });

  it.each([
    ["name", { name: "", surname: "García" }],
    ["surname", { name: "Ana", surname: "   " }],
  ])(
    "shows the onboarding dialog over the skeleton when the %s is empty",
    async (_field, names) => {
      // Arrange & Act
      await renderHome(async () => ok({ ...completeProfile, ...names })).rendered;

      // Assert
      expect(
        await screen.findByRole("dialog", { name: "Paso 1 de 1 · Dinos cómo te llamas" }),
      ).toBeInTheDocument();
      expect(
        screen.queryByRole("heading", { name: "Weight Tracker", level: 1 }),
      ).not.toBeInTheDocument();
      expect(screen.getAllByLabelText("Cargando").length).toBeGreaterThan(0);
    },
  );

  it("closes the dialog, shows the content and the new name after Finalizar succeeds", async () => {
    // Arrange
    await renderHome(async () => ok({ ...completeProfile, name: "", surname: "" })).rendered;
    const user = userEvent.setup();
    await user.type(await screen.findByRole("textbox", { name: "Nombre" }), "Lucía");
    await user.type(screen.getByRole("textbox", { name: "Apellidos" }), "Pérez");

    // Act
    await user.click(screen.getByRole("button", { name: "Finalizar" }));

    // Assert
    expect(
      await screen.findByRole("heading", { level: 1, name: "Weight Tracker" }),
    ).toBeInTheDocument();
    expect(screen.queryByRole("dialog")).not.toBeInTheDocument();
    expect(screen.getByText("Lucía Pérez")).toBeInTheDocument();
  });

  it("keeps the dialog with the error when Finalizar fails", async () => {
    // Arrange
    await renderHome(
      async () => ok({ ...completeProfile, name: "", surname: "" }),
      async () => fail(new DomainError("NETWORK", "")),
    ).rendered;
    const user = userEvent.setup();
    await user.type(await screen.findByRole("textbox", { name: "Nombre" }), "Lucía");
    await user.type(screen.getByRole("textbox", { name: "Apellidos" }), "Pérez");

    // Act
    await user.click(screen.getByRole("button", { name: "Finalizar" }));

    // Assert
    expect(await screen.findByText("No hay conexión con el servidor.")).toBeInTheDocument();
    expect(screen.getByRole("dialog")).toBeInTheDocument();
    expect(
      screen.queryByRole("heading", { name: "Weight Tracker", level: 1 }),
    ).not.toBeInTheDocument();
  });

  it("shows the error and reads the profile again when Reintentar is pressed", async () => {
    // Arrange
    const getProfile = vi
      .fn<() => Promise<Result<UserProfile, DomainError>>>()
      .mockResolvedValueOnce(fail(new DomainError("NETWORK", "")))
      .mockResolvedValueOnce(ok(completeProfile));
    const { read, rendered } = renderHome(getProfile);
    await rendered;
    const user = userEvent.setup();
    await screen.findByText("No hay conexión con el servidor.");
    expect(screen.queryByRole("dialog")).not.toBeInTheDocument();

    // Act
    await user.click(screen.getByRole("button", { name: "Reintentar" }));

    // Assert
    expect(
      await screen.findByRole("heading", { level: 1, name: "Weight Tracker" }),
    ).toBeInTheDocument();
    expect(screen.queryByText("No hay conexión con el servidor.")).not.toBeInTheDocument();
    expect(read).toHaveBeenCalledTimes(2);
  });
  it("refreshes the session and shows the profile when the access token has expired", async () => {
    // Arrange
    const read = vi
      .fn<() => Promise<Result<UserProfile, DomainError>>>()
      .mockResolvedValueOnce(fail(new DomainError("UNAUTHORIZED", "")))
      .mockResolvedValue(ok(completeProfile));

    // Act
    await renderWithProviders(<HomeView />, {
      ...signedIn,
      getMyProfileUseCase: () => ({ execute: read }),
      refreshSessionUseCase: () => ({
        execute: async () => ok({ accessToken: "a2", refreshToken: "r2" }),
      }),
    });

    // Assert
    expect(
      await screen.findByRole("heading", { level: 1, name: "Weight Tracker" }),
    ).toBeInTheDocument();
    expect(screen.getByText("Ana García")).toBeInTheDocument();
    expect(read).toHaveBeenCalledTimes(2);
    expect(replace).not.toHaveBeenCalled();
  });

  it("redirects to login and shows no profile when the session cannot be refreshed", async () => {
    // Arrange
    const read = vi.fn(async () => fail(new DomainError("UNAUTHORIZED", "")));

    // Act
    await renderWithProviders(<HomeView />, {
      ...signedIn,
      getMyProfileUseCase: () => ({ execute: read }),
      refreshSessionUseCase: () => ({
        execute: async () => fail(new DomainError("UNAUTHORIZED", "")),
      }),
    });

    // Assert
    await waitFor(() => expect(replace).toHaveBeenCalledWith("/login"));
    expect(screen.queryByRole("heading", { name: "Weight Tracker" })).not.toBeInTheDocument();
    expect(screen.queryByText("Ana García")).not.toBeInTheDocument();
    expect(screen.queryByText(/Tu sesión no es válida/)).not.toBeInTheDocument();
    expect(read).toHaveBeenCalledTimes(1);
  });
});
