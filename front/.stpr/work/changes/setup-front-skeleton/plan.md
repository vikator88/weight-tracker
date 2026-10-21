# Plan: setup-front-skeleton

Status: **implemented**

## 1. Change Summary

Bring the `front/` skeleton in line with `.stpr/template/STPR_INVARIANTS.md` so that the first feature (login)
can be planned against a real base. After this change the monorepo has formatting, linting with import rules
that enforce the dependency rule, all test tiers runnable, i18n, the UI libraries, the DI container, the HTTP error
mapper and generated API types. It delivers **no feature**.

## 2. Scope

In scope:

- `core`: renames to the `Interface` convention, default exports with named barrels, `domain/common`, the DI
  container contract and implementation, typed tokens, the HTTP error mapper, OpenAPI snapshot and type generation,
  Vitest.
- `backoffice`: static export, removal of server-only Next features and of the create-next-app scaffold, MUI,
  i18n, container and providers, a placeholder home screen, Vitest + React Testing Library, Playwright.
- `app`: removal of the create-expo-app scaffold, React Native Paper, i18n, container and providers, a placeholder
  home screen, Jest + `jest-expo` + React Native Testing Library.
- Root: Prettier, ESLint in every workspace, `test` and `test:e2e` Turbo tasks, updated `README.md`.

Out of scope:

- Any feature, screen beyond the placeholder, use case, repository or storage adapter. The `KeyValueStore`
  adapters (`localStorage`, `expo-sqlite`) arrive with login.
- Any API change. Specific error codes beyond HTTP status need one (see section 9) and are deferred.
- App E2E (accepted gap).
- CI configuration.

## 3. Fronts And Roles

Both fronts are touched, with identical scope and no role-dependent behaviour. The placeholder home screen is
public and shows a translated title only; it exists to prove routing, providers, UI library and i18n end to end.

## 4. Classification

| Piece                                                    | Building block                                                     | Location                                                       |
| -------------------------------------------------------- | ------------------------------------------------------------------ | -------------------------------------------------------------- |
| `Result`, `ok`, `fail`                                   | Domain common (named-export helper family)                         | `core/src/domain/common/result.ts`                             |
| `DomainError`                                            | Domain common                                                      | `core/src/domain/common/domain-error.ts`                       |
| `DomainErrorCode`                                        | Domain common (string union of the codes in section 9)             | `core/src/domain/common/domain-error-code.ts`                  |
| Generated API types                                      | Entity types (generated)                                           | `core/src/domain/generated/api.ts`                             |
| `UseCaseInterface`                                       | Use case contract (renamed from `UseCase`)                         | `core/src/application/ports/use-case-interface.ts`             |
| `KeyValueStoreInterface`, `NotificationServiceInterface` | Platform ports (renamed)                                           | `core/src/application/ports/`                                  |
| `HttpClientInterface`                                    | Repository transport contract (renamed, moved from infrastructure) | `core/src/application/ports/http-client-interface.ts`          |
| `ContainerInterface<D>`                                  | Container contract                                                 | `core/src/application/ports/container-interface.ts`            |
| `Dependencies`                                           | Key-to-type map of every registrable port and use case             | `core/src/application/dependencies.ts`                         |
| `FetchHttpClient`, `HttpError`                           | Repository transport                                               | `core/src/infrastructure/http/`                                |
| `HttpErrorMapper`                                        | Infrastructure, single status-to-code mapping                      | `core/src/infrastructure/http/http-error-mapper.ts`            |
| `Container`                                              | Infrastructure, implements `ContainerInterface`                    | `core/src/infrastructure/di/container.ts`                      |
| `container.ts` (each front)                              | Composition root                                                   | `<front>/src/infrastructure/container.ts`                      |
| i18n setup (each front)                                  | Front infrastructure                                               | `<front>/src/infrastructure/i18n.ts`                           |
| `ContainerProvider` / `useContainer`                     | State hook (holds the container)                                   | `<front>/src/presentation/common/context/`                     |
| `AppProviders`                                           | Root providers component                                           | `<front>/src/presentation/common/components/app-providers.tsx` |
| `HomeView`                                               | View (placeholder)                                                 | `<front>/src/presentation/common/views/home-view.tsx`          |
| Root layout and index route                              | Routes                                                             | `<front>/src/app/`                                             |
| Translation parity test                                  | Unit test                                                          | `<front>/test/i18n/translation-parity.test.ts`                 |

`HttpClientInterface` moves to `application/ports` because repository implementations depend on it through
the contract, as any other port. `HttpError` stays in infrastructure: only infrastructure throws and catches it.

## 5. Canonical Template Mapping

