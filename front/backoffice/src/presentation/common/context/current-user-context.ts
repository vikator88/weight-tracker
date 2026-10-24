import { createContext } from "react";
import type { DomainErrorCode, UserProfile } from "@weight-tracker/core/domain";

export type CurrentUserStatus = "idle" | "loading" | "loaded" | "failed";

export interface CurrentUserContextValue {
  status: CurrentUserStatus;
  profile: UserProfile | null;
  errorCode: DomainErrorCode | null;
  /** Reads the profile when none was requested yet; does nothing otherwise. */
  load: () => void;
  /** Reads the profile again, after a failure. */
  reload: () => void;
  /** Stores the profile an update answered with. */
  replace: (profile: UserProfile) => void;
  /** Forgets the profile, as on logout. */
  clear: () => void;
}

const CurrentUserContext = createContext<CurrentUserContextValue | null>(null);

export default CurrentUserContext;
