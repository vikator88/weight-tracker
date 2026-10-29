import { useCallback, useState } from "react";
import type {
  CreatedUserResponse,
  CreateUserRequest,
  DomainErrorCode,
} from "@weight-tracker/core/domain";
import useSession from "@/presentation/auth/hooks/use-session";
import useContainer from "@/presentation/common/hooks/use-container";

interface CreateUserState {
  creating: boolean;
  errorCode: DomainErrorCode | null;
  /** Resolves to the created user, or `null` when the API refused it. */
  create: (request: CreateUserRequest) => Promise<CreatedUserResponse | null>;
  reset: () => void;
}

export default function useCreateUser(): CreateUserState {
  const container = useContainer();
  const { runWithSession } = useSession();
  const [creating, setCreating] = useState(false);
  const [errorCode, setErrorCode] = useState<DomainErrorCode | null>(null);

  async function create(request: CreateUserRequest) {
    setCreating(true);
    setErrorCode(null);

    const result = await runWithSession(() =>
      container.resolve("createUserUseCase").execute(request),
    );

    setCreating(false);
    if (result.ok) {
      return result.value;
    }
    setErrorCode(result.error.code);
    return null;
  }

  const reset = useCallback(() => {
    setCreating(false);
    setErrorCode(null);
  }, []);

  return { creating, errorCode, create, reset };
}
