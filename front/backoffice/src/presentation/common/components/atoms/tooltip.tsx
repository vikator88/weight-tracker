"use client";

import { type ComponentProps } from "react";
import MuiTooltip from "@mui/material/Tooltip";

/** The project's Tooltip: the MUI widget, themed by the MUI theme. */
export default function Tooltip(props: ComponentProps<typeof MuiTooltip>) {
  return <MuiTooltip {...props} />;
}
