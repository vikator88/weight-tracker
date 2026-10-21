# Tasks: setup-front-skeleton

Execute in order. Each block ends green on the commands it names before the next starts.

## 1. Root tooling

1. Add `prettier` and `eslint-config-prettier` at the root; add `.prettierrc.json` (double quotes, semicolons,
   trailing commas, `printWidth` 100) and `.prettierignore` (`node_modules`, `.next`, `out`, `.expo`, `.turbo`,
   `pnpm-lock.yaml`, `core/src/domain/generated`, `core/openapi`).
2. Root scripts: `lint` = `turbo run lint && prettier --check .`, `format` = `prettier --write .`, `test` =
   `turbo run test`, `test:e2e` = `turbo run test:e2e`.
3. `turbo.json`: add `test` (depends on `^typecheck`) and `test:e2e` (depends on `build`, not cached).
4. Run `pnpm format` once and check the diff contains formatting only.

## 2. Core: conventions

1. Move `result.ts` and `domain-error.ts` to `core/src/domain/common/`. `DomainError` default-exported, typed by
   `DomainErrorCode` (`core/src/domain/common/domain-error-code.ts`, the seven codes in plan section 9).
2. Rename and default-export the contracts: `use-case-interface.ts` (`UseCaseInterface`, `execute` returning
   `Promise<Result<TOutput, DomainError>>`), `key-value-store-interface.ts`, `notification-service-interface.ts`,
   and move `HttpClientInterface` to `core/src/application/ports/http-client-interface.ts`.
3. Default-export `FetchHttpClient` and `HttpError` from their own files in `core/src/infrastructure/http/`.
4. Rewrite every barrel as named re-exports of defaults; keep `result.ts` as named exports.
5. Update both fronts' `http-client.ts` imports.
6. `pnpm typecheck`.

## 3. Core: error mapper and DI

1. `core/src/infrastructure/http/http-error-mapper.ts`: maps an `HttpError`, a `fetch` rejection or a JSON parse
   failure to a `DomainError`, following the table in plan section 9. `detail` becomes the message.
2. Make `FetchHttpClient` raise a distinguishable error on invalid JSON so the mapper can tell it apart. It is still
   the only code that throws.
3. `core/src/application/ports/container-interface.ts` (`ContainerInterface<D>` with `resolve<K extends keyof D>(key: K): D[K]`).
4. `core/src/application/dependencies.ts` (`Dependencies`: `httpClient`, `keyValueStore`, `notificationService`).
5. `core/src/infrastructure/di/container.ts`: `createContainer<D>(registrations)`, lazy singletons, `transient(factory)`.
6. `pnpm typecheck`.

## 4. Core: OpenAPI

1. Add `openapi-typescript` as a dev dependency of `core`.
2. Scripts `openapi:refresh` (download `http://localhost:5199/openapi/v1.json` to `core/openapi/v1.json`) and
   `openapi:generate` (generate `core/src/domain/generated/api.ts`).
3. With the API running in Development, run both and commit the snapshot and the generated file. If the API cannot be
   started, stop and report; do not hand-write the snapshot.
4. Export the generated types from `core/src/domain/index.ts` under a `generated` namespace for alias files to use.

## 5. Core: lint and tests

1. `core/eslint.config.mjs` with `typescript-eslint`, `eslint-config-prettier`, and the `no-restricted-imports` rules
   for `domain`, `application` and UI packages (plan section 10). `lint` script.
2. Add Vitest; `core/vitest.config.ts`; `test` script; `core/test` excluded from the domain/application `tsconfig`
   but typechecked through a test `tsconfig`.
3. Tests: `http-error-mapper.test.ts`, `fetch-http-client.test.ts`, `container.test.ts` (plan section 12), AAA.
4. `pnpm --filter @weight-tracker/core typecheck lint test`.

## 6. Backoffice

1. Read in `backoffice/node_modules/next/dist/docs/`: static exports, `next.config` options for Cache Components
   and partial prefetching, root layout, and client components. Stop and re-plan if `output: "export"` conflicts.
