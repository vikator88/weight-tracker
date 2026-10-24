import {
  type Dependencies,
  GetMyProfileUseCase,
  GetPreferredLanguageUseCase,
  GetSessionUseCase,
  LoginUseCase,
  LogoutUseCase,
  SetPreferredLanguageUseCase,
  UpdateMyProfileUseCase,
  SignupUseCase,
} from "@weight-tracker/core/application";
import {
  AuthRepository,
  Container,
  FetchHttpClient,
  UserRepository,
} from "@weight-tracker/core/infrastructure";
import LocalStorageKeyValueStore from "./local-storage-key-value-store";
import NoOpNotificationService from "./no-op-notification-service";

const container = new Container<Dependencies>({
  httpClient: (c) =>
    new FetchHttpClient({
      baseUrl: process.env.NEXT_PUBLIC_API_URL ?? "http://localhost:5199",
      // Every request carries the stored access token, when there is one.
      getAccessToken: async () => {
        const session = await c.resolve("getSessionUseCase").execute();
        return session.ok ? (session.value?.accessToken ?? null) : null;
      },
    }),
  keyValueStore: () => new LocalStorageKeyValueStore(),
  notificationService: () => new NoOpNotificationService(),
  authRepository: (c) => new AuthRepository(c.resolve("httpClient")),
  userRepository: (c) => new UserRepository(c.resolve("httpClient")),
  signupUseCase: (c) =>
    new SignupUseCase(c.resolve("authRepository"), c.resolve("keyValueStore"), 2), // TRAINER
  loginUseCase: (c) => new LoginUseCase(c.resolve("authRepository"), c.resolve("keyValueStore")),
  getSessionUseCase: (c) => new GetSessionUseCase(c.resolve("keyValueStore")),
  logoutUseCase: (c) => new LogoutUseCase(c.resolve("keyValueStore")),
  getPreferredLanguageUseCase: (c) => new GetPreferredLanguageUseCase(c.resolve("keyValueStore")),
  setPreferredLanguageUseCase: (c) => new SetPreferredLanguageUseCase(c.resolve("keyValueStore")),
  getMyProfileUseCase: (c) => new GetMyProfileUseCase(c.resolve("userRepository")),
  updateMyProfileUseCase: (c) => new UpdateMyProfileUseCase(c.resolve("userRepository")),
});

export default container;
