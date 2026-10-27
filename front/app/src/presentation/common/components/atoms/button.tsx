import { type ComponentProps } from "react";
import { Button as PaperButton } from "react-native-paper";

/** The project's Button: the React Native Paper widget, themed by the Paper theme. */
export default function Button(props: ComponentProps<typeof PaperButton>) {
  return <PaperButton {...props} />;
}
