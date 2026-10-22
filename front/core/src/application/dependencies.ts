import type { AuthResponse, LoginRequest, Session, SignupCredentials } from "../domain";
import type {
  AuthRepositoryInterface,
  HttpClientInterface,
  KeyValueStoreInterface,
  NotificationServiceInterface,
  UseCaseInterface,
} from "./ports";

/** Every port and use case a front's container must register, by key. */
export default interface Dependencies {
  httpClient: HttpClientInterface;
  keyValueStore: KeyValueStoreInterface;
  notificationService: NotificationServiceInterface;
  authRepository: AuthRepositoryInterface;
  signupUseCase: UseCaseInterface<SignupCredentials, AuthResponse>;
  loginUseCase: UseCaseInterface<LoginRequest, AuthResponse>;
  getSessionUseCase: UseCaseInterface<void, Session | null>;
}
