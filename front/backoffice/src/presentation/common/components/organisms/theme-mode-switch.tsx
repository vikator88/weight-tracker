"use client";

import DarkModeOutlined from "@mui/icons-material/DarkModeOutlined";
import LightModeOutlined from "@mui/icons-material/LightModeOutlined";
import Box from "@mui/material/Box";
import Switch from "@/presentation/common/components/atoms/switch";
import { useTranslation } from "react-i18next";
import useThemeMode from "@/presentation/common/hooks/use-theme-mode";

/** Top-bar switch between the light and dark theme. */
export default function ThemeModeSwitch() {
  const { t } = useTranslation("common");
  const { mode, toggleMode } = useThemeMode();
  const dark = mode === "dark";

  return (
    <Box sx={{ display: "flex", alignItems: "center", color: "text.secondary" }}>
      {dark ? <DarkModeOutlined fontSize="small" /> : <LightModeOutlined fontSize="small" />}
      <Switch
        checked={dark}
        onChange={toggleMode}
        slotProps={{ input: { "aria-label": t("header.themeMode.label") } }}
      />
    </Box>
  );
}
