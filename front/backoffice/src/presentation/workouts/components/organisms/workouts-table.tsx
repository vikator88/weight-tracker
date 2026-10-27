"use client";

import EditOutlined from "@mui/icons-material/EditOutlined";
import type { Workout } from "@weight-tracker/core/domain";
import NextLink from "next/link";
import IconButton from "@/presentation/common/components/atoms/icon-button";
import Table from "@/presentation/common/components/atoms/table";
import TableBody from "@/presentation/common/components/atoms/table-body";
import TableCell from "@/presentation/common/components/atoms/table-cell";
import TableContainer from "@/presentation/common/components/atoms/table-container";
import TableHead from "@/presentation/common/components/atoms/table-head";
import TableRow from "@/presentation/common/components/atoms/table-row";
import Typography from "@/presentation/common/components/atoms/typography";
import { useTranslation } from "react-i18next";

interface WorkoutsTableProps {
  workouts: Workout[];
  /** Whether the trainer column is rendered at all. */
  showTrainer: boolean;
}

/**
 * Formats a `YYYY-MM-DD` workout date as that calendar day. Built in local time, because `new Date("YYYY-MM-DD")`
 * means UTC midnight and shows the previous day west of UTC.
 */
function formatWorkoutDate(workoutDate: string, language: string): string {
  const [year, month, day] = workoutDate.split("-").map(Number);

  return new Intl.DateTimeFormat(language, { dateStyle: "medium" }).format(
    new Date(year, month - 1, day),
  );
}

/** The workouts in the order the API returns them, one row each. */
export default function WorkoutsTable({ workouts, showTrainer }: WorkoutsTableProps) {
  const { t, i18n } = useTranslation("workouts");

  if (workouts.length === 0) {
    return <Typography color="text.secondary">{t("assignedPlans.empty")}</Typography>;
  }

  return (
    <TableContainer>
      <Table>
        <TableHead>
          <TableRow>
            <TableCell>{t("assignedPlans.columns.name")}</TableCell>
            <TableCell>{t("assignedPlans.columns.date")}</TableCell>
            <TableCell>{t("assignedPlans.columns.athlete")}</TableCell>
            {showTrainer && <TableCell>{t("assignedPlans.columns.trainer")}</TableCell>}
            <TableCell align="right">{t("assignedPlans.columns.exercises")}</TableCell>
            <TableCell />
          </TableRow>
        </TableHead>
        <TableBody>
          {workouts.map((workout) => (
            <TableRow key={workout.id}>
              <TableCell>{workout.name}</TableCell>
              <TableCell>{formatWorkoutDate(workout.workoutDate, i18n.language)}</TableCell>
              <TableCell>{workout.userId}</TableCell>
              {showTrainer && <TableCell>{workout.trainerId ?? ""}</TableCell>}
              <TableCell align="right">{workout.exercises.length}</TableCell>
              <TableCell align="right" padding="checkbox">
                <IconButton
                  component={NextLink}
                  href={`/workout?id=${encodeURIComponent(workout.id)}`}
                  aria-label={t("assignedPlans.edit", { name: workout.name })}
                >
                  <EditOutlined />
                </IconButton>
              </TableCell>
            </TableRow>
          ))}
        </TableBody>
      </Table>
    </TableContainer>
  );
}
