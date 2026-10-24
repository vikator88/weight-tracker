import type { DomainError, Result, UpdateProfileRequest, UserProfile } from "../../../domain";
import type { UseCaseInterface, UserRepositoryInterface } from "../../ports";

/** Replaces the signed-in user's own name and surname. The API owns every rule about the values. */
export default class UpdateMyProfileUseCase implements UseCaseInterface<
  UpdateProfileRequest,
  UserProfile
> {
  constructor(private readonly userRepository: UserRepositoryInterface) {}

  execute(request: UpdateProfileRequest): Promise<Result<UserProfile, DomainError>> {
    return this.userRepository.updateMe(request);
  }
}
