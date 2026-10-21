import { StyleSheet, View } from "react-native";
import { Text } from "react-native-paper";
import { useTranslation } from "react-i18next";

export default function HomeView() {
  const { t } = useTranslation("common");

  return (
    <View style={styles.container}>
      <Text variant="headlineLarge" role="heading" aria-level={1}>
        {t("home.title")}
      </Text>
    </View>
  );
}

const styles = StyleSheet.create({
  container: {
    flex: 1,
    padding: 24,
  },
});
