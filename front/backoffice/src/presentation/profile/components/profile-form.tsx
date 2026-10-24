"use client";

import { type FormEvent, useState } from "react";
import Alert from "@mui/material/Alert";
import Box from "@mui/material/Box";
import Button from "@mui/material/Button";
import { validateUpdateProfileRequest } from "@weight-tracker/core/application";
import type { UserProfile } from "@weight-tracker/core/domain";
import { useTranslation } from "react-i18next";
import useUpdateProfile from "../hooks/use-update-profile";
import ProfileNameFields, {
  type ProfileNameErrors,
  type ProfileNameValues,
} from "./profile-name-fields";

interface ProfileFormProps {
  profile: UserProfile;
}

export default function ProfileForm({ profile }: ProfileFormProps) {
  const { t } = useTranslation("profile");
  const { t: tc } = useTranslation();
  const { saving, errorCode, saved, save, clearSaved } = useUpdateProfile();
  const [values, setValues] = useState<ProfileNameValues>({
    name: profile.name,
    surname: profile.surname,
  });
  const [errors, setErrors] = useState<ProfileNameErrors>({});

  function handleChange(field: keyof ProfileNameValues, value: string) {
    setValues((current) => ({ ...current, [field]: value }));
    clearSaved();
  }

  async function handleSubmit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();

    const violations = validateUpdateProfileRequest(values);
    const next: ProfileNameErrors = {};
    if (violations.name) {
      next.name = violations.name;
    }
    if (violations.surname) {
      next.surname = violations.surname;
    }
    setErrors(next);

    if (!next.name && !next.surname) {
      await save({ name: values.name.trim(), surname: values.surname.trim() });
    }
  }

  return (
    <Box
      component="form"
      noValidate
      onSubmit={(event) => void handleSubmit(event)}
      sx={{ display: "flex", flexDirection: "column", gap: 2, maxWidth: 480 }}
    >
      {errorCode && <Alert severity="error">{tc(`errors:${errorCode}`)}</Alert>}
      {saved && <Alert severity="success">{t("saved")}</Alert>}
      <ProfileNameFields values={values} errors={errors} onChange={handleChange} />
      <Button type="submit" variant="contained" disabled={saving}>
        {t("actions.save")}
      </Button>
    </Box>
  );
}
