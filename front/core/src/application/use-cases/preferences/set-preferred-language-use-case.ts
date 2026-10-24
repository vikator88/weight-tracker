import { DomainError, fail, ok, type Result } from "../../../domain";
import type { KeyValueStoreInterface, UseCaseInterface } from "../../ports";
import { PREFERRED_LANGUAGE_KEY } from "./get-preferred-language-use-case";

/** Remembers the language code chosen on this device. Makes no API call and survives logout. */
export default class SetPreferredLanguageUseCase implements UseCaseInterface<string, void> {
  constructor(private readonly keyValueStore: KeyValueStoreInterface) {}

  async execute(language: string): Promise<Result<void, DomainError>> {
    try {
      await this.keyValueStore.set(PREFERRED_LANGUAGE_KEY, language);

      return ok(undefined);
    } catch {
      return fail(new DomainError("UNEXPECTED", "The preferred language could not be stored"));
    }
  }
}
