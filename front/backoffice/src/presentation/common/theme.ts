"use client";

import {
  alpha,
  createTheme,
  darken,
  lighten,
  type PaletteMode,
  type Shadows,
} from "@mui/material/styles";

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

  /** Extra tokens for charts, the mood scale and gradients: `sx={{ background: (t) => t.custom.gradients.hero }}`. */
  interface Theme {
    custom: CustomTokens;
  }

  interface ThemeOptions {
    custom?: CustomTokens;
  }
}

declare module "@mui/material/Chip" {
  interface ChipPropsColorOverrides {
    strength: true;
    cardio: true;
    stretching: true;
  }
}

interface CustomTokens {
  mood: string[];
  chart: string[];
  gradients: { hero: string; energy: string };
}

/* ---------- Brand tokens, the same in both modes ---------- */
const c = {
  teal: "#0F766E",
  tealLight: "#14B8A6",
  tealBright: "#2DD4BF",
  pine: "#115E59",
  orange: "#F97316",
  burnt: "#C2410C",
  ink: "#0F172A",
  white: "#FFFFFF",
};

/* ---------- Surfaces per mode ---------- */
const surfaces = {
  light: {
    bg: "#F8FAFC",
    paper: "#FFFFFF",
    fog: "#F1F5F9",
    border: "#E2E8F0",
    input: "#FFFFFF",
    text: "#0F172A",
    textSecondary: "#475569",
    textDisabled: "#94A3B8",
    sidebar: "#0F172A",
  },
  dark: {
    bg: "#0B1120",
    paper: "#131C2E",
    fog: "#1B2740",
    border: "#263450",
    input: "#1B2740",
    text: "#F1F5F9",
    textSecondary: "#94A3B8",
    textDisabled: "#64748B",
    sidebar: "#070B14",
  },
};

export const moodColors = ["#DC2626", "#F97316", "#EAB308", "#84CC16", "#16A34A"];
export const chartColors = ["#0F766E", "#F97316", "#6366F1", "#0EA5E9", "#64748B"];

// The variables are set on <html> by next/font in the root layout; the rest is the fallback.
const HEADING_FONT =
  'var(--font-plus-jakarta-sans), "Plus Jakarta Sans", "Inter", system-ui, sans-serif';
const BODY_FONT = 'var(--font-inter), "Inter", system-ui, -apple-system, "Segoe UI", sans-serif';

/* ---------- Elevation: soft, slate-tinted shadows (25 levels required by MUI) ---------- */
const shadows = Array.from({ length: 25 }, (_, i) =>
  i === 0
    ? "none"
    : `0 ${Math.round(i * 0.5 + 1)}px ${Math.round(i * 1.2 + 2)}px ${-Math.round(i * 0.1)}px rgba(15,23,42,${Math.min(0.03 + i * 0.005, 0.14).toFixed(3)})`,
) as Shadows;

const base = createTheme();

/**
 * One colour per exercise type, with its pastel tone for the cards of that type: a soft tint in light mode and a
 * deep one in dark mode, so the card text keeps its contrast.
 */
function exerciseTypeColor(main: string, name: string, mode: PaletteMode) {
  const pastel = mode === "dark" ? darken(main, 0.7) : lighten(main, 0.88);

  return { ...base.palette.augmentColor({ color: { main }, name }), pastel };
}

