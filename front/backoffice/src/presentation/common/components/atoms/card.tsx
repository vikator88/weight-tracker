"use client";

import { type ComponentProps } from "react";
import MuiCard from "@mui/material/Card";

/** The project's Card: the MUI widget, themed by the MUI theme. */
export default function Card(props: ComponentProps<typeof MuiCard>) {
  return <MuiCard {...props} />;
}
