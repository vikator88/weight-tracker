import type { KeyValueStoreInterface } from "@weight-tracker/core/application";
import * as SQLite from "expo-sqlite";

const DATABASE_NAME = "weight-tracker.db";

/** Keeps the session on the device. SQLite data is not encrypted, an accepted decision of the invariants. */
export default class SqliteKeyValueStore implements KeyValueStoreInterface {
  private database: Promise<SQLite.SQLiteDatabase> | null = null;

  async get(key: string): Promise<string | null> {
    const database = await this.open();
    const row = await database.getFirstAsync<{ value: string }>(
      "SELECT value FROM key_value WHERE key = ?",
      key,
    );

    return row?.value ?? null;
  }

  async set(key: string, value: string): Promise<void> {
    const database = await this.open();
    await database.runAsync(
      "INSERT OR REPLACE INTO key_value (key, value) VALUES (?, ?)",
      key,
      value,
    );
  }

  async remove(key: string): Promise<void> {
    const database = await this.open();
    await database.runAsync("DELETE FROM key_value WHERE key = ?", key);
  }

  /** Opens the database and creates its table on first use; never at import or construction time. */
  private open(): Promise<SQLite.SQLiteDatabase> {
    if (this.database === null) {
      this.database = SQLite.openDatabaseAsync(DATABASE_NAME).then(async (database) => {
        await database.execAsync(
          "CREATE TABLE IF NOT EXISTS key_value (key TEXT PRIMARY KEY NOT NULL, value TEXT NOT NULL)",
        );
        return database;
      });
      // A failed open must be retried by the next call instead of being cached.
      this.database.catch(() => {
        this.database = null;
      });
    }

    return this.database;
  }
}
