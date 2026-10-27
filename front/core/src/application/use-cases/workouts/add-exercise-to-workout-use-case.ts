import type {
  AddExerciseToWorkoutInput,
  DomainError,
  Result,
  WorkoutExerciseDetail,
} from "../../../domain";
import type { UseCaseInterface, WorkoutRepositoryInterface } from "../../ports";

/** Adds an exercise with its effort and set prescriptions to a workout. */
export default class AddExerciseToWorkoutUseCase implements UseCaseInterface<
  AddExerciseToWorkoutInput,
  WorkoutExerciseDetail
> {
  constructor(private readonly workoutRepository: WorkoutRepositoryInterface) {}

  execute({
    workoutId,
    ...request
  }: AddExerciseToWorkoutInput): Promise<Result<WorkoutExerciseDetail, DomainError>> {
    return this.workoutRepository.addExercise(workoutId, request);
  }
}
