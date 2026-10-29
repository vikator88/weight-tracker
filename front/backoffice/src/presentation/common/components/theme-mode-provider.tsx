"use client";

import { useCallback, useEffect, useMemo, useState, type ReactNode } from "react";
import CssBaseline from "@mui/material/CssBaseline";
import { ThemeProvider } from "@mui/material/styles";
import useContainer from "../hooks/use-container";
import ThemeModeContext, { themeModes, type ThemeMode } from "../context/theme-mode-context";
import { buildTheme } from "../theme";

interface ThemeModeProviderProps {
  children: ReactNode;
}

/** Applies the light or dark theme, starting light until the mode chosen on this device is read. */
export default function ThemeModeProvider({ children }: ThemeModeProviderProps) {
  const container = useContainer();
  const [mode, setMode] = useState<ThemeMode>("light");

  useEffect(() => {
    let active = true;

    void container
      .resolve("getPreferredThemeModeUseCase")
      .execute()
      .then((result) => {
        const stored = themeModes.find((candidate) => result.ok && candidate === result.value);
        if (active && stored) {
          setMode(stored);
        }
      });

    return () => {
      active = false;
    };
  }, [container]);

  const toggleMode = useCallback(() => {
    const next: ThemeMode = mode === "light" ? "dark" : "light";
    setMode(next);
    // A failed write only means the next load starts light again.
    void container.resolve("setPreferredThemeModeUseCase").execute(next);
  }, [container, mode]);

  const theme = useMemo(() => buildTheme(mode), [mode]);
  const value = useMemo(() => ({ mode, toggleMode }), [mode, toggleMode]);

  return (
    <ThemeModeContext value={value}>
      <ThemeProvider theme={theme}>
        <CssBaseline />
        {children}
      </ThemeProvider>
    </ThemeModeContext>
  );
}
