import { useState } from "react";
import type { DomainErrorCode } from "@weight-tracker/core/domain";
import useSession from "@/presentation/auth/hooks/use-session";
import useContainer from "./use-container";

interface LogoutState {
  loggingOut: boolean;
  errorCode: DomainErrorCode | null;
  /** Resolves to `true` when the session was cleared; the caller decides where to go next. */
  logout: () => Promise<boolean>;
}

export default function useLogout(): LogoutState {
  const container = useContainer();
  const { end } = useSession();
  const [loggingOut, setLoggingOut] = useState(false);
  const [errorCode, setErrorCode] = useState<DomainErrorCode | null>(null);

  async function logout() {
    setLoggingOut(true);
    setErrorCode(null);

    const result = await container.resolve("logoutUseCase").execute();

    if (result.ok) {
      end();
      return true;
    }
    setErrorCode(result.error.code);
    setLoggingOut(false);
    return false;
  }

  return { loggingOut, errorCode, logout };
}
