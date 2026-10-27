"use client";

import { Suspense, useId, useMemo, useState } from "react";
import { arrayMove } from "@dnd-kit/sortable";
import ArrowBackOutlined from "@mui/icons-material/ArrowBackOutlined";
import Box from "@mui/material/Box";
import Container from "@mui/material/Container";
import {
  validateWorkoutDetailsForm,
  type WorkoutDetailsFormValues,
  type WorkoutDetailsFormViolations,
} from "@weight-tracker/core/application";
import type {
  Exercise,
  UpdateWorkoutInput,
  WorkoutDetail,
  WorkoutExerciseDetail,
} from "@weight-tracker/core/domain";
import NextLink from "next/link";
import { useSearchParams } from "next/navigation";
import { useTranslation } from "react-i18next";
import Alert from "@/presentation/common/components/atoms/alert";
import Link from "@/presentation/common/components/atoms/link";
import Typography from "@/presentation/common/components/atoms/typography";
import HomeSkeleton from "@/presentation/common/components/molecules/home-skeleton";
import LoadError from "@/presentation/common/components/molecules/load-error";
import AppShell from "@/presentation/common/components/organisms/app-shell";
import ExerciseCatalogPanel from "@/presentation/workouts/components/organisms/exercise-catalog-panel";
import ExerciseConfigurationPanel from "@/presentation/workouts/components/organisms/exercise-configuration-panel";
import WorkoutDetailsCard from "@/presentation/workouts/components/organisms/workout-details-card";
import WorkoutExercisesList, {
  type WorkoutExerciseItem,
} from "@/presentation/workouts/components/organisms/workout-exercises-list";
import useExercises from "@/presentation/workouts/hooks/use-exercises";
import useRemoveWorkoutExercise from "@/presentation/workouts/hooks/use-remove-workout-exercise";
import useReorderWorkoutExercises from "@/presentation/workouts/hooks/use-reorder-workout-exercises";
import useUpdateWorkout from "@/presentation/workouts/hooks/use-update-workout";
import useWorkout from "@/presentation/workouts/hooks/use-workout";

function WorkoutNotFound() {
  const { t } = useTranslation("workouts");

  return (
    <Box sx={{ py: 2 }}>
      <Typography sx={{ mb: 1 }}>{t("workout.notFound")}</Typography>
      <Link component={NextLink} href="/assigned-plans">
        {t("workout.backToList")}
      </Link>
    </Box>
  );
}

/** The page heading and the way back to the list. */
function WorkoutHeader() {
  const { t } = useTranslation("workouts");

  return (
    <Box sx={{ mb: 3 }}>
      <Typography variant="h4" component="h1">
        {t("workout.editTitle")}
      </Typography>
      <Link
        component={NextLink}
        href="/assigned-plans"
        underline="hover"
        sx={{ display: "inline-flex", alignItems: "center", gap: 0.5, mt: 0.5 }}
      >
        <ArrowBackOutlined fontSize="small" />
        {t("workout.back")}
      </Link>
    </Box>
  );
}

/** Builds the update from valid values. The API replaces every editable property, so athlete and trainer go back as read. */
function toUpdate(workout: WorkoutDetail, values: WorkoutDetailsFormValues): UpdateWorkoutInput {
  const input: UpdateWorkoutInput = {
    id: workout.id,
    name: values.name.trim(),
    workoutDate: values.workoutDate,
    userId: workout.userId,
    trainerId: workout.trainerId,
  };
  const description = values.description.trim();

  if (description !== "") {
    input.description = description;
  }
  return input;
}

/** A catalog exercise added on screen: no effort and no sets yet; they are defined in the configuration card. */
function pendingItemOf(exercise: Exercise): WorkoutExerciseItem {
  return {
    workoutExercise: {
      id: `pending-${exercise.id}`,
      exerciseId: exercise.id,
      effort: "",
      sets: [],
      superSeriesId: null,
    },
    exercise,
    pending: true,
  };
}

/** The entries sorted by their place in `ids`; any entry not in `ids` goes last, in its current order. */
function inOrderOf(entries: WorkoutExerciseDetail[], ids: string[]) {
  const placeOf = (entry: WorkoutExerciseDetail) => {
    const index = ids.indexOf(entry.id);
    return index === -1 ? ids.length : index;
  };
  return [...entries].sort((a, b) => placeOf(a) - placeOf(b));
}

