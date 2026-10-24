import type { HttpClientInterface, UserRepositoryInterface } from "../../application";
import {
  type DomainError,
  fail,
  ok,
  type Result,
  type UpdateProfileRequest,
  type UserProfile,
} from "../../domain";
import { mapHttpError } from "../http";

export default class UserRepository implements UserRepositoryInterface {
  constructor(private readonly httpClient: HttpClientInterface) {}

  async getMe(): Promise<Result<UserProfile, DomainError>> {
    try {
      const response = await this.httpClient.request<UserProfile>({
        method: "GET",
        path: "/users/me",
      });

      return ok(response);
    } catch (error) {
      return fail(mapHttpError(error));
    }
  }

  async updateMe(request: UpdateProfileRequest): Promise<Result<UserProfile, DomainError>> {
    try {
      const response = await this.httpClient.request<UserProfile>({
        method: "PUT",
        path: "/users/me",
        body: request,
      });

      return ok(response);
    } catch (error) {
      return fail(mapHttpError(error));
    }
  }
}
