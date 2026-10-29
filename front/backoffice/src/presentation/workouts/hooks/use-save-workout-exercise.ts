import { useState } from "react";
import type {
  AddExerciseRequest,
  DomainErrorCode,
  WorkoutExerciseDetail,
} from "@weight-tracker/core/domain";
import useSession from "@/presentation/auth/hooks/use-session";
import useContainer from "@/presentation/common/hooks/use-container";

interface SaveWorkoutExerciseState {
  saving: boolean;
  errorCode: DomainErrorCode | null;
  /**
   * Adds an exercise chosen from the catalog to the workout, or replaces a saved one's effort and sets. Resolves to the
   * entry the API answers with, or `null` when it refused it.
   */
  save: (
    workoutId: string,
    adding: boolean,
    request: AddExerciseRequest & { exerciseId: string },
  ) => Promise<WorkoutExerciseDetail | null>;
}

export default function useSaveWorkoutExercise(): SaveWorkoutExerciseState {
  const container = useContainer();
  const { runWithSession } = useSession();
  const [saving, setSaving] = useState(false);
  const [errorCode, setErrorCode] = useState<DomainErrorCode | null>(null);

  async function save(
    workoutId: string,
    adding: boolean,
    request: AddExerciseRequest & { exerciseId: string },
  ) {
    setSaving(true);
    setErrorCode(null);

    const result = await runWithSession(() =>
      adding
        ? container.resolve("addExerciseToWorkoutUseCase").execute({ workoutId, ...request })
        : container.resolve("updateWorkoutExerciseUseCase").execute({ workoutId, ...request }),
    );

    setSaving(false);
    if (result.ok) {
      return result.value;
    }
    setErrorCode(result.error.code);
    return null;
  }

  return { saving, errorCode, save };
}
