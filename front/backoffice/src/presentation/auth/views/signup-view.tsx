"use client";

import { useState } from "react";
import Container from "@mui/material/Container";
import Link from "@mui/material/Link";
import Typography from "@mui/material/Typography";
import NextLink from "next/link";
import { useRouter } from "next/navigation";
import { useTranslation } from "react-i18next";
import type { DomainErrorCode } from "@weight-tracker/core/domain";
import useContainer from "@/presentation/common/hooks/use-container";
import SignupForm from "../components/signup-form";

const signupErrorCodes: DomainErrorCode[] = ["VALIDATION", "UNAUTHORIZED", "CONFLICT"];

export default function SignupView() {
  const { t } = useTranslation("auth");
  const container = useContainer();
  const router = useRouter();
  const [submitting, setSubmitting] = useState(false);
  const [errorCode, setErrorCode] = useState<DomainErrorCode | null>(null);

  async function handleSubmit(credentials: { email: string; password: string }) {
    setSubmitting(true);
    setErrorCode(null);

    const result = await container.resolve("signupUseCase").execute(credentials);

    if (result.ok) {
      router.replace("/");
      return;
    }
    setErrorCode(result.error.code);
    setSubmitting(false);
  }

  const errorMessage =
    errorCode === null
      ? undefined
      : signupErrorCodes.includes(errorCode)
        ? t(`signup.errors.${errorCode}`)
        : t(`errors:${errorCode}`);

  return (
    <Container component="main" maxWidth="xs" sx={{ py: 4 }}>
      <Typography variant="h4" component="h1" sx={{ mb: 3 }}>
        {t("signup.title")}
      </Typography>
      <SignupForm submitting={submitting} errorMessage={errorMessage} onSubmit={handleSubmit} />
      <Link component={NextLink} href="/login" sx={{ display: "inline-block", mt: 2 }}>
        {t("signup.toLogin")}
      </Link>
    </Container>
  );
}
