"use client";

import { useEffect, type ReactNode } from "react";
import Box from "@mui/material/Box";
import { usePathname, useRouter } from "next/navigation";
import RequireSession from "@/presentation/auth/components/require-session";
import useCurrentUser from "../hooks/use-current-user";
import AppHeader from "./app-header";
import NavigationDrawer from "./navigation-drawer";

interface AppShellProps {
  children: ReactNode;
}

/** Reads the profile once the session is confirmed, and keeps a user in onboarding on the home screen. */
function SignedInContent({ children }: AppShellProps) {
  const router = useRouter();
  const pathname = usePathname();
  const { load, onboardingPending } = useCurrentUser();
  const redirecting = onboardingPending && pathname !== "/";

  useEffect(() => {
    load();
  }, [load]);

  useEffect(() => {
    if (redirecting) {
      router.replace("/");
    }
  }, [redirecting, router]);

  return (
    <Box sx={{ display: "flex", minHeight: "100vh" }}>
      <NavigationDrawer />
      <Box sx={{ display: "flex", flexDirection: "column", flexGrow: 1, minWidth: 0 }}>
        <AppHeader />
        <Box component="main" sx={{ flexGrow: 1 }}>
          {redirecting ? null : children}
        </Box>
      </Box>
    </Box>
  );
}

/** Layout of every signed-in view: the session guard, the full-height drawer, and the header above the content. */
export default function AppShell({ children }: AppShellProps) {
  return (
    <RequireSession>
      <SignedInContent>{children}</SignedInContent>
    </RequireSession>
  );
}
