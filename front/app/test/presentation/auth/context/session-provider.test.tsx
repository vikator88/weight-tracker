import { Text } from "react-native";
import { fireEvent, screen, waitFor } from "@testing-library/react-native";
import { DomainError, fail, ok, type Session } from "@weight-tracker/core/domain";
import useSession from "@/presentation/auth/hooks/use-session";
import renderWithProviders from "../../../support/render-with-providers";

const session: Session = { accessToken: "access", refreshToken: "refresh" };

function Probe() {
  const { status, errorCode, refresh } = useSession();

  return (
    <>
      <Text>{`status:${status}`}</Text>
      <Text>{`error:${errorCode ?? "none"}`}</Text>
      <Text onPress={() => void refresh()}>refresh</Text>
    </>
  );
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

  it("reads the session again on refresh", async () => {
    // Arrange
    const results = [ok(null), ok(session)];
    const execute = jest.fn(async () => results.shift() ?? ok(session));
    await renderWithProviders(<Probe />, { getSessionUseCase: () => ({ execute }) });
    await screen.findByText("status:signedOut");

    // Act
    await fireEvent.press(screen.getByText("refresh"));

    // Assert
    await waitFor(() => expect(screen.getByText("status:signedIn")).toBeOnTheScreen());
    expect(execute).toHaveBeenCalledTimes(2);
  });
});
