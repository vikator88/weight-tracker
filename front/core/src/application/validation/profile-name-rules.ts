import type { UpdateProfileRequest } from "../../domain";

export const PROFILE_NAME_MIN_LENGTH = 3;
export const PROFILE_NAME_MAX_LENGTH = 100;

export type ProfileNameViolation = "required" | "minLength" | "maxLength";

/** Length rules both fronts apply to a name or surname. The value is trimmed before it is measured. */
export function validateProfileName(value: string): ProfileNameViolation | null {
  const length = value.trim().length;

  if (length === 0) {
    return "required";
  }
  if (length < PROFILE_NAME_MIN_LENGTH) {
    return "minLength";
  }
  if (length > PROFILE_NAME_MAX_LENGTH) {
    return "maxLength";
  }
  return null;
}

export function validateUpdateProfileRequest(request: UpdateProfileRequest): {
  name: ProfileNameViolation | null;
  surname: ProfileNameViolation | null;
} {
  return {
    name: validateProfileName(request.name ?? ""),
    surname: validateProfileName(request.surname ?? ""),
  };
}
