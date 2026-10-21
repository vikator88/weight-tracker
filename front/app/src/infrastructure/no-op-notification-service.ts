import type { NotificationServiceInterface } from "@weight-tracker/core/application";

/** Stand-in until a change brings expo-notifications. */
export default class NoOpNotificationService implements NotificationServiceInterface {
  async requestPermission(): Promise<boolean> {
    return false;
  }

  async notify(): Promise<void> {}
}
