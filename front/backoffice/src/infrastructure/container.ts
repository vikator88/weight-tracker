import {
  type Dependencies,
  GetSessionUseCase,
  LoginUseCase,
  SignupUseCase,
} from "@weight-tracker/core/application";
import { AuthRepository, Container, FetchHttpClient } from "@weight-tracker/core/infrastructure";
import LocalStorageKeyValueStore from "./local-storage-key-value-store";
import NoOpNotificationService from "./no-op-notification-service";

const container = new Container<Dependencies>({
  httpClient: () =>
    new FetchHttpClient({ baseUrl: process.env.NEXT_PUBLIC_API_URL ?? "http://localhost:5199" }),
  keyValueStore: () => new LocalStorageKeyValueStore(),
  notificationService: () => new NoOpNotificationService(),
  authRepository: (c) => new AuthRepository(c.resolve("httpClient")),
  signupUseCase: (c) =>
    new SignupUseCase(c.resolve("authRepository"), c.resolve("keyValueStore"), 2), // TRAINER
  loginUseCase: (c) => new LoginUseCase(c.resolve("authRepository"), c.resolve("keyValueStore")),
  getSessionUseCase: (c) => new GetSessionUseCase(c.resolve("keyValueStore")),
});

export default container;
