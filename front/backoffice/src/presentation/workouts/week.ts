/**
 * Workout weeks run Monday to Sunday. Every function takes and returns `YYYY-MM-DD` strings and computes in UTC, so the
 * browser's time zone never moves a date into another week.
 */

const DAY_MS = 24 * 60 * 60 * 1000;

function toUtc(date: string): number {
  const [year, month, day] = date.split("-").map(Number);
  return Date.UTC(year!, month! - 1, day!);
}

function fromUtc(time: number): string {
  const date = new Date(time);
  const pad = (value: number) => String(value).padStart(2, "0");
  return `${date.getUTCFullYear()}-${pad(date.getUTCMonth() + 1)}-${pad(date.getUTCDate())}`;
}

function addDays(date: string, days: number): string {
  return fromUtc(toUtc(date) + days * DAY_MS);
}

/** The Monday of the week that contains `date`. */
export function weekStartOf(date: string): string {
  const dayFromMonday = (new Date(toUtc(date)).getUTCDay() + 6) % 7;
  return addDays(date, -dayFromMonday);
}

/** The Wednesday of the week that starts on `monday`: the day a picked week is sent as. */
export function weekMiddleOf(monday: string): string {
  return addDays(monday, 2);
}

/** The Sunday of the week that starts on `monday`. */
export function weekEndOf(monday: string): string {
  return addDays(monday, 6);
}

/** The Monday `weeks` whole weeks away from `monday`; negative goes back. */
export function shiftWeek(monday: string, weeks: number): string {
  return addDays(monday, weeks * 7);
}

/** The Monday of the user's current week, from today's local date. */
export function currentWeekStart(): string {
  const today = new Date();
  const pad = (value: number) => String(value).padStart(2, "0");
  return weekStartOf(`${today.getFullYear()}-${pad(today.getMonth() + 1)}-${pad(today.getDate())}`);
}
