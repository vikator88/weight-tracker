import { type ComponentProps } from "react";
import { HelperText as PaperHelperText } from "react-native-paper";

/** The project's HelperText: the React Native Paper widget, themed by the Paper theme. */
export default function HelperText(props: ComponentProps<typeof PaperHelperText>) {
  return <PaperHelperText {...props} />;
}
