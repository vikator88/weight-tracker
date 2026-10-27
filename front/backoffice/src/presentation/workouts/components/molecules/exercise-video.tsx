"use client";

import Box from "@mui/material/Box";
import { useTranslation } from "react-i18next";
import Link from "@/presentation/common/components/atoms/link";

interface ExerciseVideoProps {
  url: string;
  /** Names the embedded player for assistive technology. */
  title: string;
}

/** The YouTube video id of a watch or short link, or `null` for any other address. */
function youTubeIdOf(url: string): string | null {
  try {
    const { hostname, pathname, searchParams } = new URL(url);
    const host = hostname.replace(/^www\./, "");

    if ((host === "youtube.com" || host === "m.youtube.com") && pathname === "/watch") {
      return searchParams.get("v");
    }
    if (host === "youtu.be") {
      return pathname.slice(1) || null;
    }
  } catch {
    // Not a valid address: shown as a plain link.
  }
  return null;
}

/** Embeds a YouTube video without tracking cookies; any other address is offered as a link in a new tab. */
export default function ExerciseVideo({ url, title }: ExerciseVideoProps) {
  const { t } = useTranslation("workouts");
  const videoId = youTubeIdOf(url);

  if (videoId === null) {
    return (
      <Link href={url} target="_blank" rel="noopener noreferrer">
        {t("workout.configuration.openVideo")}
      </Link>
    );
  }

  return (
    <Box
      component="iframe"
      src={`https://www.youtube-nocookie.com/embed/${encodeURIComponent(videoId)}`}
      title={title}
      allow="encrypted-media; picture-in-picture"
      allowFullScreen
      sx={{ width: "100%", aspectRatio: "16 / 9", border: 0, borderRadius: 1 }}
    />
  );
}
