import type { DomainError, Result, UpdateWorkoutInput, Workout } from "../../../domain";
import type { UseCaseInterface, WorkoutRepositoryInterface } from "../../ports";

/** Replaces a workout's editable data. Who may change `userId` and `trainerId` is decided by the API. */
export default class UpdateWorkoutUseCase implements UseCaseInterface<UpdateWorkoutInput, Workout> {
  constructor(private readonly workoutRepository: WorkoutRepositoryInterface) {}

  execute({ id, ...request }: UpdateWorkoutInput): Promise<Result<Workout, DomainError>> {
    return this.workoutRepository.update(id, request);
  }
}
