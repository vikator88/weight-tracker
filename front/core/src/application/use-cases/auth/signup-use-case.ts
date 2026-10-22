import {
  type AuthResponse,
  DomainError,
  fail,
  type Result,
  type Role,
  type SignupCredentials,
} from "../../../domain";
import type {
  AuthRepositoryInterface,
  KeyValueStoreInterface,
  UseCaseInterface,
} from "../../ports";

export const ACCESS_TOKEN_KEY = "auth.accessToken";
export const REFRESH_TOKEN_KEY = "auth.refreshToken";

/**
 * Signs up an account with the role the front was wired with, then stores the tokens.
 * The caller cannot choose the role.
 */
export default class SignupUseCase implements UseCaseInterface<SignupCredentials, AuthResponse> {
  constructor(
    private readonly authRepository: AuthRepositoryInterface,
    private readonly keyValueStore: KeyValueStoreInterface,
    private readonly role: Role,
  ) {}

  async execute(credentials: SignupCredentials): Promise<Result<AuthResponse, DomainError>> {
    const result = await this.authRepository.signup({ ...credentials, role: this.role });
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
