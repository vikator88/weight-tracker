"use client";

import Container from "@mui/material/Container";
import Typography from "@/presentation/common/components/atoms/typography";
import { useTranslation } from "react-i18next";
import OnboardingDialog from "@/presentation/onboarding/components/organisms/onboarding-dialog";
import AppShell from "@/presentation/common/components/organisms/app-shell";
import HomeSkeleton from "@/presentation/common/components/molecules/home-skeleton";
import LoadError from "@/presentation/common/components/molecules/load-error";
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
