"use client";

import AppBar from "@mui/material/AppBar";
import Box from "@mui/material/Box";
import Toolbar from "@mui/material/Toolbar";
import AlertsMenu from "./alerts-menu";
import HeaderSearch from "./header-search";
import LanguageMenu from "./language-menu";
import UserMenu from "./user-menu";

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
        <LanguageMenu />
        <AlertsMenu />
        <UserMenu />
      </Toolbar>
    </AppBar>
  );
}
