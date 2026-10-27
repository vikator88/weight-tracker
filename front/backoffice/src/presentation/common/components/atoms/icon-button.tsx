"use client";

import { type ElementType } from "react";
import MuiIconButton, { type IconButtonProps } from "@mui/material/IconButton";

/**
 * The project's IconButton: the MUI widget, themed by the MUI theme. Accepts `component` to render a router link; the
 * props follow that component, a `button` by default.
 */
export default function IconButton<C extends ElementType = "button">(
  props: IconButtonProps<C, { component?: C }>,
) {
  return <MuiIconButton {...props} />;
}
