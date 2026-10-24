import { use } from "react";
import CurrentUserContext, { type CurrentUserContextValue } from "../context/current-user-context";

interface CurrentUser extends CurrentUserContextValue {
  /** The user still has to complete onboarding. The only place this rule lives. */
  onboardingPending: boolean;
}

export default function useCurrentUser(): CurrentUser {
  const context = use(CurrentUserContext);

  if (context === null) {
    throw new Error("useCurrentUser must be used inside CurrentUserProvider");
  }

  const { status, profile } = context;
  const onboardingPending =
    status === "loaded" &&
    profile !== null &&
    (profile.name.trim() === "" || profile.surname.trim() === "");

  return { ...context, onboardingPending };
}
