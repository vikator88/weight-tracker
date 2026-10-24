// Placeholder until alerts come from the API: translation keys of three sample alerts.
const placeholderAlerts = [
  "header.alerts.mock.workoutCompleted",
  "header.alerts.mock.invitationAccepted",
  "header.alerts.mock.reviewPending",
] as const;

export default function useAlerts(): readonly string[] {
  return placeholderAlerts;
}
