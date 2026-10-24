import { describe, expect, it } from "vitest";
import type { KeyValueStoreInterface } from "../../../../src/application";
import { LogoutUseCase } from "../../../../src/application";
import { fail, ok } from "../../../../src/domain";

class FakeKeyValueStore implements KeyValueStoreInterface {
  constructor(
    public values: Record<string, string> = {},
    private readonly failOnRemove = false,
  ) {}

  async get(key: string): Promise<string | null> {
    return this.values[key] ?? null;
  }

  async set(): Promise<void> {}

  async remove(key: string): Promise<void> {
    if (this.failOnRemove) {
      throw new Error("storage unavailable");
    }
    delete this.values[key];
  }
}

describe("LogoutUseCase", () => {
  it("removes both tokens and succeeds", async () => {
    // Arrange
    const store = new FakeKeyValueStore({
      "auth.accessToken": "access",
      "auth.refreshToken": "refresh",
    });
    const useCase = new LogoutUseCase(store);

    // Act
    const result = await useCase.execute();

    // Assert
    expect(result).toEqual(ok(undefined));
    expect(store.values).toEqual({});
  });

  it("succeeds when nothing is stored", async () => {
    // Arrange
    const useCase = new LogoutUseCase(new FakeKeyValueStore());

    // Act
    const result = await useCase.execute();

    // Assert
    expect(result).toEqual(ok(undefined));
  });

  it("returns UNEXPECTED when the storage cannot remove a token", async () => {
    // Arrange
    const useCase = new LogoutUseCase(new FakeKeyValueStore({ "auth.accessToken": "a" }, true));

    // Act
    const result = await useCase.execute();

    // Assert
    expect(result).toEqual(fail(expect.objectContaining({ code: "UNEXPECTED" })));
  });
});
