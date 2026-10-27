import { useState } from "react";
import type { DomainErrorCode } from "@weight-tracker/core/domain";
import useSession from "@/presentation/auth/hooks/use-session";
import useContainer from "@/presentation/common/hooks/use-container";

interface ReorderWorkoutExercisesState {
  /** Whether a reorder request is in progress. */
  reordering: boolean;
  errorCode: DomainErrorCode | null;
  /** Sends the full order of catalog exercise ids; resolves to `true` when the API accepted it. */
  reorder: (workoutId: string, exerciseIds: string[]) => Promise<boolean>;
}

export default function useReorderWorkoutExercises(): ReorderWorkoutExercisesState {
  const container = useContainer();
  const { runWithSession } = useSession();
  const [reordering, setReordering] = useState(false);
  const [errorCode, setErrorCode] = useState<DomainErrorCode | null>(null);

  async function reorder(workoutId: string, exerciseIds: string[]) {
    setReordering(true);
    setErrorCode(null);

    const result = await runWithSession(() =>
      container.resolve("reorderWorkoutExercisesUseCase").execute({ workoutId, exerciseIds }),
    );

    setReordering(false);
    if (result.ok) {
      return true;
    }
    setErrorCode(result.error.code);
    return false;
  }

  return { reordering, errorCode, reorder };
}
