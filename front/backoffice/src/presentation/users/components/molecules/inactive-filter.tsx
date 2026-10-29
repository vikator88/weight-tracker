"use client";

import { useId } from "react";
import Box from "@mui/material/Box";
import Switch from "@/presentation/common/components/atoms/switch";
import Typography from "@/presentation/common/components/atoms/typography";

interface InactiveFilterProps {
  /** Shown beside the switch, and names it. */
  label: string;
  checked: boolean;
  onChange: (checked: boolean) => void;
}

/** A switch with its label beside it, to add the inactive users to the table. */
export default function InactiveFilter({ label, checked, onChange }: InactiveFilterProps) {
  const labelId = useId();

  return (
    <Box sx={{ display: "flex", alignItems: "center" }}>
      <Switch
        checked={checked}
        onChange={(event) => onChange(event.target.checked)}
        slotProps={{ input: { "aria-labelledby": labelId } }}
      />
      <Typography id={labelId} variant="body2">
        {label}
      </Typography>
    </Box>
  );
}
