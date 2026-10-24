import { describe, expect, it } from "vitest";
import type { HttpClientInterface, HttpRequest } from "../../../src/application";
import { ok, type UpdateProfileRequest, type UserProfile } from "../../../src/domain";
import { HttpError, UserRepository } from "../../../src/infrastructure";

const profile: UserProfile = {
  id: "5f1c2f0e-9d5b-4a53-8c7e-0d6f7c1a2b3c",
  email: "trainer@example.com",
  name: "Ana",
  surname: "García",
  role: "TRAINER",
};

const updateRequest: UpdateProfileRequest = { name: "Ana", surname: "García" };

class FakeHttpClient implements HttpClientInterface {
  readonly requests: HttpRequest[] = [];

  constructor(private readonly respond: () => Promise<unknown>) {}

  async request<T>(httpRequest: HttpRequest): Promise<T> {
    this.requests.push(httpRequest);
    return (await this.respond()) as T;
  }
}

describe("UserRepository", () => {
  describe("getMe", () => {
    it("gets /users/me without a body", async () => {
      // Arrange
      const httpClient = new FakeHttpClient(async () => profile);
      const repository = new UserRepository(httpClient);

      // Act
      await repository.getMe();

      // Assert
      expect(httpClient.requests).toEqual([{ method: "GET", path: "/users/me" }]);
    });

    it("returns the profile on success", async () => {
      // Arrange
      const repository = new UserRepository(new FakeHttpClient(async () => profile));

      // Act
      const result = await repository.getMe();

      // Assert
      expect(result).toEqual(ok(profile));
    });

    it.each([
      [401, "UNAUTHORIZED"],
      [500, "UNEXPECTED"],
    ])("maps status %i to %s", async (status, code) => {
      // Arrange
      const httpClient = new FakeHttpClient(async () => {
        throw new HttpError(status, { status, detail: "Something failed" });
      });
      const repository = new UserRepository(httpClient);

      // Act
      const result = await repository.getMe();

      // Assert
      expect(result).toEqual({ ok: false, error: expect.objectContaining({ code }) });
    });

    it("returns the network code when there is no response", async () => {
      // Arrange
      const httpClient = new FakeHttpClient(async () => {
        throw new TypeError("Failed to fetch");
      });
      const repository = new UserRepository(httpClient);

      // Act
      const result = await repository.getMe();

      // Assert
      expect(result).toEqual({ ok: false, error: expect.objectContaining({ code: "NETWORK" }) });
    });
  });

  describe("updateMe", () => {
    it("puts the request to /users/me unchanged", async () => {
      // Arrange
      const httpClient = new FakeHttpClient(async () => profile);
      const repository = new UserRepository(httpClient);

      // Act
      await repository.updateMe(updateRequest);

      // Assert
      expect(httpClient.requests).toEqual([
        { method: "PUT", path: "/users/me", body: updateRequest },
      ]);
    });

    it("returns the updated profile on success", async () => {
      // Arrange
      const repository = new UserRepository(new FakeHttpClient(async () => profile));

      // Act
      const result = await repository.updateMe(updateRequest);

      // Assert
      expect(result).toEqual(ok(profile));
    });

    it.each([
      [400, "VALIDATION"],
      [401, "UNAUTHORIZED"],
      [500, "UNEXPECTED"],
    ])("maps status %i to %s", async (status, code) => {
      // Arrange
      const httpClient = new FakeHttpClient(async () => {
        throw new HttpError(status, { status, detail: "Something failed" });
      });
      const repository = new UserRepository(httpClient);

      // Act
      const result = await repository.updateMe(updateRequest);

      // Assert
      expect(result).toEqual({ ok: false, error: expect.objectContaining({ code }) });
    });

    it("returns the network code when there is no response", async () => {
      // Arrange
      const httpClient = new FakeHttpClient(async () => {
        throw new TypeError("Failed to fetch");
      });
      const repository = new UserRepository(httpClient);

      // Act
      const result = await repository.updateMe(updateRequest);

      // Assert
      expect(result).toEqual({ ok: false, error: expect.objectContaining({ code: "NETWORK" }) });
    });

    it("keeps the problem detail as the message", async () => {
      // Arrange
      const httpClient = new FakeHttpClient(async () => {
        throw new HttpError(400, { status: 400, detail: "Name is too long" });
      });
      const repository = new UserRepository(httpClient);

      // Act
      const result = await repository.updateMe(updateRequest);

      // Assert
      expect(result).toEqual({
        ok: false,
        error: expect.objectContaining({ message: "Name is too long" }),
      });
    });
  });
});
