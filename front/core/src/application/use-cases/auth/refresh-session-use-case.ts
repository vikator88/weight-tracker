import { DomainError, fail, ok, type Result, type Session } from "../../../domain";
import type {
  AuthRepositoryInterface,
  KeyValueStoreInterface,
  UseCaseInterface,
} from "../../ports";
import { ACCESS_TOKEN_KEY, REFRESH_TOKEN_KEY } from "./signup-use-case";

/**
 * Renews the session with the stored refresh token and stores the new tokens. A rejected or missing refresh token
 * clears the session and fails with `UNAUTHORIZED`; any other failure keeps the tokens.
 */
export default class RefreshSessionUseCase implements UseCaseInterface<void, Session> {
  // The API revokes a refresh token once used, so concurrent callers share the refresh in flight.
  private inFlight: Promise<Result<Session, DomainError>> | null = null;

  constructor(
    private readonly authRepository: AuthRepositoryInterface,
    private readonly keyValueStore: KeyValueStoreInterface,
  ) {}

  execute(): Promise<Result<Session, DomainError>> {
    if (this.inFlight === null) {
      this.inFlight = this.refresh().finally(() => {
        this.inFlight = null;
      });
    }

    return this.inFlight;
  }

  private async refresh(): Promise<Result<Session, DomainError>> {
    let refreshToken: string | null;
    try {
      refreshToken = await this.keyValueStore.get(REFRESH_TOKEN_KEY);
    } catch {
      return fail(new DomainError("UNEXPECTED", "The session could not be read"));
    }

    if (!refreshToken) {
      return this.end("There is no session to refresh");
    }

    const result = await this.authRepository.refresh({ refreshToken });
    if (!result.ok) {
      const rejected = result.error.code === "UNAUTHORIZED" || result.error.code === "VALIDATION";
      return rejected ? this.end(result.error.message) : result;
    }

    try {
      await this.keyValueStore.set(ACCESS_TOKEN_KEY, result.value.accessToken);
      await this.keyValueStore.set(REFRESH_TOKEN_KEY, result.value.refreshToken);
    } catch {
      return fail(new DomainError("UNEXPECTED", "The session could not be stored"));
    }

    return ok({ accessToken: result.value.accessToken, refreshToken: result.value.refreshToken });
  }

  private async end(message: string): Promise<Result<Session, DomainError>> {
    try {
      // The refresh token goes first: an access token alone already reads as no session.
      await this.keyValueStore.remove(REFRESH_TOKEN_KEY);
      await this.keyValueStore.remove(ACCESS_TOKEN_KEY);
    } catch {
      return fail(new DomainError("UNEXPECTED", "The session could not be cleared"));
    }

    return fail(new DomainError("UNAUTHORIZED", message));
  }
}
