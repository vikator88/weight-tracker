"use client";

import Container from "@mui/material/Container";
import Typography from "@/presentation/common/components/atoms/typography";
import { useTranslation } from "react-i18next";
import HomeSkeleton from "@/presentation/common/components/molecules/home-skeleton";
import LoadError from "@/presentation/common/components/molecules/load-error";
import AppShell from "@/presentation/common/components/organisms/app-shell";
import useCurrentUser from "@/presentation/common/hooks/use-current-user";
import ProfileForm from "@/presentation/profile/components/organisms/profile-form";

function ProfileContent() {
  const { t } = useTranslation("profile");
  const { status, profile, errorCode, reload } = useCurrentUser();

  if (status === "failed" && errorCode) {
    return <LoadError code={errorCode} onRetry={reload} />;
  }
  if (status !== "loaded" || profile === null) {
    return <HomeSkeleton />;
  }

  return (
    <Container sx={{ py: 4 }}>
      <Typography variant="h4" component="h1" sx={{ mb: 3 }}>
        {t("title")}
      </Typography>
      <ProfileForm profile={profile} />
    </Container>
  );
}

export default function ProfileView() {
  return (
    <AppShell>
      <ProfileContent />
    </AppShell>
  );
}
