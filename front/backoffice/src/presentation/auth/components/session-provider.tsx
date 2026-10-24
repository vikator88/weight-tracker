"use client";

import { useCallback, useEffect, useMemo, useRef, useState, type ReactNode } from "react";
import type { DomainError, Result } from "@weight-tracker/core/domain";
import useContainer from "@/presentation/common/hooks/use-container";
import SessionContext, {
  type SessionContextValue,
  type SessionStatus,
} from "../context/session-context";

interface SessionProviderProps {
  children: ReactNode;
}

/** Holds whether a session is stored, for the guard and every signed-in screen. Read once on mount. */
export default function SessionProvider({ children }: SessionProviderProps) {
  const container = useContainer();
  const [status, setStatus] = useState<SessionStatus>("restoring");
  const endListeners = useRef(new Set<() => void>());

  const read = useCallback(async (): Promise<SessionStatus> => {
    const result = await container.resolve("getSessionUseCase").execute();
    return result.ok && result.value !== null ? "signedIn" : "signedOut";
  }, [container]);

  const reload = useCallback(async () => {
    setStatus(await read());
  }, [read]);

  const end = useCallback(() => {
    setStatus("signedOut");
    endListeners.current.forEach((listener) => listener());
  }, []);

  const onEnd = useCallback((listener: () => void) => {
    endListeners.current.add(listener);
    return () => {
      endListeners.current.delete(listener);
    };
  }, []);

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
        end();
      }
      return refreshed;
    },
    [container, end],
  );

  useEffect(() => {
    let active = true;

    void read().then((next) => {
      if (active) {
        setStatus(next);
      }
    });

    return () => {
      active = false;
    };
  }, [read]);

  const value = useMemo<SessionContextValue>(
    () => ({ status, runWithSession, end, onEnd, reload }),
    [status, runWithSession, end, onEnd, reload],
  );

  return <SessionContext value={value}>{children}</SessionContext>;
}
