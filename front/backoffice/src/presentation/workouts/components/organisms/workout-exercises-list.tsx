"use client";

import DeleteOutlined from "@mui/icons-material/DeleteOutlined";
import EditOutlined from "@mui/icons-material/EditOutlined";
import type { DomainErrorCode, Exercise, WorkoutExerciseDetail } from "@weight-tracker/core/domain";
import { useTranslation } from "react-i18next";
import Alert from "@/presentation/common/components/atoms/alert";
import IconButton from "@/presentation/common/components/atoms/icon-button";
import List from "@/presentation/common/components/atoms/list";
import Typography from "@/presentation/common/components/atoms/typography";
import ExerciseListItem, {
  EXERCISE_LIST_MAX_HEIGHT,
} from "@/presentation/workouts/components/molecules/exercise-list-item";
import useExerciseLabels from "@/presentation/workouts/hooks/use-exercise-labels";

/** A workout exercise with its catalog entry, when the catalog has one. */
export interface WorkoutExerciseItem {
  workoutExercise: WorkoutExerciseDetail;
  exercise?: Exercise;
  /** Added from the catalog on screen and not saved yet. */
  pending?: boolean;
}

interface WorkoutExercisesListProps {
  items: WorkoutExerciseItem[];
  selectedId: string | null;
  onSelect: (workoutExerciseId: string) => void;
  onRemove: (item: WorkoutExerciseItem) => void;
  /** The entry whose removal is in progress. */
  removingId: string | null;
  /** Why the last removal failed, if it did. */
  removeErrorCode: DomainErrorCode | null;
}

/**
 * The workout's exercises, saved ones in the API's order and then the pending ones, numbered, under an "Ejercicios"
 * header. The pencil selects one to configure and the trash removes it; exercises are added from the catalog.
 */
export default function WorkoutExercisesList({
  items,
  selectedId,
  onSelect,
  onRemove,
  removingId,
  removeErrorCode,
}: WorkoutExercisesListProps) {
  const { t } = useTranslation("workouts");
  const { t: tc } = useTranslation();
  const { nameOf, muscleLabel, bodyPartLabel, bodyPartColor, effortLabel, setSummary } =
    useExerciseLabels();

  /** The sets on one line, comma separated, and the effort on the next; empty properties are left out. */
  function linesOf({ sets, effort }: WorkoutExerciseDetail) {
    const lines: string[] = [];

    if (sets.length > 0) {
      const summaries = sets.map(({ count, target }) =>
        setSummary({ count, type: target.type, value: target.value, maxValue: target.maxValue }),
      );
      lines.push(t("workout.exercises.setsLine", { sets: summaries.join(", ") }));
    }
    if (effort) {
      lines.push(t("workout.exercises.effortLine", { effort: effortLabel(effort) }));
    }
    return lines;
  }

  return (
    <>
      <Typography variant="subtitle1" component="h3" sx={{ fontWeight: 600 }}>
        {t("workout.exercises.title")}
      </Typography>
      {removeErrorCode && (
        <Alert severity="error" sx={{ mt: 1 }}>
          {tc(`errors:${removeErrorCode}`)}
        </Alert>
      )}
      {items.length === 0 ? (
        <Typography color="text.secondary" sx={{ mt: 2 }}>
          {t("workout.exercises.empty")}
        </Typography>
      ) : (
        <List
          aria-label={t("workout.exercises.title")}
          sx={{ mt: 1, maxHeight: EXERCISE_LIST_MAX_HEIGHT, overflowY: "auto" }}
        >
          {items.map((item, index) => {
            const { workoutExercise, exercise, pending } = item;

            return (
              <ExerciseListItem
                key={workoutExercise.id}
                position={index + 1}
                name={exercise ? nameOf(exercise) : workoutExercise.exerciseId}
                labels={
                  exercise
                    ? [
                        {
                          text: bodyPartLabel(exercise.bodyPart),
                          color: bodyPartColor(exercise.bodyPart),
                        },
                        { text: muscleLabel(exercise.muscle) },
                      ]
                    : []
                }
                note={pending ? t("workout.exercises.pending") : undefined}
                lines={linesOf(workoutExercise)}
                selected={workoutExercise.id === selectedId}
                actions={
                  <>
                    <IconButton
                      aria-label={t("workout.exercises.remove")}
                      disabled={removingId === workoutExercise.id}
                      onClick={() => onRemove(item)}
                    >
                      <DeleteOutlined />
                    </IconButton>
                    <IconButton
                      edge="end"
                      aria-label={t("workout.exercises.edit")}
                      aria-pressed={workoutExercise.id === selectedId}
                      onClick={() => onSelect(workoutExercise.id)}
                    >
                      <EditOutlined />
                    </IconButton>
                  </>
                }
              />
            );
          })}
        </List>
      )}
    </>
  );
}
