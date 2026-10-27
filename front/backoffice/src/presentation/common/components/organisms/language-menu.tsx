"use client";

import { useState } from "react";
import TranslateOutlined from "@mui/icons-material/TranslateOutlined";
import Button from "@/presentation/common/components/atoms/button";
import Menu from "@/presentation/common/components/atoms/menu";
import MenuItem from "@/presentation/common/components/atoms/menu-item";
import { useTranslation } from "react-i18next";
import useContainer from "@/presentation/common/hooks/use-container";

const languages = ["es", "en"] as const;

export default function LanguageMenu() {
  const { t, i18n } = useTranslation("common");
  const container = useContainer();
  const [anchor, setAnchor] = useState<HTMLElement | null>(null);

  async function choose(language: (typeof languages)[number]) {
    setAnchor(null);
    await i18n.changeLanguage(language);
    // A failed write only means the next load falls back to the browser language.
    await container.resolve("setPreferredLanguageUseCase").execute(language);
  }

  return (
    <>
      <Button
        color="inherit"
        startIcon={<TranslateOutlined />}
        aria-label={t("header.language.label")}
        aria-haspopup="true"
        aria-expanded={anchor !== null}
        onClick={(event) => setAnchor(event.currentTarget)}
      >
        {i18n.language.toUpperCase()}
      </Button>
      <Menu anchorEl={anchor} open={anchor !== null} onClose={() => setAnchor(null)}>
        {languages.map((language) => (
          <MenuItem
            key={language}
            selected={i18n.language === language}
            onClick={() => void choose(language)}
          >
            {t(`header.language.${language}`)}
          </MenuItem>
        ))}
      </Menu>
    </>
  );
}
