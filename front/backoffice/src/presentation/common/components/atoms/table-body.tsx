"use client";

import { type ComponentProps } from "react";
import MuiTableBody from "@mui/material/TableBody";

/** The project's TableBody: the MUI widget, themed by the MUI theme. */
export default function TableBody(props: ComponentProps<typeof MuiTableBody>) {
  return <MuiTableBody {...props} />;
}
