"use client";

import { type ComponentProps } from "react";
import MuiListSubheader from "@mui/material/ListSubheader";

/** The project's ListSubheader: the MUI widget, themed by the MUI theme. */
export default function ListSubheader(props: ComponentProps<typeof MuiListSubheader>) {
  return <MuiListSubheader {...props} />;
}
