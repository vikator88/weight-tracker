# Design

## Context

See `proposal.md` (Why) and `specs/account-signup/spec.md` (behaviour).

Current state, from the sources:

- `core` has the skeleton only: `Result` / `DomainError`, `UseCaseInterface`, `HttpClientInterface` and
  `FetchHttpClient`, `mapHttpError` (`400`, `401`, `403`, `404`, `409`; anything else is `UNEXPECTED`; a rejected
  `fetch` is `NETWORK`), and the typed `Container`.
- `Dependencies` has `httpClient`, `keyValueStore` and `notificationService`. `Registrations<Dependencies>` needs a
  factory for every key, so a new key breaks compilation until both fronts register it.
- Generated contract: `SignupRequest = { email: string; password: string; role: Role }`, `Role = number`,
  `AuthResponse = { accessToken; refreshToken; expiresIn }`.
- No `Auth` reference exists in `.stpr/template/` (all `pending`, login slice). This change establishes it.

## Goals / Non-Goals

**Goals:**

- Establish the `Auth` domain area shape (aliases, port, repository, use case, tests) so the login slice adds to it.
- Make the role a wiring decision that presentation cannot reach.

**Non-Goals:**

- Any presentation code, translation, state hook or E2E.
- A shared "repository base" or error-handling helper. One repository does not justify an abstraction.

## Decisions

### File map

| Building block            | File                                                                                                                                                                                       |
| ------------------------- | ------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------ |
| Entity type (aliases)     | `core/src/domain/auth/auth.ts`                                                                                                                                                             |
| Repository contract       | `core/src/application/ports/auth-repository-interface.ts`                                                                                                                                  |
| Use case                  | `core/src/application/use-cases/auth/signup-use-case.ts`                                                                                                                                   |
| Repository implementation | `core/src/infrastructure/repositories/auth-repository.ts`                                                                                                                                  |
| Barrels                   | `core/src/domain/index.ts`, `core/src/application/{index,ports/index}.ts`, `core/src/application/use-cases/{index,auth/index}.ts`, `core/src/infrastructure/{index,repositories/index}.ts` |
| Composition contract      | `core/src/application/dependencies.ts`                                                                                                                                                     |
| Composition roots         | `backoffice/src/infrastructure/container.ts`, `app/src/infrastructure/container.ts`                                                                                                        |
| Tests                     | `core/test/application/use-cases/auth/signup-use-case.test.ts`, `core/test/infrastructure/repositories/auth-repository.test.ts`                                                            |

### Domain aliases

`auth.ts` holds named type exports only (the TS1284 exception in the invariants), importing only `components`:

```ts
export type SignupRequest = components["schemas"]["SignupRequest"];
export type AuthResponse = components["schemas"]["AuthResponse"];
export type Role = components["schemas"]["Role"];
export type SignupCredentials = Omit<SignupRequest, "role">;
```

`SignupCredentials` is derived from the generated type, not written by hand, so it follows the snapshot if the
request changes. The alternative was to put it in the use case file, but keeping every `Auth` shape in one alias
file is the convention the invariants describe.

### Repository contract and implementation

```ts
interface AuthRepositoryInterface {
  signup(request: SignupRequest): Promise<Result<AuthResponse, DomainError>>;
}
```

The contract takes the full `SignupRequest`, including `role`. The repository is transport: it does not know about
fronts. `AuthRepository` receives `HttpClientInterface` in its constructor, calls
`request<AuthResponse>({ method: "POST", path: "/auth/signup", body: request })`, returns `ok(response)`, and in a
`catch` returns `fail(mapHttpError(error))`. It never rethrows. This shape becomes the reference for the rest of
the `Auth` area.

### Use case and role injection

```ts
class SignupUseCase implements UseCaseInterface<SignupCredentials, AuthResponse> {
  constructor(
    private readonly authRepository: AuthRepositoryInterface,
    private readonly role: Role,
  ) {}
  execute(credentials) {
    return this.authRepository.signup({ ...credentials, role: this.role });
  }
}
```

