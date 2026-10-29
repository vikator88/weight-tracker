import type { DomainError, Result, UserSummary } from "../../../domain";
import type { UseCaseInterface, UserRepositoryInterface } from "../../ports";

/**
 * Switches a deactivated account back on. Who may do it, and whether the account is already active, belong to
 * the API: the id is forwarded unchanged.
 */
export default class ActivateUserUseCase implements UseCaseInterface<string, UserSummary> {
  constructor(private readonly userRepository: UserRepositoryInterface) {}

  execute(id: string): Promise<Result<UserSummary, DomainError>> {
    return this.userRepository.activate(id);
  }
}
