import { useCallback, useEffect, useMemo, useState, type ReactNode } from "react";
import type { DomainError, DomainErrorCode, Result } from "@weight-tracker/core/domain";
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

  const reload = useCallback(async () => {
    setSnapshot(await read());
  }, [read]);

  const runWithSession = useCallback(
    async <T,>(call: () => Promise<Result<T, DomainError>>): Promise<Result<T, DomainError>> => {
      const result = await call();
      if (result.ok || result.error.code !== "UNAUTHORIZED") {
        return result;
      }

      const refreshed = await container.resolve("refreshSessionUseCase").execute();
      if (refreshed.ok) {
        // Retried once only, so a call that keeps failing as unauthorized cannot loop.
        return call();
      }
      if (refreshed.error.code === "UNAUTHORIZED") {
        // The session gate reacts to the signed-out state; nothing navigates here.
        setSnapshot({ status: "signedOut", errorCode: null });
      }
      return refreshed;
    },
    [container],
  );

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

  const value = useMemo(
    () => ({ ...snapshot, reload, runWithSession }),
    [snapshot, reload, runWithSession],
  );

  return <SessionContext value={value}>{children}</SessionContext>;
}
