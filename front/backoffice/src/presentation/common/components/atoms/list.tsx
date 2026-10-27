"use client";

import { type ComponentProps } from "react";
import MuiList from "@mui/material/List";

/** The project's List: the MUI widget, themed by the MUI theme. */
export default function List(props: ComponentProps<typeof MuiList>) {
  return <MuiList {...props} />;
}
