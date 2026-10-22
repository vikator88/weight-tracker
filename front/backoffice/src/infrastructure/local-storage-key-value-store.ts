import type { KeyValueStoreInterface } from "@weight-tracker/core/application";

/** Persists values in the browser's `localStorage`. Only touched inside the methods: prerendering has none. */
export default class LocalStorageKeyValueStore implements KeyValueStoreInterface {
  async get(key: string): Promise<string | null> {
    return localStorage.getItem(key);
  }

  async set(key: string, value: string): Promise<void> {
    localStorage.setItem(key, value);
  }

  async remove(key: string): Promise<void> {
    localStorage.removeItem(key);
  }
}
