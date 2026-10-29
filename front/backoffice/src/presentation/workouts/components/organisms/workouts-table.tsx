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
import useWeekLabel from "@/presentation/workouts/hooks/use-week-label";
import { useTranslation } from "react-i18next";

interface WorkoutsTableProps {
  workouts: Workout[];
  /** Whether the trainer column is rendered at all. */
  showTrainer: boolean;
}

/** The workouts in the order the API returns them, one row each. */
export default function WorkoutsTable({ workouts, showTrainer }: WorkoutsTableProps) {
  const { t } = useTranslation("workouts");
  const weekLabelOf = useWeekLabel();

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
              <TableCell>{weekLabelOf(workout.workoutDate)}</TableCell>
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
