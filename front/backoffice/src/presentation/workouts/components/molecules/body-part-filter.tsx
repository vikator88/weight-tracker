"use client";

import Box from "@mui/material/Box";
import Chip from "@/presentation/common/components/atoms/chip";

interface BodyPartFilterProps {
  bodyParts: { code: string; label: string }[];
  selected: string | null;
  onToggle: (code: string) => void;
}

/** One pill per body part. Pills that do not fit wrap onto the next line; the list never scrolls sideways. */
export default function BodyPartFilter({ bodyParts, selected, onToggle }: BodyPartFilterProps) {
  return (
    <Box sx={{ display: "flex", flexWrap: "wrap", gap: 1 }}>
      {bodyParts.map(({ code, label }) => (
        <Chip
          key={code}
          label={label}
          clickable
          color={selected === code ? "primary" : "default"}
          variant={selected === code ? "filled" : "outlined"}
          aria-pressed={selected === code}
          onClick={() => onToggle(code)}
        />
      ))}
    </Box>
  );
}
