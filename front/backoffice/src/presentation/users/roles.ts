import type { Role } from "@weight-tracker/core/domain";

/**
 * The roles a user can have, in the order the screen shows them. The API reads a role as a number and answers it as
 * its code; each entry pairs both.
 */
export const userRoles: { code: string; value: Role }[] = [
  { code: "USER", value: 1 },
  { code: "TRAINER", value: 2 },
  { code: "ADMIN", value: 3 },
];
