import { useCallback, useEffect, useRef, useState } from "react";
import type { DomainErrorCode, UserSummary } from "@weight-tracker/core/domain";
import useSession from "@/presentation/auth/hooks/use-session";
import useContainer from "@/presentation/common/hooks/use-container";

type UsersStatus = "loading" | "loaded" | "failed";

interface UsersState {
  status: UsersStatus;
  users: UserSummary[];
  errorCode: DomainErrorCode | null;
  reload: () => void;
}

/** Reads the users the caller may see when the screen opens. Held by the screen only, so it resets on exit. */
export default function useUsers(): UsersState {
  const container = useContainer();
  const { runWithSession } = useSession();
  const [status, setStatus] = useState<UsersStatus>("loading");
  const [users, setUsers] = useState<UserSummary[]>([]);
  const [errorCode, setErrorCode] = useState<DomainErrorCode | null>(null);
  // Bumped by every read and on unmount, so an answer that arrives after either is ignored.
  const generation = useRef(0);

  // State changes only when the answer arrives; the caller sets "loading" beforehand if it needs to.
  const request = useCallback(() => {
    const current = ++generation.current;

    void runWithSession(() => container.resolve("getUsersUseCase").execute()).then((result) => {
      if (current !== generation.current) {
        return;
      }
      if (result.ok) {
        setUsers(result.value);
        setStatus("loaded");
      } else {
        setErrorCode(result.error.code);
        setStatus("failed");
      }
    });
  }, [container, runWithSession]);

  const reload = useCallback(() => {
    setStatus("loading");
    setErrorCode(null);
    request();
  }, [request]);

  // The initial state is already "loading", so opening the screen only sends the request.
  useEffect(() => {
    request();
    return () => {
      generation.current += 1;
    };
  }, [request]);

  return { status, users, errorCode, reload };
}
