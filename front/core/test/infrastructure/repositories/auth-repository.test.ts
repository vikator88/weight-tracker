import { describe, expect, it } from "vitest";
import type { HttpClientInterface, HttpRequest } from "../../../src/application";
import { type AuthResponse, ok, type SignupRequest } from "../../../src/domain";
import { AuthRepository, HttpError } from "../../../src/infrastructure";

const request: SignupRequest = { email: "athlete@example.com", password: "Str0ng!pass", role: 1 };

const tokens: AuthResponse = { accessToken: "access", refreshToken: "refresh", expiresIn: 900 };

class FakeHttpClient implements HttpClientInterface {
  readonly requests: HttpRequest[] = [];

  constructor(private readonly respond: () => Promise<unknown>) {}

  async request<T>(httpRequest: HttpRequest): Promise<T> {
    this.requests.push(httpRequest);
    return (await this.respond()) as T;
  }
}

describe("AuthRepository", () => {
  describe("signup", () => {
    it("posts the request to /auth/signup", async () => {
      // Arrange
      const httpClient = new FakeHttpClient(async () => tokens);
      const repository = new AuthRepository(httpClient);

      // Act
      await repository.signup(request);

      // Assert
      expect(httpClient.requests).toEqual([
        { method: "POST", path: "/auth/signup", body: request },
      ]);
    });

    it("returns the tokens on success", async () => {
      // Arrange
      const repository = new AuthRepository(new FakeHttpClient(async () => tokens));

      // Act
      const result = await repository.signup(request);

      // Assert
      expect(result).toEqual(ok(tokens));
    });

    it.each([
      [400, "VALIDATION"],
      [401, "UNAUTHORIZED"],
      [403, "FORBIDDEN"],
      [409, "CONFLICT"],
      [500, "UNEXPECTED"],
    ])("maps status %i to %s", async (status, code) => {
      // Arrange
      const httpClient = new FakeHttpClient(async () => {
        throw new HttpError(status, { status, detail: "Something failed" });
      });
      const repository = new AuthRepository(httpClient);

      // Act
      const result = await repository.signup(request);

      // Assert
      expect(result).toEqual({ ok: false, error: expect.objectContaining({ code }) });
    });

    it("returns the network code when there is no response", async () => {
      // Arrange
      const httpClient = new FakeHttpClient(async () => {
        throw new TypeError("Failed to fetch");
      });
      const repository = new AuthRepository(httpClient);

      // Act
      const result = await repository.signup(request);

      // Assert
      expect(result).toEqual({ ok: false, error: expect.objectContaining({ code: "NETWORK" }) });
    });

    it("keeps the problem detail as the message", async () => {
      // Arrange
      const httpClient = new FakeHttpClient(async () => {
        throw new HttpError(409, { status: 409, detail: "Email already exists" });
      });
      const repository = new AuthRepository(httpClient);

      // Act
      const result = await repository.signup(request);

      // Assert
      expect(result).toEqual({
        ok: false,
        error: expect.objectContaining({ message: "Email already exists" }),
      });
    });
  });
});
