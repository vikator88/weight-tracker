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
import useSession from "../hooks/use-session";
import LoginForm from "../components/login-form";

const loginErrorCodes: DomainErrorCode[] = ["UNAUTHORIZED"];

export default function LoginView() {
  const { t } = useTranslation("auth");
  const container = useContainer();
  const { reload } = useSession();
  const router = useRouter();
  const [submitting, setSubmitting] = useState(false);
  const [errorCode, setErrorCode] = useState<DomainErrorCode | null>(null);

  async function handleSubmit(credentials: { email: string; password: string }) {
    setSubmitting(true);
    setErrorCode(null);

    const result = await container.resolve("loginUseCase").execute(credentials);

    if (result.ok) {
      // The guard reads the session state, so it must know about the new tokens before home opens.
      await reload();
      router.replace("/");
      return;
    }
    setErrorCode(result.error.code);
    setSubmitting(false);
  }

  const errorMessage =
    errorCode === null
      ? undefined
      : loginErrorCodes.includes(errorCode)
        ? t(`login.errors.${errorCode}`)
        : t(`errors:${errorCode}`);

  return (
    <Container component="main" maxWidth="xs" sx={{ py: 4 }}>
      <Typography variant="h4" component="h1" sx={{ mb: 3 }}>
        {t("login.title")}
      </Typography>
      <LoginForm submitting={submitting} errorMessage={errorMessage} onSubmit={handleSubmit} />
      <Link component={NextLink} href="/signup" sx={{ display: "inline-block", mt: 2 }}>
        {t("login.toSignup")}
      </Link>
    </Container>
  );
}
