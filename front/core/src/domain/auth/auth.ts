import type { components } from "../../contract/generated/api";

export type SignupRequest = components["schemas"]["SignupRequest"];
export type AuthResponse = components["schemas"]["AuthResponse"];
export type Role = components["schemas"]["Role"];
export type SignupCredentials = Omit<SignupRequest, "role">;
