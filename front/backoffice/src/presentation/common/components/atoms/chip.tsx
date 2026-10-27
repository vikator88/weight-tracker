"use client";

import { type ComponentProps } from "react";
import MuiChip from "@mui/material/Chip";

/** The project's Chip: the MUI widget, themed by the MUI theme. */
export default function Chip(props: ComponentProps<typeof MuiChip>) {
  return <MuiChip {...props} />;
}
