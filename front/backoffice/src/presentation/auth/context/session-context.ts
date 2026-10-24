import { createContext } from "react";
import type { DomainError, Result } from "@weight-tracker/core/domain";

export type SessionStatus = "restoring" | "signedIn" | "signedOut";

export interface SessionContextValue {
  status: SessionStatus;
  /**
   * Runs a protected call. When it fails as unauthorized, renews the session once and runs it once more; when the
   * session cannot be renewed, ends it.
   */
  runWithSession: <T>(
    call: () => Promise<Result<T, DomainError>>,
  ) => Promise<Result<T, DomainError>>;
  /** Marks the session as ended, once its tokens are gone. */
  end: () => void;
  /** Calls `listener` every time the session ends; returns the unsubscribe. */
  onEnd: (listener: () => void) => () => void;
  /** Reads the stored session again; called after anything that writes the tokens. */
  reload: () => Promise<void>;
}

const SessionContext = createContext<SessionContextValue | null>(null);

export default SessionContext;
