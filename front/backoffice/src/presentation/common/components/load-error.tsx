"use client";

import Alert from "@mui/material/Alert";
import Button from "@mui/material/Button";
import type { DomainErrorCode } from "@weight-tracker/core/domain";
import { useTranslation } from "react-i18next";

interface LoadErrorProps {
  code: DomainErrorCode;
  onRetry: () => void;
}

/** A failed load, with the way to try again. */
export default function LoadError({ code, onRetry }: LoadErrorProps) {
  const { t } = useTranslation("common");

  return (
    <Alert
      severity="error"
      sx={{ mx: 3, mt: 2 }}
      action={
        <Button color="inherit" size="small" onClick={onRetry}>
          {t("loadError.retry")}
        </Button>
      }
    >
      {t(`errors:${code}`)}
    </Alert>
  );
}