/** The editor once both reads are in: it owns the workout's form, its save, and the screen's local selections. */
function LoadedWorkout({ workout, exercises }: { workout: WorkoutDetail; exercises: Exercise[] }) {
  const { t: tc } = useTranslation();
  const { t } = useTranslation("workouts");
  const structureTitleId = useId();
  const configurationTitleId = useId();
  const { saving, errorCode, saved, save, clearSaved } = useUpdateWorkout();
  const [values, setValues] = useState<WorkoutDetailsFormValues>({
    name: workout.name,
    description: workout.description ?? "",
    workoutDate: workout.workoutDate,
  });
  const [errors, setErrors] = useState<WorkoutDetailsFormViolations>({});
  const [filterText, setFilterText] = useState("");
  const [selectedBodyPart, setSelectedBodyPart] = useState<string | null>(null);
  const [selectedId, setSelectedId] = useState<string | null>(null);
  // Added from the catalog on screen; nothing is sent until their sets can be saved.
  const [pending, setPending] = useState<WorkoutExerciseItem[]>([]);
  // The workout's saved exercises, kept up to date with what each exercise save answers.
  const [savedExercises, setSavedExercises] = useState<WorkoutExerciseDetail[]>(workout.exercises);
  const [lastSavedId, setLastSavedId] = useState<string | null>(null);
  const removal = useRemoveWorkoutExercise();
  const reordering = useReorderWorkoutExercises();

  const catalogById = useMemo(
    () => new Map<string, Exercise>(exercises.map((exercise) => [exercise.id, exercise])),
    [exercises],
  );
  const items = useMemo<WorkoutExerciseItem[]>(
    () => [
      ...savedExercises.map((workoutExercise) => ({
        workoutExercise,
        exercise: catalogById.get(workoutExercise.exerciseId),
      })),
      ...pending,
    ],
    [savedExercises, catalogById, pending],
  );
  // An exercise already in the workout, saved or pending, leaves the catalog: it cannot be added twice.
  const available = useMemo(() => {
    const inWorkout = new Set(items.map((item) => item.workoutExercise.exerciseId));
    return exercises.filter((exercise) => !inWorkout.has(exercise.id));
  }, [exercises, items]);
  const selected = items.find((item) => item.workoutExercise.id === selectedId) ?? null;

  function handleAdd(exercise: Exercise) {
    const item = pendingItemOf(exercise);
    setPending((current) => [...current, item]);
    setSelectedId(item.workoutExercise.id);
  }

  /** A saved pending exercise joins the saved ones; an updated one replaces its entry. It stays selected. */
  function handleExerciseSaved(entry: WorkoutExerciseDetail, item: WorkoutExerciseItem) {
    if (item.pending) {
      setPending((current) =>
        current.filter((other) => other.workoutExercise.id !== item.workoutExercise.id),
      );
      setSavedExercises((current) => [...current, entry]);
    } else {
      setSavedExercises((current) =>
        current.map((other) => (other.exerciseId === entry.exerciseId ? entry : other)),
      );
    }
    setSelectedId(entry.id);
    setLastSavedId(entry.id);
  }

  /**
   * A pending exercise is only dropped from the screen; a saved one is removed through the API first. Either way it
   * returns to the catalog, which leaves out only the exercises still in the workout.
   */
  async function handleRemove(item: WorkoutExerciseItem) {
    const { id, exerciseId } = item.workoutExercise;

    if (item.pending) {
      setPending((current) => current.filter((other) => other.workoutExercise.id !== id));
    } else if (await removal.remove(workout.id, id, exerciseId)) {
      setSavedExercises((current) => current.filter((other) => other.id !== id));
    } else {
      return;
    }
    setSelectedId((current) => (current === id ? null : current));
  }

  /**
   * The dropped exercise moves at once and the full saved order is sent. A refused order is put back by entry id, so
   * an exercise saved or removed while the request was in flight is kept as it is.
   */
  async function handleReorder(activeId: string, overId: string) {
    const from = savedExercises.findIndex((entry) => entry.id === activeId);
    const to = savedExercises.findIndex((entry) => entry.id === overId);
    if (from === -1 || to === -1 || from === to) {
      return;
    }

    const previousIds = savedExercises.map((entry) => entry.id);
    const next = arrayMove(savedExercises, from, to);
    setSavedExercises(next);

    if (
      !(await reordering.reorder(
        workout.id,
        next.map((entry) => entry.exerciseId),
      ))
    ) {
      setSavedExercises((current) => inOrderOf(current, previousIds));
    }
  }

  function handleChange(field: keyof WorkoutDetailsFormValues, value: string) {
    setValues((current) => ({ ...current, [field]: value }));
    clearSaved();
  }

  async function handleSave() {
    const violations = validateWorkoutDetailsForm(values);
    setErrors(violations);
    if (Object.keys(violations).length > 0) {
      return;
    }

    await save(toUpdate(workout, values));
  }

  return (
    <>
      <WorkoutHeader />
      {errorCode && (
        <Alert severity="error" sx={{ mb: 3 }}>
          {tc(`errors:${errorCode}`)}
        </Alert>
      )}
      {saved && (
        <Alert severity="success" sx={{ mb: 3 }}>
          {t("workout.details.saved")}
        </Alert>
      )}
      <Box
        sx={{
          display: "grid",
          gridTemplateColumns: {
            xs: "minmax(0, 1fr)",
            md: "minmax(0, 25fr) minmax(0, 40fr) minmax(0, 35fr)",
          },
          gap: 3,
          alignItems: "start",
        }}
      >
        <ExerciseCatalogPanel
          exercises={available}
          onAdd={handleAdd}
          filterText={filterText}
          onFilterTextChange={setFilterText}
          selectedBodyPart={selectedBodyPart}
          onToggleBodyPart={(bodyPart) =>
            setSelectedBodyPart((current) => (current === bodyPart ? null : bodyPart))
          }
        />
        <Box component="section" aria-labelledby={structureTitleId} sx={{ minWidth: 0 }}>
          <WorkoutDetailsCard
            values={values}
            errors={errors}
            saving={saving}
            onSave={() => void handleSave()}
            disabled={saving}
            onChange={handleChange}
            titleId={structureTitleId}
          >
            <WorkoutExercisesList
              items={items}
              selectedId={selectedId}
              onSelect={setSelectedId}
              onRemove={(item) => void handleRemove(item)}
              removingId={removal.removingId}
              removeErrorCode={removal.errorCode}
              onReorder={(activeId, overId) => void handleReorder(activeId, overId)}
              reordering={reordering.reordering}
              reorderErrorCode={reordering.errorCode}
            />
          </WorkoutDetailsCard>
        </Box>
        <Box component="section" aria-labelledby={configurationTitleId} sx={{ minWidth: 0 }}>
          {/* Keyed by the selection, so each one starts from the stored values. */}
          <ExerciseConfigurationPanel
            key={selected?.workoutExercise.id ?? "none"}
            item={selected}
            titleId={configurationTitleId}
            workoutId={workout.id}
            justSaved={selected !== null && selected.workoutExercise.id === lastSavedId}
            onSaved={handleExerciseSaved}
            onEdit={() => setLastSavedId(null)}
          />
        </Box>
      </Box>
    </>
  );
}

