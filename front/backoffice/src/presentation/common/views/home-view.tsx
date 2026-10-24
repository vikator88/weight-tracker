"use client";

import Container from "@mui/material/Container";
import Typography from "@mui/material/Typography";
import { useTranslation } from "react-i18next";
import AppShell from "../components/app-shell";

export default function HomeView() {
  const { t } = useTranslation("common");

  return (
    <AppShell>
      <Container sx={{ py: 4 }}>
        <Typography variant="h3" component="h1">
          {t("home.title")}
        </Typography>
        <Typography>{t("home.signedIn")}</Typography>
      </Container>
    </AppShell>
  );
}
