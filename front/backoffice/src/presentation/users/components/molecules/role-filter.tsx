"use client";

import { useId } from "react";
import Box from "@mui/material/Box";
import Chip from "@/presentation/common/components/atoms/chip";
import Typography from "@/presentation/common/components/atoms/typography";

export interface RoleFilterOption {
  code: string;
  label: string;
}

interface RoleFilterProps {
  /** Shown above the pills, and names their group. */
  label: string;
  options: RoleFilterOption[];
  selected: string | null;
  onToggle: (code: string) => void;
}

/**
 * A small label, then one pill per role: outlined while unselected, filled while selected. Pills that do not fit wrap
 * onto the next line; the group never scrolls sideways.
 */
export default function RoleFilter({ label, options, selected, onToggle }: RoleFilterProps) {
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
        {options.map(({ code, label: text }) => (
          <Chip
            key={code}
            label={text}
            clickable
            color="primary"
            variant={selected === code ? "filled" : "outlined"}
            aria-pressed={selected === code}
            onClick={() => onToggle(code)}
          />
        ))}
      </Box>
    </Box>
  );
}
