import { DomainError, fail, ok, type Result } from "../../../domain";
import type { KeyValueStoreInterface, UseCaseInterface } from "../../ports";

export const PREFERRED_THEME_MODE_KEY = "preferences.themeMode";

/** Reads the theme mode (light or dark) chosen on this device. Makes no API call; nothing stored is not a failure. */
export default class GetPreferredThemeModeUseCase implements UseCaseInterface<void, string | null> {
  constructor(private readonly keyValueStore: KeyValueStoreInterface) {}

  async execute(): Promise<Result<string | null, DomainError>> {
    try {
      return ok(await this.keyValueStore.get(PREFERRED_THEME_MODE_KEY));
    } catch {
      return fail(new DomainError("UNEXPECTED", "The preferred theme mode could not be read"));
    }
  }
}
