# Change Proposal: Add User Profile Use Cases

Source brief for `/opsx:propose`. Artifacts go to `openspec/changes/add-user-profile-usecases/`.

## Change

Bring the API's `GET /users/me` and `PUT /users/me` into `core`: refresh the contract snapshot, add the `users` domain
aliases, a `UserRepository`, and two use cases, `GetMyProfileUseCase` and `UpdateMyProfileUseCase`. Both fronts register
them. No screen is added.

## Objective

The API now lets an authenticated user read and edit their own profile (id, email, name, surname, role). The fronts have
no way to call it: the snapshot does not list the routes, and `core` has no users area. The header's user menu shows a
placeholder user (`useCurrentUser`) because of exactly this gap.

After this change `core` offers both operations through use cases that return `Result` and never throw, so a later
change can replace the placeholder and add a profile screen without touching `core`.

## Capabilities

- Read the signed-in user's profile through a use case: `id`, `email`, `name`, `surname`, `role`.
- Replace the signed-in user's `name` and `surname` through a use case, and get the updated profile back.
- Every API failure comes back as a `DomainError` code, with the API's validation detail kept in the message.
- Both use cases are available from each front's container, ready for presentation to use.

## Fronts And Roles

- backoffice -> lands here: wiring only -> roles: `TRAINER`, `ADMIN` -> the container registers the repository and the
  two use cases. No screen, hook or copy changes.
- app -> lands here: wiring only -> roles: `USER` -> same registrations. No screen, hook or copy changes.

The API allows every authenticated role to read and edit **only their own** profile, because the route has no id and acts
on the token's user. The front adds no role check and has nothing to hide. There is no way to ask for another user's
profile, and `core` must not offer one.

## Classification

Contract:

- Snapshot refresh + regeneration -> `core/openapi/v1.json` and `core/src/contract/generated/api.ts` -> its own step,
  reviewed on its own (see API Contract).

Core:

- `UserProfile` (alias of `UserProfileResponse`) and `UpdateProfileRequest` -> entity types -> `core/src/domain/users/user.ts`
- `UserRepositoryInterface` (`getMe`, `updateMe`) -> repository contract -> `core/src/application/ports/user-repository-interface.ts`
- `UserRepository` -> repository implementation -> `core/src/infrastructure/repositories/user-repository.ts`
- `GetMyProfileUseCase` -> use case -> `core/src/application/use-cases/users/get-my-profile-use-case.ts`
- `UpdateMyProfileUseCase` -> use case -> `core/src/application/use-cases/users/update-my-profile-use-case.ts`
- `Dependencies` keys `userRepository`, `getMyProfileUseCase`, `updateMyProfileUseCase` -> composition contract -> `core/src/application/dependencies.ts`
- Barrels re-export the new types, port, repository and use cases (domain, ports, repositories, `use-cases/users`)

Backoffice and app:

- Registrations of the three keys -> composition root -> `backoffice/src/infrastructure/container.ts` and `app/src/infrastructure/container.ts`
- Placeholders for the three keys -> test helper -> `backoffice/test/support/render-with-providers.tsx` and `app/test/support/render-with-providers.tsx`

The names follow the API's own (`UpdateMyProfileUseCase`, `UserProfileResponse`), and "profile" rather than "me" keeps the
domain wording readable. `getMe` / `updateMe` on the repository mirror the routes.

## Canonical Template Mapping

- Domain aliases -> `core/src/domain/auth/auth.ts` (skeleton-backed) -> aliases over `components["schemas"][...]` only,
  one area folder, re-exported from the domain barrel. No hand-written type.
- `UserRepositoryInterface` / `UserRepository` -> `auth-repository-interface.ts` and `auth-repository.ts`
  (skeleton-backed) -> same operation shape: call `HttpClientInterface`, return the response as the domain type, failures
  through `mapHttpError`. `getMe` has no body; `updateMe` sends the request unchanged.
- `GetMyProfileUseCase` -> `core/src/application/use-cases/auth/get-session-use-case.ts` (skeleton-backed) for the
  no-input `UseCaseInterface<void, …>` shape, and `login-use-case.ts` for delegating to a repository. It stores nothing.
- `UpdateMyProfileUseCase` -> `login-use-case.ts` (skeleton-backed) -> input forwarded unchanged to the repository, the
  repository's `Result` returned as is.
- Core tests -> `get-session-use-case.test.ts`, `login-use-case.test.ts` and `auth-repository.test.ts` (skeleton-backed).
- Both `container.ts` and both `render-with-providers.tsx` -> extended in place, structure kept.

`.stpr/template/` and its README are not updated here. Promoting the users slice is its own step.

## Screens And Navigation

None. No route, view or component changes.

## API Contract

