"use client";

import { type FormEvent, useState } from "react";
import Box from "@mui/material/Box";
import {
  type CreateUserFormValues,
  type CreateUserFormViolations,
  validateCreateUserForm,
} from "@weight-tracker/core/application";
import type { CreateUserRequest } from "@weight-tracker/core/domain";
import { useTranslation } from "react-i18next";
import Alert from "@/presentation/common/components/atoms/alert";
import Button from "@/presentation/common/components/atoms/button";
import Dialog from "@/presentation/common/components/atoms/dialog";
import DialogActions from "@/presentation/common/components/atoms/dialog-actions";
import DialogContent from "@/presentation/common/components/atoms/dialog-content";
import DialogTitle from "@/presentation/common/components/atoms/dialog-title";
import MenuItem from "@/presentation/common/components/atoms/menu-item";
import TextField from "@/presentation/common/components/atoms/text-field";
import ProfileNameFields, {
  type ProfileNameErrors,
} from "@/presentation/common/components/molecules/profile-name-fields";
import useCreateUser from "@/presentation/users/hooks/use-create-user";
import { userRoles } from "@/presentation/users/roles";

interface CreateUserDialogProps {
  open: boolean;
  onClose: () => void;
  /** Called once the API has created the user, before the dialog closes. */
  onCreated: () => void;
}

const emptyValues: CreateUserFormValues = {
  email: "",
  password: "",
  role: String(userRoles[0].value),
  name: "",
  surname: "",
  dateBirth: "",
};

/** Builds the request from valid values: names trimmed, and without the optional keys left empty. */
function toRequest(values: CreateUserFormValues): CreateUserRequest {
  const request: CreateUserRequest = {
    email: values.email.trim(),
    password: values.password,
    role: Number(values.role) as CreateUserRequest["role"],
  };
  const name = values.name.trim();
  const surname = values.surname.trim();

  if (name !== "") {
    request.name = name;
  }
  if (surname !== "") {
    request.surname = surname;
  }
  if (values.dateBirth !== "") {
    request.dateBirth = values.dateBirth;
  }
  return request;
}

/**
 * The form lives inside the dialog, which unmounts it on close: every opening starts empty, on the first role and
 * without errors.
 */
function CreateUserForm({ onClose, onCreated }: Omit<CreateUserDialogProps, "open">) {
  const { t } = useTranslation("users");
  const { t: tc } = useTranslation();
  const { creating, errorCode, create } = useCreateUser();
  const [values, setValues] = useState<CreateUserFormValues>(emptyValues);
  const [errors, setErrors] = useState<CreateUserFormViolations>({});

  async function handleSubmit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();

    const violations = validateCreateUserForm(values);
    setErrors(violations);
    if (Object.keys(violations).length > 0) {
      return;
    }

    const user = await create(toRequest(values));
    if (user) {
      onCreated();
      onClose();
    }
  }

  function change(name: keyof CreateUserFormValues, value: string) {
    setValues((current) => ({ ...current, [name]: value }));
  }

  function field(name: "email" | "password" | "role" | "dateBirth") {
    const violation = errors[name];

    return {
      label: t(`create.fields.${name}`),
      value: values[name],
      disabled: creating,
      onChange: (event: { target: { value: string } }) => change(name, event.target.value),
      error: Boolean(violation),
      helperText: violation ? t(`create.validation.${violation}`) : undefined,
    };
  }

  const nameErrors = { name: errors.name, surname: errors.surname } as ProfileNameErrors;

  return (
    <Box component="form" noValidate onSubmit={(event) => void handleSubmit(event)}>
      <DialogTitle id="create-user-title">{t("create.title")}</DialogTitle>
      <DialogContent>
        <Box sx={{ display: "flex", flexDirection: "column", gap: 2, pt: 1 }}>
          {errorCode && (
            <Alert severity="error">
              {t(`create.errors.${errorCode}`, { defaultValue: tc(`errors:${errorCode}`) })}
            </Alert>
          )}
          <TextField {...field("email")} type="email" autoComplete="off" autoFocus />
          <TextField {...field("password")} type="password" autoComplete="new-password" />
          <TextField {...field("role")} select>
            {userRoles.map(({ code, value }) => (
              <MenuItem key={code} value={String(value)}>
                {t(`roles.${code}`)}
              </MenuItem>
            ))}
          </TextField>
          <ProfileNameFields
            values={{ name: values.name, surname: values.surname }}
            errors={nameErrors}
            disabled={creating}
            onChange={change}
          />
          <TextField
            {...field("dateBirth")}
            type="date"
            slotProps={{ inputLabel: { shrink: true } }}
          />
        </Box>
      </DialogContent>
      <DialogActions>
        <Button onClick={onClose} disabled={creating}>
          {t("create.cancel")}
        </Button>
        <Button type="submit" variant="contained" disabled={creating}>
          {t("create.submit")}
        </Button>
      </DialogActions>
    </Box>
  );
}

/** Creates a user of any role. Escape, a click outside and Cancelar close it without a request. */
export default function CreateUserDialog({ open, onClose, onCreated }: CreateUserDialogProps) {
  return (
    <Dialog
      open={open}
      onClose={onClose}
      fullWidth
      maxWidth="sm"
      aria-labelledby="create-user-title"
    >
      <CreateUserForm onClose={onClose} onCreated={onCreated} />
    </Dialog>
  );
}
