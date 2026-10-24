export interface CurrentUser {
  name: string;
  role: "TRAINER" | "ADMIN";
}

// Placeholder until the API exposes the signed-in user; replace this body, not its callers.
const placeholderUser: CurrentUser = { name: "Usuario de prueba", role: "TRAINER" };

export default function useCurrentUser(): CurrentUser {
  return placeholderUser;
}
