"use client";

import type { CSSProperties, ReactNode, Ref } from "react";
import Box from "@mui/material/Box";
import Chip from "@/presentation/common/components/atoms/chip";
import ListItem from "@/presentation/common/components/atoms/list-item";
import Typography from "@/presentation/common/components/atoms/typography";
import type { LabelColor } from "@/presentation/workouts/hooks/use-exercise-labels";

/** Lists of exercises scroll inside their card past this height instead of lengthening the page. */
export const EXERCISE_LIST_MAX_HEIGHT = "min(60vh, 640px)";

export interface ExerciseLabel {
  text: string;
  color?: LabelColor;
}

interface ExerciseListItemProps {
  name: string;
  /** Shown as small labels under the name, in this order. */
  labels: ExerciseLabel[];
  /** A short status shown beside the name, such as "Pendiente". */
  note?: string;
  /** Extra facts shown under the labels, one per line. */
  lines?: string[];
  /** The exercise's place in its list, shown in a circle before the name. */
  position?: number;
  selected?: boolean;
  /** Shown on the left, before the position, such as a drag handle. */
  leading?: ReactNode;
  /** Icon buttons shown on the right. */
  actions: ReactNode;
  /** The item's node and inline style, for a wrapper that moves it, such as a sortable list. */
  ref?: Ref<HTMLLIElement>;
  style?: CSSProperties;
}

/** One exercise: its name, labels under it, and its actions on the right. Shared by the catalog and the workout. */
export default function ExerciseListItem({
  name,
  labels,
  note,
  lines = [],
  position,
  selected = false,
  leading,
  actions,
  ref,
  style,
}: ExerciseListItemProps) {
  return (
    <ListItem
      ref={ref}
      style={style}
      divider
      aria-current={selected ? "true" : undefined}
      sx={{
        gap: 1.5,
        alignItems: "flex-start",
        bgcolor: selected ? "action.selected" : undefined,
      }}
    >
      {leading && <Box sx={{ display: "flex", flexShrink: 0, alignSelf: "center" }}>{leading}</Box>}
      {position !== undefined && (
        <Box
          sx={{
            flexShrink: 0,
            width: 24,
            height: 24,
            mt: 0.25,
            borderRadius: "50%",
            bgcolor: "primary.light",
            color: "primary.contrastText",
            display: "flex",
            alignItems: "center",
            justifyContent: "center",
          }}
        >
          <Typography variant="caption" component="span" sx={{ fontWeight: 600 }}>
            {position}
          </Typography>
        </Box>
      )}
      <Box sx={{ minWidth: 0, flex: 1 }}>
        <Typography sx={{ overflowWrap: "anywhere" }}>
          {name}
          {note && (
            <Typography
              component="span"
              variant="caption"
              color="text.secondary"
              sx={{ ml: 1, fontStyle: "italic" }}
            >
              {note}
            </Typography>
          )}
        </Typography>
        {labels.length > 0 && (
          <Box sx={{ display: "flex", flexWrap: "wrap", gap: 0.5, mt: 0.5 }}>
            {labels.map(({ text, color = "default" }, index) => (
              // Body part and muscle can share a text (Pecho, Pecho), so the position is the key.
              <Chip key={index} label={text} size="small" variant="outlined" color={color} />
            ))}
          </Box>
        )}
        {lines.map((line) => (
          <Typography key={line} variant="body2" color="text.secondary" sx={{ mt: 0.5 }}>
            {line}
          </Typography>
        ))}
      </Box>
      {/* Inline rather than MUI's absolutely positioned secondary action, so names get all the width left. */}
      <Box sx={{ display: "flex", flexShrink: 0, alignSelf: "center" }}>{actions}</Box>
    </ListItem>
  );
}
