"use client";

import { type ComponentProps } from "react";
import MuiTableCell from "@mui/material/TableCell";

/** The project's TableCell: the MUI widget, themed by the MUI theme. */
export default function TableCell(props: ComponentProps<typeof MuiTableCell>) {
  return <MuiTableCell {...props} />;
}
