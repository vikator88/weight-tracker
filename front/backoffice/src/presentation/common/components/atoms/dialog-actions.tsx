"use client";

import { type ComponentProps } from "react";
import MuiDialogActions from "@mui/material/DialogActions";

/** The project's DialogActions: the MUI widget, themed by the MUI theme. */
export default function DialogActions(props: ComponentProps<typeof MuiDialogActions>) {
  return <MuiDialogActions {...props} />;
}
