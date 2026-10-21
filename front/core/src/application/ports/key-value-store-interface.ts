/** Platform-specific: implemented by each front (e.g. localStorage on web, SQLite on mobile). */
export default interface KeyValueStoreInterface {
  get(key: string): Promise<string | null>;
  set(key: string, value: string): Promise<void>;
  remove(key: string): Promise<void>;
}
