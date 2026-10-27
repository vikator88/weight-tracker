"use client";

import { useId, useMemo } from "react";
import AddOutlined from "@mui/icons-material/AddOutlined";
import Box from "@mui/material/Box";
import type { Exercise } from "@weight-tracker/core/domain";
import { useTranslation } from "react-i18next";
import Card from "@/presentation/common/components/atoms/card";
import CardContent from "@/presentation/common/components/atoms/card-content";
import IconButton from "@/presentation/common/components/atoms/icon-button";
import List from "@/presentation/common/components/atoms/list";
import TextField from "@/presentation/common/components/atoms/text-field";
import Typography from "@/presentation/common/components/atoms/typography";
import ExerciseListItem, {
  EXERCISE_LIST_MAX_HEIGHT,
} from "@/presentation/workouts/components/molecules/exercise-list-item";
import BodyPartFilter from "@/presentation/workouts/components/molecules/body-part-filter";
import useExerciseLabels from "@/presentation/workouts/hooks/use-exercise-labels";

interface ExerciseCatalogPanelProps {
  /** The catalog exercises that are not in the workout yet. */
  exercises: Exercise[];
  filterText: string;
  onFilterTextChange: (text: string) => void;
  selectedBodyPart: string | null;
  onToggleBodyPart: (bodyPart: string) => void;
  onAdd: (exercise: Exercise) => void;
}

/** Lowercased and without accents, so "sentadi" finds "Sentadilla" and "cuadriceps" finds "Cuádriceps". */
function normalize(text: string): string {
  return text.normalize("NFD").replace(/\p{M}/gu, "").toLowerCase();
}

/** The catalog, filtered locally by name and body part. The plus adds an exercise to the workout. */
export default function ExerciseCatalogPanel({
  exercises,
  filterText,
  onFilterTextChange,
  selectedBodyPart,
  onToggleBodyPart,
  onAdd,
}: ExerciseCatalogPanelProps) {
  const { t } = useTranslation("workouts");
  const { nameOf, muscleLabel, bodyPartLabel, bodyPartColor } = useExerciseLabels();
  const titleId = useId();

  const bodyParts = useMemo(
    () =>
      [...new Set(exercises.map((exercise) => exercise.bodyPart))]
        .map((code) => ({ code, label: bodyPartLabel(code) }))
        .sort((a, b) => a.label.localeCompare(b.label)),
    [exercises, bodyPartLabel],
  );

  const visible = useMemo(() => {
    const text = normalize(filterText.trim());

    return exercises.filter(
      (exercise) =>
        (selectedBodyPart === null || exercise.bodyPart === selectedBodyPart) &&
        normalize(nameOf(exercise)).includes(text),
    );
  }, [exercises, filterText, selectedBodyPart, nameOf]);

  return (
    <Box component="section" aria-labelledby={titleId} sx={{ minWidth: 0 }}>
      <Card variant="outlined">
        <CardContent>
          <Typography id={titleId} variant="h6" component="h2" sx={{ mb: 2 }}>
            {t("workout.catalog.title")}
          </Typography>
          <TextField
            label={t("workout.catalog.filter")}
            type="search"
            size="small"
            fullWidth
            value={filterText}
            onChange={(event) => onFilterTextChange(event.target.value)}
            sx={{ mb: 2 }}
          />
          <BodyPartFilter
            bodyParts={bodyParts}
            selected={selectedBodyPart}
            onToggle={onToggleBodyPart}
          />
          {visible.length === 0 ? (
            <Typography color="text.secondary" sx={{ mt: 2 }}>
              {t("workout.catalog.empty")}
            </Typography>
          ) : (
            <List
              aria-label={t("workout.catalog.title")}
              sx={{ mt: 1, maxHeight: EXERCISE_LIST_MAX_HEIGHT, overflowY: "auto" }}
            >
              {visible.map((exercise) => (
                <ExerciseListItem
                  key={exercise.id}
                  name={nameOf(exercise)}
                  labels={[
                    {
                      text: bodyPartLabel(exercise.bodyPart),
                      color: bodyPartColor(exercise.bodyPart),
                    },
                    { text: muscleLabel(exercise.muscle) },
                  ]}
                  actions={
                    <IconButton
                      edge="end"
                      aria-label={t("workout.catalog.add")}
                      onClick={() => onAdd(exercise)}
                    >
                      <AddOutlined />
                    </IconButton>
                  }
                />
              ))}
            </List>
          )}
        </CardContent>
      </Card>
    </Box>
  );
}
