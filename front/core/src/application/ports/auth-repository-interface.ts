import type { AuthResponse, DomainError, Result, SignupRequest } from "../../domain";

export default interface AuthRepositoryInterface {
  signup(request: SignupRequest): Promise<Result<AuthResponse, DomainError>>;
}
