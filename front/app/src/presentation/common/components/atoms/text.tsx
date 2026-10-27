import { type ComponentProps } from "react";
import { Text as PaperText } from "react-native-paper";

/** The project's Text: the React Native Paper widget, themed by the Paper theme. */
export default function Text(props: ComponentProps<typeof PaperText>) {
  return <PaperText {...props} />;
}
