# Design

## Context

See proposal.md for motivation and scope, and the `account-profile` spec for the required behaviour.

- `core` has one domain area, `auth`. It has aliases in `domain/auth/auth.ts`, the `AuthRepositoryInterface` port,
  `AuthRepository` over `HttpClientInterface`, and use cases in `application/use-cases/auth`.
- `AuthRepository` calls `httpClient.request<T>({ method, path, body })`, returns `ok(response)`, and turns any throw
  into `fail(mapHttpError(error))`. `mapHttpError` already maps 400/401/403/404/409, `NETWORK` and `UNEXPECTED`, and
  keeps a ProblemDetails `detail` as the message.
- The snapshot is refreshed by `pnpm --filter @weight-tracker/core openapi:refresh` (it fetches
  `http://localhost:5199/openapi/v1.json`, or `OPENAPI_URL`). Then `openapi:generate` rewrites
  `src/contract/generated/api.ts`.
- On the API side, `add-users-me-endpoints` declares `GET /users/me` (`200` `UserProfileResponse`, `401`) and
  `PUT /users/me` (body `UpdateProfileRequest`, `200` `UserProfileResponse`, `400` `ValidationProblemDetails`, `401`).
  `UserProfileResponse` serialises `role` as a string (`Role.ToString()`), unlike `SignupRequest.role`, which is an
  integer.
- `FetchHttpClient` sends a Bearer token only through the optional `getAccessToken`, which neither front passes today.

## Goals / Non-Goals

**Goals:**

- A `users` area in `core` that mirrors `auth` file by file, so the next area copies the same shape.
- The snapshot refresh lands as its own step, before any code that depends on it.

**Non-Goals:**

- Sending the access token from the fronts, refresh-on-401, caching the profile, or any UI.

## Decisions

### Step order: snapshot first

1. Run the API from the `add-users-me-endpoints` code (Debug API, port 5199).
2. `openapi:refresh`, then `openapi:generate`.
3. Review the diff. It must only add `/users/me` (get, put) and the schemas `UserProfileResponse` and
   `UpdateProfileRequest` (plus `ValidationProblemDetails` only if it was not already there). Any other difference
   means the running API is not the expected one, so the change stops there.

Nothing hand-written is created before step 3. Then the aliases use whatever names the generator produced.

### Domain: `core/src/domain/users/user.ts`

```ts
import type { components } from "../../contract/generated/api";

export type UserProfile = components["schemas"]["UserProfileResponse"];
export type UpdateProfileRequest = components["schemas"]["UpdateProfileRequest"];
```

- `UserProfile` drops the transport suffix `Response`, like `AuthResponse` would if renamed. The request keeps its
  generated name, as `LoginRequest` does.
- `role` stays as generated (a string). It is not unified with the integer `Role` alias from `auth`: they are different
  schemas in the contract, and mapping between them would be a hand-written rule.
- Re-exported as types from `domain/index.ts`.

### Port and repository

- `UserRepositoryInterface` in `application/ports/user-repository-interface.ts`:
  - `getMe(): Promise<Result<UserProfile, DomainError>>`
  - `updateMe(request: UpdateProfileRequest): Promise<Result<UserProfile, DomainError>>`
  - Re-exported from `ports/index.ts`.
- `UserRepository` in `infrastructure/repositories/user-repository.ts`, a copy of `AuthRepository`'s method shape:
  - `getMe` → `{ method: "GET", path: "/users/me" }`, with no `body` key, so `FetchHttpClient` sends no
    `Content-Type`;
  - `updateMe` → `{ method: "PUT", path: "/users/me", body: request }`.
  - Re-exported from `repositories/index.ts`.
- _Why `getMe` / `updateMe`:_ the repository mirrors the transport (the route). The use cases carry the domain name
  (profile).

### Use cases: `application/use-cases/users/`

