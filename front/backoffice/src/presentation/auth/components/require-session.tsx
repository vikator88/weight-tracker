"use client";

import { type ReactNode, useEffect, useState } from "react";
import Box from "@mui/material/Box";
import CircularProgress from "@mui/material/CircularProgress";
import { useRouter } from "next/navigation";
import useContainer from "@/presentation/common/hooks/use-container";

interface RequireSessionProps {
  children: ReactNode;
}

/** Renders its children only when a session is stored; otherwise sends the visitor to login. */
export default function RequireSession({ children }: RequireSessionProps) {
  const container = useContainer();
  const router = useRouter();
  const [authorized, setAuthorized] = useState(false);

  useEffect(() => {
    let active = true;

    void container
      .resolve("getSessionUseCase")
      .execute()
      .then((result) => {
        if (!active) {
          return;
        }
        if (result.ok && result.value !== null) {
          setAuthorized(true);
        } else {
          router.replace("/login");
        }
      });

    return () => {
      active = false;
    };
  }, [container, router]);

  if (!authorized) {
    return (
      <Box sx={{ display: "flex", justifyContent: "center", py: 8 }}>
        <CircularProgress aria-label="loading" />
      </Box>
    );
  }

  return children;
}
