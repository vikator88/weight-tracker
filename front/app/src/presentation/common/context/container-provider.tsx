import type { ReactNode } from "react";
import type { ContainerInterface, Dependencies } from "@weight-tracker/core/application";
import ContainerContext from "./container-context";

interface ContainerProviderProps {
  container: ContainerInterface<Dependencies>;
  children: ReactNode;
}

export default function ContainerProvider({ container, children }: ContainerProviderProps) {
  return <ContainerContext value={container}>{children}</ContainerContext>;
}
