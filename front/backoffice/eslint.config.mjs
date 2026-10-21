import { defineConfig, globalIgnores } from "eslint/config";
import nextVitals from "eslint-config-next/core-web-vitals";
import nextTs from "eslint-config-next/typescript";
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

const eslintConfig = defineConfig([
  ...nextVitals,
  ...nextTs,
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
  // Override default ignores of eslint-config-next.
  globalIgnores([
    // Default ignores of eslint-config-next:
    ".next/**",
    "out/**",
    "build/**",
    "next-env.d.ts",
    // Test reports:
    "playwright-report/**",
    "test-results/**",
  ]),
]);

export default eslintConfig;