/** The backoffice theme in either mode. The sidebar is dark in both. */
export function buildTheme(mode: PaletteMode) {
  const s = surfaces[mode];
  const isDark = mode === "dark";
  // Brighter teal in dark mode, for contrast on the dark surfaces.
  const primary = isDark ? c.tealLight : c.teal;
  const primaryHover = isDark ? c.tealBright : c.pine;
  const primaryStrong = isDark ? c.tealLight : c.pine;

  return createTheme({
    palette: {
      mode,
      primary: isDark
        ? { main: primary, dark: c.pine, contrastText: s.bg }
        : { main: primary, light: c.tealLight, dark: c.pine, contrastText: c.white },
      secondary: {
        main: c.orange,
        dark: c.burnt,
        contrastText: c.ink, // white on orange fails contrast; ink passes
      },
      success: { main: "#16A34A", contrastText: c.white },
      warning: { main: "#D97706", contrastText: c.white },
      error: { main: "#DC2626", contrastText: c.white },
      info: { main: "#2563EB", contrastText: c.white },
      background: { default: s.bg, paper: s.paper },
      text: { primary: s.text, secondary: s.textSecondary, disabled: s.textDisabled },
      divider: s.border,
      action: {
        hover: alpha(primary, isDark ? 0.08 : 0.06),
        selected: alpha(primary, isDark ? 0.14 : 0.1),
        focus: alpha(primary, isDark ? 0.16 : 0.12),
      },
      strength: exerciseTypeColor("#2F80ED", "strength", mode),
      cardio: exerciseTypeColor("#E5484D", "cardio", mode),
      stretching: exerciseTypeColor("#27AE60", "stretching", mode),
    },

    shape: { borderRadius: 14 },

    spacing: 8,

    shadows,

    typography: {
      fontFamily: BODY_FONT,
      fontSize: 14,
      h1: {
        fontFamily: HEADING_FONT,
        fontSize: "2.5rem",
        lineHeight: 1.2,
        fontWeight: 800,
        letterSpacing: "-0.03em",
      },
      h2: {
        fontFamily: HEADING_FONT,
        fontSize: "2rem",
        lineHeight: 1.25,
        fontWeight: 800,
        letterSpacing: "-0.02em",
      },
      h3: {
        fontFamily: HEADING_FONT,
        fontSize: "1.75rem",
        lineHeight: 1.29,
        fontWeight: 700,
        letterSpacing: "-0.02em",
      },
      h4: {
        fontFamily: HEADING_FONT,
        fontSize: "1.5rem",
        lineHeight: 1.33,
        fontWeight: 700,
        letterSpacing: "-0.01em",
      },
      h5: { fontFamily: HEADING_FONT, fontSize: "1.25rem", lineHeight: 1.4, fontWeight: 700 },
      h6: { fontFamily: HEADING_FONT, fontSize: "1.125rem", lineHeight: 1.45, fontWeight: 700 },
      subtitle1: { fontSize: "1rem", lineHeight: 1.5, fontWeight: 600 },
      subtitle2: { fontSize: "0.875rem", lineHeight: 1.43, fontWeight: 600 },
      body1: { fontSize: "1rem", lineHeight: 1.5, fontWeight: 400 },
      body2: { fontSize: "0.875rem", lineHeight: 1.57, fontWeight: 400 },
      button: {
        fontSize: "0.9375rem",
        fontWeight: 700,
        letterSpacing: "0.01em",
        textTransform: "none",
      },
      caption: { fontSize: "0.75rem", lineHeight: 1.33, fontWeight: 500 },
      overline: { fontSize: "0.75rem", lineHeight: 1.33, fontWeight: 700, letterSpacing: "0.12em" },
    },

    transitions: {
      duration: { shortest: 150, shorter: 200, short: 250, standard: 300, complex: 375 },
    },

    components: {
      MuiCssBaseline: {
        styleOverrides: {
          body: { WebkitFontSmoothing: "antialiased", MozOsxFontSmoothing: "grayscale" },
          "::selection": { background: alpha(primary, 0.2) },
        },
      },

      /* Buttons: confident, tall, rounded */
      MuiButton: {
        defaultProps: { disableElevation: true, size: "medium" },
        styleOverrides: {
          root: {
            borderRadius: 12,
            minHeight: 44,
            paddingInline: 20,
            transition: "all 200ms ease",
            "&:active": { transform: "translateY(1px)" },
            // MUI 9 has no compound keys such as `containedPrimary`; variant and colour pairs go here.
            variants: [
              {
                props: { variant: "contained", color: "primary" },
                style: {
                  "&:hover": {
                    backgroundColor: primaryHover,
                    boxShadow: `0 8px 20px ${alpha(primary, 0.28)}`,
                  },
                },
              },
              {
                props: { variant: "contained", color: "secondary" },
                style: {
                  "&:hover": {
                    backgroundColor: c.burnt,
                    boxShadow: `0 8px 20px ${alpha(c.orange, 0.32)}`,
                  },
                },
              },
              {
                props: { variant: "outlined", color: "primary" },
                style: {
                  borderWidth: 1.5,
                  "&:hover": { borderWidth: 1.5, backgroundColor: alpha(primary, 0.05) },
                },
              },
              {
                props: { variant: "text", color: "primary" },
                style: { "&:hover": { backgroundColor: alpha(primary, 0.06) } },
              },
            ],
          },
          sizeSmall: { minHeight: 36, paddingInline: 14, fontSize: "0.8125rem", borderRadius: 10 },
          sizeLarge: { minHeight: 52, paddingInline: 28, fontSize: "1rem", borderRadius: 14 },
        },
      },

      MuiIconButton: {
        styleOverrides: {
          root: { borderRadius: 12, transition: "background-color 200ms ease" },
        },
      },

      MuiFab: {
        styleOverrides: {
          root: { borderRadius: 16, boxShadow: `0 10px 24px ${alpha(c.orange, 0.35)}` },
        },
      },

      /* Surfaces */
      MuiPaper: {
        styleOverrides: {
          root: { backgroundImage: "none" },
          rounded: { borderRadius: 16 },
        },
      },

      MuiCard: {
        defaultProps: { elevation: 0 },
        styleOverrides: {
          root: {
            borderRadius: 16,
            border: `1px solid ${s.border}`,
            boxShadow: shadows[1],
            transition: "box-shadow 200ms ease, transform 200ms ease, border-color 200ms ease",
            "&:hover": { borderColor: alpha(primary, 0.25) },
          },
        },
      },

      MuiCardContent: {
        styleOverrides: {
          root: { padding: 24, "&:last-child": { paddingBottom: 24 } },
        },
      },

      MuiCardHeader: {
        styleOverrides: {
          root: { padding: 24 },
        },
      },

      MuiDivider: {
        styleOverrides: {
          root: { borderColor: s.border },
        },
      },

      /* Inputs */
      MuiTextField: {
        defaultProps: { variant: "outlined", size: "medium", fullWidth: true },
      },

      MuiOutlinedInput: {
        styleOverrides: {
          root: {
            borderRadius: 12,
            backgroundColor: s.input,
            transition: "border-color 200ms ease, box-shadow 200ms ease",
            "& .MuiOutlinedInput-notchedOutline": { borderColor: s.border },
            "&:hover .MuiOutlinedInput-notchedOutline": { borderColor: s.textSecondary },
            "&.Mui-focused": { boxShadow: `0 0 0 4px ${alpha(primary, 0.12)}` },
            "&.Mui-focused .MuiOutlinedInput-notchedOutline": {
              borderColor: primary,
              borderWidth: 2,
            },
            "&.Mui-error .MuiOutlinedInput-notchedOutline": { borderColor: "#DC2626" },
          },
          input: {
            padding: "12px 14px",
            variants: [{ props: { size: "small" }, style: { padding: "9px 12px" } }],
          },
        },
      },

      MuiInputLabel: {
        styleOverrides: {
          root: { fontWeight: 500, color: s.textSecondary },
        },
      },

      MuiSelect: {
        defaultProps: { variant: "outlined" },
      },

      MuiSwitch: {
        styleOverrides: {
          switchBase: {
            "&.Mui-checked": { color: c.white },
            "&.Mui-checked + .MuiSwitch-track": { backgroundColor: primary, opacity: 1 },
          },
          track: { backgroundColor: s.textSecondary, opacity: 0.3 },
        },
      },

      MuiSlider: {
        styleOverrides: {
          root: { color: primary, height: 6 },
          thumb: { width: 20, height: 20, boxShadow: shadows[3] },
          rail: { opacity: 1, backgroundColor: s.border },
        },
      },

      /* Feedback & status */
      MuiChip: {
        styleOverrides: {
          root: { borderRadius: 999, fontWeight: 600, height: 30 },
          label: { paddingInline: 12 },
          sizeSmall: { height: 24, fontSize: "0.75rem" },
        },
      },

      MuiAlert: {
        styleOverrides: {
          root: {
            borderRadius: 12,
            alignItems: "center",
            variants: [
              {
                props: { variant: "standard", severity: "success" },
                style: { backgroundColor: alpha("#16A34A", 0.1) },
              },
              {
                props: { variant: "standard", severity: "warning" },
                style: { backgroundColor: alpha("#D97706", 0.1) },
              },
              {
                props: { variant: "standard", severity: "error" },
                style: { backgroundColor: alpha("#DC2626", 0.08) },
              },
              {
                props: { variant: "standard", severity: "info" },
                style: { backgroundColor: alpha("#2563EB", 0.08) },
              },
            ],
          },
        },
      },

      MuiLinearProgress: {
        styleOverrides: {
          root: { height: 10, borderRadius: 999, backgroundColor: s.border },
          bar: { borderRadius: 999, backgroundColor: primary },
        },
      },

      MuiCircularProgress: {
        styleOverrides: {
          circle: { strokeLinecap: "round" },
        },
      },

      MuiTooltip: {
        styleOverrides: {
          tooltip: {
            backgroundColor: c.ink,
            borderRadius: 8,
            fontSize: "0.75rem",
            fontWeight: 500,
            padding: "6px 10px",
          },
          arrow: { color: c.ink },
        },
      },

      MuiAvatar: {
        styleOverrides: {
          root: { fontWeight: 700, backgroundColor: alpha(primary, 0.12), color: primaryStrong },
        },
      },

      /* Navigation */
      MuiAppBar: {
        defaultProps: { elevation: 0, color: "inherit" },
        styleOverrides: {
          root: {
            backgroundColor: alpha(s.paper, 0.85),
            backdropFilter: "saturate(180%) blur(12px)",
            color: s.text,
            borderBottom: `1px solid ${s.border}`,
          },
        },
      },

      // The drawer is dark, so its list items and subheaders are styled here rather than globally:
      // a list on a white surface keeps MUI's defaults.
      MuiDrawer: {
        styleOverrides: {
          paper: {
            backgroundColor: s.sidebar,
            color: c.white,
            borderRight: "none",
            "& .MuiListSubheader-root": {
              backgroundColor: "transparent",
              color: alpha(c.white, 0.5),
            },
            "& .MuiListItemButton-root": {
              borderRadius: 12,
              marginInline: 8,
              marginBlock: 2,
              color: alpha(c.white, 0.72),
              "&:hover": { backgroundColor: alpha(c.white, 0.06), color: c.white },
              "&.Mui-selected": {
                backgroundColor: alpha(c.tealLight, 0.16),
                color: c.tealLight,
                "&:hover": { backgroundColor: alpha(c.tealLight, 0.22) },
              },
            },
          },
        },
      },

      MuiListItemIcon: {
        styleOverrides: {
          root: { color: "inherit", minWidth: 40 },
        },
      },

      MuiTabs: {
        styleOverrides: {
          indicator: { height: 3, borderRadius: 3, backgroundColor: primary },
        },
      },

      MuiTab: {
        styleOverrides: {
          root: { textTransform: "none", fontWeight: 600, minHeight: 44, fontSize: "0.9375rem" },
        },
      },

      MuiToggleButton: {
        styleOverrides: {
          root: {
            borderRadius: 12,
            textTransform: "none",
            fontWeight: 600,
            "&.Mui-selected": { backgroundColor: alpha(primary, 0.12), color: primaryStrong },
          },
        },
      },

      /* Overlays */
      MuiDialog: {
        styleOverrides: {
          paper: { borderRadius: 20, boxShadow: shadows[12] },
        },
      },

      MuiDialogTitle: {
        styleOverrides: {
          root: {
            fontFamily: HEADING_FONT,
            fontWeight: 700,
            fontSize: "1.25rem",
            paddingBlock: 20,
          },
        },
      },

      MuiDialogActions: {
        styleOverrides: {
          root: { padding: "16px 24px 20px", gap: 8 },
        },
      },

      MuiMenu: {
        styleOverrides: {
          paper: { borderRadius: 14, boxShadow: shadows[8], border: `1px solid ${s.border}` },
        },
      },

      MuiMenuItem: {
        styleOverrides: {
          root: { borderRadius: 8, marginInline: 6, minHeight: 40 },
        },
      },

      /* Data */
      MuiTableContainer: {
        styleOverrides: {
          root: { borderRadius: 16, border: `1px solid ${s.border}`, backgroundColor: s.paper },
        },
      },

      MuiTableCell: {
        styleOverrides: {
          root: { borderBottom: `1px solid ${s.border}`, padding: "14px 16px" },
          head: {
            color: s.textSecondary,
            fontWeight: 600,
            fontSize: "0.75rem",
            textTransform: "uppercase",
            letterSpacing: "0.06em",
            backgroundColor: s.fog,
          },
        },
      },
    },

    custom: {
      mood: moodColors,
      chart: chartColors,
      gradients: {
        hero: `linear-gradient(135deg, ${c.teal} 0%, ${c.pine} 100%)`,
        energy: `linear-gradient(135deg, ${c.orange} 0%, ${c.burnt} 100%)`,
      },
    },
  });
}

export const lightTheme = buildTheme("light");
export const darkTheme = buildTheme("dark");

export default lightTheme;
