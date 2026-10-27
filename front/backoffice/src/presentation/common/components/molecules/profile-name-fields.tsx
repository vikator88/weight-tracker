"use client";

import TextField from "@/presentation/common/components/atoms/text-field";
import {
  PROFILE_NAME_MAX_LENGTH,
  PROFILE_NAME_MIN_LENGTH,
  type ProfileNameViolation,
} from "@weight-tracker/core/application";
import { useTranslation } from "react-i18next";

export interface ProfileNameValues {
  name: string;
  surname: string;
}

export type ProfileNameErrors = Partial<Record<keyof ProfileNameValues, ProfileNameViolation>>;

interface ProfileNameFieldsProps {
  values: ProfileNameValues;
  errors: ProfileNameErrors;
  disabled?: boolean;
  onChange: (field: keyof ProfileNameValues, value: string) => void;
}

/** Nombre and Apellidos, with the shared length rules shown as translated messages under each field. */
export default function ProfileNameFields({
  values,
  errors,
  disabled = false,
  onChange,
}: ProfileNameFieldsProps) {
  const { t } = useTranslation("profile");

  function messageOf(violation: ProfileNameViolation | undefined) {
    return violation
      ? t(`validation.${violation}`, { min: PROFILE_NAME_MIN_LENGTH, max: PROFILE_NAME_MAX_LENGTH })
      : undefined;
  }

  return (
    <>
      <TextField
        label={t("fields.name")}
        autoComplete="given-name"
        value={values.name}
        disabled={disabled}
        onChange={(event) => onChange("name", event.target.value)}
        error={Boolean(errors.name)}
        helperText={messageOf(errors.name)}
      />
      <TextField
        label={t("fields.surname")}
        autoComplete="family-name"
        value={values.surname}
        disabled={disabled}
        onChange={(event) => onChange("surname", event.target.value)}
        error={Boolean(errors.surname)}
        helperText={messageOf(errors.surname)}
      />
    </>
  );
}
