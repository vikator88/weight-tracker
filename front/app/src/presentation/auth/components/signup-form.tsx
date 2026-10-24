import { useRef, useState } from "react";
import { StyleSheet, View, type TextInput as NativeTextInput } from "react-native";
import { Button, HelperText, Text, TextInput } from "react-native-paper";
import { Link } from "expo-router";
import { useTranslation } from "react-i18next";

interface SignupFormProps {
  submitting: boolean;
  errorMessage: string | null;
  onSubmit: (credentials: { email: string; password: string }) => void;
}

type FieldError = "emailRequired" | "emailFormat" | "passwordRequired" | null;

const EMAIL_FORMAT = /^[^\s@]+@[^\s@]+\.[^\s@]+$/;

export default function SignupForm({ submitting, errorMessage, onSubmit }: SignupFormProps) {
  const { t } = useTranslation("auth");
  const [email, setEmail] = useState("");
  const [password, setPassword] = useState("");
  const [emailError, setEmailError] = useState<FieldError>(null);
  const [passwordError, setPasswordError] = useState<FieldError>(null);
  const passwordRef = useRef<NativeTextInput>(null);

  function handleSubmit() {
    if (submitting) {
      return;
    }

    const trimmedEmail = email.trim();
    const trimmedPassword = password.trim();
    const nextEmailError: FieldError = !trimmedEmail
      ? "emailRequired"
      : EMAIL_FORMAT.test(trimmedEmail)
        ? null
        : "emailFormat";
    const nextPasswordError: FieldError = trimmedPassword ? null : "passwordRequired";

    setEmailError(nextEmailError);
    setPasswordError(nextPasswordError);

    if (!nextEmailError && !nextPasswordError) {
      onSubmit({ email: trimmedEmail, password: trimmedPassword });
    }
  }

  return (
    <View style={styles.form}>
      <Text variant="headlineMedium" role="heading" aria-level={1}>
        {t("signup.title")}
      </Text>
      <View>
        <TextInput
          label={t("signup.email")}
          accessibilityLabel={t("signup.email")}
          value={email}
          onChangeText={setEmail}
          error={emailError !== null}
          keyboardType="email-address"
          autoCapitalize="none"
          autoComplete="email"
          returnKeyType="next"
          submitBehavior="submit"
          onSubmitEditing={() => passwordRef.current?.focus()}
        />
        <HelperText type="error" visible={emailError !== null}>
          {emailError ? t(`signup.validation.${emailError}`) : ""}
        </HelperText>
      </View>
      <View>
        <TextInput
          ref={passwordRef}
          label={t("signup.password")}
          accessibilityLabel={t("signup.password")}
          value={password}
          onChangeText={setPassword}
          error={passwordError !== null}
          secureTextEntry
          autoCapitalize="none"
          autoComplete="new-password"
          returnKeyType="go"
          onSubmitEditing={handleSubmit}
        />
        <HelperText type="error" visible={passwordError !== null}>
          {passwordError ? t(`signup.validation.${passwordError}`) : ""}
        </HelperText>
      </View>
      <HelperText type="error" visible={errorMessage !== null}>
        {errorMessage ?? ""}
      </HelperText>
      <Button mode="contained" onPress={handleSubmit} loading={submitting} disabled={submitting}>
        {t("signup.submit")}
      </Button>
      <Link href="/login" replace asChild>
        <Button mode="text" disabled={submitting}>
          {t("signup.toLogin")}
        </Button>
      </Link>
    </View>
  );
}

const styles = StyleSheet.create({
  form: {
    gap: 8,
  },
});
