"use client";

import { useCallback, useMemo, useState, type ReactNode } from "react";
import NavigationDrawerContext from "../context/navigation-drawer-context";

interface NavigationDrawerProviderProps {
  children: ReactNode;
}

/**
 * Holds whether the drawer is expanded above the views, since every view renders its own shell and the drawer would
 * otherwise expand again on each navigation.
 */
export default function NavigationDrawerProvider({ children }: NavigationDrawerProviderProps) {
  const [expanded, setExpanded] = useState(true);
  const toggleExpanded = useCallback(() => setExpanded((current) => !current), []);
  const value = useMemo(() => ({ expanded, toggleExpanded }), [expanded, toggleExpanded]);

  return <NavigationDrawerContext value={value}>{children}</NavigationDrawerContext>;
}
