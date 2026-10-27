"use client";

import { type ComponentProps } from "react";
import MuiDialogTitle from "@mui/material/DialogTitle";

/** The project's DialogTitle: the MUI widget, themed by the MUI theme. */
export default function DialogTitle(props: ComponentProps<typeof MuiDialogTitle>) {
  return <MuiDialogTitle {...props} />;
}
