import { describe, expect, it } from "vitest";
import type { KeyValueStoreInterface } from "../../../../src/application";
import { SetPreferredThemeModeUseCase } from "../../../../src/application";
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

describe("SetPreferredThemeModeUseCase", () => {
  it("stores the theme mode under the preferences key", async () => {
    // Arrange
    const store = new FakeKeyValueStore();
    const useCase = new SetPreferredThemeModeUseCase(store);

    // Act
    const result = await useCase.execute("dark");

    // Assert
    expect(result).toEqual(ok(undefined));
    expect(store.values).toEqual({ "preferences.themeMode": "dark" });
  });

  it("returns UNEXPECTED when the storage cannot be written", async () => {
    // Arrange
    const useCase = new SetPreferredThemeModeUseCase(new FakeKeyValueStore(true));

    // Act
    const result = await useCase.execute("dark");

    // Assert
    expect(result).toEqual(fail(expect.objectContaining({ code: "UNEXPECTED" })));
  });
});
