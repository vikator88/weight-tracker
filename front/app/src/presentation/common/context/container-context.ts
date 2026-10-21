import { createContext } from "react";
import type { ContainerInterface, Dependencies } from "@weight-tracker/core/application";

const ContainerContext = createContext<ContainerInterface<Dependencies> | null>(null);

export default ContainerContext;
