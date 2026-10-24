import { describe, expect, it } from "vitest";
import type { KeyValueStoreInterface } from "../../../../src/application";
import { GetPreferredLanguageUseCase } from "../../../../src/application";
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

describe("GetPreferredLanguageUseCase", () => {
  it("returns the stored language", async () => {
    // Arrange
    const useCase = new GetPreferredLanguageUseCase(
      new FakeKeyValueStore({ "preferences.language": "en" }),
    );

    // Act
    const result = await useCase.execute();

    // Assert
    expect(result).toEqual(ok("en"));
  });

  it("returns no language when nothing is stored", async () => {
    // Arrange
    const useCase = new GetPreferredLanguageUseCase(new FakeKeyValueStore());

    // Act
    const result = await useCase.execute();

    // Assert
    expect(result).toEqual(ok(null));
  });

  it("returns UNEXPECTED when the storage cannot be read", async () => {
    // Arrange
    const useCase = new GetPreferredLanguageUseCase(new FakeKeyValueStore({}, true));

    // Act
    const result = await useCase.execute();

    // Assert
    expect(result).toEqual(fail(expect.objectContaining({ code: "UNEXPECTED" })));
  });
});
