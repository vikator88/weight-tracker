import { use } from "react";
import SessionContext, { type SessionContextValue } from "../context/session-context";

export default function useSession(): SessionContextValue {
  const context = use(SessionContext);

  if (context === null) {
    throw new Error("useSession must be used inside SessionProvider");
  }

  return context;
}
