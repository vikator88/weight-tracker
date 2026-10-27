"use client";

import { type ElementType } from "react";
import MuiTypography, { type TypographyProps } from "@mui/material/Typography";

/** The project's Typography: the MUI widget, themed by the MUI theme. Accepts `component` to pick the element. */
export default function Typography(props: TypographyProps<ElementType>) {
  return <MuiTypography {...props} />;
}
