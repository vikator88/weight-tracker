import type { DomainError, Result, UserProfile } from "../../../domain";
import type { UseCaseInterface, UserRepositoryInterface } from "../../ports";

/** Reads the signed-in user's own profile. The API resolves who that is from the access token. */
export default class GetMyProfileUseCase implements UseCaseInterface<void, UserProfile> {
  constructor(private readonly userRepository: UserRepositoryInterface) {}

  execute(): Promise<Result<UserProfile, DomainError>> {
    return this.userRepository.getMe();
  }
}
