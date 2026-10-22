import type { AuthResponse, DomainError, Result, Role, SignupCredentials } from "../../../domain";
import type { AuthRepositoryInterface, UseCaseInterface } from "../../ports";

/** Signs up an account with the role the front was wired with. The caller cannot choose it. */
export default class SignupUseCase implements UseCaseInterface<SignupCredentials, AuthResponse> {
  constructor(
    private readonly authRepository: AuthRepositoryInterface,
    private readonly role: Role,
  ) {}

  execute(credentials: SignupCredentials): Promise<Result<AuthResponse, DomainError>> {
    return this.authRepository.signup({ ...credentials, role: this.role });
  }
}
