# Front

pnpm + Turborepo monorepo for the weight-tracker frontends. How to work here: `AGENTS.md`. Rules:
`.stpr/template/STPR_INVARIANTS.md`.

| Workspace     | Package                | Stack                                         | Users            |
| ------------- | ---------------------- | --------------------------------------------- | ---------------- |
| `core/`       | `@weight-tracker/core` | Plain TypeScript                              | both fronts      |
| `backoffice/` | `backoffice`           | Next.js static SPA (`output: "export"`) + MUI | admins, trainers |
| `app/`        | `app`                  | Expo (managed / CNG) + React Native Paper     | athletes         |

## Core layers

`core` is consumed as TypeScript source (no build step) through subpath exports:

- `@weight-tracker/core/domain` — anemic types generated from the API contract, `Result`, `DomainError`.
  No rules, no platform globals.
- `@weight-tracker/core/application` — use cases and **ports** (`HttpClientInterface`, `KeyValueStoreInterface`,
  `ContainerInterface`, ...) plus the `Dependencies` map every front's container registers. No platform globals.
- `@weight-tracker/core/infrastructure` — implementations that work on both platforms: `FetchHttpClient`, the HTTP
  error mapper, the DI `Container`.

`domain` and `application` are typechecked with `lib: ["ES2022"]` and no `types`, so any DOM, Node or React Native
API used there fails `typecheck`. ESLint enforces the dependency rule between layers. Platform-specific ports are
implemented in each front under `src/infrastructure/`, where `container.ts` wires everything.

## Commands

Requires Node `^22.13.0 || >=24` and pnpm (pinned in `packageManager`).

```bash
pnpm install
pnpm dev:backoffice   # next dev
pnpm dev:app          # expo start
pnpm typecheck
pnpm lint             # ESLint per workspace + Prettier check
pnpm format           # Prettier write
pnpm test             # core (Vitest), backoffice (Vitest + RTL), app (Jest + RNTL)
pnpm test:e2e         # backoffice Playwright against the static build; API calls intercepted
pnpm build
```

Done means `pnpm typecheck && pnpm lint && pnpm test` passes. The first `pnpm test:e2e` on a machine needs
`pnpm --filter backoffice exec playwright install chromium`.

## API contract

The domain types come from the API's OpenAPI document, committed as `core/openapi/v1.json`:

```bash
pnpm --filter @weight-tracker/core openapi:refresh    # download from the running API (OPENAPI_URL overrides)
pnpm --filter @weight-tracker/core openapi:generate   # regenerate core/src/domain/generated/api.ts
```

The API serves the document only in Development, at `http://localhost:5199/openapi/v1.json`.

Env vars: `NEXT_PUBLIC_API_URL` (backoffice), `EXPO_PUBLIC_API_URL` (app). Both fall back to `http://localhost:5199`.
