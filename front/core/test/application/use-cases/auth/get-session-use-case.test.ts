import { describe, expect, it } from "vitest";
import type { KeyValueStoreInterface } from "../../../../src/application";
import { GetSessionUseCase } from "../../../../src/application";
import { fail, ok } from "../../../../src/domain";

class FakeKeyValueStore implements KeyValueStoreInterface {
  constructor(
    private readonly values: Record<string, string> = {},
    private readonly failOnGet = false,
  ) {}

  async get(key: string): Promise<string | null> {
    if (this.failOnGet) {
      throw new Error("storage unavailable");
    }
    return this.values[key] ?? null;
  }

  async set(): Promise<void> {}

  async remove(): Promise<void> {}
}

describe("GetSessionUseCase", () => {
  it("returns the session when both tokens are stored", async () => {
    // Arrange
    const store = new FakeKeyValueStore({
      "auth.accessToken": "access",
      "auth.refreshToken": "refresh",
    });
    const useCase = new GetSessionUseCase(store);

    // Act
    const result = await useCase.execute();

    // Assert
    expect(result).toEqual(ok({ accessToken: "access", refreshToken: "refresh" }));
  });

  it("returns no session when nothing is stored", async () => {
    // Arrange
    const useCase = new GetSessionUseCase(new FakeKeyValueStore());

    // Act
    const result = await useCase.execute();

    // Assert
    expect(result).toEqual(ok(null));
  });

  it("returns no session when only the access token is stored", async () => {
    // Arrange
    const useCase = new GetSessionUseCase(new FakeKeyValueStore({ "auth.accessToken": "access" }));

    // Act
    const result = await useCase.execute();

    // Assert
    expect(result).toEqual(ok(null));
  });

  it("returns UNEXPECTED when the storage cannot be read", async () => {
    // Arrange
    const useCase = new GetSessionUseCase(new FakeKeyValueStore({}, true));

    // Act
    const result = await useCase.execute();

    // Assert
    expect(result).toEqual(fail(expect.objectContaining({ code: "UNEXPECTED" })));
  });
});
