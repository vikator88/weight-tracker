import { Stack } from "expo-router";
import { StatusBar } from "expo-status-bar";
import AppProviders from "@/presentation/common/components/app-providers";

export default function RootLayout() {
  return (
    <AppProviders>
      <Stack />
      <StatusBar style="auto" />
    </AppProviders>
  );
}
