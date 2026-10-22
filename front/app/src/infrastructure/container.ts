import { type Dependencies, SignupUseCase } from "@weight-tracker/core/application";
import { AuthRepository, Container, FetchHttpClient } from "@weight-tracker/core/infrastructure";
import InMemoryKeyValueStore from "./in-memory-key-value-store";
import NoOpNotificationService from "./no-op-notification-service";

const container = new Container<Dependencies>({
  httpClient: () =>
    new FetchHttpClient({ baseUrl: process.env.EXPO_PUBLIC_API_URL ?? "http://localhost:5199" }),
  keyValueStore: () => new InMemoryKeyValueStore(),
  notificationService: () => new NoOpNotificationService(),
  authRepository: (c) => new AuthRepository(c.resolve("httpClient")),
  signupUseCase: (c) => new SignupUseCase(c.resolve("authRepository"), 1), // USER
});

export default container;
