import { useCallback, useEffect, useMemo, useState, type ReactNode } from "react";
import type { DomainErrorCode } from "@weight-tracker/core/domain";
import useContainer from "@/presentation/common/hooks/use-container";
import SessionContext, { type SessionStatus } from "./session-context";

interface SessionProviderProps {
  children: ReactNode;
}

interface SessionSnapshot {
  status: SessionStatus;
  errorCode: DomainErrorCode | null;
}

export default function SessionProvider({ children }: SessionProviderProps) {
  const container = useContainer();
  const [snapshot, setSnapshot] = useState<SessionSnapshot>({
    status: "restoring",
    errorCode: null,
  });

  const read = useCallback(async (): Promise<SessionSnapshot> => {
    const result = await container.resolve("getSessionUseCase").execute();

    if (!result.ok) {
      return { status: "signedOut", errorCode: result.error.code };
    }

    return { status: result.value ? "signedIn" : "signedOut", errorCode: null };
  }, [container]);

  const refresh = useCallback(async () => {
    setSnapshot(await read());
  }, [read]);

  useEffect(() => {
    let active = true;

    // Restore the stored session once on mount.
    void read().then((next) => {
      if (active) {
        setSnapshot(next);
      }
    });

    return () => {
      active = false;
    };
  }, [read]);

  const value = useMemo(() => ({ ...snapshot, refresh }), [snapshot, refresh]);

  return <SessionContext value={value}>{children}</SessionContext>;
}