function WorkoutEditor({ id }: { id: string | null }) {
  const workout = useWorkout(id);
  const catalog = useExercises();

  if (workout.status === "failed" && workout.errorCode === "NOT_FOUND") {
    return <WorkoutNotFound />;
  }
  if (workout.status === "failed" && workout.errorCode) {
    return <LoadError code={workout.errorCode} onRetry={workout.reload} />;
  }
  if (catalog.status === "failed" && catalog.errorCode) {
    return <LoadError code={catalog.errorCode} onRetry={catalog.reload} />;
  }
  if (workout.status !== "loaded" || catalog.status !== "loaded" || workout.workout === null) {
    return <HomeSkeleton />;
  }

  return <LoadedWorkout workout={workout.workout} exercises={catalog.exercises} />;
}

/** Reads the id from the query string, which needs a `Suspense` boundary in a static export. */
function WorkoutScreen() {
  const id = useSearchParams().get("id");

  // Keyed by the id, so opening another workout starts from a clean screen.
  return <WorkoutEditor key={id ?? "none"} id={id} />;
}

export default function WorkoutView() {
  return (
    <AppShell>
      <Container maxWidth="xl" sx={{ py: 4 }}>
        <Suspense fallback={<HomeSkeleton />}>
          <WorkoutScreen />
        </Suspense>
      </Container>
    </AppShell>
  );
}
