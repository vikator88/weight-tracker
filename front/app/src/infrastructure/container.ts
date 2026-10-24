import {
  type Dependencies,
  GetPreferredLanguageUseCase,
  GetSessionUseCase,
  LoginUseCase,
  LogoutUseCase,
  SetPreferredLanguageUseCase,
  SignupUseCase,
} from "@weight-tracker/core/application";
import { AuthRepository, Container, FetchHttpClient } from "@weight-tracker/core/infrastructure";
import NoOpNotificationService from "./no-op-notification-service";
import SqliteKeyValueStore from "./sqlite-key-value-store";

const container = new Container<Dependencies>({
  httpClient: () =>
    new FetchHttpClient({ baseUrl: process.env.EXPO_PUBLIC_API_URL ?? "http://localhost:5199" }),
  keyValueStore: () => new SqliteKeyValueStore(),
  notificationService: () => new NoOpNotificationService(),
  authRepository: (c) => new AuthRepository(c.resolve("httpClient")),
  signupUseCase: (c) =>
    new SignupUseCase(c.resolve("authRepository"), c.resolve("keyValueStore"), 1), // USER
  loginUseCase: (c) => new LoginUseCase(c.resolve("authRepository"), c.resolve("keyValueStore")),
  getSessionUseCase: (c) => new GetSessionUseCase(c.resolve("keyValueStore")),
  logoutUseCase: (c) => new LogoutUseCase(c.resolve("keyValueStore")),
  getPreferredLanguageUseCase: (c) => new GetPreferredLanguageUseCase(c.resolve("keyValueStore")),
  setPreferredLanguageUseCase: (c) => new SetPreferredLanguageUseCase(c.resolve("keyValueStore")),
});

export default container;
