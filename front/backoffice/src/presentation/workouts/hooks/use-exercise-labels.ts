import { useCallback, useMemo } from "react";
import type { Exercise } from "@weight-tracker/core/domain";
import { useTranslation } from "react-i18next";

/** A chip colour of the MUI theme. */
export type LabelColor =
  "default" | "primary" | "secondary" | "error" | "info" | "success" | "warning";

/** One colour per API body part, so a body part looks the same everywhere. */
const BODY_PART_COLORS: Record<string, LabelColor> = {
  CHEST: "error",
  BACK: "info",
  LEGS: "success",
  ARMS: "warning",
  SHOULDERS: "secondary",
  CORE: "primary",
  NECK: "default",
};

/** The set target types that have a summary of their own; any other is summarised generically. */
const SUMMARY_TYPES = new Set(["Reps", "Duration", "MaxReps", "RepsRange"]);

export interface SetSummaryValues {
  count: string | number;
  type: string;
  value?: string | number | null;
  maxValue?: string | number | null;
}

interface ExerciseLabels {
  /** The name in the active language, else the first translation, else the exercise id. */
  nameOf: (exercise: Exercise) => string;
  muscleLabel: (code: string) => string;
  bodyPartLabel: (code: string) => string;
  effortLabel: (code: string) => string;
  targetTypeLabel: (type: string) => string;
  /** One set as a short text, such as "2 × 15 repeticiones"; values are given as typed or as numbers. */
  setSummary: (set: SetSummaryValues) => string;
  /** The chip colour of a body part; unknown codes are neutral. */
  bodyPartColor: (code: string) => LabelColor;
}

/** The single place that turns catalog codes into text. An unknown code is shown as it comes. */
export default function useExerciseLabels(): ExerciseLabels {
  const { t, i18n } = useTranslation("workouts");
  const language = i18n.language;

  const nameOf = useCallback(
    (exercise: Exercise) =>
      (
        exercise.translations.find((translation) => translation.languageCode === language) ??
        exercise.translations[0]
      )?.name ?? exercise.id,
    [language],
  );

  return useMemo(() => {
    const labelIn = (group: string) => (code: string) =>
      t(`${group}.${code}`, { defaultValue: code });

    return {
      nameOf,
      muscleLabel: labelIn("muscles"),
      bodyPartLabel: labelIn("bodyParts"),
      effortLabel: labelIn("efforts"),
      targetTypeLabel: labelIn("targetTypes"),
      setSummary: ({ count, type, value, maxValue }: SetSummaryValues) =>
        t(`workout.configuration.summary.${SUMMARY_TYPES.has(type) ? type : "other"}`, {
          // `series`, not `count`: i18next reserves `count` for plurals.
          series: count,
          value: value ?? "",
          max: maxValue ?? "",
        }),
      bodyPartColor: (code: string) => BODY_PART_COLORS[code] ?? "default",
    };
  }, [t, nameOf]);
}
