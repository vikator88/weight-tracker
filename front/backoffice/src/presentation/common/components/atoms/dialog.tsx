"use client";

import { type ComponentProps } from "react";
import MuiDialog from "@mui/material/Dialog";

/** The project's Dialog: the MUI widget, themed by the MUI theme. */
export default function Dialog(props: ComponentProps<typeof MuiDialog>) {
  return <MuiDialog {...props} />;
}
