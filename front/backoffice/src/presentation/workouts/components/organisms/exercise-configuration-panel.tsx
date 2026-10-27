"use client";

import { type ReactNode, useState } from "react";
import AddOutlined from "@mui/icons-material/AddOutlined";
import DeleteOutlined from "@mui/icons-material/DeleteOutlined";
import Box from "@mui/material/Box";
import {
  type ExerciseConfigurationViolations,
  type SetFormValues,
  type SetFormViolations,
  targetFieldsOf,
  validateExerciseConfiguration,
  validateSetForm,
} from "@weight-tracker/core/application";
import type {
  AddExerciseRequest,
  SetPrescription,
  WorkoutExerciseDetail,
} from "@weight-tracker/core/domain";
import { useTranslation } from "react-i18next";
import Alert from "@/presentation/common/components/atoms/alert";
import Button from "@/presentation/common/components/atoms/button";
import Card from "@/presentation/common/components/atoms/card";
import CardContent from "@/presentation/common/components/atoms/card-content";
import Chip from "@/presentation/common/components/atoms/chip";
import IconButton from "@/presentation/common/components/atoms/icon-button";
import List from "@/presentation/common/components/atoms/list";
import ListItem from "@/presentation/common/components/atoms/list-item";
import MenuItem from "@/presentation/common/components/atoms/menu-item";
import TextField from "@/presentation/common/components/atoms/text-field";
import Typography from "@/presentation/common/components/atoms/typography";
import ExerciseVideo from "@/presentation/workouts/components/molecules/exercise-video";
import type { WorkoutExerciseItem } from "@/presentation/workouts/components/organisms/workout-exercises-list";
import useExerciseLabels from "@/presentation/workouts/hooks/use-exercise-labels";
import useSaveWorkoutExercise from "@/presentation/workouts/hooks/use-save-workout-exercise";

type Effort = NonNullable<AddExerciseRequest["effort"]>;

/** The API's `Effort` values, lightest first. */
const EFFORTS: Effort[] = ["LOW", "MIDLOW", "MID", "MIDHIGH", "HIGH"];

function isEffort(value: string): value is Effort {
  return (EFFORTS as string[]).includes(value);
}
/** The API's set target types. */
const TARGET_TYPES = ["Reps", "Duration", "MaxReps", "RepsRange"];

const emptyDraft: SetFormValues = { count: "", type: "Reps", value: "", maxValue: "" };

interface ExerciseConfigurationPanelProps {
  item: WorkoutExerciseItem | null;
  titleId: string;
  workoutId: string;
  /** The item was saved just now: show the confirmation until the next edit. */
  justSaved: boolean;
  /** Called with the entry the API answered with, after a successful save. */
  onSaved: (entry: WorkoutExerciseDetail, item: WorkoutExerciseItem) => void;
  onEdit: () => void;
}

/** The "Detalles del ejercicio" card around the panel's content. */
function DetailsCard({ titleId, children }: { titleId: string; children: ReactNode }) {
  const { t } = useTranslation("workouts");

  return (
    <Card variant="outlined">
      <CardContent>
        <Typography id={titleId} variant="h6" component="h2" sx={{ mb: 2 }}>
          {t("workout.configuration.title")}
        </Typography>
        {children}
      </CardContent>
    </Card>
  );
}

function toFormValues({ count, target }: WorkoutExerciseDetail["sets"][number]): SetFormValues {
  return {
    count: String(count),
    type: target.type,
    value: target.value == null ? "" : String(target.value),
    maxValue: target.maxValue == null ? "" : String(target.maxValue),
  };
}

/** A listed set as the API expects it: numbers, and only the target fields its type needs. */
function toPrescription(set: SetFormValues): SetPrescription {
  const needs = targetFieldsOf(set.type);
  const prescription: SetPrescription = { count: Number(set.count), targetType: set.type };

  if (needs.value) {
    prescription.target = Number(set.value);
  }
  if (needs.maxValue) {
    prescription.maxTarget = Number(set.maxValue);
  }
  return prescription;
}

