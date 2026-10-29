"use client";

import { createTheme, lighten } from "@mui/material/styles";

declare module "@mui/material/styles" {
  interface Palette {
    strength: Palette["primary"];
    cardio: Palette["primary"];
    stretching: Palette["primary"];
  }

  interface PaletteOptions {
    strength?: PaletteOptions["primary"];
    cardio?: PaletteOptions["primary"];
    stretching?: PaletteOptions["primary"];
  }

  interface PaletteColor {
    /** A very soft tone of the colour, for large backgrounds such as an exercise card. */
    pastel?: string;
  }

  interface SimplePaletteColorOptions {
    pastel?: string;
  }
}

declare module "@mui/material/Chip" {
  interface ChipPropsColorOverrides {
    strength: true;
    cardio: true;
    stretching: true;
  }
}

const base = createTheme();

/** One colour per exercise type, with its pastel tone for the cards of that type. */
function exerciseTypeColor(main: string, name: string) {
  return { ...base.palette.augmentColor({ color: { main }, name }), pastel: lighten(main, 0.88) };
}

/** Signed-in screens sit on a light grey so their cards and tables stand out; surfaces stay white. */
const theme = createTheme({
  palette: {
    background: {
      default: "#f4f6f8",
    },
    strength: exerciseTypeColor("#2F80ED", "strength"),
    cardio: exerciseTypeColor("#E5484D", "cardio"),
    stretching: exerciseTypeColor("#27AE60", "stretching"),
  },
});

export default theme;
