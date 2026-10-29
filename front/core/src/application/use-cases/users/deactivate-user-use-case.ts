import type { DomainError, Result, UserSummary } from "../../../domain";
import type { UseCaseInterface, UserRepositoryInterface } from "../../ports";

/**
 * Switches another user's account off, so they can no longer log in nor be given workouts. Who may do it, and on
 * which account, belong to the API: the id is forwarded unchanged.
 */
export default class DeactivateUserUseCase implements UseCaseInterface<string, UserSummary> {
  constructor(private readonly userRepository: UserRepositoryInterface) {}

  execute(id: string): Promise<Result<UserSummary, DomainError>> {
    return this.userRepository.deactivate(id);
  }
}
