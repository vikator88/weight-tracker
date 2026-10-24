import { DomainError, fail, ok, type Result } from "../../../domain";
import type { KeyValueStoreInterface, UseCaseInterface } from "../../ports";

export const PREFERRED_LANGUAGE_KEY = "preferences.language";

/** Reads the language code chosen on this device. Makes no API call; nothing stored is not a failure. */
export default class GetPreferredLanguageUseCase implements UseCaseInterface<void, string | null> {
  constructor(private readonly keyValueStore: KeyValueStoreInterface) {}

  async execute(): Promise<Result<string | null, DomainError>> {
    try {
      return ok(await this.keyValueStore.get(PREFERRED_LANGUAGE_KEY));
    } catch {
      return fail(new DomainError("UNEXPECTED", "The preferred language could not be read"));
    }
  }
}
