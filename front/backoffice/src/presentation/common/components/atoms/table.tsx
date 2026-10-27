"use client";

import { type ComponentProps } from "react";
import MuiTable from "@mui/material/Table";

/** The project's Table: the MUI widget, themed by the MUI theme. */
export default function Table(props: ComponentProps<typeof MuiTable>) {
  return <MuiTable {...props} />;
}
