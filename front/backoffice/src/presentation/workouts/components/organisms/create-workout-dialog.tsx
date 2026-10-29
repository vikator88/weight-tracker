"use client";

import { type FormEvent, useState } from "react";
import Box from "@mui/material/Box";
import {
  type CreateWorkoutFormValues,
  type CreateWorkoutFormViolations,
  validateCreateWorkoutForm,
  WORKOUT_DESCRIPTION_MAX_LENGTH,
  WORKOUT_NAME_MAX_LENGTH,
} from "@weight-tracker/core/application";
import type { CreateWorkoutRequest } from "@weight-tracker/core/domain";
import { useRouter } from "next/navigation";
import { useTranslation } from "react-i18next";
import Alert from "@/presentation/common/components/atoms/alert";
import Button from "@/presentation/common/components/atoms/button";
import Dialog from "@/presentation/common/components/atoms/dialog";
import DialogActions from "@/presentation/common/components/atoms/dialog-actions";
import DialogContent from "@/presentation/common/components/atoms/dialog-content";
import DialogTitle from "@/presentation/common/components/atoms/dialog-title";
import TextField from "@/presentation/common/components/atoms/text-field";
import useCurrentUser from "@/presentation/common/hooks/use-current-user";
import WeekField from "@/presentation/workouts/components/molecules/week-field";
import useCreateWorkout from "@/presentation/workouts/hooks/use-create-workout";
import { currentWeekStart, weekMiddleOf } from "@/presentation/workouts/week";

interface CreateWorkoutDialogProps {
  open: boolean;
  onClose: () => void;
}

const emptyValues: CreateWorkoutFormValues = {
  name: "",
  description: "",
  workoutDate: "",
  userId: "",
  trainerId: "",
};

const maxLengthOf: Partial<Record<keyof CreateWorkoutFormValues, number>> = {
  name: WORKOUT_NAME_MAX_LENGTH,
  description: WORKOUT_DESCRIPTION_MAX_LENGTH,
};

/** Builds the request from valid values: trimmed, and without the optional keys that do not apply. */
function toRequest(values: CreateWorkoutFormValues, withTrainer: boolean): CreateWorkoutRequest {
  const request: CreateWorkoutRequest = {
    name: values.name.trim(),
    // The form holds the week's Monday; the week is sent as its Wednesday.
    workoutDate: weekMiddleOf(values.workoutDate),
    userId: values.userId.trim(),
  };
  const description = values.description.trim();

  if (description !== "") {
    request.description = description;
  }
  if (withTrainer) {
    request.trainerId = values.trainerId.trim();
  }
  return request;
}

/**
 * The form lives inside the dialog, which unmounts it on close: every opening starts empty, on the current week and
 * without errors.
 */
function CreateWorkoutForm({ onClose }: { onClose: () => void }) {
  const { t } = useTranslation("workouts");
  const { t: tc } = useTranslation();
  const router = useRouter();
  const { profile } = useCurrentUser();
  const { creating, errorCode, create } = useCreateWorkout();
  const [values, setValues] = useState<CreateWorkoutFormValues>(() => ({
    ...emptyValues,
    workoutDate: currentWeekStart(),
  }));
  const [errors, setErrors] = useState<CreateWorkoutFormViolations>({});
  // Mirrors whom the API requires a trainer from; the API keeps the rule.
  const withTrainer = profile?.role === "ADMIN";

  async function handleSubmit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();

    const violations = validateCreateWorkoutForm(values, { withTrainer });
    setErrors(violations);
    if (Object.keys(violations).length > 0) {
      return;
    }

    const workout = await create(toRequest(values, withTrainer));
    if (workout) {
      onClose();
      router.push(`/workout?id=${encodeURIComponent(workout.id)}`);
    }
  }

  function field(name: keyof CreateWorkoutFormValues) {
    const violation = errors[name];

    return {
      label: t(`createWorkout.fields.${name === "workoutDate" ? "date" : name}`),
      value: values[name],
      disabled: creating,
      onChange: (event: { target: { value: string } }) =>
        setValues((current) => ({ ...current, [name]: event.target.value })),
      error: Boolean(violation),
      helperText: violation
        ? t(`createWorkout.validation.${violation}`, { max: maxLengthOf[name] })
        : undefined,
    };
  }

  return (
    <Box component="form" noValidate onSubmit={(event) => void handleSubmit(event)}>
      <DialogTitle id="create-workout-title">{t("createWorkout.title")}</DialogTitle>
      <DialogContent>
        <Box sx={{ display: "flex", flexDirection: "column", gap: 2, pt: 1 }}>
          {errorCode && <Alert severity="error">{tc(`errors:${errorCode}`)}</Alert>}
          <TextField {...field("name")} autoFocus />
          <TextField {...field("description")} multiline minRows={2} />
          <WeekField
            value={values.workoutDate}
            onChange={(monday) => setValues((current) => ({ ...current, workoutDate: monday }))}
            disabled={creating}
            error={
              errors.workoutDate ? t(`createWorkout.validation.${errors.workoutDate}`) : undefined
            }
          />
          <TextField {...field("userId")} />
          {withTrainer && <TextField {...field("trainerId")} />}
        </Box>
      </DialogContent>
      <DialogActions>
        <Button onClick={onClose} disabled={creating}>
          {t("createWorkout.actions.cancel")}
        </Button>
        <Button type="submit" variant="contained" disabled={creating}>
          {t("createWorkout.actions.create")}
        </Button>
      </DialogActions>
    </Box>
  );
}

/** Creates a workout and opens its screen. Escape, a click outside and Cancelar close it without a request. */
export default function CreateWorkoutDialog({ open, onClose }: CreateWorkoutDialogProps) {
  return (
    <Dialog
      open={open}
      onClose={onClose}
      fullWidth
      maxWidth="sm"
      aria-labelledby="create-workout-title"
    >
      <CreateWorkoutForm onClose={onClose} />
    </Dialog>
  );
}
