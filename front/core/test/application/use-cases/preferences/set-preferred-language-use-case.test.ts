import { describe, expect, it } from "vitest";
import type { KeyValueStoreInterface } from "../../../../src/application";
import { SetPreferredLanguageUseCase } from "../../../../src/application";
import { fail, ok } from "../../../../src/domain";

class FakeKeyValueStore implements KeyValueStoreInterface {
  readonly values: Record<string, string> = {};

  constructor(private readonly failOnSet = false) {}

  async get(key: string): Promise<string | null> {
    return this.values[key] ?? null;
  }

  async set(key: string, value: string): Promise<void> {
    if (this.failOnSet) {
      throw new Error("storage unavailable");
    }
    this.values[key] = value;
  }

  async remove(): Promise<void> {}
}

describe("SetPreferredLanguageUseCase", () => {
  it("stores the language under the preferences key", async () => {
    // Arrange
    const store = new FakeKeyValueStore();
    const useCase = new SetPreferredLanguageUseCase(store);

    // Act
    const result = await useCase.execute("en");

    // Assert
    expect(result).toEqual(ok(undefined));
    expect(store.values).toEqual({ "preferences.language": "en" });
  });

  it("returns UNEXPECTED when the storage cannot be written", async () => {
    // Arrange
    const useCase = new SetPreferredLanguageUseCase(new FakeKeyValueStore(true));

    // Act
    const result = await useCase.execute("en");

    // Assert
    expect(result).toEqual(fail(expect.objectContaining({ code: "UNEXPECTED" })));
  });
});
