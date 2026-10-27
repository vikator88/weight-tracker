import type {
  AddExerciseRequest,
  CreateWorkoutRequest,
  DomainError,
  Result,
  UpdateWorkoutRequest,
  Workout,
  WorkoutExerciseDetail,
} from "../../domain";

export default interface WorkoutRepositoryInterface {
  create(request: CreateWorkoutRequest): Promise<Result<Workout, DomainError>>;
  update(id: string, request: UpdateWorkoutRequest): Promise<Result<Workout, DomainError>>;
  addExercise(
    workoutId: string,
    request: AddExerciseRequest,
  ): Promise<Result<WorkoutExerciseDetail, DomainError>>;
}
