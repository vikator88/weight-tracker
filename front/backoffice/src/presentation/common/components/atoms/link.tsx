"use client";

import { type ElementType } from "react";
import MuiLink, { type LinkProps } from "@mui/material/Link";

/** The project's Link: the MUI widget, themed by the MUI theme. Accepts `component` to render a router link. */
export default function Link(props: LinkProps<ElementType>) {
  return <MuiLink {...props} />;
}
