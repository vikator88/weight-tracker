"use client";

import { type ComponentProps } from "react";
import MuiToolbar from "@mui/material/Toolbar";

/** The project's Toolbar: the MUI widget, themed by the MUI theme. */
export default function Toolbar(props: ComponentProps<typeof MuiToolbar>) {
  return <MuiToolbar {...props} />;
}
