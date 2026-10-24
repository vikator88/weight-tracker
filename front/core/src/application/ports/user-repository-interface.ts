import type { DomainError, Result, UpdateProfileRequest, UserProfile } from "../../domain";

export default interface UserRepositoryInterface {
  getMe(): Promise<Result<UserProfile, DomainError>>;
  updateMe(request: UpdateProfileRequest): Promise<Result<UserProfile, DomainError>>;
}
