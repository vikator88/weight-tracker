"use client";

import Container from "@mui/material/Container";
import Typography from "@mui/material/Typography";
import { useTranslation } from "react-i18next";
import RequireSession from "@/presentation/auth/components/require-session";

export default function HomeView() {
  const { t } = useTranslation("common");

  return (
    <RequireSession>
      <Container component="main" sx={{ py: 4 }}>
        <Typography variant="h3" component="h1">
          {t("home.title")}
        </Typography>
        <Typography>{t("home.signedIn")}</Typography>
      </Container>
    </RequireSession>
  );
}