- `GET /users/me` -> in `core/openapi/v1.json`: **no, yet** -> response `UserProfileResponse` (`id`, `email`, `name`,
  `surname`, `role`); failures `401`.
- `PUT /users/me` -> **no, yet** -> request `UpdateProfileRequest` (`name`, `surname`), response `UserProfileResponse`;
  failures `400`, `401`.

The routes are implemented by the API change `add-users-me-endpoints` (`api/openspec/changes/add-users-me-endpoints/`),
which is not archived yet. This change depends on it and refreshes the snapshot: run the API (Debug API, port 5199) from
that code, then `pnpm --filter @weight-tracker/core openapi:refresh` and `openapi:generate`. Review the diff of
`core/openapi/v1.json` and the generated types. It must contain only the two routes and their two schemas; anything else
means the API drifted, and the change stops there. The refresh is its own commit-sized step, before any hand-written
code. If the generated schema names differ from the two above, the aliases follow the generated names.

The API takes `name` and `surname` as required strings. It trims them, rejects blanks and values over 100 characters
with `400`, and ignores any other field (`email`, `role`) sent in the body. Those limits are business rules owned by the
API.

## Held State

None. Neither use case stores anything, and no provider or hook is added. In particular they do not touch the stored
tokens, and they do not cache the profile.

## Forms And Structural Validation

None here. A future profile form would mirror the API's declared limits (required, maximum 100). The snapshot may not
declare the 100 limit, in which case the form change decides, not this one. `core` adds no validation.

## Errors And Copy

- Missing or invalid token -> `UNAUTHORIZED` (401) -> already mapped and translated (`errors:UNAUTHORIZED`).
- Blank, missing or too long `name` / `surname` on `PUT` -> `VALIDATION` (400) -> already mapped (`errors:VALIDATION`).
- No response -> `NETWORK` -> already mapped.
- Any other status -> `UNEXPECTED` -> already mapped.

No new error code and no HTTP error mapper change. No new copy keys.

## Platform Capabilities And Dependencies

None. No new port and no new package. The one existing port this relies on is the HTTP client's access token (see Notes).

## Out Of Scope

- Any screen, hook or provider. `useCurrentUser` keeps returning the placeholder until a later change swaps it for
  `GetMyProfileUseCase`.
- Sending the access token (see Notes), reactive refresh on `401`, and `RefreshSessionUseCase`.
- Changing email, role or password, and reading or editing another user's profile. The API does not offer them.
- Caching the profile, or holding it in a session provider.
- Updating `.stpr/template/` or its README.
- Must not change: the API, the HTTP error mapper, the auth use cases, the stored tokens.

## Notes

- **Neither front sends the token today.** `FetchHttpClient` adds `Authorization: Bearer …` only when its
  `getAccessToken` option is provided, and both `container.ts` files construct it with `baseUrl` only. The auth routes
  are public, so nothing has needed it. `/users/me` is protected: calling these use cases from a front as wired today
  answers `401`. This change does not wire it, because it is front infrastructure with its own test and decision (where
  the token is read from, and what a `401` should do). It is the first thing the follow-up that uses the profile has to
  add, and the design records it as a known prerequisite. Core tests are unaffected, since they fake `HttpClientInterface`.
- **Builds on the API change `add-users-me-endpoints`.** Start only after that API is implemented and runnable, since the
  snapshot comes from it. The front change is independent of when the API change is archived.
- **No stop case.** The routes will be in `core/openapi/v1.json` once refreshed, no domain type is hand-written, and no
  business rule enters the front. If the refresh does not show the routes, stop: that is an API problem.
- **Verification tiers:**
  - core unit: `get-my-profile-use-case.test.ts`: returns the repository's profile; returns the repository's failure
    unchanged; calls `getMe` once and nothing else.
  - core unit: `update-my-profile-use-case.test.ts`: forwards `name` and `surname` unchanged; returns the updated profile;
    returns a failure unchanged (`VALIDATION`, `UNAUTHORIZED`).
  - core unit: `user-repository.test.ts`, following `auth-repository.test.ts`: `getMe` sends `GET /users/me` with no
    body; `updateMe` sends `PUT /users/me` with the body unchanged; success returns the response; `400` → `VALIDATION`,
    `401` → `UNAUTHORIZED`, `500` → `UNEXPECTED`, network failure → `NETWORK`.
  - front tiers: no component or E2E test, since nothing visible changes. The existing suites must still pass with the new
    keys registered and the new placeholders in place.
  - container tests: `backoffice/test/infrastructure/container.test.ts` and `app/test/infrastructure/container.test.ts`
    gain the three new keys wherever they list or resolve the registrations.
- Done means `pnpm typecheck && pnpm lint && pnpm test` passes from `front/`. No E2E flow is planned.
- No deviation from a skeleton reference is allowed.
