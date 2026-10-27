import type { DomainError, Result, Workout } from "../../../domain";
import type { UseCaseInterface, WorkoutRepositoryInterface } from "../../ports";

/** Lists the workouts the caller may see. Which ones that is, per role, is decided by the API. */
export default class GetWorkoutsUseCase implements UseCaseInterface<void, Workout[]> {
  constructor(private readonly workoutRepository: WorkoutRepositoryInterface) {}

  execute(): Promise<Result<Workout[], DomainError>> {
    return this.workoutRepository.getAll();
  }
}
