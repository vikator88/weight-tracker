"use client";

import { type ComponentProps } from "react";
import MuiListItem from "@mui/material/ListItem";

/** The project's ListItem: the MUI widget, themed by the MUI theme. */
export default function ListItem(props: ComponentProps<typeof MuiListItem>) {
  return <MuiListItem {...props} />;
}
