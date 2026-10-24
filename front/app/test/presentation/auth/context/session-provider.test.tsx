import { useState } from "react";
import { Text } from "react-native";
import { fireEvent, screen, waitFor } from "@testing-library/react-native";
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
  const { status, errorCode, reload, runWithSession } = useSession();
  const [outcome, setOutcome] = useState("none");

  return (
    <>
      <Text>{`status:${status}`}</Text>
      <Text>{`error:${errorCode ?? "none"}`}</Text>
      <Text>{`outcome:${outcome}`}</Text>
      <Text onPress={() => void reload()}>reload</Text>
      <Text
        onPress={() =>
          void runWithSession(call).then((result) =>
            setOutcome(result.ok ? result.value : result.error.code),
          )
        }
      >
        run
      </Text>
    </>
  );
}

function refreshing(result: Result<Session, DomainError>) {
  const execute = jest.fn(async () => result);
  return { execute, registration: { refreshSessionUseCase: () => ({ execute }) } };
}

describe("SessionProvider", () => {
  it("starts restoring while the session is being read", async () => {
    // Arrange
    const getSessionUseCase = () => ({ execute: () => new Promise<never>(() => {}) });

    // Act
    await renderWithProviders(<Probe />, { getSessionUseCase });

    // Assert
    expect(screen.getByText("status:restoring")).toBeOnTheScreen();
  });

  it("is signed in when a session is stored", async () => {
    // Arrange
    const getSessionUseCase = () => ({ execute: async () => ok(session) });

    // Act
    await renderWithProviders(<Probe />, { getSessionUseCase });

    // Assert
    expect(await screen.findByText("status:signedIn")).toBeOnTheScreen();
  });

  it("is signed out when no session is stored", async () => {
    // Arrange
    const getSessionUseCase = () => ({ execute: async () => ok(null) });

    // Act
    await renderWithProviders(<Probe />, { getSessionUseCase });

    // Assert
    expect(await screen.findByText("status:signedOut")).toBeOnTheScreen();
    expect(screen.getByText("error:none")).toBeOnTheScreen();
  });

  it("is signed out and holds the error code when the session cannot be read", async () => {
    // Arrange
    const getSessionUseCase = () => ({
      execute: async () => fail(new DomainError("UNEXPECTED", "The session could not be read")),
    });

    // Act
    await renderWithProviders(<Probe />, { getSessionUseCase });

    // Assert
    expect(await screen.findByText("status:signedOut")).toBeOnTheScreen();
    expect(screen.getByText("error:UNEXPECTED")).toBeOnTheScreen();
  });

  it("reads the session again on reload", async () => {
    // Arrange
    const results = [ok(null), ok(session)];
    const execute = jest.fn(async () => results.shift() ?? ok(session));
    await renderWithProviders(<Probe />, { getSessionUseCase: () => ({ execute }) });
    await screen.findByText("status:signedOut");

    // Act
    await fireEvent.press(screen.getByText("reload"));

    // Assert
    await waitFor(() => expect(screen.getByText("status:signedIn")).toBeOnTheScreen());
    expect(execute).toHaveBeenCalledTimes(2);
  });
  it("refreshes once and runs the call again when it fails as unauthorized", async () => {
    // Arrange
    const results: Result<string, DomainError>[] = [failure("UNAUTHORIZED"), ok("done")];
    const call = jest.fn(async () => results.shift() ?? ok("done"));
    const refresh = refreshing(ok(session));
    await renderWithProviders(<Probe call={call} />, { ...signedIn, ...refresh.registration });
    await screen.findByText("status:signedIn");

    // Act
    await fireEvent.press(screen.getByText("run"));

    // Assert
    expect(await screen.findByText("outcome:done")).toBeOnTheScreen();
    expect(call).toHaveBeenCalledTimes(2);
    expect(refresh.execute).toHaveBeenCalledTimes(1);
    expect(screen.getByText("status:signedIn")).toBeOnTheScreen();
  });

  it("returns the retried failure without refreshing again", async () => {
    // Arrange
    const call = jest.fn(async () => failure("UNAUTHORIZED"));
    const refresh = refreshing(ok(session));
    await renderWithProviders(<Probe call={call} />, { ...signedIn, ...refresh.registration });
    await screen.findByText("status:signedIn");

    // Act
    await fireEvent.press(screen.getByText("run"));

    // Assert
    expect(await screen.findByText("outcome:UNAUTHORIZED")).toBeOnTheScreen();
    expect(call).toHaveBeenCalledTimes(2);
    expect(refresh.execute).toHaveBeenCalledTimes(1);
  });

  it("is signed out when the refresh fails as unauthorized", async () => {
    // Arrange
    const call = jest.fn(async () => failure("UNAUTHORIZED"));
    const refresh = refreshing(failure("UNAUTHORIZED"));
    await renderWithProviders(<Probe call={call} />, { ...signedIn, ...refresh.registration });
    await screen.findByText("status:signedIn");

    // Act
    await fireEvent.press(screen.getByText("run"));

    // Assert
    expect(await screen.findByText("status:signedOut")).toBeOnTheScreen();
    expect(screen.getByText("error:none")).toBeOnTheScreen();
    expect(screen.getByText("outcome:UNAUTHORIZED")).toBeOnTheScreen();
    expect(call).toHaveBeenCalledTimes(1);
  });

  it("stays signed in and returns the failure when the refresh gets no response", async () => {
    // Arrange
    const call = jest.fn(async () => failure("UNAUTHORIZED"));
    const refresh = refreshing(failure("NETWORK"));
    await renderWithProviders(<Probe call={call} />, { ...signedIn, ...refresh.registration });
    await screen.findByText("status:signedIn");

    // Act
    await fireEvent.press(screen.getByText("run"));

    // Assert
    expect(await screen.findByText("outcome:NETWORK")).toBeOnTheScreen();
    expect(screen.getByText("status:signedIn")).toBeOnTheScreen();
    expect(call).toHaveBeenCalledTimes(1);
  });

  it("does not refresh a failure other than unauthorized", async () => {
    // Arrange
    const call = jest.fn(async () => failure("NETWORK"));
    const refresh = refreshing(ok(session));
    await renderWithProviders(<Probe call={call} />, { ...signedIn, ...refresh.registration });
    await screen.findByText("status:signedIn");

    // Act
    await fireEvent.press(screen.getByText("run"));

    // Assert
    expect(await screen.findByText("outcome:NETWORK")).toBeOnTheScreen();
    expect(refresh.execute).not.toHaveBeenCalled();
    expect(call).toHaveBeenCalledTimes(1);
  });
});
