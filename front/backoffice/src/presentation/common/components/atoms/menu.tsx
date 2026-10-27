"use client";

import { type ComponentProps } from "react";
import MuiMenu from "@mui/material/Menu";

/** The project's Menu: the MUI widget, themed by the MUI theme. */
export default function Menu(props: ComponentProps<typeof MuiMenu>) {
  return <MuiMenu {...props} />;
}
