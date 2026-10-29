import { createContext } from "react";

export const themeModes = ["light", "dark"] as const;

export type ThemeMode = (typeof themeModes)[number];

export interface ThemeModeContextValue {
  mode: ThemeMode;
  /** Flips between light and dark and remembers the choice on this device. */
  toggleMode: () => void;
}

/** Light, with no switching, outside `ThemeModeProvider`. */
const ThemeModeContext = createContext<ThemeModeContextValue>({
  mode: "light",
  toggleMode: () => {},
});

export default ThemeModeContext;
