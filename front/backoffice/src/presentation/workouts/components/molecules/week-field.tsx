"use client";

import { useId, useRef } from "react";
import CalendarMonthOutlined from "@mui/icons-material/CalendarMonthOutlined";
import ChevronLeft from "@mui/icons-material/ChevronLeft";
import ChevronRight from "@mui/icons-material/ChevronRight";
import Box from "@mui/material/Box";
import { useTranslation } from "react-i18next";
import IconButton from "@/presentation/common/components/atoms/icon-button";
import TextField from "@/presentation/common/components/atoms/text-field";
import Typography from "@/presentation/common/components/atoms/typography";
import useWeekLabel from "@/presentation/workouts/hooks/use-week-label";
import { shiftWeek, weekStartOf } from "@/presentation/workouts/week";

interface WeekFieldProps {
  /** The Monday of the selected week, as `YYYY-MM-DD`. */
  value: string;
  /** Called with the Monday of the newly selected week. */
  onChange: (monday: string) => void;
  disabled?: boolean;
  /** A message under the field, such as a structural violation. */
  error?: string;
}

/**
 * Picks a Monday-to-Sunday week: a caption, the selected week as text, buttons to step one week back or forward, and a
 * calendar button that opens the browser's date picker, where any day picked selects its week.
 */
export default function WeekField({ value, onChange, disabled = false, error }: WeekFieldProps) {
  const { t } = useTranslation("workouts");
  const weekLabelOf = useWeekLabel();
  const labelId = useId();
  const pickerRef = useRef<HTMLInputElement>(null);

  function openPicker() {
    const picker = pickerRef.current;
    if (!picker) {
      return;
    }
    if (typeof picker.showPicker === "function") {
      picker.showPicker();
    } else {
      picker.focus();
      picker.click();
    }
  }

  return (
    <Box role="group" aria-labelledby={labelId} sx={{ position: "relative" }}>
      <Typography
        id={labelId}
        variant="caption"
        component="p"
        color={error ? "error" : "text.secondary"}
        sx={{ mb: 0.5, fontWeight: 600 }}
      >
        {t("createWorkout.fields.date")}
      </Typography>
      <Box sx={{ display: "flex", alignItems: "center", gap: 0.5 }}>
        <IconButton
          aria-label={t("workout.week.previous")}
          disabled={disabled}
          onClick={() => onChange(shiftWeek(value, -1))}
        >
          <ChevronLeft />
        </IconButton>
        <Typography aria-live="polite" sx={{ flex: 1, textAlign: "center", minWidth: 0 }}>
          {weekLabelOf(value)}
        </Typography>
        <IconButton
          aria-label={t("workout.week.next")}
          disabled={disabled}
          onClick={() => onChange(shiftWeek(value, 1))}
        >
          <ChevronRight />
        </IconButton>
        <IconButton aria-label={t("workout.week.pick")} disabled={disabled} onClick={openPicker}>
          <CalendarMonthOutlined />
        </IconButton>
      </Box>
      {/* Rendered but out of sight: `showPicker()` needs a rendered input; the calendar button is the control. */}
      <TextField
        type="date"
        value={value}
        disabled={disabled}
        inputRef={pickerRef}
        onChange={(event) => {
          if (event.target.value !== "") {
            onChange(weekStartOf(event.target.value));
          }
        }}
        aria-hidden
        slotProps={{ htmlInput: { tabIndex: -1, "aria-label": t("workout.week.pick") } }}
        sx={{
          position: "absolute",
          right: 0,
          bottom: 0,
          width: "1px",
          height: "1px",
          opacity: 0,
          overflow: "hidden",
          pointerEvents: "none",
        }}
      />
      {error && (
        <Typography variant="caption" color="error" sx={{ display: "block", mt: 0.5 }}>
          {error}
        </Typography>
      )}
    </Box>
  );
}
