import { use } from "react";
import SessionContext, { type SessionState } from "../context/session-context";

export default function useSession(): SessionState {
  return use(SessionContext);
}
