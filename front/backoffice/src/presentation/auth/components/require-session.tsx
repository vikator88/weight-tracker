"use client";

import { type ReactNode, useEffect } from "react";
import Box from "@mui/material/Box";
import CircularProgress from "@mui/material/CircularProgress";
import { useRouter } from "next/navigation";
import useSession from "../hooks/use-session";

interface RequireSessionProps {
  children: ReactNode;
}

/** Renders its children only while a session is stored; otherwise, or once it ends, sends the visitor to login. */
export default function RequireSession({ children }: RequireSessionProps) {
  const { status } = useSession();
  const router = useRouter();

  useEffect(() => {
    if (status === "signedOut") {
      router.replace("/login");
    }
  }, [status, router]);

  if (status !== "signedIn") {
    return (
      <Box sx={{ display: "flex", justifyContent: "center", py: 8 }}>
        <CircularProgress aria-label="loading" />
      </Box>
    );
  }

  return children;
}
