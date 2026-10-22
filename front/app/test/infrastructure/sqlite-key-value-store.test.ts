import SqliteKeyValueStore from "@/infrastructure/sqlite-key-value-store";

const rows = new Map<string, string>();
const mockOpenDatabaseAsync = jest.fn();
const execAsync = jest.fn(async (_source: string) => {});

jest.mock("expo-sqlite", () => ({
  openDatabaseAsync: (...args: unknown[]) => mockOpenDatabaseAsync(...args),
}));

beforeEach(() => {
  rows.clear();
  execAsync.mockClear();
  mockOpenDatabaseAsync.mockReset();
  mockOpenDatabaseAsync.mockImplementation(async () => ({
    execAsync,
    getFirstAsync: async (_source: string, key: string) =>
      rows.has(key) ? { value: rows.get(key) } : null,
    runAsync: async (source: string, key: string, value?: string) => {
      if (source.startsWith("INSERT")) {
        rows.set(key, value as string);
      } else {
        rows.delete(key);
      }
    },
  }));
});

describe("SqliteKeyValueStore", () => {
  it("returns null for a missing key", async () => {
    // Arrange
    const store = new SqliteKeyValueStore();

    // Act
    const value = await store.get("missing");

    // Assert
    expect(value).toBeNull();
  });

  it("returns a stored value", async () => {
    // Arrange
    const store = new SqliteKeyValueStore();
    await store.set("auth.accessToken", "access");

    // Act
    const value = await store.get("auth.accessToken");

    // Assert
    expect(value).toBe("access");
  });

  it("overwrites an existing value", async () => {
    // Arrange
    const store = new SqliteKeyValueStore();
    await store.set("auth.accessToken", "old");
    await store.set("auth.accessToken", "new");

    // Act
    const value = await store.get("auth.accessToken");

    // Assert
    expect(value).toBe("new");
  });

  it("removes a value", async () => {
    // Arrange
    const store = new SqliteKeyValueStore();
    await store.set("auth.accessToken", "access");
    await store.remove("auth.accessToken");

    // Act
    const value = await store.get("auth.accessToken");

    // Assert
    expect(value).toBeNull();
  });

  it("opens the database and creates the table once across calls", async () => {
    // Arrange
    const store = new SqliteKeyValueStore();
    await store.set("a", "1");
    await store.get("a");

    // Act
    await store.remove("a");

    // Assert
    expect(mockOpenDatabaseAsync).toHaveBeenCalledTimes(1);
    expect(execAsync).toHaveBeenCalledTimes(1);
  });

  it("does not open the database until it is used", () => {
    // Arrange & Act
    new SqliteKeyValueStore();

    // Assert
    expect(mockOpenDatabaseAsync).not.toHaveBeenCalled();
  });
});
