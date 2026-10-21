import { defineConfig, globalIgnores } from "eslint/config";
import prettier from "eslint-config-prettier";
import tseslint from "typescript-eslint";

const uiPackages = {
  group: ["react", "react-*", "next", "next/*", "expo", "expo-*", "@mui/*", "react-native-paper"],
  message: "core must not depend on a front or a UI library.",
};

const toLayer = (layer) => ({
  regex: `(^|/)${layer}(/|$)`,
  message: `This layer may not import ${layer} (see STPR_INVARIANTS.md, "The Dependency Rule").`,
});

export default defineConfig([
  globalIgnores(["src/domain/generated/**"]),
  tseslint.configs.recommended,
  {
    rules: {
      "no-restricted-imports": ["error", { patterns: [uiPackages] }],
    },
  },
  {
    files: ["src/domain/**"],
    rules: {
      "no-restricted-imports": [
        "error",
        { patterns: [uiPackages, toLayer("application"), toLayer("infrastructure")] },
      ],
    },
  },
  {
    files: ["src/application/**"],
    rules: {
      "no-restricted-imports": ["error", { patterns: [uiPackages, toLayer("infrastructure")] }],
    },
  },
  prettier,
]);
