import { use } from "react";
import ThemeModeContext, { type ThemeModeContextValue } from "../context/theme-mode-context";

export default function useThemeMode(): ThemeModeContextValue {
  return use(ThemeModeContext);
}
