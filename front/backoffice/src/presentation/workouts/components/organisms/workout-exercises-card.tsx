"use client";

import type { ReactNode, Ref } from "react";
import AddOutlined from "@mui/icons-material/AddOutlined";
import Box from "@mui/material/Box";
import { useTranslation } from "react-i18next";
import Button from "@/presentation/common/components/atoms/button";
import Card from "@/presentation/common/components/atoms/card";
import CardContent from "@/presentation/common/components/atoms/card-content";
import Typography from "@/presentation/common/components/atoms/typography";

interface WorkoutExercisesCardProps {
  titleId: string;
  /** Añadir ejercicio was pressed: the catalog opens. */
  onAdd: () => void;
  /** The add button, so the screen can return the focus to it once an add flow ends. */
  addButtonRef?: Ref<HTMLButtonElement>;
  /** Shown below the header: the workout's exercises. */
  children: ReactNode;
}

/** The workout's exercises under an "Ejercicios" header, with the action that adds one from the catalog. */
export default function WorkoutExercisesCard({
  titleId,
  onAdd,
  addButtonRef,
  children,
}: WorkoutExercisesCardProps) {
  const { t } = useTranslation("workouts");

  return (
    <Card variant="outlined">
      <CardContent>
        <Box
          sx={{
            display: "flex",
            alignItems: "center",
            justifyContent: "space-between",
            flexWrap: "wrap",
            gap: 1,
            mb: 1,
          }}
        >
          <Typography id={titleId} variant="h6" component="h2">
            {t("workout.exercises.title")}
          </Typography>
          <Button
            variant="contained"
            startIcon={<AddOutlined />}
            onClick={onAdd}
            ref={addButtonRef}
          >
            {t("workout.exercises.add")}
          </Button>
        </Box>
        {children}
      </CardContent>
    </Card>
  );
}
