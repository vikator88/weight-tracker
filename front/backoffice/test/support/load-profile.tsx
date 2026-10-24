import { useEffect, type ReactNode } from "react";
import useCurrentUser from "@/presentation/common/hooks/use-current-user";

/** Stands in for `AppShell` in tests of a single component: asks for the profile once on mount. */
export default function LoadProfile({ children }: { children: ReactNode }) {
  const { load } = useCurrentUser();

  useEffect(() => {
    load();
  }, [load]);

  return children;
}
