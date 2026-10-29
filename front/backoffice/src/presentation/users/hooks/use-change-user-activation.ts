import { useCallback, useState } from "react";
import type { DomainErrorCode, UserSummary } from "@weight-tracker/core/domain";
import useSession from "@/presentation/auth/hooks/use-session";
import useContainer from "@/presentation/common/hooks/use-container";

interface ChangeUserActivationState {
  submitting: boolean;
  errorCode: DomainErrorCode | null;
  /**
   * Deactivates the user when the list shows them active, and activates them otherwise. Resolves to `null` once the
   * API has changed it, or to the error code when it refused.
   */
  change: (user: UserSummary) => Promise<DomainErrorCode | null>;
  reset: () => void;
}

export default function useChangeUserActivation(): ChangeUserActivationState {
  const container = useContainer();
  const { runWithSession } = useSession();
  const [submitting, setSubmitting] = useState(false);
  const [errorCode, setErrorCode] = useState<DomainErrorCode | null>(null);

  async function change(user: UserSummary) {
    setSubmitting(true);
    setErrorCode(null);

    // Which operation follows the state the list showed; the API answers `CONFLICT` if that state is stale.
    const key = user.isActive ? "deactivateUserUseCase" : "activateUserUseCase";
    const result = await runWithSession(() => container.resolve(key).execute(user.id));

    setSubmitting(false);
    if (result.ok) {
      return null;
    }
    setErrorCode(result.error.code);
    return result.error.code;
  }

  const reset = useCallback(() => {
    setSubmitting(false);
    setErrorCode(null);
  }, []);

  return { submitting, errorCode, change, reset };
}
