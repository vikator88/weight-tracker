import type {
  CreatedUserResponse,
  CreateUserRequest,
  DomainError,
  Result,
  UpdateProfileRequest,
  UserProfile,
  UserSummary,
} from "../../domain";

export default interface UserRepositoryInterface {
  getMe(): Promise<Result<UserProfile, DomainError>>;
  updateMe(request: UpdateProfileRequest): Promise<Result<UserProfile, DomainError>>;
  getAll(): Promise<Result<UserSummary[], DomainError>>;
  create(request: CreateUserRequest): Promise<Result<CreatedUserResponse, DomainError>>;
  activate(id: string): Promise<Result<UserSummary, DomainError>>;
  deactivate(id: string): Promise<Result<UserSummary, DomainError>>;
}
