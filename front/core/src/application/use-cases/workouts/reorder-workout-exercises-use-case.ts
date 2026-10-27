import type {
  DomainError,
  ReorderWorkoutExercisesInput,
  Result,
  WorkoutDetail,
} from "../../../domain";
import type { UseCaseInterface, WorkoutRepositoryInterface } from "../../ports";

/**
 * Sends the full new order of a workout's exercises. Whether the list matches the workout is decided by the API.
 */
export default class ReorderWorkoutExercisesUseCase implements UseCaseInterface<
  ReorderWorkoutExercisesInput,
  WorkoutDetail
> {
  constructor(private readonly workoutRepository: WorkoutRepositoryInterface) {}

  execute({
    workoutId,
    ...request
  }: ReorderWorkoutExercisesInput): Promise<Result<WorkoutDetail, DomainError>> {
    return this.workoutRepository.reorderExercises(workoutId, request);
  }
}
