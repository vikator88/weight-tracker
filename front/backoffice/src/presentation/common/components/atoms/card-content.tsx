"use client";

import { type ComponentProps } from "react";
import MuiCardContent from "@mui/material/CardContent";

/** The project's CardContent: the MUI widget, themed by the MUI theme. */
export default function CardContent(props: ComponentProps<typeof MuiCardContent>) {
  return <MuiCardContent {...props} />;
}
