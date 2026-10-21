import type Notification from "./notification";

/** Platform-specific: implemented by each front (e.g. Web Notifications, expo-notifications). */
export default interface NotificationServiceInterface {
  requestPermission(): Promise<boolean>;
  notify(notification: Notification): Promise<void>;
}
