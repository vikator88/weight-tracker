import { DomainError, fail, ok, type Result } from "../../../domain";
import type { KeyValueStoreInterface, UseCaseInterface } from "../../ports";
import { PREFERRED_THEME_MODE_KEY } from "./get-preferred-theme-mode-use-case";

/** Remembers the theme mode (light or dark) chosen on this device. Makes no API call and survives logout. */
export default class SetPreferredThemeModeUseCase implements UseCaseInterface<string, void> {
  constructor(private readonly keyValueStore: KeyValueStoreInterface) {}

  async execute(mode: string): Promise<Result<void, DomainError>> {
    try {
      await this.keyValueStore.set(PREFERRED_THEME_MODE_KEY, mode);

      return ok(undefined);
    } catch {
      return fail(new DomainError("UNEXPECTED", "The preferred theme mode could not be stored"));
    }
  }
}
