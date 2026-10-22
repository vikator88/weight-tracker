# Manual review checklist

Use this checklist during the `Review` phase, after code generation and test execution, to manually verify that the
change stays aligned with the template and conventions.

This checklist is a guide for human review. AI may help inspect the codebase, compare files, and summarize findings,
but final validation remains manual.

**Reference:** Compare against **`.stpr/template/`** as the source of truth.

---

## Architecture

- [ ] The dependency rule holds: `core/domain` imports nothing, `core/application` only `domain`, `core/infrastructure`
      only `domain` and `application`.
- [ ] `core` imports no front, React, Next, Expo, React Native or UI library.
- [ ] `core/domain` and `core/application` use no platform globals, and their `tsconfig` was not widened.
- [ ] `backoffice` and `app` do not import each other and share no UI, styles, translations or presentation code.
- [ ] `src/presentation` never imports `@weight-tracker/core/infrastructure`; only `src/infrastructure` does.
- [ ] No business rule was added to the front; form validation is structural only and mirrors the API's limits.

## Structure

- [ ] Code sits in the layer and domain-area folder its building block requires (`STPR_INVARIANTS.md`, "Workspace layout").
- [ ] Files under `src/app` are routes only: each imports one view and default-exports it.
- [ ] Views, components, hooks and context live under `src/presentation/<area>` or `src/presentation/common`.
- [ ] Platform adapters and `container.ts` live under the front's `src/infrastructure`.

## Domain

- [ ] Entity types are aliases of generated OpenAPI types; no domain type was hand-written.
- [ ] `core/src/contract/generated` was not edited by hand, and any snapshot refresh is its own reviewed step.
- [ ] The domain holds no behaviour and no validation.

## Application And Infrastructure

- [ ] Every API interaction goes through a use case, including reads.
- [ ] Each use case is a class implementing `UseCaseInterface` with one public `execute` returning `Result`.
- [ ] Use cases take dependencies through the constructor and do not reference the container.
- [ ] Repositories return the generated types as they are, with no DTO-to-domain mapping.
- [ ] Repositories convert `HttpError` and network failures through the single HTTP error mapper.
- [ ] The only `throw` is `FetchHttpClient` raising `HttpError`, plus the wiring guard in `useContainer`; no use
      case, repository, other hook or view throws.
- [ ] New repositories, adapters and use cases are registered in each affected front's `container.ts`.
- [ ] Token refresh happens only through `RefreshSessionUseCase`, never in an HTTP interceptor.

## Presentation

- [ ] Presentation reaches use cases through the container only.
- [ ] State is held in Context API providers exposed through custom hooks; no other state library was added.
- [ ] Failed `Result` values are rendered as UI; no `try/catch` is used for flow control.
- [ ] No user-visible string is hard-coded; every new key exists in `es` and `en`.
- [ ] `DomainError` codes are shown through `errors.<code>` translations.
- [ ] Each front's behaviour for the feature matches what the plan states for that front and role.

## Stack

- [ ] Backoffice: `output: "export"` is kept; no server actions, route handlers, middleware or server-side data access.
- [ ] Backoffice: views are client components; the root layout renders only the shell and providers.
- [ ] Backoffice: UI uses MUI only; no CSS Modules or other styling library was added.
- [ ] App: UI uses React Native Paper and `StyleSheet` only.
- [ ] App: no file was created or edited under `ios/` or `android/`; native configuration is in `app.json` or config plugins.
- [ ] App: dependencies were added with `expo install`; any native-code library is called out in the plan.
- [ ] Core: no decorator, no `reflect-metadata`, and any new runtime dependency is named in the plan.

## Naming

- [ ] File names are kebab-case.
- [ ] Each file default-exports its main symbol and barrels re-export by name; named exports appear only in
      helper families such as `result.ts` and in type-alias files.
- [ ] Contracts end in `Interface`; implementations and data shapes do not.

## Reference parity

- [ ] Naming and structure match the template references selected in the plan.
- [ ] `.stpr/template/` is still identical to its counterparts (from `front/`):
      `for f in $(cd .stpr/template && find core backoffice app -type f 2>/dev/null); do cmp -s ".stpr/template/$f" "$f" || echo "DRIFT: $f"; done`
- [ ] Deviations from `.stpr/template/` were either corrected or captured back into the template or planning assets.

## Tests

- [ ] No test calls the real API.
- [ ] Every new use case, repository and error-mapper branch has a unit test in `core/test`.
- [ ] Every new or changed view has a component test, with use cases faked through the container, covering success
      and each failure.
- [ ] Role-dependent views cover every role branch and assert what is hidden.
- [ ] New backoffice flows have a Playwright E2E with intercepted API calls when the plan requires it.
- [ ] The translation parity test passes in each affected front.
- [ ] Tests live outside `src`, mirroring its structure.
- [ ] Every new test body is labelled `// Arrange`, `// Act`, `// Assert` (or `// Arrange & Act`), phases separated
      by a blank line, with exactly one Act.

## Verification

- [ ] `pnpm typecheck` passes.
- [ ] `pnpm lint` passes, including Prettier.
- [ ] `pnpm test` passes.

---

**Resources:** `.stpr/README.md`, `AGENTS.md`, `.stpr/template/STPR_INVARIANTS.md`, `.stpr/template/README.md`,
`.stpr/plan/prompts/`, `.stpr/plan/openspec/`, `openspec/config.yaml`.
