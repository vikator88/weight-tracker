"use client";

import { useId, useMemo } from "react";
import AddOutlined from "@mui/icons-material/AddOutlined";
import CloseOutlined from "@mui/icons-material/CloseOutlined";
import Box from "@mui/material/Box";
import type { Exercise } from "@weight-tracker/core/domain";
import { useTranslation } from "react-i18next";
import Dialog from "@/presentation/common/components/atoms/dialog";
import DialogContent from "@/presentation/common/components/atoms/dialog-content";
import DialogTitle from "@/presentation/common/components/atoms/dialog-title";
import IconButton from "@/presentation/common/components/atoms/icon-button";
import List from "@/presentation/common/components/atoms/list";
import TextField from "@/presentation/common/components/atoms/text-field";
import Typography from "@/presentation/common/components/atoms/typography";
import ExerciseListItem, {
  EXERCISE_GRID_COLUMNS,
  EXERCISE_LIST_MAX_HEIGHT,
} from "@/presentation/workouts/components/molecules/exercise-list-item";
import LabelFilter from "@/presentation/workouts/components/molecules/label-filter";
import useExerciseLabels from "@/presentation/workouts/hooks/use-exercise-labels";

interface ExerciseCatalogDialogProps {
  open: boolean;
  /** The catalog exercises that are not in the workout yet. */
  exercises: Exercise[];
  filterText: string;
  onFilterTextChange: (text: string) => void;
  selectedBodyPart: string | null;
  onToggleBodyPart: (bodyPart: string) => void;
  /** Only meaningful while a body part is selected; the screen clears it when the body part changes. */
  selectedMuscle: string | null;
  onToggleMuscle: (muscle: string) => void;
  /** Body parts whose catalog exercises all work one muscle: their exercises show no muscle label. */
  singleMuscleBodyParts: ReadonlySet<string>;
  onChoose: (exercise: Exercise) => void;
  onClose: () => void;
}

/** Lowercased and without accents, so "sentadi" finds "Sentadilla" and "cuadriceps" finds "Cuádriceps". */
function normalize(text: string): string {
  return text.normalize("NFD").replace(/\p{M}/gu, "").toLowerCase();
}

/**
 * The catalog, filtered locally by name, body part and, once a body part is selected, muscle. The plus chooses an exercise to configure; Cerrar, Escape and a
 * click outside close it without choosing. The screen keeps the filters, so they survive a reopening.
 */
export default function ExerciseCatalogDialog({
  open,
  exercises,
  filterText,
  onFilterTextChange,
  selectedBodyPart,
  onToggleBodyPart,
  selectedMuscle,
  onToggleMuscle,
  singleMuscleBodyParts,
  onChoose,
  onClose,
}: ExerciseCatalogDialogProps) {
  const { t } = useTranslation("workouts");
  const { nameOf, muscleLabel, bodyPartLabel, bodyPartColor, exerciseLabelsOf } =
    useExerciseLabels();
  const titleId = useId();

  const bodyParts = useMemo(
    () =>
      [...new Set(exercises.map((exercise) => exercise.bodyPart))]
        .map((code) => ({ code, label: bodyPartLabel(code), color: bodyPartColor(code) }))
        .sort((a, b) => a.label.localeCompare(b.label)),
    [exercises, bodyPartLabel, bodyPartColor],
  );

  // A muscle belongs to its body part, so its pills take that body part's colour.
  const muscles = useMemo(
    () =>
      selectedBodyPart === null
        ? []
        : [
            ...new Set(
              exercises
                .filter((exercise) => exercise.bodyPart === selectedBodyPart)
                .map((exercise) => exercise.muscle),
            ),
          ]
            .map((code) => ({
              code,
              label: muscleLabel(code),
              color: bodyPartColor(selectedBodyPart),
            }))
            .sort((a, b) => a.label.localeCompare(b.label)),
    [exercises, selectedBodyPart, muscleLabel, bodyPartColor],
  );

  const visible = useMemo(() => {
    const text = normalize(filterText.trim());

    return exercises.filter(
      (exercise) =>
        (selectedBodyPart === null || exercise.bodyPart === selectedBodyPart) &&
        (selectedMuscle === null || exercise.muscle === selectedMuscle) &&
        normalize(nameOf(exercise)).includes(text),
    );
  }, [exercises, filterText, selectedBodyPart, selectedMuscle, nameOf]);

  return (
    <Dialog
      open={open}
      onClose={onClose}
      fullWidth
      maxWidth={false}
      aria-labelledby={titleId}
      slotProps={{ paper: { sx: { width: { md: "75vw" } } } }}
    >
      <DialogTitle
        id={titleId}
        sx={{ display: "flex", alignItems: "center", justifyContent: "space-between", gap: 1 }}
      >
        {t("workout.catalog.title")}
        <IconButton edge="end" aria-label={t("workout.catalog.close")} onClick={onClose}>
          <CloseOutlined />
        </IconButton>
      </DialogTitle>
      <DialogContent>
        <Box sx={{ pt: 1 }}>
          <TextField
            label={t("workout.catalog.filter")}
            type="search"
            size="small"
            fullWidth
            value={filterText}
            onChange={(event) => onFilterTextChange(event.target.value)}
            sx={{ mb: 2 }}
          />
          <LabelFilter
            label={t("workout.catalog.bodyParts")}
            options={bodyParts}
            selected={selectedBodyPart}
            onToggle={onToggleBodyPart}
          />
          {/* A single muscle has nothing to choose between. */}
          {muscles.length > 1 && (
            <Box sx={{ mt: 1 }}>
              <LabelFilter
                label={t("workout.catalog.muscles")}
                options={muscles}
                selected={selectedMuscle}
                onToggle={onToggleMuscle}
              />
            </Box>
          )}
          {visible.length === 0 ? (
            <Typography color="text.secondary" sx={{ mt: 2 }}>
              {t("workout.catalog.empty")}
            </Typography>
          ) : (
            <List
              aria-label={t("workout.catalog.title")}
              sx={{
                mt: 1,
                display: "grid",
                gridTemplateColumns: EXERCISE_GRID_COLUMNS,
                gap: 2,
                maxHeight: EXERCISE_LIST_MAX_HEIGHT,
                overflowY: "auto",
              }}
            >
              {visible.map((exercise) => (
                <ExerciseListItem
                  key={exercise.id}
                  name={nameOf(exercise)}
                  labels={exerciseLabelsOf(exercise, !singleMuscleBodyParts.has(exercise.bodyPart))}
                  actions={
                    <IconButton
                      edge="end"
                      aria-label={t("workout.catalog.choose")}
                      onClick={() => onChoose(exercise)}
                    >
                      <AddOutlined />
                    </IconButton>
                  }
                />
              ))}
            </List>
          )}
        </Box>
      </DialogContent>
    </Dialog>
  );
}
