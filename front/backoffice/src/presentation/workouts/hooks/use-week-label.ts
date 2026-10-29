import { useCallback } from "react";
import { useTranslation } from "react-i18next";
import { weekEndOf, weekStartOf } from "@/presentation/workouts/week";

/** Turns a workout date into the week that contains it, as text in the active language. */
export default function useWeekLabel(): (date: string) => string {
  const { t, i18n } = useTranslation("workouts");
  const language = i18n.language;

  return useCallback(
    (date: string) => {
      const monday = weekStartOf(date);
      const sunday = weekEndOf(monday);
      const currentYear = String(new Date().getFullYear());
      // The year only when it is not obvious: a week in another year, or spanning two.
      const withYear = monday.slice(0, 4) !== currentYear || sunday.slice(0, 4) !== currentYear;
      const format = new Intl.DateTimeFormat(language, {
        day: "numeric",
        month: "short",
        year: withYear ? "numeric" : undefined,
        // The days are UTC midnights; formatting them in UTC keeps them on their calendar day.
        timeZone: "UTC",
      });
      const dayOf = (day: string) => format.format(new Date(`${day}T00:00:00Z`));

      return t("workout.week.label", { start: dayOf(monday), end: dayOf(sunday) });
    },
    [t, language],
  );
}
