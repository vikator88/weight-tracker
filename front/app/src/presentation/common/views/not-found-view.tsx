import { StyleSheet, View } from "react-native";
import { Button, Text } from "react-native-paper";
import { Link } from "expo-router";
import { useTranslation } from "react-i18next";

export default function NotFoundView() {
  const { t } = useTranslation("common");

  return (
    <View style={styles.container}>
      <Text variant="headlineMedium" role="heading" aria-level={1}>
        {t("notFound.title")}
      </Text>
      <Link href="/" asChild>
        <Button mode="contained">{t("notFound.goHome")}</Button>
      </Link>
    </View>
  );
}

const styles = StyleSheet.create({
  container: {
    flex: 1,
    justifyContent: "center",
    gap: 16,
    padding: 24,
  },
});
