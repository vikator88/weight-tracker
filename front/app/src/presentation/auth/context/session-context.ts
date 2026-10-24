import { createContext } from "react";
import type { DomainError, DomainErrorCode, Result } from "@weight-tracker/core/domain";

export type SessionStatus = "restoring" | "signedIn" | "signedOut";

export interface SessionState {
  status: SessionStatus;
  errorCode: DomainErrorCode | null;
  /** Reads the stored session again; called after anything that writes or clears the tokens. */
  reload: () => Promise<void>;
  /**
   * Runs a protected call. When it fails as unauthorized, renews the session once and runs it once more; when the
   * session cannot be renewed, ends it.
   */
  runWithSession: <T>(
    call: () => Promise<Result<T, DomainError>>,
  ) => Promise<Result<T, DomainError>>;
}

const SessionContext = createContext<SessionState>({
  status: "restoring",
  errorCode: null,
  reload: async () => {},
  runWithSession: (call) => call(),
});

export default SessionContext;
