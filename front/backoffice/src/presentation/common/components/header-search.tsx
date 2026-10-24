"use client";

import SearchOutlined from "@mui/icons-material/SearchOutlined";
import InputAdornment from "@mui/material/InputAdornment";
import TextField from "@mui/material/TextField";
import { useTranslation } from "react-i18next";

/** Search box placeholder: it takes text but triggers nothing until search exists. */
export default function HeaderSearch() {
  const { t } = useTranslation("common");

  return (
    <TextField
      size="small"
      placeholder={t("header.search.placeholder")}
      sx={{ width: "50%" }}
      slotProps={{
        htmlInput: { "aria-label": t("header.search.label") },
        input: {
          startAdornment: (
            <InputAdornment position="start">
              <SearchOutlined />
            </InputAdornment>
          ),
        },
      }}
    />
  );
}