No canonical reference exists yet (every feature entry in `.stpr/template/README.md` is `pending`). This change
**establishes the skeleton-backed patterns** listed as `skeleton` / `skeleton (from setup change)` in the map:
`Result` / `DomainError`, `UseCaseInterface`, `KeyValueStoreInterface`, `HttpClientInterface` / `FetchHttpClient`,
the HTTP error mapper, the DI container and the translation parity test. Nothing is copied into `.stpr/template/`
by this change; the first copies come from login.

## 6. Held State

- `ContainerProvider` holds the container built at startup. It never changes during the app's life.
- i18next holds the current language, detected once at startup. No language switcher in this change.
- No other state.

## 7. Screens And Navigation

| Front      | Route                     | View       | Content                                   |
| ---------- | ------------------------- | ---------- | ----------------------------------------- |
| backoffice | `/` (`src/app/page.tsx`)  | `HomeView` | MUI `Typography` with `common:home.title` |
| app        | `/` (`src/app/index.tsx`) | `HomeView` | Paper `Text` with `common:home.title`     |

The layouts render `AppProviders` around `children` (backoffice, inside `<html><body>`) or around `<Stack />`
(app). The create-next-app and create-expo-app scaffold content and assets are removed.

## 8. API Contract

- Source: `GET http://localhost:5199/openapi/v1.json`, which the API serves only in Development.
- `core` scripts:
  - `openapi:refresh` downloads the document to `core/openapi/v1.json` (needs the API running);
  - `openapi:generate` runs `openapi-typescript` from the committed snapshot into `core/src/domain/generated/api.ts`.
- Both the snapshot and the generated file are committed. `typecheck` does not need the API.
- No alias files are created in this change; they arrive with each domain area, starting with `auth` in login.

## 9. Errors And Copy

The API's `ProblemDetails` carries `status` and an English `detail`, with no machine-readable code. The mapper
therefore maps by status:

| Source                          | `DomainError` code |
| ------------------------------- | ------------------ |
| 400                             | `VALIDATION`       |
| 401                             | `UNAUTHORIZED`     |
| 403                             | `FORBIDDEN`        |
| 404                             | `NOT_FOUND`        |
| 409                             | `CONFLICT`         |
| any other HTTP status           | `UNEXPECTED`       |
| `fetch` rejects (no response)   | `NETWORK`          |
| response body is not valid JSON | `UNEXPECTED`       |

`detail` is kept as the `DomainError` message for logs, never shown to the user. Distinguishing errors that share a
status (for example `WorkoutLockedException` and `EmailAlreadyExistException`, both 409) needs the API to add a code
to `ProblemDetails`. That is a separate API change, not planned here.

Copy (proposed, both fronts; `common` and `errors` namespaces):

| Key                   | es                                                    | en                                               |
| --------------------- | ----------------------------------------------------- | ------------------------------------------------ |
| `common:home.title`   | Weight Tracker                                        | Weight Tracker                                   |
| `errors:VALIDATION`   | Revisa los datos introducidos.                        | Please check the data you entered.               |
| `errors:UNAUTHORIZED` | Tu sesión no es válida. Vuelve a iniciar sesión.      | Your session is not valid. Please sign in again. |
| `errors:FORBIDDEN`    | No tienes permiso para realizar esta acción.          | You are not allowed to do this.                  |
| `errors:NOT_FOUND`    | No hemos encontrado lo que buscas.                    | We couldn't find what you were looking for.      |
| `errors:CONFLICT`     | La operación entra en conflicto con el estado actual. | This action conflicts with the current state.    |
| `errors:NETWORK`      | No hay conexión con el servidor.                      | Can't reach the server.                          |
| `errors:UNEXPECTED`   | Ha ocurrido un error inesperado.                      | Something went wrong.                            |

## 10. Technical Decisions

Formatting and linting

- Prettier at the root, one config for all workspaces: double quotes, semicolons, trailing commas, `printWidth` 100.
  The app's single-quoted scaffold is reformatted.
- ESLint flat config per workspace with `eslint-config-prettier`:
  - `core`: `typescript-eslint`;
  - `backoffice`: the existing `eslint-config-next` config;
  - `app`: `eslint-config-expo`.
- `no-restricted-imports` rules enforce the dependency rule:
  - `core/src/domain` may not import `application` or `infrastructure`;
  - `core/src/application` may not import `infrastructure`;
  - `core` may not import `react`, `react-native`, `next`, `expo*`, `@mui/*` or `react-native-paper`;
  - `<front>/src/presentation` may not import `@weight-tracker/core/infrastructure`, nor `@/infrastructure/*`
    except `@/infrastructure/container` from `app-providers.tsx`.
- Root `lint` runs `turbo run lint` and then `prettier --check .`; root `format` runs `prettier --write .`.

