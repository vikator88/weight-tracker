"use client";

import Container from "@mui/material/Container";
import Typography from "@mui/material/Typography";
import { useTranslation } from "react-i18next";
import OnboardingDialog from "@/presentation/onboarding/components/onboarding-dialog";
import AppShell from "../components/app-shell";
import HomeSkeleton from "../components/home-skeleton";
import LoadError from "../components/load-error";
import useCurrentUser from "../hooks/use-current-user";

function HomeContent() {
  const { t } = useTranslation("common");
  const { status, errorCode, onboardingPending, reload } = useCurrentUser();

  if (status === "failed" && errorCode) {
    return (
      <>
        <LoadError code={errorCode} onRetry={reload} />
        <HomeSkeleton />
      </>
    );
  }
  if (status !== "loaded") {
    return <HomeSkeleton />;
  }
  if (onboardingPending) {
    return (
      <>
        <HomeSkeleton />
        <OnboardingDialog />
      </>
    );
  }

  return (
    <Container sx={{ py: 4 }}>
      <Typography variant="h3" component="h1">
        {t("home.title")}
      </Typography>
      <Typography>{t("home.signedIn")}</Typography>
    </Container>
  );
}

export default function HomeView() {
  return (
    <AppShell>
      <HomeContent />
    </AppShell>
  );
}
