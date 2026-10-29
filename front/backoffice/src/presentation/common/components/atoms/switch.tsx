"use client";

import { type ComponentProps } from "react";
import MuiSwitch from "@mui/material/Switch";

/** The project's Switch: the MUI widget, themed by the MUI theme. */
export default function Switch(props: ComponentProps<typeof MuiSwitch>) {
  return <MuiSwitch {...props} />;
}
