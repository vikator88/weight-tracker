import { screen, waitFor } from "@testing-library/react";
import { beforeEach, describe, expect, it, vi } from "vitest";
import { DomainError, fail, ok } from "@weight-tracker/core/domain";
import type { Result, Session } from "@weight-tracker/core/domain";
import HomeView from "@/presentation/common/views/home-view";
import renderWithProviders from "../../../support/render-with-providers";

const replace = vi.fn();

vi.mock("next/navigation", () => ({ useRouter: () => ({ replace }) }));

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

  it("redirects to signup and hides the content when there is no session", async () => {
    // Arrange & Act
    await renderWithProviders(<HomeView />, getSession(Promise.resolve(ok(null))));

    // Assert
    await waitFor(() => expect(replace).toHaveBeenCalledWith("/signup"));
    expect(screen.queryByRole("heading", { name: "Weight Tracker" })).not.toBeInTheDocument();
  });

  it("redirects to signup and hides the content when the session cannot be read", async () => {
    // Arrange & Act
    await renderWithProviders(
      <HomeView />,
      getSession(Promise.resolve(fail(new DomainError("UNEXPECTED", "")))),
    );

    // Assert
    await waitFor(() => expect(replace).toHaveBeenCalledWith("/signup"));
    expect(screen.queryByRole("heading", { name: "Weight Tracker" })).not.toBeInTheDocument();
  });

  it("shows only a progress indicator while the session is being read", async () => {
    // Arrange & Act
    await renderWithProviders(<HomeView />, getSession(new Promise(() => {})));

    // Assert
    expect(screen.getByRole("progressbar")).toBeInTheDocument();
    expect(screen.queryByRole("heading", { name: "Weight Tracker" })).not.toBeInTheDocument();
  });
});
