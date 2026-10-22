# Template Reference Map

This file is the entrypoint for using `.stpr/template/` with minimal context.

Use it together with:

- `STPR_INVARIANTS.md`
- the specific reference files listed below
- the reference tests only when the task changes behavior or coverage

## Goal

Use this file to choose the right reference quickly. The goal is not to load the whole template, but to open the
smallest useful set of files for the task.

Default reading order:

1. classify every piece with the building blocks in `STPR_INVARIANTS.md`
2. pick the canonical reference
3. open only the `minimal set`
4. open `optional files` only if the task needs them
5. open tests last, not first

When selecting a reference, first decide whether it is:

- `template-backed`: the implementation reference exists inside `.stpr/template/`
- `skeleton-backed`: the implementation reference lives in the workspace sources (`core/`, `backoffice/`, `app/`)

## Status

**The template is not populated yet.** The first canonical slice is **login**, delivered by its own change under
`openspec/changes/`. Until it lands, entries marked `pending` below have no reference: a change that needs one
must say so in its plan and establish the pattern there (see `STPR_INVARIANTS.md`, section 1, "Template").

The `add-signup-usecase` change already set the shape of the `Auth` domain area in the skeleton. Its rows below are
skeleton-backed: read them from the workspace sources and extend them, do not replace them:

- `core/src/domain/auth/auth.ts`
- `core/src/application/ports/auth-repository-interface.ts`
- `core/src/application/use-cases/auth/signup-use-case.ts`
- `core/src/infrastructure/repositories/auth-repository.ts`
- `core/test/application/use-cases/auth/signup-use-case.test.ts`
- `core/test/infrastructure/repositories/auth-repository.test.ts`

When the login change is approved and verified, its files are copied into `.stpr/template/` under the same paths
and every `pending` entry it covers is switched to `template-backed`.

## Canonical Items Classification

| Item                                         | Building block                       | Why                                                                 | Origin                               |
| -------------------------------------------- | ------------------------------------ | ------------------------------------------------------------------- | ------------------------------------ |
| `Result` / `DomainError`                     | Domain common                        | The only success/failure contract                                   | skeleton                             |
| `UseCaseInterface`                           | Use case contract                    | One public `execute`, returns `Result`                              | skeleton                             |
| `KeyValueStoreInterface`                     | Platform port                        | Implemented differently per front                                   | skeleton                             |
| `HttpClientInterface` / `FetchHttpClient`    | Repository transport                 | The only code that throws (`HttpError`)                             | skeleton                             |
| HTTP error mapper                            | Infrastructure                       | Single status-to-`DomainError` mapping point                        | skeleton (from setup change)         |
| DI container                                 | Infrastructure                       | Typed registration, no decorators                                   | skeleton (from setup change)         |
| `Auth` entity aliases                        | Entity type                          | Aliases over the generated OpenAPI types                            | skeleton (from `add-signup-usecase`) |
| `AuthRepositoryInterface` / `AuthRepository` | Repository contract / implementation | Per-area port over `HttpClientInterface`, errors through the mapper | skeleton (from `add-signup-usecase`) |
| `SignupUseCase`                              | Use case                             | One operation, role fixed at registration in each `container.ts`    | skeleton (from `add-signup-usecase`) |
| `LoginUseCase`                               | Use case                             | Calls a repository and a platform port, never throws                | `pending` (login)                    |
| `RefreshSessionUseCase`                      | Use case                             | Refresh-token rotation                                              | `pending` (login)                    |
| `LocalStorageKeyValueStore`                  | Platform adapter (backoffice)        | Browser-only API behind a port                                      | `pending` (login)                    |
| `SqliteKeyValueStore`                        | Platform adapter (app)               | Native storage behind a port                                        | `pending` (login)                    |
| `container.ts` (each front)                  | Composition root                     | Wires core and platform adapters                                    | `pending` (login)                    |
| `SessionProvider` / `useSession`             | State hook                           | Context state, reactive refresh-and-retry                           | `pending` (login)                    |
| Login view + form (each front)               | View / component                     | Structural validation, i18n, `Result` to UI                         | `pending` (login)                    |
| Login route (each front)                     | Route                                | Default-exports a view, nothing else                                | `pending` (login)                    |
| Use case test                                | Unit test                            | AAA, fakes for ports                                                | skeleton (from `add-signup-usecase`) |
| Repository test                              | Unit test                            | Faked `HttpClientInterface`, every error code                       | skeleton (from `add-signup-usecase`) |
| View test (each front)                       | Component test                       | Fakes through the container, success and failures                   | `pending` (login)                    |
| Login E2E (backoffice)                       | E2E test                             | Playwright with intercepted API calls                               | `pending` (login)                    |
| Translation parity test (each front)         | Unit test                            | Every key in `es` and `en`                                          | skeleton (from setup change)         |

## Template Contents

`.stpr/template/` will hold curated copies of canonical files, under the same relative paths they have in `front/`
(for example `.stpr/template/core/src/application/use-cases/auth/login-use-case.ts`). They must stay
byte-identical to their counterparts. Check parity from `front/` with:

```bash
for f in $(cd .stpr/template && find core backoffice app -type f 2>/dev/null); do
  cmp -s ".stpr/template/$f" "$f" || echo "DRIFT: $f"
done
```

Everything not copied there is skeleton-backed and must be read from the workspace sources.

## Rules

- Do not describe a skeleton-backed capability as if its canonical files existed inside `.stpr/template/`.
- When a capability is skeleton-backed, use the skeleton as the implementation reference and
  `STPR_INVARIANTS.md` as the contract.
- When a capability is skeleton-backed, preserve the local structural and typing patterns already used by that
  skeleton reference unless the plan explicitly approves a deviation.

## Quick Selection Rules

These apply once the login slice is template-backed.

- If you need a new API operation, add a use case modelled on `LoginUseCase`, a method on the domain area's
  repository contract and implementation modelled on `AuthRepository`, and register both in each front's `container.ts`.
- If a new domain area appears, add its alias file, repository contract and implementation following `Auth`.
  Never hand-write a type the snapshot lacks; that is a stop case.
- If a front needs a platform capability, add a port in `core/src/application/ports` and an adapter per front,
  modelled on the `KeyValueStore` pair.
- If a screen needs state, model it on `SessionProvider` / `useSession`.
- If you need a new screen, model the route, view and component on the login ones in the same front. The other
  front is never a reference for this front's UI.
- If a view behaves differently per role, cover every role branch and assert what is hidden.

## Minimal Context Loading

Do not start by reading all files of a reference.

- New API operation in an existing domain area:
  open the area's repository contract and implementation, one use case, one repository test, and `container.ts`
  in each affected front
- New screen:
  open the closest route, view and component in the same front, the area's translation files, and one view test
- New domain area:
  open the `Auth` alias file, `AuthRepositoryInterface`, `AuthRepository`, `LoginUseCase` and their tests
- New platform capability:
  open `KeyValueStoreInterface` and both adapters
