"use client";

import type { ComponentType } from "react";
import AssignmentOutlined from "@mui/icons-material/AssignmentOutlined";
import CalendarMonthOutlined from "@mui/icons-material/CalendarMonthOutlined";
import DescriptionOutlined from "@mui/icons-material/DescriptionOutlined";
import EventNoteOutlined from "@mui/icons-material/EventNoteOutlined";
import FitnessCenterOutlined from "@mui/icons-material/FitnessCenterOutlined";
import GroupOutlined from "@mui/icons-material/GroupOutlined";
import HomeOutlined from "@mui/icons-material/HomeOutlined";
import PendingActionsOutlined from "@mui/icons-material/PendingActionsOutlined";
import PersonAddAltOutlined from "@mui/icons-material/PersonAddAltOutlined";
import RateReviewOutlined from "@mui/icons-material/RateReviewOutlined";
import Box from "@mui/material/Box";
import Drawer from "@mui/material/Drawer";
import List from "@mui/material/List";
import ListItemButton from "@mui/material/ListItemButton";
import ListItemIcon from "@mui/material/ListItemIcon";
import ListItemText from "@mui/material/ListItemText";
import ListSubheader from "@mui/material/ListSubheader";
import Toolbar from "@mui/material/Toolbar";
import Typography from "@mui/material/Typography";
import NextLink from "next/link";
import { usePathname } from "next/navigation";
import { useTranslation } from "react-i18next";

const DRAWER_WIDTH = 260;

interface MenuItemDefinition {
  label: string;
  icon: ComponentType;
}

/** Sections after Inicio. Items have no screen yet, so they render as buttons that do nothing. */
const sections: { title: string; items: MenuItemDefinition[] }[] = [
  {
    title: "menu.athletes.title",
    items: [
      { label: "menu.athletes.all", icon: GroupOutlined },
      { label: "menu.athletes.pendingInvitations", icon: PersonAddAltOutlined },
    ],
  },
  {
    title: "menu.programming.title",
    items: [
      { label: "menu.programming.assignedPlans", icon: AssignmentOutlined },
      { label: "menu.programming.calendar", icon: CalendarMonthOutlined },
    ],
  },
  {
    title: "menu.library.title",
    items: [
      { label: "menu.library.workoutTemplates", icon: DescriptionOutlined },
      { label: "menu.library.exerciseCatalog", icon: FitnessCenterOutlined },
    ],
  },
  {
    title: "menu.tracking.title",
    items: [
      { label: "menu.tracking.workoutFeedback", icon: RateReviewOutlined },
      { label: "menu.tracking.weeklyReviews", icon: EventNoteOutlined },
      { label: "menu.tracking.pendingReview", icon: PendingActionsOutlined },
    ],
  },
];

export default function NavigationDrawer() {
  const { t } = useTranslation("common");
  const pathname = usePathname();

  return (
    <Drawer
      variant="permanent"
      sx={{ width: DRAWER_WIDTH, flexShrink: 0 }}
      slotProps={{
        paper: {
          component: "nav",
          "aria-label": t("menu.label"),
          sx: { width: DRAWER_WIDTH, boxSizing: "border-box" },
        },
      }}
    >
      {/* Same height as the header's toolbar, so both rows line up. */}
      <Toolbar sx={{ gap: 1.5 }}>
        <Box component="img" src="/logo.svg" alt="" sx={{ width: 32, height: 32 }} />
        <Typography variant="h6" component="span" noWrap>
          {t("menu.appName")}
        </Typography>
      </Toolbar>
      <List>
        <ListItemButton component={NextLink} href="/" selected={pathname === "/"}>
          <ListItemIcon>
            <HomeOutlined />
          </ListItemIcon>
          <ListItemText primary={t("menu.home")} />
        </ListItemButton>
      </List>
      {sections.map((section) => (
        <List key={section.title} subheader={<ListSubheader>{t(section.title)}</ListSubheader>}>
          {section.items.map(({ label, icon: Icon }) => (
            <ListItemButton key={label}>
              <ListItemIcon>
                <Icon />
              </ListItemIcon>
              <ListItemText primary={t(label)} />
            </ListItemButton>
          ))}
        </List>
      ))}
    </Drawer>
  );
}
