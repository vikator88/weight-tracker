import { createContext } from "react";

export interface NavigationDrawerContextValue {
  /** Whether the drawer shows its labels; collapsed, it is a rail of icons. */
  expanded: boolean;
  toggleExpanded: () => void;
}

/** Expanded, with no collapsing, outside `NavigationDrawerProvider`. */
const NavigationDrawerContext = createContext<NavigationDrawerContextValue>({
  expanded: true,
  toggleExpanded: () => {},
});

export default NavigationDrawerContext;
