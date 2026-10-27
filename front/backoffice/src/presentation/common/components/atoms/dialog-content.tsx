"use client";

import { type ComponentProps } from "react";
import MuiDialogContent from "@mui/material/DialogContent";

/** The project's DialogContent: the MUI widget, themed by the MUI theme. */
export default function DialogContent(props: ComponentProps<typeof MuiDialogContent>) {
  return <MuiDialogContent {...props} />;
}
