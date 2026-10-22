# Change Proposal: Add Signup Use Case

Source brief for `/opsx:propose`. Artifacts go to `openspec/changes/add-signup-usecase/`.

## Change

Add a `SignupUseCase` to `core` and wire it in both fronts, each one fixed to the role it serves.

## Objective

Both fronts will need to create accounts: athletes sign up from the app, trainers sign up from the backoffice. The
API already exposes `POST /auth/signup`, but `core` has no `Auth` area yet. This change adds the core pieces and
makes the operation resolvable from each front's container, so that later screen changes only need to build UI.

The role is fixed by the front, not chosen by the caller. A front cannot sign up a role it does not serve.

## Capabilities

- Sign up a new account with email and password through a use case that returns `Result` and never throws.
- The app signs up `USER` accounts only.
- The backoffice signs up `TRAINER` accounts only.

## Fronts And Roles

- backoffice -> lands here: yes, wiring only -> roles: `TRAINER` -> `signupUseCase` is registered with role
  `TRAINER`. No screen calls it yet.
- app -> lands here: yes, wiring only -> roles: `USER` -> `signupUseCase` is registered with role `USER`. No
  screen calls it yet.

Both fronts share one use case and one repository. The only difference is the role passed at registration. The use
case input has no `role` field, so presentation cannot override it. `ADMIN` is never registered in either front,
and the API rejects it anyway (`403`).

## Classification

- `Auth` aliases (`SignupRequest`, `AuthResponse`, `Role`) -> entity type -> `core/src/domain/auth/auth.ts`
- `AuthRepositoryInterface` (`signup`) -> repository contract -> `core/src/application/ports/auth-repository-interface.ts`
- `SignupUseCase` -> use case -> `core/src/application/use-cases/auth/signup-use-case.ts`
- `AuthRepository` (`signup`) -> repository implementation -> `core/src/infrastructure/repositories/auth-repository.ts`
- `Dependencies` keys `authRepository`, `signupUseCase` -> composition contract -> `core/src/application/dependencies.ts`
- Registration with role `TRAINER` -> composition root -> `backoffice/src/infrastructure/container.ts`
- Registration with role `USER` -> composition root -> `app/src/infrastructure/container.ts`
- Barrels re-export the new symbols -> `core/src/domain/index.ts`, `core/src/application/index.ts`,
  `core/src/application/ports/index.ts`, `core/src/infrastructure/index.ts`

## Canonical Template Mapping

- `Auth` aliases -> `pending` (login) -> no reference exists. This change establishes the alias-file pattern
  following the example in `STPR_INVARIANTS.md` ("Domain: anemic, generated").
- `AuthRepositoryInterface` / `AuthRepository` -> `pending` (login) -> no reference exists. This change establishes
  it: methods return `Promise<Result<T, DomainError>>`, call `HttpClientInterface.request`, and send every caught
  failure through `mapHttpError`.
- `SignupUseCase` -> `pending` (`LoginUseCase`) -> no reference exists. This change establishes it on
  `UseCaseInterface` (`core/src/application/ports/use-case-interface.ts`, skeleton-backed).
- `Dependencies` and both `container.ts` -> skeleton-backed -> extend the existing files; keep their structure.
- Use case and repository tests -> `pending` (login) -> no reference exists. Follow the structure of
  `core/test/infrastructure/http/http-error-mapper.test.ts` (skeleton-backed) and the AAA rules in the invariants.

The login change must then build on these files instead of creating its own. `.stpr/template/` is not updated by
this change: it stays empty until the login slice is approved, as `.stpr/template/README.md` states.

## Screens And Navigation

None. No route, view, component or state hook is added or changed in either front.

## API Contract

- `POST /auth/signup` -> in `core/openapi/v1.json`: yes -> request `SignupRequest` (`email`, `password`, `role`),
  response `AuthResponse` (`accessToken`, `refreshToken`, `expiresIn`).

No snapshot refresh. No API change.

## Held State

None. The use case returns the `AuthResponse` to its caller. It does not store tokens. Storing a session belongs to
the login slice (`SessionProvider`, `KeyValueStoreInterface`).

## Forms And Structural Validation

None. No form is added. The use case does not validate email or password: the API does that.

## Errors And Copy

These come from the API's `ExceptionMapper` and map through the existing `codeByStatus`:

- Weak password -> `VALIDATION` (400)
- Malformed email -> `UNAUTHORIZED` (401). The API reports it as `InvalidCredentialsException` on purpose.
- `ADMIN` role requested -> `FORBIDDEN` (403). Neither front can trigger it, since neither registers `ADMIN`.
- Email already registered -> `CONFLICT` (409)
- No response -> `NETWORK`

No new error code. No change to the HTTP error mapper. No copy keys, because no screen shows these errors yet.

The snapshot only documents `400` for this endpoint. The others come from the API source. That gap is not fixed
here.

## Platform Capabilities And Dependencies

None.

## Out Of Scope

- Any signup screen, form, route, translation or navigation in either front.
- Storing tokens or starting a session after signup. That is the login slice.
- Login, refresh and logout use cases.
- Updating `.stpr/template/`.
- Any API change, including documenting `401`/`403`/`409` and the `Role` names in the OpenAPI document.
- Must not change: any file under `backoffice/src` or `app/src` other than `src/infrastructure/container.ts`.

## Notes

- **Why the fronts change at all.** `Registrations<Dependencies>` requires a factory for every key, so adding
  `authRepository` and `signupUseCase` to `Dependencies` does not compile until both containers register them. The
  only front changes are those registrations and the matching do-nothing entries in each front's
  `test/support/render-with-providers.tsx`, which builds the same registrations for component tests.
- **Role is a constructor argument.** `new SignupUseCase(authRepository, role)`. `execute({ email, password })`
  builds the `SignupRequest` with the configured role. The input type is `Omit<SignupRequest, "role">`, derived from
  the generated type, not written by hand.
- **Role values.** The snapshot types `Role` as a bare `integer` with no names. The API enum is `USER = 1`,
  `TRAINER = 2`, `ADMIN = 3`. A hand-written `Role` enum in the domain would be a hand-written domain type, which is
  a stop case. So each container passes the literal with a comment: `1 // USER` in the app and `2 // TRAINER` in
  the backoffice. If named roles are wanted later, that is an API change that publishes the enum names, plus a
  snapshot refresh.
- **Not a business rule.** "The app signs up athletes, the backoffice signs up trainers" is the product split from
  the invariants, applied in wiring. The API still decides what roles are allowed.
- **Verification tiers:**
  - core unit: `core/test/application/use-cases/auth/signup-use-case.test.ts`, which checks that the configured role
    is sent, the caller's email and password pass through, and the repository's success and failure come back
    unchanged.
  - core unit: `core/test/infrastructure/repositories/auth-repository.test.ts`, which checks the request (method,
    path, body), the success result, and each of 400, 401, 403, 409 and network failure becoming its `DomainError`.
  - No component or E2E tests: no view changes. The registrations are checked by `pnpm typecheck`. The role value in
    each container is checked in review.
- Done means `pnpm typecheck && pnpm lint && pnpm test` passes from `front/`. No E2E is required.
- No conflict with a stop case. No deviation from a template reference is allowed: where a reference is `pending`,
  this change establishes it as described above.
