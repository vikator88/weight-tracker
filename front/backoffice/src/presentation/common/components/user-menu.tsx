"use client";

import { useState } from "react";
import ExpandMoreOutlined from "@mui/icons-material/ExpandMoreOutlined";
import LogoutOutlined from "@mui/icons-material/LogoutOutlined";
import PersonOutlined from "@mui/icons-material/PersonOutlined";
import SettingsOutlined from "@mui/icons-material/SettingsOutlined";
import Alert from "@mui/material/Alert";
import Avatar from "@mui/material/Avatar";
import Box from "@mui/material/Box";
import Divider from "@mui/material/Divider";
import IconButton from "@mui/material/IconButton";
import ListItemIcon from "@mui/material/ListItemIcon";
import ListItemText from "@mui/material/ListItemText";
import Menu from "@mui/material/Menu";
import MenuItem from "@mui/material/MenuItem";
import Skeleton from "@mui/material/Skeleton";
import Typography from "@mui/material/Typography";
import { useRouter } from "next/navigation";
import { useTranslation } from "react-i18next";
import useCurrentUser from "../hooks/use-current-user";
import useLogout from "../hooks/use-logout";

/** First letter of the name and of the surname: "Ana" "García" → "AG". */
function initials(name: string, surname: string) {
  return [name, surname].map((word) => word.trim().charAt(0).toUpperCase()).join("");
}

export default function UserMenu() {
  const { t } = useTranslation("common");
  const router = useRouter();
  const { profile, status, onboardingPending, clear } = useCurrentUser();
  const { loggingOut, errorCode, logout } = useLogout();
  const showUser = status === "loaded" && profile !== null && !onboardingPending;
  const [anchor, setAnchor] = useState<HTMLElement | null>(null);

  async function handleLogout() {
    if (await logout()) {
      clear();
      router.replace("/login");
    }
  }

  return (
    <Box sx={{ display: "flex", alignItems: "center", gap: 1.5 }}>
      {showUser ? (
        <>
          <Box sx={{ textAlign: "right" }}>
            <Typography variant="body2">{`${profile.name} ${profile.surname}`}</Typography>
            <Typography variant="caption" color="text.secondary">
              {t(`header.user.roles.${profile.role}`)}
            </Typography>
          </Box>
          <Avatar>{initials(profile.name, profile.surname)}</Avatar>
        </>
      ) : (
        <>
          <Box sx={{ textAlign: "right" }} aria-label={t("loading")}>
            <Skeleton variant="text" width={120} />
            <Skeleton variant="text" width={72} sx={{ ml: "auto" }} />
          </Box>
          <Skeleton variant="circular" width={40} height={40} />
        </>
      )}
      <IconButton
        color="inherit"
        aria-label={t("header.user.menuLabel")}
        aria-haspopup="true"
        aria-expanded={anchor !== null}
        onClick={(event) => setAnchor(event.currentTarget)}
      >
        <ExpandMoreOutlined />
      </IconButton>
      <Menu anchorEl={anchor} open={anchor !== null} onClose={() => setAnchor(null)}>
        <MenuItem
          onClick={() => {
            setAnchor(null);
            router.push("/profile");
          }}
        >
          <ListItemIcon>
            <PersonOutlined />
          </ListItemIcon>
          <ListItemText>{t("header.user.profile")}</ListItemText>
        </MenuItem>
        <MenuItem onClick={() => setAnchor(null)}>
          <ListItemIcon>
            <SettingsOutlined />
          </ListItemIcon>
          <ListItemText>{t("header.user.preferences")}</ListItemText>
        </MenuItem>
        <Divider />
        <MenuItem disabled={loggingOut} onClick={() => void handleLogout()}>
          <ListItemIcon>
            <LogoutOutlined />
          </ListItemIcon>
          <ListItemText>{t("header.user.logout")}</ListItemText>
        </MenuItem>
        {errorCode && (
          <Alert severity="error" sx={{ mx: 2, mb: 1 }}>
            {t(`errors:${errorCode}`)}
          </Alert>
        )}
      </Menu>
    </Box>
  );
}