- **Role as a constructor argument, chosen over** (a) a `role` field in the input, which lets presentation pick any
  role, and (b) two classes `SignupAthleteUseCase` / `SignupTrainerUseCase`, which duplicates one operation for a
  value that is wiring. A constructor argument keeps one use case in `core` and puts the decision in the
  composition root, where the product split already lives.
- The use case does not touch `KeyValueStoreInterface`: signup stores no session (see proposal).
- Spreading `credentials` before `role` means a caller that sneaks a `role` in at runtime is still overridden.

### Dependencies keys

```ts
authRepository: AuthRepositoryInterface;
signupUseCase: UseCaseInterface<SignupCredentials, AuthResponse>;
```

`signupUseCase` is typed by the interface, not the class, so presentation tests can register a fake without
subclassing.

### Registration per front

Both containers add the same `authRepository` factory and a `signupUseCase` factory that differs only in the role:

```ts
authRepository: (c) => new AuthRepository(c.resolve("httpClient")),
signupUseCase: (c) => new SignupUseCase(c.resolve("authRepository"), 2), // TRAINER  (backoffice)
signupUseCase: (c) => new SignupUseCase(c.resolve("authRepository"), 1), // USER     (app)
```

Both are lazy singletons (the `Container` default): neither holds state. `AuthRepository` comes from
`@weight-tracker/core/infrastructure` and `SignupUseCase` from `@weight-tracker/core/application`. Both imports are
in `src/infrastructure`, so presentation still never imports core infrastructure.

**Literal over named constant.** The snapshot has no role names. A `Role` enum or `ROLES` map in `core` would be a
hand-written domain type, which is a stop case. The literal plus a comment is the smallest honest option until the
API publishes names.

### Errors and translations

The codes are `VALIDATION`, `UNAUTHORIZED`, `FORBIDDEN`, `CONFLICT`, `NETWORK` and `UNEXPECTED`, all already
produced by `mapHttpError`. No mapper change. No `es` / `en` keys, because no screen shows them. The first signup
screen must translate them.

### State

None. No provider, no context, no stored value.

### Tests

Both are Vitest, AAA, with the API mocked:

- **Use case**: an in-test fake `AuthRepositoryInterface` that records the request it receives and returns a chosen
  `Result`. Cases: the configured role is sent (one case per role, `1` and `2`); email and password pass through
  unchanged; success is returned unchanged; a failure is returned unchanged.
- **Repository**: an in-test fake `HttpClientInterface` that records the `HttpRequest` and resolves a value or
  rejects with `HttpError(status, { detail })` / a plain `Error`. Cases: request is `POST /auth/signup` with the
  given body; success returns `ok(response)`; `it.each` over `400`, `401`, `403`, `409`, `500` asserting the code;
  a rejected `fetch` gives `NETWORK`; the problem detail becomes the message.

**Test-helper exception.** Each front's `test/support/render-with-providers.tsx` builds a full `Registrations<Dependencies>` of
do-nothing fakes, so it must gain `authRepository` and `signupUseCase` entries
(`({ signup: async () => undefined as never })`, `({ execute: async () => undefined as never })`), in the same
style as its existing entries. This is the only change to a front test helper; views still override the keys they
use.

The core fakes live in the test files. There is no shared fake yet, and creating a test-helper module is left to the login
change, when a second consumer exists.

## Risks / Trade-offs

- [The numeric role in each container is not type-checked against its meaning; a swapped `1`/`2` compiles] → The
  role values are a named review item in tasks. A follow-up API change publishing role names removes the literal.
- [The snapshot documents only `400`, so `401`/`403`/`409` rest on the API source, not the contract] → The tests pin
  the mapping. Documenting them is an API-side change, out of scope.
- [This change sets the `Auth` pattern before login, which the template README names as the first canonical slice]
  → The login change must extend these files, not replace them. If it needs a different shape, it changes them
  explicitly in its own plan.
- [No front-tier test checks the wiring] → `pnpm typecheck` catches a missing or mistyped registration. Only the
  role value depends on review.
