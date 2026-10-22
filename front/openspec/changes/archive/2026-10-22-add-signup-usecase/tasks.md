# Tasks

## 1. Core domain: `Auth` aliases

- [x] 1.1 Create `core/src/domain/auth/auth.ts` with the `SignupRequest`, `AuthResponse`, `Role` and `SignupCredentials` aliases from `design.md`, importing only `components` from the generated contract; verify with `pnpm --filter @weight-tracker/core typecheck`
- [x] 1.2 Re-export the aliases by name from `core/src/domain/index.ts`; verify they import from `@weight-tracker/core/domain` by running the typecheck again

## 2. Core application: port and use case

- [x] 2.1 Create `core/src/application/ports/auth-repository-interface.ts` (default-exported interface with `signup(request: SignupRequest)`) and re-export it from `ports/index.ts`; verify with the core typecheck
- [x] 2.2 Create `core/src/application/use-cases/auth/signup-use-case.ts` implementing `UseCaseInterface<SignupCredentials, AuthResponse>`, with `authRepository` and `role` as constructor arguments and the role spread after the credentials; add the `use-cases/auth/index.ts` and `use-cases/index.ts` barrels and export from `core/src/application/index.ts`; verify with the core typecheck
- [x] 2.3 Add `core/test/application/use-cases/auth/signup-use-case.test.ts` with an in-test fake repository, covering role `1` and role `2` being sent, email and password passing through unchanged, a role smuggled into the input at runtime being overridden, success returned unchanged, and failure returned unchanged; all in labelled AAA phases with one Act; verify with `pnpm --filter @weight-tracker/core test`

## 3. Core infrastructure: repository

- [x] 3.1 Create `core/src/infrastructure/repositories/auth-repository.ts` (`AuthRepository implements AuthRepositoryInterface`, `HttpClientInterface` in the constructor, `POST /auth/signup`, `ok(response)` on success, `fail(mapHttpError(error))` in the `catch`, no rethrow), add `repositories/index.ts` and export from `core/src/infrastructure/index.ts`; verify with the core typecheck
- [x] 3.2 Add `core/test/infrastructure/repositories/auth-repository.test.ts` with an in-test fake `HttpClientInterface`, covering the request method, path and body, success, an `it.each` over `400` → `VALIDATION`, `401` → `UNAUTHORIZED`, `403` → `FORBIDDEN`, `409` → `CONFLICT`, `500` → `UNEXPECTED`, a rejected request → `NETWORK`, and the problem detail kept as the message; AAA with one Act each; verify with `pnpm --filter @weight-tracker/core test`

## 4. Core composition contract

- [x] 4.1 Add `authRepository: AuthRepositoryInterface` and `signupUseCase: UseCaseInterface<SignupCredentials, AuthResponse>` to `core/src/application/dependencies.ts`; verify the core typecheck passes and that both fronts' typechecks now fail on the missing keys (expected until groups 5 and 6)
- [x] 4.2 Review core against `design.md` and `STPR_INVARIANTS.md`: kebab-case files, default exports (named for the alias file), `Interface` suffix on the contract, no `throw`, no business rule, no edit under `core/src/contract/generated`, domain imports only `components`; verify by reading the diff of `core/`

## 5. Backoffice wiring

- [x] 5.1 In `backoffice/src/infrastructure/container.ts`, register `authRepository` (`new AuthRepository(c.resolve("httpClient"))`) and `signupUseCase` (`new SignupUseCase(c.resolve("authRepository"), 2) // TRAINER`); add do-nothing `authRepository` and `signupUseCase` entries to `unusedDependencies` in `backoffice/test/support/render-with-providers.tsx`; verify with `pnpm --filter backoffice typecheck`
- [x] 5.2 Review the backoffice: the role is `2`, and `git diff --stat -- backoffice/` shows only `src/infrastructure/container.ts` and `test/support/render-with-providers.tsx` (plus the Next agent-rules block in `backoffice/AGENTS.md`, if `next dev` re-added it)

## 6. App wiring

- [x] 6.1 In `app/src/infrastructure/container.ts`, register `authRepository` the same way and `signupUseCase` (`new SignupUseCase(c.resolve("authRepository"), 1) // USER`); add the same do-nothing entries to `app/test/support/render-with-providers.tsx`; verify with `pnpm --filter app typecheck`
- [x] 6.2 Review the app: the role is `1`, and `git diff --stat -- app/` shows only `src/infrastructure/container.ts` and `test/support/render-with-providers.tsx`

## 7. Integration checks

- [x] 7.1 Run the template drift check from `AGENTS.md` and verify it prints nothing (`.stpr/template/` is untouched)
- [x] 7.2 Run `pnpm typecheck && pnpm lint && pnpm test` from `front/` and verify it passes (no E2E: the plan names no E2E flow)
