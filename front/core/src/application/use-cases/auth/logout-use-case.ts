import { DomainError, fail, ok, type Result } from "../../../domain";
import type { KeyValueStoreInterface, UseCaseInterface } from "../../ports";
import { ACCESS_TOKEN_KEY, REFRESH_TOKEN_KEY } from "./signup-use-case";

/** Ends the session on this device by removing both tokens. Makes no API call. */
export default class LogoutUseCase implements UseCaseInterface<void, void> {
  constructor(private readonly keyValueStore: KeyValueStoreInterface) {}

  async execute(): Promise<Result<void, DomainError>> {
    try {
      // The refresh token goes first: an access token alone already reads as no session.
      await this.keyValueStore.remove(REFRESH_TOKEN_KEY);
      await this.keyValueStore.remove(ACCESS_TOKEN_KEY);

      return ok(undefined);
    } catch {
      return fail(new DomainError("UNEXPECTED", "The session could not be cleared"));
    }
  }
}
