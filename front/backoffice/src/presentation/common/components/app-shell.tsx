"use client";

import type { ReactNode } from "react";
import Box from "@mui/material/Box";
import RequireSession from "@/presentation/auth/components/require-session";
import AppHeader from "./app-header";
import NavigationDrawer from "./navigation-drawer";

interface AppShellProps {
  children: ReactNode;
}

/** Layout of every signed-in view: the session guard, the full-height drawer, and the header above the content. */
export default function AppShell({ children }: AppShellProps) {
  return (
    <RequireSession>
      <Box sx={{ display: "flex", minHeight: "100vh" }}>
        <NavigationDrawer />
        <Box sx={{ display: "flex", flexDirection: "column", flexGrow: 1, minWidth: 0 }}>
          <AppHeader />
          <Box component="main" sx={{ flexGrow: 1 }}>
            {children}
          </Box>
        </Box>
      </Box>
    </RequireSession>
  );
}
