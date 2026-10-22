# Proposal

Source brief: `.stpr/plan/openspec/proposal-change-add-signup-usecase.md`.

## Why

Athletes will sign up from the app and trainers from the backoffice. The API already exposes `POST /auth/signup`,
but `core` has no `Auth` domain area, so neither front can call it. This change adds the operation to `core` and
makes it resolvable from each front's container, so later screen changes only need to build UI.

## What Changes

- Add the `Auth` domain area to `core`: domain aliases over the generated `SignupRequest`, `AuthResponse` and
  `Role`; an `AuthRepositoryInterface` port with a `signup` method; its `AuthRepository` implementation over
  `HttpClientInterface`; and a `SignupUseCase`.
- The role is fixed when the use case is registered. The caller passes only email and password.
- Add `authRepository` and `signupUseCase` to `Dependencies` and register them in both fronts' `container.ts`:
  the app with role `USER`, the backoffice with role `TRAINER`.
- No UI change. No route, view, component, state hook or translation is added or changed in either front.
- No API change, no snapshot refresh and no new error code.

## Capabilities

### New Capabilities

- `account-signup`: creating an account through the API, with the role decided by the front that serves it, and the
  outcome returned as a `Result` with a `DomainError` code on failure.

### Modified Capabilities

None.

## Fronts And Roles

- **app**: lands as wiring only. It signs up `USER` accounts and cannot sign up `TRAINER` or `ADMIN`.
- **backoffice**: lands as wiring only. It signs up `TRAINER` accounts and cannot sign up `USER` or `ADMIN`.
- No screen exists, so there is nothing to show or hide per role.

## Canonical References

Every reference this change needs is `pending` (login slice) in `.stpr/template/README.md`, so **this change
establishes the pattern**:

- `Auth` aliases: established here, following the alias example in `STPR_INVARIANTS.md` ("Domain: anemic, generated").
- `AuthRepositoryInterface` / `AuthRepository`: established here.
- `SignupUseCase`: established here on `UseCaseInterface` (skeleton-backed).
- `Dependencies` and both `container.ts`: skeleton-backed. Extend them and keep their structure.
- Use case and repository tests: established here. Structure follows
  `core/test/infrastructure/http/http-error-mapper.test.ts` and the AAA rules.

The login change then builds on these files. `.stpr/template/` is not updated here.

## Constraints And Conflicts

- **Role names are not in the snapshot.** `Role` is a bare `integer`. A hand-written role enum would be a
  hand-written domain type (stop case), so each container passes the numeric value (`1` USER, `2` TRAINER) with a
  comment. Getting named roles is a later API change.
- **Not a business rule.** Fixing the role per front applies the product split from the invariants. The API still
  decides which roles are allowed and rejects `ADMIN` with `403`.
- **Fronts must change.** `Registrations<Dependencies>` needs a factory for every key, so both `container.ts` files
  must register the new keys. No other front file changes.
- No other conflict with the template, the invariants or a stop case.

## API Contract

- `POST /auth/signup` is in `core/openapi/v1.json`. It takes `SignupRequest` (`email`, `password`, `role`) and
  returns `AuthResponse` (`accessToken`, `refreshToken`, `expiresIn`).
- The snapshot documents only `400`. The API also returns `401` (malformed email), `403` (`ADMIN`) and `409`
  (email taken). All of them map through the existing HTTP error mapper.

## Acceptance Criteria

- The app's `signupUseCase` sends role `USER`, and the backoffice's sends role `TRAINER`, whatever the caller does.
- On success, the use case returns the API's `AuthResponse` unchanged and stores nothing.
- On failure it returns `VALIDATION`, `UNAUTHORIZED`, `FORBIDDEN`, `CONFLICT` or `NETWORK`, and never throws.
- Core unit tests cover the use case and the repository, with the API mocked through `HttpClientInterface`.
- `pnpm typecheck && pnpm lint && pnpm test` passes from `front/`.

## Impact

- `core`: new `src/domain/auth`, `src/application/ports/auth-repository-interface.ts`,
  `src/application/use-cases/auth`, `src/infrastructure/repositories`, their barrels, and `dependencies.ts`; new
  tests under `core/test`.
- `backoffice` and `app`: only `src/infrastructure/container.ts` and the component-test helper `test/support/render-with-providers.tsx`, which
  also builds a full `Registrations<Dependencies>`.
- No new dependency. No API change.
