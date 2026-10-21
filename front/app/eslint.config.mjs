import { defineConfig, globalIgnores } from "eslint/config";
import expoConfig from "eslint-config-expo/flat.js";
import prettier from "eslint-config-prettier";

const coreInfrastructure = {
  group: ["@weight-tracker/core/infrastructure"],
  message: "Presentation reaches core only through domain and application (STPR_INVARIANTS.md).",
};

const frontInfrastructure = (allowed = []) => ({
  regex: allowed.length
    ? `^(@/|(\\.\\./)+)infrastructure/(?!(${allowed.join("|")})$)`
    : `^(@/|(\\.\\./)+)infrastructure(/|$)`,
  message: "Only app-providers.tsx imports the composition root (container, i18n).",
});

export default defineConfig([
  expoConfig,
  {
    files: ["src/presentation/**"],
    rules: {
      "no-restricted-imports": ["error", { patterns: [coreInfrastructure, frontInfrastructure()] }],
    },
  },
  {
    files: ["src/presentation/common/components/app-providers.tsx"],
    rules: {
      "no-restricted-imports": [
        "error",
        { patterns: [coreInfrastructure, frontInfrastructure(["container", "i18n"])] },
      ],
    },
  },
  prettier,
  globalIgnores([".expo/**", "dist/**", "expo-env.d.ts"]),
]);
