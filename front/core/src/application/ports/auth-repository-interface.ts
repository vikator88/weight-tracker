import type {
  AuthResponse,
  DomainError,
  LoginRequest,
  RefreshRequest,
  Result,
  SignupRequest,
} from "../../domain";

export default interface AuthRepositoryInterface {
  signup(request: SignupRequest): Promise<Result<AuthResponse, DomainError>>;
  login(request: LoginRequest): Promise<Result<AuthResponse, DomainError>>;
  refresh(request: RefreshRequest): Promise<Result<AuthResponse, DomainError>>;
}
