"use client";

import { type ComponentProps } from "react";
import MuiIconButton from "@mui/material/IconButton";

/** The project's IconButton: the MUI widget, themed by the MUI theme. */
export default function IconButton(props: ComponentProps<typeof MuiIconButton>) {
  return <MuiIconButton {...props} />;
}
