import type { KeyValueStoreInterface } from "@weight-tracker/core/application";

/** Stand-in until login brings the SQLite adapter. Nothing survives a restart. */
export default class InMemoryKeyValueStore implements KeyValueStoreInterface {
  private readonly values = new Map<string, string>();

  async get(key: string): Promise<string | null> {
    return this.values.get(key) ?? null;
  }

  async set(key: string, value: string): Promise<void> {
    this.values.set(key, value);
  }

  async remove(key: string): Promise<void> {
    this.values.delete(key);
  }
}
