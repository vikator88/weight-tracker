import type { DomainError, RemoveExerciseFromWorkoutInput, Result } from "../../../domain";
import type { UseCaseInterface, WorkoutRepositoryInterface } from "../../ports";

/** Removes an exercise, and its set prescriptions, from a workout. */
export default class RemoveExerciseFromWorkoutUseCase implements UseCaseInterface<
  RemoveExerciseFromWorkoutInput,
  void
> {
  constructor(private readonly workoutRepository: WorkoutRepositoryInterface) {}

  execute({
    workoutId,
    exerciseId,
  }: RemoveExerciseFromWorkoutInput): Promise<Result<void, DomainError>> {
    return this.workoutRepository.removeExercise(workoutId, exerciseId);
  }
}
