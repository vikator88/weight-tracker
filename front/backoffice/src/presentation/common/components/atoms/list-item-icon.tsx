"use client";

import { type ComponentProps } from "react";
import MuiListItemIcon from "@mui/material/ListItemIcon";

/** The project's ListItemIcon: the MUI widget, themed by the MUI theme. */
export default function ListItemIcon(props: ComponentProps<typeof MuiListItemIcon>) {
  return <MuiListItemIcon {...props} />;
}
