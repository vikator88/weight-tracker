"use client";

import Box from "@mui/material/Box";
import Container from "@mui/material/Container";
import Skeleton from "@/presentation/common/components/atoms/skeleton";
import { useTranslation } from "react-i18next";

/** Placeholder sections shown where the home content will go. */
export default function HomeSkeleton() {
  const { t } = useTranslation("common");

  return (
    <Container sx={{ py: 4 }} aria-busy="true" aria-label={t("loading")}>
      <Box
        sx={{ display: "grid", gridTemplateColumns: { xs: "1fr", md: "repeat(3, 1fr)" }, gap: 2 }}
      >
        {[0, 1, 2].map((card) => (
          <Skeleton key={card} variant="rounded" height={96} />
        ))}
      </Box>
      <Skeleton variant="rounded" height={240} sx={{ mt: 3 }} />
      <Box sx={{ mt: 3 }}>
        {[0, 1, 2, 3, 4].map((row) => (
          <Skeleton key={row} variant="text" height={40} />
        ))}
      </Box>
    </Container>
  );
}
