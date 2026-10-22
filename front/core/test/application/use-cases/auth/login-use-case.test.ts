import { describe, expect, it } from "vitest";
import type { AuthRepositoryInterface, KeyValueStoreInterface } from "../../../../src/application";
import { LoginUseCase } from "../../../../src/application";
import {
  type AuthResponse,
  DomainError,
  fail,
  type LoginRequest,
  ok,
  type Result,
} from "../../../../src/domain";

const credentials: LoginRequest = { email: "athlete@example.com", password: "Str0ng!pass" };

const tokens: AuthResponse = { accessToken: "access", refreshToken: "refresh", expiresIn: 900 };

class FakeAuthRepository implements AuthRepositoryInterface {
  readonly requests: LoginRequest[] = [];

  constructor(private readonly result: Result<AuthResponse, DomainError>) {}

  async signup(): Promise<Result<AuthResponse, DomainError>> {
    return this.result;
  }

  async login(request: LoginRequest): Promise<Result<AuthResponse, DomainError>> {
    this.requests.push(request);
    return this.result;
  }
}

class FakeKeyValueStore implements KeyValueStoreInterface {
  readonly values = new Map<string, string>();

  constructor(private readonly failOnSet = false) {}

  async get(key: string): Promise<string | null> {
    return this.values.get(key) ?? null;
  }

  async set(key: string, value: string): Promise<void> {
    if (this.failOnSet) {
      throw new Error("storage unavailable");
    }
    this.values.set(key, value);
  }

  async remove(key: string): Promise<void> {
    this.values.delete(key);
  }
}

describe("LoginUseCase", () => {
  it("passes the email and password through unchanged, with no role", async () => {
    // Arrange
    const repository = new FakeAuthRepository(ok(tokens));
    const useCase = new LoginUseCase(repository, new FakeKeyValueStore());

    // Act
    await useCase.execute(credentials);

    // Assert
    expect(repository.requests).toEqual([credentials]);
  });

  it("returns the repository's success unchanged", async () => {
    // Arrange
    const useCase = new LoginUseCase(new FakeAuthRepository(ok(tokens)), new FakeKeyValueStore());

    // Act
    const result = await useCase.execute(credentials);

    // Assert
    expect(result).toEqual(ok(tokens));
  });

  it("returns the repository's failure unchanged", async () => {
    // Arrange
    const error = new DomainError("UNAUTHORIZED", "Invalid credentials");
    const useCase = new LoginUseCase(new FakeAuthRepository(fail(error)), new FakeKeyValueStore());

    // Act
    const result = await useCase.execute(credentials);

    // Assert
    expect(result).toEqual(fail(error));
  });

  it("stores the access and refresh tokens on success, and not expiresIn", async () => {
    // Arrange
    const store = new FakeKeyValueStore();
    const useCase = new LoginUseCase(new FakeAuthRepository(ok(tokens)), store);

    // Act
    await useCase.execute(credentials);

    // Assert
    expect([...store.values]).toEqual([
      ["auth.accessToken", "access"],
      ["auth.refreshToken", "refresh"],
    ]);
  });

  it("stores nothing when the login fails", async () => {
    // Arrange
    const store = new FakeKeyValueStore();
    const error = new DomainError("UNAUTHORIZED", "Invalid credentials");
    const useCase = new LoginUseCase(new FakeAuthRepository(fail(error)), store);

    // Act
    await useCase.execute(credentials);

    // Assert
    expect(store.values.size).toBe(0);
  });

  it("returns UNEXPECTED when the tokens cannot be stored", async () => {
    // Arrange
    const useCase = new LoginUseCase(
      new FakeAuthRepository(ok(tokens)),
      new FakeKeyValueStore(true),
    );

    // Act
    const result = await useCase.execute(credentials);

    // Assert
    expect(result).toEqual(fail(expect.objectContaining({ code: "UNEXPECTED" })));
  });
});
