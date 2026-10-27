"use client";

import { type ComponentProps } from "react";
import MuiTextField from "@mui/material/TextField";

/** The project's TextField: the MUI widget, themed by the MUI theme. */
export default function TextField(props: ComponentProps<typeof MuiTextField>) {
  return <MuiTextField {...props} />;
}
