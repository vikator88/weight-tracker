import type { AuthRepositoryInterface, HttpClientInterface } from "../../application";
import {
  type AuthResponse,
  type DomainError,
  fail,
  type LoginRequest,
  ok,
  type RefreshRequest,
  type Result,
  type SignupRequest,
} from "../../domain";
import { mapHttpError } from "../http";

export default class AuthRepository implements AuthRepositoryInterface {
  constructor(private readonly httpClient: HttpClientInterface) {}

  async signup(request: SignupRequest): Promise<Result<AuthResponse, DomainError>> {
    try {
      const response = await this.httpClient.request<AuthResponse>({
        method: "POST",
        path: "/auth/signup",
        body: request,
      });

      return ok(response);
    } catch (error) {
      return fail(mapHttpError(error));
    }
  }

  async login(request: LoginRequest): Promise<Result<AuthResponse, DomainError>> {
    try {
      const response = await this.httpClient.request<AuthResponse>({
        method: "POST",
        path: "/auth/login",
        body: request,
      });

      return ok(response);
    } catch (error) {
      return fail(mapHttpError(error));
    }
  }

  async refresh(request: RefreshRequest): Promise<Result<AuthResponse, DomainError>> {
    try {
      const response = await this.httpClient.request<AuthResponse>({
        method: "POST",
        path: "/auth/refresh",
        body: request,
      });

      return ok(response);
    } catch (error) {
      return fail(mapHttpError(error));
    }
  }
}
