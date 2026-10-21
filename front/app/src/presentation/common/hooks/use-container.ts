import { use } from "react";
import type { ContainerInterface, Dependencies } from "@weight-tracker/core/application";
import ContainerContext from "../context/container-context";

export default function useContainer(): ContainerInterface<Dependencies> {
  const container = use(ContainerContext);

  if (!container) {
    // Wiring mistake, never a runtime condition (STPR_INVARIANTS.md, "Infrastructure").
    throw new Error("useContainer must be used inside ContainerProvider");
  }

  return container;
}
