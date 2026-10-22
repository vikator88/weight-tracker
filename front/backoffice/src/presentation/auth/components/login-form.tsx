"use client";

import { type FormEvent, useState } from "react";
import Alert from "@mui/material/Alert";
import Box from "@mui/material/Box";
import Button from "@mui/material/Button";
import TextField from "@mui/material/TextField";
import { useTranslation } from "react-i18next";

const emailPattern = /^[^\s@]+@[^\s@]+\.[^\s@]+$/;

interface LoginFormProps {
  submitting: boolean;
  errorMessage?: string;
  onSubmit: (credentials: { email: string; password: string }) => void;
}

interface FieldErrors {
  email?: string;
  password?: string;
}

export default function LoginForm({ submitting, errorMessage, onSubmit }: LoginFormProps) {
  const { t } = useTranslation("auth");
  const [email, setEmail] = useState("");
  const [password, setPassword] = useState("");
  const [errors, setErrors] = useState<FieldErrors>({});

  function handleSubmit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();

    const next: FieldErrors = {};
    if (email === "") {
      next.email = t("login.validation.emailRequired");
    } else if (!emailPattern.test(email)) {
      next.email = t("login.validation.emailFormat");
    }
    if (password === "") {
      next.password = t("login.validation.passwordRequired");
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
        label={t("login.email")}
        type="email"
        autoComplete="email"
        value={email}
        onChange={(event) => setEmail(event.target.value)}
        error={Boolean(errors.email)}
        helperText={errors.email}
      />
      <TextField
        label={t("login.password")}
        type="password"
        autoComplete="current-password"
        value={password}
        onChange={(event) => setPassword(event.target.value)}
        error={Boolean(errors.password)}
        helperText={errors.password}
      />
      <Button type="submit" variant="contained" disabled={submitting}>
        {t("login.submit")}
      </Button>
    </Box>
  );
}
