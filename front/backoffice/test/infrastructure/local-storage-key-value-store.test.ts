import { beforeEach, describe, expect, it } from "vitest";
import LocalStorageKeyValueStore from "@/infrastructure/local-storage-key-value-store";

describe("LocalStorageKeyValueStore", () => {
  beforeEach(() => {
    localStorage.clear();
  });

  it("returns a stored value", async () => {
    // Arrange
    const store = new LocalStorageKeyValueStore();
    await store.set("key", "value");

    // Act
    const value = await store.get("key");

    // Assert
    expect(value).toBe("value");
  });

  it("returns null for a missing key", async () => {
    // Arrange
    const store = new LocalStorageKeyValueStore();

    // Act
    const value = await store.get("missing");

    // Assert
    expect(value).toBeNull();
  });

  it("removes a stored value", async () => {
    // Arrange
    const store = new LocalStorageKeyValueStore();
    await store.set("key", "value");

    // Act
    await store.remove("key");

    // Assert
    expect(localStorage.getItem("key")).toBeNull();
  });
});
