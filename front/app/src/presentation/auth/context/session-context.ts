import { createContext } from "react";
import type { DomainErrorCode } from "@weight-tracker/core/domain";

export type SessionStatus = "restoring" | "signedIn" | "signedOut";

export interface SessionState {
  status: SessionStatus;
  errorCode: DomainErrorCode | null;
  /** Reads the stored session again; called after anything that writes or clears the tokens. */
  refresh: () => Promise<void>;
}

const SessionContext = createContext<SessionState>({
  status: "restoring",
  errorCode: null,
  refresh: async () => {},
});

export default SessionContext;
