"use client";

import { type FormEvent, useState } from "react";
import Alert from "@mui/material/Alert";
import Box from "@mui/material/Box";
import Button from "@mui/material/Button";
import TextField from "@mui/material/TextField";
import { useTranslation } from "react-i18next";

const emailPattern = /^[^\s@]+@[^\s@]+\.[^\s@]+$/;

interface SignupFormProps {
  submitting: boolean;
  errorMessage?: string;
  onSubmit: (credentials: { email: string; password: string }) => void;
}

interface FieldErrors {
  email?: string;
  password?: string;
}

export default function SignupForm({ submitting, errorMessage, onSubmit }: SignupFormProps) {
  const { t } = useTranslation("auth");
  const [email, setEmail] = useState("");
  const [password, setPassword] = useState("");
  const [errors, setErrors] = useState<FieldErrors>({});

  function handleSubmit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();

    const next: FieldErrors = {};
    if (email === "") {
      next.email = t("signup.validation.emailRequired");
    } else if (!emailPattern.test(email)) {
      next.email = t("signup.validation.emailFormat");
    }
    if (password === "") {
      next.password = t("signup.validation.passwordRequired");
    }
    setErrors(next);

    if (!next.email && !next.password) {
      onSubmit({ email, password });
    }
  }

  return (
    <Box
      component="form"
      noValidate
      onSubmit={handleSubmit}
      sx={{ display: "flex", flexDirection: "column", gap: 2 }}
    >
      {errorMessage && <Alert severity="error">{errorMessage}</Alert>}
      <TextField
        label={t("signup.email")}
        type="email"
        autoComplete="email"
        value={email}
        onChange={(event) => setEmail(event.target.value)}
        error={Boolean(errors.email)}
        helperText={errors.email}
      />
      <TextField
        label={t("signup.password")}
        type="password"
        autoComplete="new-password"
        value={password}
        onChange={(event) => setPassword(event.target.value)}
        error={Boolean(errors.password)}
        helperText={errors.password}
      />
      <Button type="submit" variant="contained" disabled={submitting}>
        {t("signup.submit")}
      </Button>
    </Box>
  );
}
