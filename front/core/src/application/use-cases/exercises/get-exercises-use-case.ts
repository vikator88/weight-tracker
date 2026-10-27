import type { DomainError, Exercise, Result } from "../../../domain";
import type { ExerciseRepositoryInterface, UseCaseInterface } from "../../ports";

/** Lists the exercise catalog. The endpoint is public, so no session is needed. */
export default class GetExercisesUseCase implements UseCaseInterface<void, Exercise[]> {
  constructor(private readonly exerciseRepository: ExerciseRepositoryInterface) {}

  execute(): Promise<Result<Exercise[], DomainError>> {
    return this.exerciseRepository.getAll();
  }
}