Exports and naming

- Every file default-exports its main symbol; barrels re-export by name. `result.ts` keeps named exports.
- `export type` / `import type` are kept as `verbatimModuleSyntax` requires.

DI container

- Hand-written in `core/src/infrastructure/di/container.ts`, implementing `ContainerInterface`. No decorators, no
  `reflect-metadata`, no dependency.
- `core/src/application/dependencies.ts` declares `interface Dependencies`, mapping a key to each port and use case
  type (today: `httpClient`, `keyValueStore`, `notificationService`).
- Each front builds its container with `createContainer<Dependencies>(registrations)`, where `registrations` maps
  every key to a factory receiving the container. A missing key or a wrong type fails to compile, so `resolve(key)`
  cannot fail at runtime and nothing needs to throw.
- Factories are lazy singletons; `transient(factory)` opts a key out.
- Because the key set is `Dependencies`, both fronts register every key, including use cases only one front shows.
  This keeps one shared contract at the cost of a few unused registrations.
- Until login, `keyValueStore` and `notificationService` are registered as in-memory or no-op stand-ins in
  `<front>/src/infrastructure/` so the container compiles. Login replaces `keyValueStore`.

Backoffice

- `next.config.ts`: `output: "export"`, keep `transpilePackages`, remove `cacheComponents` and `partialPrefetching`
  (both need a server runtime). If the Next 16 docs show `output: "export"` cannot run without them, stop and
  re-plan.
- MUI: `@mui/material`, `@emotion/react`, `@emotion/styled`, and `@mui/material-nextjs` for the App Router style cache.
  A default theme in `src/presentation/common/theme.ts`. `globals.css`, `page.module.css` and `public/*.svg` are removed.
- i18n: `i18next` + `react-i18next`, language from `navigator.languages`, no extra detector package.

App

- React Native Paper with its default MD3 theme in `src/presentation/common/theme.ts`, added with `expo install`.
  It needs no native code beyond what Expo Go ships.
- i18n: `i18next` + `react-i18next` + `expo-localization`.
- The scaffold `src/app/index.tsx` content is replaced by the placeholder route.

Testing

- `core`: Vitest, `core/test` mirroring `core/src`.
- `backoffice`: Vitest + `@testing-library/react` + `jsdom`, `backoffice/test`; Playwright in `backoffice/e2e`,
  served from the static export (`next build`, then a static server), Chromium only.
- `app`: Jest + `jest-expo` + `@testing-library/react-native`, `app/test`.
- Root `test` runs every unit and component suite through Turbo. `test:e2e` is separate, because it needs a build,
  and is required only when a plan names an E2E flow.

## 11. Implementation Constraints

- Read the Next 16 docs in `backoffice/node_modules/next/dist/docs/` before touching `next.config.ts`, layouts or
  MUI integration; read the Expo SDK 57 docs before adding Paper, `expo-localization` or Jest.
- Add app dependencies with `pnpm exec expo install`.
- Keep `@/*` path aliases as they are.
- Keep the `http://localhost:5199` fallback and the env variable names.
- Do not widen `core/tsconfig.json` (`lib: ["ES2022"]`, `types: []`).
- Keep the `next dev` block in `backoffice/AGENTS.md` untouched.

## 12. Minimum Test Coverage

- `core/test/infrastructure/http/http-error-mapper.test.ts`: every row of the table in section 9.
- `core/test/infrastructure/http/fetch-http-client.test.ts`: bearer header added when a token exists, JSON body,
  `HttpError` on a non-2xx response.
- `core/test/infrastructure/di/container.test.ts`: singleton returns the same instance, transient a new one,
  a factory receives the container to resolve its own dependencies.
- `<front>/test/infrastructure/container.test.ts`: every key of `Dependencies` resolves.
- `<front>/test/i18n/translation-parity.test.ts`: every key in `es` exists in `en` and the other way round.
- `<front>/test/presentation/common/views/home-view.test.tsx`: renders the `es` title.
- `backoffice/e2e/home.spec.ts`: the static build serves `/` with the title. This proves the E2E pipeline.
- All tests follow the Arrange / Act / Assert convention.

## 13. Review Checkpoints

- `pnpm typecheck && pnpm lint && pnpm test` passes from `front/`; `pnpm test:e2e` passes.
- `pnpm --filter backoffice build` produces a static `out/` with no server output.
- A deliberate forbidden import (for example `@weight-tracker/core/infrastructure` from a view) fails `pnpm lint`;
  it is tried and reverted during review.
- No scaffold content remains in either front.
- `.stpr/review/CHECKLIST_REVISION.md` architecture, naming and stack sections hold.
- `front/README.md` reflects the new scripts.
