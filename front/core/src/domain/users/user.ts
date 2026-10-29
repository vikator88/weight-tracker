import type { components } from "../../contract/generated/api";

export type UserProfile = components["schemas"]["UserProfileResponse"];
/** One user in the list: the profile plus whether the account is active and since when it is not. */
export type UserSummary = components["schemas"]["UserSummaryResponse"];
export type UpdateProfileRequest = components["schemas"]["UpdateProfileRequest"];
export type CreateUserRequest = components["schemas"]["CreateUserRequest"];
export type CreatedUserResponse = components["schemas"]["CreatedUserResponse"];
