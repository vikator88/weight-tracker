import { DomainError, fail } from "../../../domain";
import type { Result, UpdateProfileRequest, UserProfile } from "../../../domain";
import type { UseCaseInterface, UserRepositoryInterface } from "../../ports";
import { validateUpdateProfileRequest } from "../../validation";

/**
 * Replaces the signed-in user's own name and surname. Both values are trimmed and checked against the shared
 * length rules before any request, so an invalid value fails with `VALIDATION` and never reaches the API.
 */
export default class UpdateMyProfileUseCase implements UseCaseInterface<
  UpdateProfileRequest,
  UserProfile
> {
  constructor(private readonly userRepository: UserRepositoryInterface) {}

  execute(request: UpdateProfileRequest): Promise<Result<UserProfile, DomainError>> {
    const trimmed = { name: (request.name ?? "").trim(), surname: (request.surname ?? "").trim() };
    const violations = validateUpdateProfileRequest(trimmed);

    if (violations.name) {
      return Promise.resolve(fail(new DomainError("VALIDATION", `name: ${violations.name}`)));
    }
    if (violations.surname) {
      return Promise.resolve(fail(new DomainError("VALIDATION", `surname: ${violations.surname}`)));
    }
    return this.userRepository.updateMe(trimmed);
  }
}
