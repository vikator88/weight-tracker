"use client";

import { type ComponentProps } from "react";
import Paper from "@mui/material/Paper";
import MuiTableContainer from "@mui/material/TableContainer";

/** The project's TableContainer: the MUI widget on a Paper surface, themed by the MUI theme. */
export default function TableContainer(props: ComponentProps<typeof MuiTableContainer>) {
  return <MuiTableContainer component={Paper} {...props} />;
}
