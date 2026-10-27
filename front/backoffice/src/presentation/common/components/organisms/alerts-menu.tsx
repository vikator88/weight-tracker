"use client";

import { useState } from "react";
import NotificationsOutlined from "@mui/icons-material/NotificationsOutlined";
import Badge from "@mui/material/Badge";
import IconButton from "@/presentation/common/components/atoms/icon-button";
import ListSubheader from "@/presentation/common/components/atoms/list-subheader";
import Menu from "@/presentation/common/components/atoms/menu";
import MenuItem from "@/presentation/common/components/atoms/menu-item";
import { useTranslation } from "react-i18next";
import useAlerts from "@/presentation/common/hooks/use-alerts";

export default function AlertsMenu() {
  const { t } = useTranslation("common");
  const alerts = useAlerts();
  const [anchor, setAnchor] = useState<HTMLElement | null>(null);

  return (
    <>
      <IconButton
        color="inherit"
        aria-label={t("header.alerts.label")}
        aria-haspopup="true"
        aria-expanded={anchor !== null}
        onClick={(event) => setAnchor(event.currentTarget)}
      >
        <Badge badgeContent={alerts.length} color="error">
          <NotificationsOutlined />
        </Badge>
      </IconButton>
      <Menu anchorEl={anchor} open={anchor !== null} onClose={() => setAnchor(null)}>
        <ListSubheader>{t("header.alerts.title")}</ListSubheader>
        {alerts.map((alert) => (
          <MenuItem key={alert} onClick={() => setAnchor(null)}>
            {t(alert)}
          </MenuItem>
        ))}
      </Menu>
    </>
  );
}
