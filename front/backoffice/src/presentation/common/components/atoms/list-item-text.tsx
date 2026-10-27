"use client";

import { type ComponentProps } from "react";
import MuiListItemText from "@mui/material/ListItemText";

/** The project's ListItemText: the MUI widget, themed by the MUI theme. */
export default function ListItemText(props: ComponentProps<typeof MuiListItemText>) {
  return <MuiListItemText {...props} />;
}
