"use client";

import { type ComponentProps } from "react";
import MuiTableRow from "@mui/material/TableRow";

/** The project's TableRow: the MUI widget, themed by the MUI theme. */
export default function TableRow(props: ComponentProps<typeof MuiTableRow>) {
  return <MuiTableRow {...props} />;
}
