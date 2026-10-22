import { describe, expect, it } from "vitest";
import type { AuthRepositoryInterface, KeyValueStoreInterface } from "../../../../src/application";
import { SignupUseCase } from "../../../../src/application";
import {
  type AuthResponse,
  DomainError,
  fail,
  ok,
  type Result,
  type SignupCredentials,
  type SignupRequest,
} from "../../../../src/domain";

const credentials: SignupCredentials = { email: "athlete@example.com", password: "Str0ng!pass" };

const tokens: AuthResponse = { accessToken: "access", refreshToken: "refresh", expiresIn: 900 };

class FakeAuthRepository implements AuthRepositoryInterface {
  readonly requests: SignupRequest[] = [];

  constructor(private readonly result: Result<AuthResponse, DomainError>) {}

  async signup(request: SignupRequest): Promise<Result<AuthResponse, DomainError>> {
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

describe("SignupUseCase", () => {
  it.each([
    [1, "USER"],
    [2, "TRAINER"],
  ])("sends the configured role %i (%s)", async (role) => {
    // Arrange
    const repository = new FakeAuthRepository(ok(tokens));
    const useCase = new SignupUseCase(repository, new FakeKeyValueStore(), role);

    // Act
    await useCase.execute(credentials);

    // Assert
    expect(repository.requests).toEqual([expect.objectContaining({ role })]);
  });

  it("passes the email and password through unchanged", async () => {
    // Arrange
    const repository = new FakeAuthRepository(ok(tokens));
    const useCase = new SignupUseCase(repository, new FakeKeyValueStore(), 1);

    // Act
    await useCase.execute(credentials);

    // Assert
    expect(repository.requests).toEqual([{ ...credentials, role: 1 }]);
  });

  it("overrides a role smuggled into the input at runtime", async () => {
    // Arrange
    const repository = new FakeAuthRepository(ok(tokens));
    const useCase = new SignupUseCase(repository, new FakeKeyValueStore(), 1);
    const smuggled = { ...credentials, role: 3 } as SignupCredentials;

    // Act
    await useCase.execute(smuggled);

    // Assert
    expect(repository.requests[0]?.role).toBe(1);
  });

  it("returns the repository's success unchanged", async () => {
    // Arrange
    const useCase = new SignupUseCase(
      new FakeAuthRepository(ok(tokens)),
      new FakeKeyValueStore(),
      1,
    );

    // Act
    const result = await useCase.execute(credentials);

    // Assert
    expect(result).toEqual(ok(tokens));
  });

  it("returns the repository's failure unchanged", async () => {
    // Arrange
    const error = new DomainError("CONFLICT", "Email already exists");
    const useCase = new SignupUseCase(
      new FakeAuthRepository(fail(error)),
      new FakeKeyValueStore(),
      1,
    );

    // Act
    const result = await useCase.execute(credentials);

    // Assert
    expect(result).toEqual(fail(error));
  });

  it("stores the access and refresh tokens on success, and not expiresIn", async () => {
    // Arrange
    const store = new FakeKeyValueStore();
    const useCase = new SignupUseCase(new FakeAuthRepository(ok(tokens)), store, 1);

    // Act
    await useCase.execute(credentials);

    // Assert
    expect([...store.values]).toEqual([
      ["auth.accessToken", "access"],
      ["auth.refreshToken", "refresh"],
    ]);
  });

  it("stores nothing when the signup fails", async () => {
    // Arrange
    const store = new FakeKeyValueStore();
    const error = new DomainError("CONFLICT", "Email already exists");
    const useCase = new SignupUseCase(new FakeAuthRepository(fail(error)), store, 1);

    // Act
    await useCase.execute(credentials);

    // Assert
    expect(store.values.size).toBe(0);
  });

  it("returns UNEXPECTED when the tokens cannot be stored", async () => {
    // Arrange
    const useCase = new SignupUseCase(
      new FakeAuthRepository(ok(tokens)),
      new FakeKeyValueStore(true),
      1,
    );

    // Act
    const result = await useCase.execute(credentials);

    // Assert
    expect(result).toEqual(fail(expect.objectContaining({ code: "UNEXPECTED" })));
  });
});
