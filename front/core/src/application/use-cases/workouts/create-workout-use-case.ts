import type { CreateWorkoutRequest, DomainError, Result, Workout } from "../../../domain";
import type { UseCaseInterface, WorkoutRepositoryInterface } from "../../ports";

/**
 * Creates a workout without exercises. Which of `userId` and `trainerId` are required depends on the caller's role,
 * and the API decides: the input is forwarded unchanged.
 */
export default class CreateWorkoutUseCase implements UseCaseInterface<
  CreateWorkoutRequest,
  Workout
> {
  constructor(private readonly workoutRepository: WorkoutRepositoryInterface) {}

  execute(request: CreateWorkoutRequest): Promise<Result<Workout, DomainError>> {
    return this.workoutRepository.create(request);
  }
}
