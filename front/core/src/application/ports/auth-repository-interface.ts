import type { AuthResponse, DomainError, LoginRequest, Result, SignupRequest } from "../../domain";

export default interface AuthRepositoryInterface {
  signup(request: SignupRequest): Promise<Result<AuthResponse, DomainError>>;
  login(request: LoginRequest): Promise<Result<AuthResponse, DomainError>>;
}
