import type {
  AddExerciseToWorkoutInput,
  AuthResponse,
  CreateWorkoutRequest,
  Exercise,
  GetWorkoutByIdInput,
  RemoveExerciseFromWorkoutInput,
  ReorderWorkoutExercisesInput,
  LoginRequest,
  Session,
  SignupCredentials,
  UpdateProfileRequest,
  UpdateWorkoutExerciseInput,
  UpdateWorkoutInput,
  UserProfile,
  Workout,
  WorkoutDetail,
  WorkoutExerciseDetail,
} from "../domain";
import type {
  AuthRepositoryInterface,
  ExerciseRepositoryInterface,
  HttpClientInterface,
  KeyValueStoreInterface,
  NotificationServiceInterface,
  UseCaseInterface,
  UserRepositoryInterface,
  WorkoutRepositoryInterface,
} from "./ports";

/** Every port and use case a front's container must register, by key. */
export default interface Dependencies {
  httpClient: HttpClientInterface;
  keyValueStore: KeyValueStoreInterface;
  notificationService: NotificationServiceInterface;
  authRepository: AuthRepositoryInterface;
  userRepository: UserRepositoryInterface;
  workoutRepository: WorkoutRepositoryInterface;
  exerciseRepository: ExerciseRepositoryInterface;
  signupUseCase: UseCaseInterface<SignupCredentials, AuthResponse>;
  loginUseCase: UseCaseInterface<LoginRequest, AuthResponse>;
  getSessionUseCase: UseCaseInterface<void, Session | null>;
  logoutUseCase: UseCaseInterface<void, void>;
  refreshSessionUseCase: UseCaseInterface<void, Session>;
  getPreferredLanguageUseCase: UseCaseInterface<void, string | null>;
  setPreferredLanguageUseCase: UseCaseInterface<string, void>;
  getMyProfileUseCase: UseCaseInterface<void, UserProfile>;
  updateMyProfileUseCase: UseCaseInterface<UpdateProfileRequest, UserProfile>;
  getWorkoutsUseCase: UseCaseInterface<void, Workout[]>;
  getWorkoutByIdUseCase: UseCaseInterface<GetWorkoutByIdInput, WorkoutDetail>;
  createWorkoutUseCase: UseCaseInterface<CreateWorkoutRequest, Workout>;
  updateWorkoutUseCase: UseCaseInterface<UpdateWorkoutInput, Workout>;
  addExerciseToWorkoutUseCase: UseCaseInterface<AddExerciseToWorkoutInput, WorkoutExerciseDetail>;
  updateWorkoutExerciseUseCase: UseCaseInterface<UpdateWorkoutExerciseInput, WorkoutExerciseDetail>;
  removeExerciseFromWorkoutUseCase: UseCaseInterface<RemoveExerciseFromWorkoutInput, void>;
  reorderWorkoutExercisesUseCase: UseCaseInterface<ReorderWorkoutExercisesInput, WorkoutDetail>;
  getExercisesUseCase: UseCaseInterface<void, Exercise[]>;
}
