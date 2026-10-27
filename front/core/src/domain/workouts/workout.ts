import type { components } from "../../contract/generated/api";

export type Workout = components["schemas"]["WorkoutResponse"];
export type WorkoutExerciseDetail = components["schemas"]["WorkoutExerciseDetailResponse"];
export type CreateWorkoutRequest = components["schemas"]["CreateWorkoutRequest"];
export type UpdateWorkoutRequest = components["schemas"]["UpdateWorkoutRequest"];
export type AddExerciseRequest = components["schemas"]["AddOrUpdateExerciseRequest"];
export type SetPrescription = components["schemas"]["SetPrescriptionRequest"];

/** The workout id travels in the path, the rest of the input in the body. */
export type UpdateWorkoutInput = UpdateWorkoutRequest & { id: string };
export type AddExerciseToWorkoutInput = AddExerciseRequest & { workoutId: string };
