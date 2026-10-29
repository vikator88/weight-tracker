import type { CreatedUserResponse, CreateUserRequest, DomainError, Result } from "../../../domain";
import type { UseCaseInterface, UserRepositoryInterface } from "../../ports";

/**
 * Creates a user of any role on someone else's behalf. Who may do it, and the email and password rules, belong to
 * the API: the input is forwarded unchanged, and no session is started for the created user.
 */
export default class CreateUserUseCase implements UseCaseInterface<
  CreateUserRequest,
  CreatedUserResponse
> {
  constructor(private readonly userRepository: UserRepositoryInterface) {}

  execute(request: CreateUserRequest): Promise<Result<CreatedUserResponse, DomainError>> {
    return this.userRepository.create(request);
  }
}
