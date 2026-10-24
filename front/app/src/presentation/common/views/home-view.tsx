import { useState } from "react";
import { StyleSheet, View } from "react-native";
import { Button, HelperText, Text } from "react-native-paper";
import { useTranslation } from "react-i18next";
import type { DomainErrorCode } from "@weight-tracker/core/domain";
import useSession from "@/presentation/auth/hooks/use-session";
import useContainer from "@/presentation/common/hooks/use-container";

export default function HomeView() {
  const { t } = useTranslation("common");
  const container = useContainer();
  const session = useSession();
  const [loggingOut, setLoggingOut] = useState(false);
  const [errorCode, setErrorCode] = useState<DomainErrorCode | null>(null);

  // Temporary: a quick way to end the session while developing. Moves to a proper place later.
  async function handleLogout() {
    setLoggingOut(true);
    setErrorCode(null);

    const result = await container.resolve("logoutUseCase").execute();

    if (result.ok) {
      // The session gate reacts to the refreshed state; nothing navigates here.
      await session.refresh();
      return;
    }

    setErrorCode(result.error.code);
    setLoggingOut(false);
  }

  return (
    <View style={styles.container}>
      <Text variant="headlineLarge" role="heading" aria-level={1}>
        {t("home.title")}
      </Text>
      <Text variant="bodyLarge">{t("home.signedIn")}</Text>
      <HelperText type="error" visible={errorCode !== null}>
        {errorCode ? t(`errors:${errorCode}`) : ""}
      </HelperText>
      <Button
        mode="outlined"
        onPress={() => void handleLogout()}
        loading={loggingOut}
        disabled={loggingOut}
      >
        {t("home.logout")}
      </Button>
    </View>
  );
}

const styles = StyleSheet.create({
  container: {
    flex: 1,
    padding: 24,
  },
});
