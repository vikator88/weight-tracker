"use client";

import { createTheme } from "@mui/material/styles";

/** Signed-in screens sit on a light grey so their cards and tables stand out; surfaces stay white. */
const theme = createTheme({
  palette: {
    background: {
      default: "#f4f6f8",
    },
  },
});

export default theme;
