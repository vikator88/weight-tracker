import type { UserProfile, UserSummary } from "@weight-tracker/core/domain";

/** Lower case and without accents, so "garcia" finds "García". */
function normalized(value: string): string {
  return value
    .normalize("NFD")
    .replace(/\p{Diacritic}/gu, "")
    .toLocaleLowerCase();
}

/** Name and surname in one line, or `null` when the user has neither. */
export function fullNameOf(user: Pick<UserProfile, "name" | "surname">): string | null {
  const fullName = `${user.name} ${user.surname}`.trim();

  return fullName === "" ? null : fullName;
}

/**
 * The users the table shows: the active ones, plus the inactive ones when asked; of the selected role, if any; whose
 * email, name or surname contain the search text. A view filter over the loaded list; it sends nothing and keeps the
 * API's order.
 */
export function filterUsers(
  users: UserSummary[],
  search: string,
  role: string | null,
  showInactive: boolean,
): UserSummary[] {
  const text = normalized(search.trim());

  return users.filter(
    (user) =>
      (showInactive || user.isActive) &&
      (role === null || user.role === role) &&
      (text === "" ||
        [user.email, user.name, user.surname].some((field) => normalized(field).includes(text))),
  );
}