2. `next.config.ts`: `output: "export"`, remove `cacheComponents` and `partialPrefetching`, keep `transpilePackages`.
3. Remove `src/app/page.module.css`, `src/app/globals.css`, `public/*.svg` and the scaffold `page.tsx` content.
4. Install `@mui/material`, `@emotion/react`, `@emotion/styled`, `@mui/material-nextjs`, `i18next`, `react-i18next`.
5. `src/presentation/common/theme.ts`, `src/infrastructure/i18n.ts` (`es` default, `en` fallback, language from
   `navigator.languages`), `src/i18n/{es,en}/{common,errors}.json` with the copy in plan section 9.
6. `src/infrastructure/in-memory-key-value-store.ts` and `no-op-notification-service.ts` (stand-ins until login),
   `src/infrastructure/container.ts`.
7. `src/presentation/common/context/container-context.tsx` (`ContainerProvider`, `useContainer`),
   `src/presentation/common/components/app-providers.tsx` (`"use client"`; MUI cache and theme, `CssBaseline`,
   i18n, container), `src/presentation/common/views/home-view.tsx` (`"use client"`).
8. `src/app/layout.tsx` renders `<html lang="es"><body><AppProviders>{children}</AppProviders></body></html>`;
   `src/app/page.tsx` default-exports `HomeView`.
9. ESLint: add `eslint-config-prettier` and the presentation `no-restricted-imports` rules.
10. Vitest + `@testing-library/react` + `@testing-library/jest-dom` + `jsdom`; `test` script; tests
    `test/i18n/translation-parity.test.ts`, `test/presentation/common/views/home-view.test.tsx`.
11. Playwright (`@playwright/test`, Chromium); `playwright.config.ts` serving `out/` with a static server;
    `test:e2e` script; `e2e/home.spec.ts`.
12. `pnpm --filter backoffice typecheck lint test build test:e2e`; confirm `out/` exists and holds only static files.

## 7. App

1. Read the Expo SDK 57 docs for React Native Paper setup, `expo-localization`, and unit testing with `jest-expo`.
2. `pnpm exec expo install react-native-paper expo-localization`, then add `i18next` and `react-i18next`.
3. Replace the scaffold content of `src/app/index.tsx`.
4. `src/presentation/common/theme.ts`, `src/infrastructure/i18n.ts` (`es` default, `en` fallback, language from
   `expo-localization`), `src/i18n/{es,en}/{common,errors}.json` with the copy in plan section 9.
5. In-memory and no-op stand-ins and `src/infrastructure/container.ts`, as in the backoffice.
6. `src/presentation/common/context/container-context.tsx`, `src/presentation/common/components/app-providers.tsx`
   (`PaperProvider`, i18n, container), `src/presentation/common/views/home-view.tsx`.
7. `src/app/_layout.tsx` renders `<AppProviders><Stack /></AppProviders>` plus the status bar;
   `src/app/index.tsx` default-exports `HomeView`.
8. ESLint: `eslint-config-expo`, `eslint-config-prettier`, the presentation `no-restricted-imports` rules; `lint` script.
9. Jest + `jest-expo` + `@testing-library/react-native`; `test` script; tests `test/i18n/translation-parity.test.ts`,
   `test/presentation/common/views/home-view.test.tsx`, `test/infrastructure/container.test.ts`.
10. `pnpm --filter app typecheck lint test`; `pnpm exec expo-doctor` reports no issues.

## 8. Docs

1. Update `front/README.md`: new scripts, test tiers, OpenAPI refresh, static backoffice.
2. Check `front/AGENTS.md` and `.stpr/template/STPR_INVARIANTS.md` still match what was built; report any gap
   instead of silently editing the invariants.

## 9. Review

1. Backoffice review: plan section 13 checkpoints plus checklist sections Architecture, Structure, Presentation,
   Stack, Naming.
2. App review: same.
3. Core review: Architecture, Domain, Application And Infrastructure, Naming.
4. Try a forbidden import in a view, confirm `pnpm lint` fails, revert.
5. Full run from `front/`: `pnpm typecheck && pnpm lint && pnpm test && pnpm test:e2e`.
