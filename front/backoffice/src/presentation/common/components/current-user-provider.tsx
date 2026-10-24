"use client";

import { useCallback, useMemo, useRef, useState, type ReactNode } from "react";
import type { DomainErrorCode, UserProfile } from "@weight-tracker/core/domain";
import CurrentUserContext, {
  type CurrentUserContextValue,
  type CurrentUserStatus,
} from "../context/current-user-context";
import useContainer from "../hooks/use-container";

interface CurrentUserProviderProps {
  children: ReactNode;
}

/** Holds the signed-in user's profile for every signed-in screen. It reads nothing until `load` is called. */
export default function CurrentUserProvider({ children }: CurrentUserProviderProps) {
  const container = useContainer();
  const [status, setStatus] = useState<CurrentUserStatus>("idle");
  const [profile, setProfile] = useState<UserProfile | null>(null);
  const [errorCode, setErrorCode] = useState<DomainErrorCode | null>(null);
  const statusRef = useRef<CurrentUserStatus>("idle");
  // Bumped by every read and by `clear`, so an answer that arrives after either is ignored.
  const generation = useRef(0);

  const changeStatus = useCallback((next: CurrentUserStatus) => {
    statusRef.current = next;
    setStatus(next);
  }, []);

  const read = useCallback(() => {
    const current = ++generation.current;
    changeStatus("loading");
    setErrorCode(null);

    void container
      .resolve("getMyProfileUseCase")
      .execute()
      .then((result) => {
        if (current !== generation.current) {
          return;
        }
        if (result.ok) {
          setProfile(result.value);
          changeStatus("loaded");
        } else {
          setErrorCode(result.error.code);
          changeStatus("failed");
        }
      });
  }, [container, changeStatus]);

  const load = useCallback(() => {
    if (statusRef.current === "idle") {
      read();
    }
  }, [read]);

  const replace = useCallback(
    (next: UserProfile) => {
      generation.current += 1;
      setProfile(next);
      setErrorCode(null);
      changeStatus("loaded");
    },
    [changeStatus],
  );

  const clear = useCallback(() => {
    generation.current += 1;
    setProfile(null);
    setErrorCode(null);
    changeStatus("idle");
  }, [changeStatus]);

  const value = useMemo<CurrentUserContextValue>(
    () => ({ status, profile, errorCode, load, reload: read, replace, clear }),
    [status, profile, errorCode, load, read, replace, clear],
  );

  return <CurrentUserContext value={value}>{children}</CurrentUserContext>;
}
