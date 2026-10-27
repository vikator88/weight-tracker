import type { DomainError, GetWorkoutByIdInput, Result, WorkoutDetail } from "../../../domain";
import type { UseCaseInterface, WorkoutRepositoryInterface } from "../../ports";

/** Reads one workout with its exercises. Whether the caller may see it is decided by the API. */
export default class GetWorkoutByIdUseCase implements UseCaseInterface<
  GetWorkoutByIdInput,
  WorkoutDetail
> {
  constructor(private readonly workoutRepository: WorkoutRepositoryInterface) {}

  execute({ id }: GetWorkoutByIdInput): Promise<Result<WorkoutDetail, DomainError>> {
    return this.workoutRepository.getById(id);
  }
}
