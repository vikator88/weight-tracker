"use client";

import { useId } from "react";
import Box from "@mui/material/Box";
import Chip from "@/presentation/common/components/atoms/chip";
import Typography from "@/presentation/common/components/atoms/typography";
import type { LabelColor } from "@/presentation/workouts/hooks/use-exercise-labels";

export interface LabelFilterOption {
  code: string;
  label: string;
  color: LabelColor;
}

interface LabelFilterProps {
  /** Shown above the pills, and names their group. */
  label: string;
  options: LabelFilterOption[];
  selected: string | null;
  onToggle: (code: string) => void;
}

/**
 * A small label, then one pill per option, in its colour: outlined while unselected, filled while selected. Pills that
 * do not fit wrap onto the next line; the group never scrolls sideways.
 */
export default function LabelFilter({ label, options, selected, onToggle }: LabelFilterProps) {
  const labelId = useId();

  return (
    <Box role="group" aria-labelledby={labelId}>
      <Typography
        id={labelId}
        variant="caption"
        component="p"
        color="text.secondary"
        sx={{ mb: 0.5, fontWeight: 600 }}
      >
        {label}
      </Typography>
      <Box sx={{ display: "flex", flexWrap: "wrap", gap: 1 }}>
        {options.map(({ code, label: text, color }) => (
          <Chip
            key={code}
            label={text}
            clickable
            color={color}
            variant={selected === code ? "filled" : "outlined"}
            aria-pressed={selected === code}
            onClick={() => onToggle(code)}
          />
        ))}
      </Box>
    </Box>
  );
}
