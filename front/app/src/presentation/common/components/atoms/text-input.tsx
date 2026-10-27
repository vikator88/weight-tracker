import { type ComponentProps } from "react";
import { TextInput as PaperTextInput } from "react-native-paper";

/** The project's TextInput: the React Native Paper widget, themed by the Paper theme. */
export default function TextInput(props: ComponentProps<typeof PaperTextInput>) {
  return <PaperTextInput {...props} />;
}
