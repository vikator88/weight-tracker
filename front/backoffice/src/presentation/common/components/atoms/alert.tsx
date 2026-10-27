"use client";

import { type ComponentProps } from "react";
import MuiAlert from "@mui/material/Alert";

/** The project's Alert: the MUI widget, themed by the MUI theme. */
export default function Alert(props: ComponentProps<typeof MuiAlert>) {
  return <MuiAlert {...props} />;
}
