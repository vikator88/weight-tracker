import { screen, waitFor } from "@testing-library/react";
import { beforeEach, describe, expect, it, vi } from "vitest";
import { DomainError, fail, ok } from "@weight-tracker/core/domain";
import type { Result, Session } from "@weight-tracker/core/domain";
import HomeView from "@/presentation/common/views/home-view";
import renderWithProviders from "../../../support/render-with-providers";

const replace = vi.fn();

vi.mock("next/navigation", () => ({
  useRouter: () => ({ replace }),
  usePathname: () => "/",
}));

function getSession(result: Promise<Result<Session | null, DomainError>>) {
  return { getSessionUseCase: () => ({ execute: () => result }) };
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
});
