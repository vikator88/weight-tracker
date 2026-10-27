import {
  AddExerciseToWorkoutUseCase,
  CreateWorkoutUseCase,
  type Dependencies,
  GetExercisesUseCase,
  GetMyProfileUseCase,
  GetPreferredLanguageUseCase,
  GetSessionUseCase,
  GetWorkoutByIdUseCase,
  GetWorkoutsUseCase,
  LoginUseCase,
  LogoutUseCase,
  RefreshSessionUseCase,
  RemoveExerciseFromWorkoutUseCase,
  SetPreferredLanguageUseCase,
  UpdateMyProfileUseCase,
  UpdateWorkoutExerciseUseCase,
  UpdateWorkoutUseCase,
  SignupUseCase,
} from "@weight-tracker/core/application";
import {
  AuthRepository,
  Container,
  ExerciseRepository,
  FetchHttpClient,
  UserRepository,
  WorkoutRepository,
} from "@weight-tracker/core/infrastructure";
import LocalStorageKeyValueStore from "./local-storage-key-value-store";
import NoOpNotificationService from "./no-op-notification-service";

const container = new Container<Dependencies>({
  httpClient: (c) =>
    new FetchHttpClient({
      baseUrl: process.env.NEXT_PUBLIC_API_URL ?? "http://localhost:5199",
      // Every request carries the stored access token, when there is one.
      getAccessToken: async () => {
        const session = await c.resolve("getSessionUseCase").execute();
        return session.ok ? (session.value?.accessToken ?? null) : null;
      },
    }),
  keyValueStore: () => new LocalStorageKeyValueStore(),
  notificationService: () => new NoOpNotificationService(),
  authRepository: (c) => new AuthRepository(c.resolve("httpClient")),
  userRepository: (c) => new UserRepository(c.resolve("httpClient")),
  workoutRepository: (c) => new WorkoutRepository(c.resolve("httpClient")),
  exerciseRepository: (c) => new ExerciseRepository(c.resolve("httpClient")),
  signupUseCase: (c) =>
    new SignupUseCase(c.resolve("authRepository"), c.resolve("keyValueStore"), 2), // TRAINER
  loginUseCase: (c) => new LoginUseCase(c.resolve("authRepository"), c.resolve("keyValueStore")),
  getSessionUseCase: (c) => new GetSessionUseCase(c.resolve("keyValueStore")),
  logoutUseCase: (c) => new LogoutUseCase(c.resolve("keyValueStore")),
  refreshSessionUseCase: (c) =>
    new RefreshSessionUseCase(c.resolve("authRepository"), c.resolve("keyValueStore")),
  getPreferredLanguageUseCase: (c) => new GetPreferredLanguageUseCase(c.resolve("keyValueStore")),
  setPreferredLanguageUseCase: (c) => new SetPreferredLanguageUseCase(c.resolve("keyValueStore")),
  getMyProfileUseCase: (c) => new GetMyProfileUseCase(c.resolve("userRepository")),
  updateMyProfileUseCase: (c) => new UpdateMyProfileUseCase(c.resolve("userRepository")),
  getWorkoutsUseCase: (c) => new GetWorkoutsUseCase(c.resolve("workoutRepository")),
  getWorkoutByIdUseCase: (c) => new GetWorkoutByIdUseCase(c.resolve("workoutRepository")),
  createWorkoutUseCase: (c) => new CreateWorkoutUseCase(c.resolve("workoutRepository")),
  updateWorkoutUseCase: (c) => new UpdateWorkoutUseCase(c.resolve("workoutRepository")),
  addExerciseToWorkoutUseCase: (c) =>
    new AddExerciseToWorkoutUseCase(c.resolve("workoutRepository")),
  updateWorkoutExerciseUseCase: (c) =>
    new UpdateWorkoutExerciseUseCase(c.resolve("workoutRepository")),
  removeExerciseFromWorkoutUseCase: (c) =>
    new RemoveExerciseFromWorkoutUseCase(c.resolve("workoutRepository")),
  getExercisesUseCase: (c) => new GetExercisesUseCase(c.resolve("exerciseRepository")),
});

export default container;
