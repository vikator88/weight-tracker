import { useState } from "react";
import type { DomainErrorCode, UpdateProfileRequest } from "@weight-tracker/core/domain";
import useContainer from "@/presentation/common/hooks/use-container";
import useCurrentUser from "@/presentation/common/hooks/use-current-user";

interface UpdateProfileState {
  saving: boolean;
  errorCode: DomainErrorCode | null;
  saved: boolean;
  /** Resolves to `true` when the profile was saved and the shared profile replaced. */
  save: (request: UpdateProfileRequest) => Promise<boolean>;
  clearSaved: () => void;
}

export default function useUpdateProfile(): UpdateProfileState {
  const container = useContainer();
  const { replace } = useCurrentUser();
  const [saving, setSaving] = useState(false);
  const [errorCode, setErrorCode] = useState<DomainErrorCode | null>(null);
  const [saved, setSaved] = useState(false);

  async function save(request: UpdateProfileRequest) {
    setSaving(true);
    setErrorCode(null);
    setSaved(false);

    const result = await container.resolve("updateMyProfileUseCase").execute(request);

    setSaving(false);
    if (result.ok) {
      replace(result.value);
      setSaved(true);
      return true;
    }
    setErrorCode(result.error.code);
    return false;
  }

  return { saving, errorCode, saved, save, clearSaved: () => setSaved(false) };
}
