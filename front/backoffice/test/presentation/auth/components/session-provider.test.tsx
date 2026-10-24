import { useState } from "react";
import { screen } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { describe, expect, it, vi } from "vitest";
import {
  DomainError,
  type DomainErrorCode,
  fail,
  ok,
  type Result,
  type Session,
} from "@weight-tracker/core/domain";
import useSession from "@/presentation/auth/hooks/use-session";
import renderWithProviders from "../../../support/render-with-providers";

const session: Session = { accessToken: "access", refreshToken: "refresh" };

const signedIn = { getSessionUseCase: () => ({ execute: async () => ok(session) }) };

function failure(code: DomainErrorCode) {
  return fail(new DomainError(code, "Something failed"));
}

interface ProbeProps {
  call?: () => Promise<Result<string, DomainError>>;
}

function Probe({ call = async () => ok("done") }: ProbeProps) {
  const { status, runWithSession, reload } = useSession();
  const [outcome, setOutcome] = useState("none");

  return (
    <>
      <p>{`status:${status}`}</p>
      <p>{`outcome:${outcome}`}</p>
      <button
        onClick={() =>
          void runWithSession(call).then((result) =>
            setOutcome(result.ok ? result.value : result.error.code),
          )
        }
      >
        run
      </button>
      <button onClick={() => void reload()}>reload</button>
    </>
  );
}

function refreshing(result: Result<Session, DomainError>) {
  const execute = vi.fn(async () => result);
  return { execute, registration: { refreshSessionUseCase: () => ({ execute }) } };
}

describe("SessionProvider", () => {
  it("starts restoring while the session is being read", async () => {
    // Arrange
    const getSessionUseCase = () => ({ execute: () => new Promise<never>(() => {}) });

    // Act
    await renderWithProviders(<Probe />, { getSessionUseCase });

    // Assert
    expect(screen.getByText("status:restoring")).toBeInTheDocument();
  });

  it("is signed in when a session is stored", async () => {
    // Arrange & Act
    await renderWithProviders(<Probe />, signedIn);

    // Assert
    expect(await screen.findByText("status:signedIn")).toBeInTheDocument();
  });

  it("is signed out when no session is stored", async () => {
    // Arrange
    const getSessionUseCase = () => ({ execute: async () => ok(null) });

    // Act
    await renderWithProviders(<Probe />, { getSessionUseCase });

    // Assert
    expect(await screen.findByText("status:signedOut")).toBeInTheDocument();
  });

  it("is signed out when the session cannot be read", async () => {
    // Arrange
    const getSessionUseCase = () => ({ execute: async () => failure("UNEXPECTED") });

    // Act
    await renderWithProviders(<Probe />, { getSessionUseCase });

    // Assert
    expect(await screen.findByText("status:signedOut")).toBeInTheDocument();
  });

  it("reads the session again on reload", async () => {
    // Arrange
    const execute = vi
      .fn<() => Promise<Result<Session | null, DomainError>>>()
      .mockResolvedValueOnce(ok(null))
      .mockResolvedValue(ok(session));
    await renderWithProviders(<Probe />, { getSessionUseCase: () => ({ execute }) });
    await screen.findByText("status:signedOut");

    // Act
    await userEvent.click(screen.getByRole("button", { name: "reload" }));

    // Assert
    expect(await screen.findByText("status:signedIn")).toBeInTheDocument();
  });

  it("refreshes once and runs the call again when it fails as unauthorized", async () => {
    // Arrange
    const call = vi
      .fn<() => Promise<Result<string, DomainError>>>()
      .mockResolvedValueOnce(failure("UNAUTHORIZED"))
      .mockResolvedValue(ok("done"));
    const refresh = refreshing(ok(session));
    await renderWithProviders(<Probe call={call} />, { ...signedIn, ...refresh.registration });
    await screen.findByText("status:signedIn");

    // Act
    await userEvent.click(screen.getByRole("button", { name: "run" }));

    // Assert
    expect(await screen.findByText("outcome:done")).toBeInTheDocument();
    expect(call).toHaveBeenCalledTimes(2);
    expect(refresh.execute).toHaveBeenCalledTimes(1);
    expect(screen.getByText("status:signedIn")).toBeInTheDocument();
  });

  it("returns the retried failure without refreshing again", async () => {
    // Arrange
    const call = vi.fn(async () => failure("UNAUTHORIZED"));
    const refresh = refreshing(ok(session));
    await renderWithProviders(<Probe call={call} />, { ...signedIn, ...refresh.registration });
    await screen.findByText("status:signedIn");

    // Act
    await userEvent.click(screen.getByRole("button", { name: "run" }));

    // Assert
    expect(await screen.findByText("outcome:UNAUTHORIZED")).toBeInTheDocument();
    expect(call).toHaveBeenCalledTimes(2);
    expect(refresh.execute).toHaveBeenCalledTimes(1);
  });

  it("ends the session when the refresh fails as unauthorized", async () => {
    // Arrange
    const call = vi.fn(async () => failure("UNAUTHORIZED"));
    const refresh = refreshing(failure("UNAUTHORIZED"));
    await renderWithProviders(<Probe call={call} />, { ...signedIn, ...refresh.registration });
    await screen.findByText("status:signedIn");

    // Act
    await userEvent.click(screen.getByRole("button", { name: "run" }));

    // Assert
    expect(await screen.findByText("status:signedOut")).toBeInTheDocument();
    expect(screen.getByText("outcome:UNAUTHORIZED")).toBeInTheDocument();
    expect(call).toHaveBeenCalledTimes(1);
  });

  it("keeps the session and returns the failure when the refresh gets no response", async () => {
    // Arrange
    const call = vi.fn(async () => failure("UNAUTHORIZED"));
    const refresh = refreshing(failure("NETWORK"));
    await renderWithProviders(<Probe call={call} />, { ...signedIn, ...refresh.registration });
    await screen.findByText("status:signedIn");

    // Act
    await userEvent.click(screen.getByRole("button", { name: "run" }));

    // Assert
    expect(await screen.findByText("outcome:NETWORK")).toBeInTheDocument();
    expect(screen.getByText("status:signedIn")).toBeInTheDocument();
    expect(call).toHaveBeenCalledTimes(1);
  });

  it("does not refresh a failure other than unauthorized", async () => {
    // Arrange
    const call = vi.fn(async () => failure("NETWORK"));
    const refresh = refreshing(ok(session));
    await renderWithProviders(<Probe call={call} />, { ...signedIn, ...refresh.registration });
    await screen.findByText("status:signedIn");

    // Act
    await userEvent.click(screen.getByRole("button", { name: "run" }));

    // Assert
    expect(await screen.findByText("outcome:NETWORK")).toBeInTheDocument();
    expect(refresh.execute).not.toHaveBeenCalled();
    expect(call).toHaveBeenCalledTimes(1);
  });
});
