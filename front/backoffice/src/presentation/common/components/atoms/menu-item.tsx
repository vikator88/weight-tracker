"use client";

import { type ComponentProps } from "react";
import MuiMenuItem from "@mui/material/MenuItem";

/** The project's MenuItem: the MUI widget, themed by the MUI theme. */
export default function MenuItem(props: ComponentProps<typeof MuiMenuItem>) {
  return <MuiMenuItem {...props} />;
}
