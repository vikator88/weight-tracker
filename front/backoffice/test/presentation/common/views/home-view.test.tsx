import { screen, waitFor } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { beforeEach, describe, expect, it, vi } from "vitest";
import { DomainError, fail, ok } from "@weight-tracker/core/domain";
import type { Result, Session } from "@weight-tracker/core/domain";
import HomeView from "@/presentation/common/views/home-view";
import renderWithProviders from "../../../support/render-with-providers";

const replace = vi.fn();

vi.mock("next/navigation", () => ({ useRouter: () => ({ replace }) }));

const signedIn = { accessToken: "a", refreshToken: "r" };

function renderSignedIn(logout: () => Promise<Result<void, DomainError>>) {
  const execute = vi.fn(logout);

  return {
    execute,
    rendered: renderWithProviders(<HomeView />, {
      ...getSession(Promise.resolve(ok(signedIn))),
      logoutUseCase: () => ({ execute }),
    }),
  };
}

function getSession(result: Promise<Result<Session | null, DomainError>>) {
  return { getSessionUseCase: () => ({ execute: () => result }) };
}

describe("HomeView", () => {
  beforeEach(() => {
    replace.mockClear();
  });

  it("shows the home content when a session is stored", async () => {
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
    expect(replace).not.toHaveBeenCalled();
  });

  it("redirects to login and hides the content when there is no session", async () => {
    // Arrange & Act
    await renderWithProviders(<HomeView />, getSession(Promise.resolve(ok(null))));

    // Assert
    await waitFor(() => expect(replace).toHaveBeenCalledWith("/login"));
    expect(screen.queryByRole("heading", { name: "Weight Tracker" })).not.toBeInTheDocument();
    expect(screen.queryByRole("button", { name: "Cerrar sesión" })).not.toBeInTheDocument();
  });

  it("redirects to login and hides the content when the session cannot be read", async () => {
    // Arrange & Act
    await renderWithProviders(
      <HomeView />,
      getSession(Promise.resolve(fail(new DomainError("UNEXPECTED", "")))),
    );

    // Assert
    await waitFor(() => expect(replace).toHaveBeenCalledWith("/login"));
    expect(screen.queryByRole("heading", { name: "Weight Tracker" })).not.toBeInTheDocument();
  });

  it("shows only a progress indicator while the session is being read", async () => {
    // Arrange & Act
    await renderWithProviders(<HomeView />, getSession(new Promise(() => {})));

    // Assert
    expect(screen.getByRole("progressbar")).toBeInTheDocument();
    expect(screen.queryByRole("heading", { name: "Weight Tracker" })).not.toBeInTheDocument();
  });

  it("shows the logout button when a session is stored", async () => {
    // Arrange & Act
    const { rendered } = renderSignedIn(async () => ok(undefined));
    await rendered;

    // Assert
    expect(await screen.findByRole("button", { name: "Cerrar sesión" })).toBeEnabled();
  });

  it("logs out and goes to login when the button is clicked", async () => {
    // Arrange
    const { execute, rendered } = renderSignedIn(async () => ok(undefined));
    await rendered;
    const button = await screen.findByRole("button", { name: "Cerrar sesión" });

    // Act
    await userEvent.setup().click(button);

    // Assert
    await waitFor(() => expect(replace).toHaveBeenCalledWith("/login"));
    expect(execute).toHaveBeenCalledTimes(1);
  });

  it("disables the logout button while logging out", async () => {
    // Arrange
    const { rendered } = renderSignedIn(() => new Promise(() => {}));
    await rendered;
    const button = await screen.findByRole("button", { name: "Cerrar sesión" });

    // Act
    await userEvent.setup().click(button);

    // Assert
    await waitFor(() => expect(button).toBeDisabled());
  });

  it("stays on home and shows the error when logging out fails", async () => {
    // Arrange
    const { rendered } = renderSignedIn(async () => fail(new DomainError("UNEXPECTED", "")));
    await rendered;
    const button = await screen.findByRole("button", { name: "Cerrar sesión" });

    // Act
    await userEvent.setup().click(button);

    // Assert
    expect(await screen.findByText("Ha ocurrido un error inesperado.")).toBeInTheDocument();
    expect(replace).not.toHaveBeenCalled();
    expect(screen.getByRole("heading", { name: "Weight Tracker" })).toBeInTheDocument();
  });
});
