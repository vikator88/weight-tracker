import type {
  AddExerciseRequest,
  CreateWorkoutRequest,
  DomainError,
  Result,
  UpdateWorkoutRequest,
  Workout,
  WorkoutDetail,
  WorkoutExerciseDetail,
} from "../../domain";

export default interface WorkoutRepositoryInterface {
  getAll(): Promise<Result<Workout[], DomainError>>;
  getById(id: string): Promise<Result<WorkoutDetail, DomainError>>;
  create(request: CreateWorkoutRequest): Promise<Result<Workout, DomainError>>;
  update(id: string, request: UpdateWorkoutRequest): Promise<Result<Workout, DomainError>>;
  addExercise(
    workoutId: string,
    request: AddExerciseRequest,
  ): Promise<Result<WorkoutExerciseDetail, DomainError>>;
  updateExercise(
    workoutId: string,
    exerciseId: string,
    request: AddExerciseRequest,
  ): Promise<Result<WorkoutExerciseDetail, DomainError>>;
  removeExercise(workoutId: string, exerciseId: string): Promise<Result<void, DomainError>>;
}
