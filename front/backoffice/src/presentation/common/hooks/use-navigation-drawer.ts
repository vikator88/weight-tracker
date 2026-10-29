import { use } from "react";
import NavigationDrawerContext, {
  type NavigationDrawerContextValue,
} from "../context/navigation-drawer-context";

export default function useNavigationDrawer(): NavigationDrawerContextValue {
  return use(NavigationDrawerContext);
}
