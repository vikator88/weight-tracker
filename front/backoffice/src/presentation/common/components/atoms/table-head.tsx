"use client";

import { type ComponentProps } from "react";
import MuiTableHead from "@mui/material/TableHead";

/** The project's TableHead: the MUI widget, themed by the MUI theme. */
export default function TableHead(props: ComponentProps<typeof MuiTableHead>) {
  return <MuiTableHead {...props} />;
}