- `GetMyProfileUseCase implements UseCaseInterface<void, UserProfile>`: takes `userRepository`, and `execute()`
  returns `this.userRepository.getMe()`.
- `UpdateMyProfileUseCase implements UseCaseInterface<UpdateProfileRequest, UserProfile>`: takes `userRepository`,
  and `execute(request)` returns `this.userRepository.updateMe(request)`.
- Both depend only on the repository, never on `KeyValueStoreInterface`, which is how the "not stored" requirement
  holds by construction.
- There is no `try/catch`. The repository already never throws, as with `LoginUseCase`'s repository call.
- The barrel `use-cases/users/index.ts` is re-exported from `use-cases/index.ts`.
- _Alternative rejected:_ letting presentation call the repository directly. The invariants require every API
  interaction to go through a use case, including simple reads.

### Composition

- `Dependencies` gains:
  - `userRepository: UserRepositoryInterface`
  - `getMyProfileUseCase: UseCaseInterface<void, UserProfile>`
  - `updateMyProfileUseCase: UseCaseInterface<UpdateProfileRequest, UserProfile>`
- Both `container.ts` register, next to `authRepository` and after the auth use cases:
  - `userRepository: (c) => new UserRepository(c.resolve("httpClient"))`
  - `getMyProfileUseCase: (c) => new GetMyProfileUseCase(c.resolve("userRepository"))`
  - `updateMyProfileUseCase: (c) => new UpdateMyProfileUseCase(c.resolve("userRepository"))`
- Both `render-with-providers.tsx` gain placeholders:
  - `userRepository: () => ({ getMe: async () => undefined as never, updateMe: async () => undefined as never })`
  - `getMyProfileUseCase` and `updateMyProfileUseCase`, each `() => ({ execute: async () => undefined as never })`

  This follows the existing placeholders for the auth keys.

- Both `container.test.ts` add the two use case keys to their `keys` list.

### Errors

Only the existing codes, through `mapHttpError`. The `400` body is ASP.NET `ValidationProblemDetails`, which carries a
`title` and an `errors` map but usually no `detail`. In that case the message falls back to the `HttpError` message,
which is fine because presentation translates by code, never by message. No new code, no mapper change.

### Tests

- `core/test/infrastructure/repositories/user-repository.test.ts`, shaped like `auth-repository.test.ts` (same
  `FakeHttpClient`):
  - `getMe`: sends exactly `{ method: "GET", path: "/users/me" }`, and success returns the profile;
  - `updateMe`: sends `{ method: "PUT", path: "/users/me", body }` with the body unchanged, and success returns the
    profile;
  - for both: `400` → `VALIDATION` (with a `detail` kept), `401` → `UNAUTHORIZED`, `500` → `UNEXPECTED`, and a
    rejected request (no `HttpError`) → `NETWORK`.
- `core/test/application/use-cases/users/get-my-profile-use-case.test.ts` and `update-my-profile-use-case.test.ts`, with
  a fake `UserRepositoryInterface`:
  - success returns the repository's value;
  - the input is forwarded unchanged (update);
  - the repository is called once;
  - a failure is returned unchanged.
- Container tests in both fronts resolve the new keys.
- No component or E2E test: nothing visible changes.

## Risks / Trade-offs

- [Both use cases answer `UNAUTHORIZED` from a real front until the token is wired] → Recorded in the proposal as the
  first prerequisite of the change that uses the profile. That change adds `getAccessToken` to each front's
  `FetchHttpClient` (reading the stored access token through a port, never presentation) with its own test.
- [The snapshot is taken from an API change that is not archived] → If that API change is revised before archiving,
  the refresh is repeated and the aliases follow. Until then the generated types match the running code.
- [`role` is a string in `UserProfile` and an integer in `SignupRequest`] → Kept as the contract states. Presentation
  translates `UserProfile.role` directly (`TRAINER`, `ADMIN`, `USER`), which already matches the header's role keys.