/**
 * Configures the workout exercise selected in the workout column: its effort and its list of sets, built with the set
 * form. Guardar adds a pending exercise to the workout or replaces a saved one's effort and sets. The caller keys it
 * by the selection, so every selection starts from the stored values.
 */
export default function ExerciseConfigurationPanel({
  item,
  titleId,
  workoutId,
  justSaved,
  onSaved,
  onEdit,
}: ExerciseConfigurationPanelProps) {
  const { t } = useTranslation("workouts");
  const { t: tc } = useTranslation();
  const {
    nameOf,
    muscleLabel,
    bodyPartLabel,
    bodyPartColor,
    effortLabel,
    targetTypeLabel,
    setSummary,
  } = useExerciseLabels();
  const { saving, errorCode, save } = useSaveWorkoutExercise();
  const [effort, setEffort] = useState(item?.workoutExercise.effort ?? "");
  const [sets, setSets] = useState<SetFormValues[]>(
    (item?.workoutExercise.sets ?? []).map(toFormValues),
  );
  const [draft, setDraft] = useState<SetFormValues>(emptyDraft);
  const [draftErrors, setDraftErrors] = useState<SetFormViolations>({});
  const [errors, setErrors] = useState<ExerciseConfigurationViolations>({});

  if (item === null) {
    return (
      <DetailsCard titleId={titleId}>
        <Typography color="text.secondary">{t("workout.configuration.empty")}</Typography>
      </DetailsCard>
    );
  }

  const { workoutExercise, exercise, pending = false } = item;
  const name = exercise ? nameOf(exercise) : workoutExercise.exerciseId;
  const efforts: string[] = isEffort(effort) || effort === "" ? EFFORTS : [...EFFORTS, effort];
  const draftNeeds = targetFieldsOf(draft.type);
  const positiveInteger = t("workout.configuration.validation.positiveInteger");

  function changeDraft(field: keyof SetFormValues, value: string) {
    setDraft((current) => ({ ...current, [field]: value }));
  }

  function addSet() {
    const violations = validateSetForm(draft);
    setDraftErrors(violations);
    if (Object.keys(violations).length > 0) {
      return;
    }
    setSets((current) => [...current, draft]);
    setDraft(emptyDraft);
    setErrors((current) => ({ ...current, sets: undefined }));
    onEdit();
  }

  function removeSet(index: number) {
    setSets((current) => current.filter((_, position) => position !== index));
    onEdit();
  }

  async function handleSave() {
    // An effort the API no longer offers counts as missing: it has to be chosen again.
    const violations = validateExerciseConfiguration({
      effort: isEffort(effort) ? effort : "",
      setCount: sets.length,
    });
    setErrors(violations);
    if (Object.keys(violations).length > 0 || item === null || !isEffort(effort)) {
      return;
    }

    const entry = await save(workoutId, pending, {
      exerciseId: workoutExercise.exerciseId,
      effort,
      sets: sets.map(toPrescription),
    });
    if (entry) {
      onSaved(entry, item);
    }
  }

  return (
    <DetailsCard titleId={titleId}>
      <Box sx={{ display: "flex", flexDirection: "column", gap: 2 }}>
        {exercise?.videoUrl && (
          <Box component="section" aria-label={t("workout.configuration.video")}>
            <ExerciseVideo url={exercise.videoUrl} title={name} />
          </Box>
        )}
        <Box>
          <Typography
            variant="subtitle1"
            component="h3"
            sx={{ fontWeight: 600, overflowWrap: "anywhere" }}
          >
            {name}
          </Typography>
          {exercise && (
            <Box sx={{ display: "flex", flexWrap: "wrap", gap: 0.5, mt: 0.5 }}>
              <Chip
                label={bodyPartLabel(exercise.bodyPart)}
                size="small"
                variant="outlined"
                color={bodyPartColor(exercise.bodyPart)}
              />
              <Chip label={muscleLabel(exercise.muscle)} size="small" variant="outlined" />
            </Box>
          )}
        </Box>
        {errorCode && <Alert severity="error">{tc(`errors:${errorCode}`)}</Alert>}
        {justSaved && <Alert severity="success">{t("workout.configuration.saved")}</Alert>}
        <TextField
          select
          label={t("workout.configuration.effort")}
          value={effort}
          disabled={saving}
          onChange={(event) => {
            setEffort(event.target.value);
            setErrors((current) => ({ ...current, effort: undefined }));
            onEdit();
          }}
          error={Boolean(errors.effort)}
          helperText={errors.effort ? t("workout.configuration.validation.effort") : undefined}
        >
          {efforts.map((code) => (
            <MenuItem key={code} value={code}>
              {effortLabel(code)}
            </MenuItem>
          ))}
        </TextField>
        <Box>
          <Typography variant="subtitle2" component="h4">
            {t("workout.configuration.sets")}
          </Typography>
          {sets.length === 0 ? (
            <Typography
              variant="body2"
              color={errors.sets ? "error" : "text.secondary"}
              sx={{ mt: 0.5 }}
            >
              {errors.sets
                ? t("workout.configuration.validation.sets")
                : t("workout.configuration.noSets")}
            </Typography>
          ) : (
            <List dense aria-label={t("workout.configuration.sets")}>
              {sets.map((set, index) => (
                <ListItem
                  // Sets have no id of their own; their position is their identity in this list.
                  key={index}
                  divider
                  disableGutters
                  secondaryAction={
                    <IconButton
                      edge="end"
                      size="small"
                      aria-label={t("workout.configuration.removeSet")}
                      disabled={saving}
                      onClick={() => removeSet(index)}
                    >
                      <DeleteOutlined fontSize="small" />
                    </IconButton>
                  }
                >
                  <Typography variant="body2">{setSummary(set)}</Typography>
                </ListItem>
              ))}
            </List>
          )}
        </Box>
        <Box
          role="group"
          aria-label={t("workout.configuration.addSet")}
          sx={{ display: "grid", gridTemplateColumns: "1fr 1fr", gap: 1 }}
        >
          <TextField
            label={t("workout.configuration.setCount")}
            type="number"
            size="small"
            value={draft.count}
            disabled={saving}
            onChange={(event) => changeDraft("count", event.target.value)}
            error={Boolean(draftErrors.count)}
            helperText={draftErrors.count ? positiveInteger : undefined}
          />
          <TextField
            select
            label={t("workout.configuration.targetType")}
            size="small"
            value={draft.type}
            disabled={saving}
            onChange={(event) => changeDraft("type", event.target.value)}
          >
            {TARGET_TYPES.map((type) => (
              <MenuItem key={type} value={type}>
                {targetTypeLabel(type)}
              </MenuItem>
            ))}
          </TextField>
          {draftNeeds.value && (
            <TextField
              label={t("workout.configuration.targetValue")}
              type="number"
              size="small"
              value={draft.value}
              disabled={saving}
              onChange={(event) => changeDraft("value", event.target.value)}
              error={Boolean(draftErrors.value)}
              helperText={draftErrors.value ? positiveInteger : undefined}
            />
          )}
          {draftNeeds.maxValue && (
            <TextField
              label={t("workout.configuration.targetMaxValue")}
              type="number"
              size="small"
              value={draft.maxValue}
              disabled={saving}
              onChange={(event) => changeDraft("maxValue", event.target.value)}
              error={Boolean(draftErrors.maxValue)}
              helperText={draftErrors.maxValue ? positiveInteger : undefined}
            />
          )}
        </Box>
        <Button
          variant="outlined"
          startIcon={<AddOutlined />}
          disabled={saving}
          onClick={addSet}
          sx={{ alignSelf: "flex-start" }}
        >
          {t("workout.configuration.addSet")}
        </Button>
        <Button variant="contained" fullWidth disabled={saving} onClick={() => void handleSave()}>
          {t("workout.configuration.save")}
        </Button>
      </Box>
    </DetailsCard>
  );
}
