import type {
  DomainError,
  Result,
  UpdateWorkoutExerciseInput,
  WorkoutExerciseDetail,
} from "../../../domain";
import type { UseCaseInterface, WorkoutRepositoryInterface } from "../../ports";

/** Replaces the effort and every set prescription of an exercise of a workout. */
export default class UpdateWorkoutExerciseUseCase implements UseCaseInterface<
  UpdateWorkoutExerciseInput,
  WorkoutExerciseDetail
> {
  constructor(private readonly workoutRepository: WorkoutRepositoryInterface) {}

  execute({
    workoutId,
    ...request
  }: UpdateWorkoutExerciseInput): Promise<Result<WorkoutExerciseDetail, DomainError>> {
    return this.workoutRepository.updateExercise(workoutId, request.exerciseId, request);
  }
}
