import {
  type AuthResponse,
  DomainError,
  fail,
  type LoginRequest,
  type Result,
} from "../../../domain";
import type {
  AuthRepositoryInterface,
  KeyValueStoreInterface,
  UseCaseInterface,
} from "../../ports";
import { ACCESS_TOKEN_KEY, REFRESH_TOKEN_KEY } from "./signup-use-case";

/** Logs in with email and password, then stores the tokens. The request carries no role. */
export default class LoginUseCase implements UseCaseInterface<LoginRequest, AuthResponse> {
  constructor(
    private readonly authRepository: AuthRepositoryInterface,
    private readonly keyValueStore: KeyValueStoreInterface,
  ) {}

  async execute(credentials: LoginRequest): Promise<Result<AuthResponse, DomainError>> {
    const result = await this.authRepository.login(credentials);
    if (!result.ok) {
      return result;
    }

    try {
      await this.keyValueStore.set(ACCESS_TOKEN_KEY, result.value.accessToken);
      await this.keyValueStore.set(REFRESH_TOKEN_KEY, result.value.refreshToken);
    } catch {
      return fail(new DomainError("UNEXPECTED", "The session could not be stored"));
    }

    return result;
  }
}
