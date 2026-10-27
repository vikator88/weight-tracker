"use client";

import { type ComponentProps } from "react";
import MuiButton from "@mui/material/Button";

/** The project's Button: the MUI widget, themed by the MUI theme. */
export default function Button(props: ComponentProps<typeof MuiButton>) {
  return <MuiButton {...props} />;
}
