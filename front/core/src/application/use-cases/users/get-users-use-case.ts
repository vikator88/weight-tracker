import type { DomainError, Result, UserSummary } from "../../../domain";
import type { UseCaseInterface, UserRepositoryInterface } from "../../ports";

/** Lists the users the caller may see. Which ones that is, per role, is decided by the API. */
export default class GetUsersUseCase implements UseCaseInterface<void, UserSummary[]> {
  constructor(private readonly userRepository: UserRepositoryInterface) {}

  execute(): Promise<Result<UserSummary[], DomainError>> {
    return this.userRepository.getAll();
  }
}
