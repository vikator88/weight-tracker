import { useEffect } from "react";
import { SplashScreen, Stack } from "expo-router";
import { StatusBar } from "expo-status-bar";
import useSession from "@/presentation/auth/hooks/use-session";
import AppProviders from "@/presentation/common/components/app-providers";

SplashScreen.preventAutoHideAsync();

function RootNavigator() {
  const { status } = useSession();

  useEffect(() => {
    if (status !== "restoring") {
      SplashScreen.hide();
    }
  }, [status]);

  if (status === "restoring") {
    return null;
  }

  const signedIn = status === "signedIn";

  return (
    <Stack screenOptions={{ headerShown: false }}>
      <Stack.Protected guard={signedIn} redirectTo="/login">
        <Stack.Screen name="(app)" />
      </Stack.Protected>
      <Stack.Protected guard={!signedIn}>
        <Stack.Screen name="(auth)" />
      </Stack.Protected>
    </Stack>
  );
}

export default function RootLayout() {
  return (
    <AppProviders>
      <RootNavigator />
      <StatusBar style="auto" />
    </AppProviders>
  );
}
