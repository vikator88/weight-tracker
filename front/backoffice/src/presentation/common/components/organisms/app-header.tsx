"use client";

import AppBar from "@mui/material/AppBar";
import Box from "@mui/material/Box";
import Toolbar from "@/presentation/common/components/atoms/toolbar";
import AlertsMenu from "@/presentation/common/components/organisms/alerts-menu";
import HeaderSearch from "@/presentation/common/components/molecules/header-search";
import LanguageMenu from "@/presentation/common/components/organisms/language-menu";
import ThemeModeSwitch from "@/presentation/common/components/organisms/theme-mode-switch";
import UserMenu from "@/presentation/common/components/organisms/user-menu";

/** Top bar of every signed-in view, beside the navigation drawer. */
export default function AppHeader() {
  return (
    <AppBar
      position="sticky"
      color="inherit"
      elevation={0}
      sx={{ borderBottom: 1, borderColor: "divider" }}
    >
      <Toolbar sx={{ gap: 2 }}>
        <HeaderSearch />
        <Box sx={{ flexGrow: 1 }} />
        <ThemeModeSwitch />
        <LanguageMenu />
        <AlertsMenu />
        <UserMenu />
      </Toolbar>
    </AppBar>
  );
}
