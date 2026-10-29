"use client";

import Chip from "@/presentation/common/components/atoms/chip";
import Tooltip from "@/presentation/common/components/atoms/tooltip";

interface UserStatusChipProps {
  /** The status, such as "Inactivo". */
  label: string;
  /** Shown on hover or focus, such as the deactivation date. */
  tooltip: string;
}

/** A small outlined chip that marks a user's status, with its detail in a tooltip reachable by keyboard too. */
export default function UserStatusChip({ label, tooltip }: UserStatusChipProps) {
  return (
    <Tooltip title={tooltip}>
      <Chip label={label} size="small" variant="outlined" tabIndex={0} />
    </Tooltip>
  );
}
