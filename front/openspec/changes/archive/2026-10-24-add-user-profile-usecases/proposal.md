# Proposal

Source brief: `.stpr/plan/openspec/proposal-change-add-user-profile-usecases.md`.

## Why

The API now lets an authenticated user read and edit their own profile through `GET /users/me` and `PUT /users/me`.
The fronts cannot call either one: the contract snapshot does not list the routes, and `core` has no users area. The
backoffice header shows a placeholder user (`useCurrentUser`) because of exactly this gap. This change brings both
operations into `core` as use cases, so a later change can show real data and add a profile screen without touching
`core`.

## What Changes

- Contract: refresh `core/openapi/v1.json` from the API and regenerate `core/src/contract/generated/api.ts`. This is its
  own reviewed step, and its diff must add only the two routes and their schemas.
- Core: a new `users` domain area with the `UserProfile` and `UpdateProfileRequest` aliases over the generated schemas.
- Core: `UserRepositoryInterface` (`getMe`, `updateMe`) and its `UserRepository` implementation over
  `HttpClientInterface`, with failures mapped by `mapHttpError`.
- Core: `GetMyProfileUseCase` (`UseCaseInterface<void, UserProfile>`) and `UpdateMyProfileUseCase`
  (`UseCaseInterface<UpdateProfileRequest, UserProfile>`).
- `Dependencies` gains `userRepository`, `getMyProfileUseCase` and `updateMyProfileUseCase`. Both fronts register them,
  and both test helpers gain placeholders.
- No screen, hook, copy, error code or dependency changes.

## Capabilities

### New Capabilities

- `account-profile`: the core operations that read and replace the signed-in user's own profile through the API,
  returning every failure as a `DomainError` code.

### Modified Capabilities

None.

## Fronts And Roles

- **backoffice** (`TRAINER`, `ADMIN`) and **app** (`USER`): wiring only. Both containers register the repository and
  the two use cases. Nothing visible changes.
- Every authenticated role may read and edit only its own profile. The routes have no id and act on the token's user.
  `core` offers no way to ask for another user's profile and adds no role check.

## Canonical References

- Domain aliases: `core/src/domain/auth/auth.ts` (skeleton-backed). Aliases over `components["schemas"]` only.
- `UserRepositoryInterface` / `UserRepository`: `auth-repository-interface.ts` / `auth-repository.ts` (skeleton-backed).
- `GetMyProfileUseCase`: `get-session-use-case.ts` for the no-input shape and `login-use-case.ts` for delegating to a
  repository. `UpdateMyProfileUseCase`: `login-use-case.ts`.
- Tests: `auth-repository.test.ts`, `login-use-case.test.ts`, `get-session-use-case.test.ts`, and both
  `container.test.ts` (skeleton-backed).
- Both `container.ts` and both `render-with-providers.tsx`: extended in place.
- `.stpr/template/` is not updated here.

## Constraints And Conflicts

- **Depends on the API change `add-users-me-endpoints`.** It is implemented in `api/` but not archived. The snapshot
  is taken from that running API. If the refresh does not show both routes, or shows anything else, stop: that is an
  API problem, not a front one.
- **The fronts do not send the access token yet.** `FetchHttpClient` adds `Authorization: Bearer …` only when given
  `getAccessToken`, and neither `container.ts` passes it. Called from a front today, both use cases would get `401`
  (`UNAUTHORIZED`). Wiring the token is front infrastructure with its own decisions, so it is out of scope here and
  recorded as the first prerequisite of the change that uses the profile. Core tests fake `HttpClientInterface` and
  are unaffected.
- **No business rule in the front.** The API trims names, requires them, limits them to 100 characters and ignores
  `email` and `role` in the body. `core` forwards the request unchanged and validates nothing.
- No conflict with the template, the invariants or a stop case.

## API Contract

- `GET /users/me`: not in the snapshot yet, added by the refresh. Response `UserProfileResponse` (`id`, `email`,
  `name`, `surname`, `role`). Failure `401`.
- `PUT /users/me`: not in the snapshot yet, added by the refresh. Request `UpdateProfileRequest` (`name`, `surname`),
  response `UserProfileResponse`. Failures `400` and `401`.

If the generated schema names differ, the aliases follow the generated names. The snapshot is the source of truth.

## Acceptance Criteria

- `core/openapi/v1.json` and the generated types list both routes and both schemas, and nothing else changed.
- Reading the profile returns the API's profile unchanged.
- Updating the profile sends `name` and `surname` unchanged and returns the updated profile.
- `400` maps to `VALIDATION`, `401` to `UNAUTHORIZED`, no response to `NETWORK`, any other failure to `UNEXPECTED`.
- No hand-written domain type, and no hand edit under `core/src/contract/generated`.
- Both containers resolve the three new keys.
- `pnpm typecheck && pnpm lint && pnpm test` passes from `front/`, with the API faked in every tier.

## Impact

- `core`: `openapi/v1.json`, `src/contract/generated/api.ts`, new `src/domain/users/user.ts`,
  `src/domain/index.ts`, new `src/application/ports/user-repository-interface.ts`, `src/application/ports/index.ts`,
  new `src/application/use-cases/users/` (two use cases and a barrel), `src/application/use-cases/index.ts`,
  `src/application/dependencies.ts`, new `src/infrastructure/repositories/user-repository.ts`, its barrel, and tests.
- `backoffice` and `app`: `src/infrastructure/container.ts`, `test/support/render-with-providers.tsx`,
  `test/infrastructure/container.test.ts`.
- No new dependency. No API change.
