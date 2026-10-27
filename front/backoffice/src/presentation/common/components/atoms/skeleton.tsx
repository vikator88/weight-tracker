"use client";

import { type ComponentProps } from "react";
import MuiSkeleton from "@mui/material/Skeleton";

/** The project's Skeleton: the MUI widget, themed by the MUI theme. */
export default function Skeleton(props: ComponentProps<typeof MuiSkeleton>) {
  return <MuiSkeleton {...props} />;
}
