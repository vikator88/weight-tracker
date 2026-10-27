"use client";

import { useId } from "react";
import {
  closestCenter,
  DndContext,
  type DragEndEvent,
  KeyboardSensor,
  PointerSensor,
  type UniqueIdentifier,
  useSensor,
  useSensors,
} from "@dnd-kit/core";
import {
  SortableContext,
  sortableKeyboardCoordinates,
  verticalListSortingStrategy,
} from "@dnd-kit/sortable";
import DeleteOutlined from "@mui/icons-material/DeleteOutlined";
import EditOutlined from "@mui/icons-material/EditOutlined";
import type { DomainErrorCode, Exercise, WorkoutExerciseDetail } from "@weight-tracker/core/domain";
import { useTranslation } from "react-i18next";
import Alert from "@/presentation/common/components/atoms/alert";
import IconButton from "@/presentation/common/components/atoms/icon-button";
import List from "@/presentation/common/components/atoms/list";
import Typography from "@/presentation/common/components/atoms/typography";
import { EXERCISE_LIST_MAX_HEIGHT } from "@/presentation/workouts/components/molecules/exercise-list-item";
import SortableExerciseListItem from "@/presentation/workouts/components/molecules/sortable-exercise-list-item";
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
  /** A saved entry was dropped onto another one's place; both are workout entry ids. */
  onReorder: (activeId: string, overId: string) => void;
  /** While a reorder is in progress no exercise can be dragged. */
  reordering: boolean;
  /** Why the last reorder failed, if it did. */
  reorderErrorCode: DomainErrorCode | null;
}

/**
 * The workout's exercises, saved ones in their current order and then the pending ones, numbered, under an
 * "Ejercicios" header. Saved ones are reordered by dragging their handle; the pencil selects one to configure and the
 * trash removes it; exercises are added from the catalog.
 */
export default function WorkoutExercisesList({
  items,
  selectedId,
  onSelect,
  onRemove,
  removingId,
  removeErrorCode,
  onReorder,
  reordering,
  reorderErrorCode,
}: WorkoutExercisesListProps) {
  const { t } = useTranslation("workouts");
  const { t: tc } = useTranslation();
  const { nameOf, muscleLabel, bodyPartLabel, bodyPartColor, effortLabel, setSummary } =
    useExerciseLabels();
  // A stable id keeps dnd-kit's generated accessibility ids the same in the static export and in the browser.
  const dndId = useId();
  const sensors = useSensors(
    useSensor(PointerSensor),
    useSensor(KeyboardSensor, { coordinateGetter: sortableKeyboardCoordinates }),
  );
  // Only saved exercises are part of the API's order; pending ones keep their place after them.
  const sortableIds = items.filter((item) => !item.pending).map((item) => item.workoutExercise.id);

  function displayNameOf({ workoutExercise, exercise }: WorkoutExerciseItem) {
    return exercise ? nameOf(exercise) : workoutExercise.exerciseId;
  }

  function nameById(id: UniqueIdentifier) {
    const item = items.find((other) => other.workoutExercise.id === id);
    return item ? displayNameOf(item) : String(id);
  }

  function positionOf(id: UniqueIdentifier) {
    return sortableIds.indexOf(String(id)) + 1;
  }

  function handleDragEnd({ active, over }: DragEndEvent) {
    if (over && active.id !== over.id) {
      onReorder(String(active.id), String(over.id));
    }
  }

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
      {reorderErrorCode && (
        <Alert severity="error" sx={{ mt: 1 }}>
          {tc(`errors:${reorderErrorCode}`)}
        </Alert>
      )}
      {items.length === 0 ? (
        <Typography color="text.secondary" sx={{ mt: 2 }}>
          {t("workout.exercises.empty")}
        </Typography>
      ) : (
        <DndContext
          id={dndId}
          sensors={sensors}
          collisionDetection={closestCenter}
          onDragEnd={handleDragEnd}
          accessibility={{
            screenReaderInstructions: { draggable: t("workout.exercises.dnd.instructions") },
            announcements: {
              onDragStart: ({ active }) =>
                t("workout.exercises.dnd.picked", { name: nameById(active.id) }),
              onDragOver: ({ active, over }) =>
                over
                  ? t("workout.exercises.dnd.over", {
                      name: nameById(active.id),
                      position: positionOf(over.id),
                    })
                  : undefined,
              onDragEnd: ({ active, over }) =>
                over
                  ? t("workout.exercises.dnd.dropped", {
                      name: nameById(active.id),
                      position: positionOf(over.id),
                    })
                  : t("workout.exercises.dnd.cancelled", { name: nameById(active.id) }),
              onDragCancel: ({ active }) =>
                t("workout.exercises.dnd.cancelled", { name: nameById(active.id) }),
            },
          }}
        >
          <SortableContext items={sortableIds} strategy={verticalListSortingStrategy}>
            <List
              aria-label={t("workout.exercises.title")}
              sx={{ mt: 1, maxHeight: EXERCISE_LIST_MAX_HEIGHT, overflowY: "auto" }}
            >
              {items.map((item, index) => {
                const { workoutExercise, exercise, pending } = item;

                return (
                  <SortableExerciseListItem
                    key={workoutExercise.id}
                    id={workoutExercise.id}
                    disabled={reordering || Boolean(pending)}
                    handleLabel={t("workout.exercises.reorder")}
                    roleDescription={t("workout.exercises.dnd.roleDescription")}
                    position={index + 1}
                    name={displayNameOf(item)}
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
          </SortableContext>
        </DndContext>
      )}
    </>
  );
}
