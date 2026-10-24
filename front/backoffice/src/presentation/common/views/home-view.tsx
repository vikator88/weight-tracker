"use client";

import { useState } from "react";
import Alert from "@mui/material/Alert";
import Button from "@mui/material/Button";
import Container from "@mui/material/Container";
import Typography from "@mui/material/Typography";
import { useRouter } from "next/navigation";
import { useTranslation } from "react-i18next";
import type { DomainErrorCode } from "@weight-tracker/core/domain";
import RequireSession from "@/presentation/auth/components/require-session";
import useContainer from "@/presentation/common/hooks/use-container";

export default function HomeView() {
  const { t } = useTranslation("common");
  const container = useContainer();
  const router = useRouter();
  const [loggingOut, setLoggingOut] = useState(false);
  const [errorCode, setErrorCode] = useState<DomainErrorCode | null>(null);

  // Temporary: a quick way to end the session while developing. Moves to a proper place later.
  async function handleLogout() {
    setLoggingOut(true);
    setErrorCode(null);

    const result = await container.resolve("logoutUseCase").execute();

    if (result.ok) {
      router.replace("/login");
      return;
    }
    setErrorCode(result.error.code);
    setLoggingOut(false);
  }

  return (
    <RequireSession>
      <Container component="main" sx={{ py: 4 }}>
        <Typography variant="h3" component="h1">
          {t("home.title")}
        </Typography>
        <Typography>{t("home.signedIn")}</Typography>
        {errorCode && (
          <Alert severity="error" sx={{ mt: 2 }}>
            {t(`errors:${errorCode}`)}
          </Alert>
        )}
        <Button
          variant="outlined"
          sx={{ mt: 2 }}
          disabled={loggingOut}
          onClick={() => void handleLogout()}
        >
          {t("home.logout")}
        </Button>
      </Container>
    </RequireSession>
  );
}
