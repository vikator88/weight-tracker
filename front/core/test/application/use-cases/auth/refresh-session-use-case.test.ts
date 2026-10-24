import { describe, expect, it } from "vitest";
import type { AuthRepositoryInterface, KeyValueStoreInterface } from "../../../../src/application";
import { RefreshSessionUseCase } from "../../../../src/application";
import {
  type AuthResponse,
  DomainError,
  type DomainErrorCode,
  fail,
  ok,
  type RefreshRequest,
  type Result,
} from "../../../../src/domain";

const storedSession = { "auth.accessToken": "old-access", "auth.refreshToken": "old-refresh" };

const tokens: AuthResponse = {
  accessToken: "new-access",
  refreshToken: "new-refresh",
  expiresIn: 900,
};

class FakeAuthRepository implements AuthRepositoryInterface {
  readonly requests: RefreshRequest[] = [];

  constructor(private readonly result: Result<AuthResponse, DomainError>) {}

  async signup(): Promise<Result<AuthResponse, DomainError>> {
    return this.result;
  }

  async login(): Promise<Result<AuthResponse, DomainError>> {
    return this.result;
  }

  async refresh(request: RefreshRequest): Promise<Result<AuthResponse, DomainError>> {
    this.requests.push(request);
    return this.result;
  }
}

class FakeKeyValueStore implements KeyValueStoreInterface {
  constructor(
    public values: Record<string, string> = {},
    private readonly failOnSet = false,
  ) {}

  async get(key: string): Promise<string | null> {
    return this.values[key] ?? null;
  }

  async set(key: string, value: string): Promise<void> {
    if (this.failOnSet) {
      throw new Error("storage unavailable");
    }
    this.values[key] = value;
  }

  async remove(key: string): Promise<void> {
    delete this.values[key];
  }
}

function failure(code: DomainErrorCode) {
  return fail(new DomainError(code, "Something failed"));
}

describe("RefreshSessionUseCase", () => {
  it("sends the stored refresh token", async () => {
    // Arrange
    const repository = new FakeAuthRepository(ok(tokens));
    const useCase = new RefreshSessionUseCase(
      repository,
      new FakeKeyValueStore({ ...storedSession }),
    );

    // Act
    await useCase.execute();

    // Assert
    expect(repository.requests).toEqual([{ refreshToken: "old-refresh" }]);
  });

  it("stores both new tokens and returns them", async () => {
    // Arrange
    const store = new FakeKeyValueStore({ ...storedSession });
    const useCase = new RefreshSessionUseCase(new FakeAuthRepository(ok(tokens)), store);

    // Act
    const result = await useCase.execute();

    // Assert
    expect(result).toEqual(ok({ accessToken: "new-access", refreshToken: "new-refresh" }));
    expect(store.values).toEqual({
      "auth.accessToken": "new-access",
      "auth.refreshToken": "new-refresh",
    });
  });

  it("returns UNAUTHORIZED, sends nothing and clears the session when no refresh token is stored", async () => {
    // Arrange
    const repository = new FakeAuthRepository(ok(tokens));
    const store = new FakeKeyValueStore({ "auth.accessToken": "old-access" });
    const useCase = new RefreshSessionUseCase(repository, store);

    // Act
    const result = await useCase.execute();

    // Assert
    expect(result).toEqual(fail(expect.objectContaining({ code: "UNAUTHORIZED" })));
    expect(repository.requests).toEqual([]);
    expect(store.values).toEqual({});
  });

  it.each<DomainErrorCode>(["UNAUTHORIZED", "VALIDATION"])(
    "clears the session and returns UNAUTHORIZED when the refresh fails with %s",
    async (code) => {
      // Arrange
      const store = new FakeKeyValueStore({ ...storedSession });
      const useCase = new RefreshSessionUseCase(new FakeAuthRepository(failure(code)), store);

      // Act
      const result = await useCase.execute();

      // Assert
      expect(result).toEqual(fail(expect.objectContaining({ code: "UNAUTHORIZED" })));
      expect(store.values).toEqual({});
    },
  );

  it.each<DomainErrorCode>(["NETWORK", "UNEXPECTED"])(
    "keeps the session and returns the failure unchanged when the refresh fails with %s",
    async (code) => {
      // Arrange
      const error = failure(code);
      const store = new FakeKeyValueStore({ ...storedSession });
      const useCase = new RefreshSessionUseCase(new FakeAuthRepository(error), store);

      // Act
      const result = await useCase.execute();

      // Assert
      expect(result).toEqual(error);
      expect(store.values).toEqual(storedSession);
    },
  );

  it("returns UNEXPECTED when the new tokens cannot be stored", async () => {
    // Arrange
    const useCase = new RefreshSessionUseCase(
      new FakeAuthRepository(ok(tokens)),
      new FakeKeyValueStore({ ...storedSession }, true),
    );

    // Act
    const result = await useCase.execute();

    // Assert
    expect(result).toEqual(fail(expect.objectContaining({ code: "UNEXPECTED" })));
  });

  it("sends one request and gives the same result to two concurrent calls", async () => {
    // Arrange
    const repository = new FakeAuthRepository(ok(tokens));
    const useCase = new RefreshSessionUseCase(
      repository,
      new FakeKeyValueStore({ ...storedSession }),
    );

    // Act
    const [first, second] = await Promise.all([useCase.execute(), useCase.execute()]);

    // Assert
    expect(repository.requests).toHaveLength(1);
    expect(second).toBe(first);
  });

  it("sends a new request with the newly stored token once the previous refresh has settled", async () => {
    // Arrange
    const repository = new FakeAuthRepository(ok(tokens));
    const useCase = new RefreshSessionUseCase(
      repository,
      new FakeKeyValueStore({ ...storedSession }),
    );
    await useCase.execute();

    // Act
    await useCase.execute();

    // Assert
    expect(repository.requests).toEqual([
      { refreshToken: "old-refresh" },
      { refreshToken: "new-refresh" },
    ]);
  });
});
