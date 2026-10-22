import { DomainError, fail, ok, type Result, type Session } from "../../../domain";
import type { KeyValueStoreInterface, UseCaseInterface } from "../../ports";
import { ACCESS_TOKEN_KEY, REFRESH_TOKEN_KEY } from "./signup-use-case";

/** Reads the stored session. Makes no API call; a missing token means no session, not a failure. */
export default class GetSessionUseCase implements UseCaseInterface<void, Session | null> {
  constructor(private readonly keyValueStore: KeyValueStoreInterface) {}

  async execute(): Promise<Result<Session | null, DomainError>> {
    try {
      const accessToken = await this.keyValueStore.get(ACCESS_TOKEN_KEY);
      const refreshToken = await this.keyValueStore.get(REFRESH_TOKEN_KEY);

      return ok(accessToken && refreshToken ? { accessToken, refreshToken } : null);
    } catch {
      return fail(new DomainError("UNEXPECTED", "The session could not be read"));
    }
  }
}
