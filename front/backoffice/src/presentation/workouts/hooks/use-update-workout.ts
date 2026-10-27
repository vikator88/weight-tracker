import { useState } from "react";
import type { DomainErrorCode, UpdateWorkoutInput, Workout } from "@weight-tracker/core/domain";
import useSession from "@/presentation/auth/hooks/use-session";
import useContainer from "@/presentation/common/hooks/use-container";

interface UpdateWorkoutState {
  saving: boolean;
  errorCode: DomainErrorCode | null;
  saved: boolean;
  /** Resolves to the saved workout, or `null` when the API refused it. */
  save: (input: UpdateWorkoutInput) => Promise<Workout | null>;
  clearSaved: () => void;
}

export default function useUpdateWorkout(): UpdateWorkoutState {
  const container = useContainer();
  const { runWithSession } = useSession();
  const [saving, setSaving] = useState(false);
  const [errorCode, setErrorCode] = useState<DomainErrorCode | null>(null);
  const [saved, setSaved] = useState(false);

  async function save(input: UpdateWorkoutInput) {
    setSaving(true);
    setErrorCode(null);
    setSaved(false);

    const result = await runWithSession(() =>
      container.resolve("updateWorkoutUseCase").execute(input),
    );

    setSaving(false);
    if (result.ok) {
      setSaved(true);
      return result.value;
    }
    setErrorCode(result.error.code);
    return null;
  }

  return { saving, errorCode, saved, save, clearSaved: () => setSaved(false) };
}
