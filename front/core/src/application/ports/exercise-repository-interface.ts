import type { DomainError, Exercise, Result } from "../../domain";

export default interface ExerciseRepositoryInterface {
  getAll(): Promise<Result<Exercise[], DomainError>>;
}
