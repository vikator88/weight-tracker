# Design

## Context

See `proposal.md` (Why) and the four delta specs (behaviour).

Current state, from the sources:

- `SignupUseCase(authRepository, role)` returns the repository's `Result` unchanged. Each front registers it with
  its role (`2 // TRAINER` in the backoffice, `1 // USER` in the app).
- `Dependencies` has `httpClient`, `keyValueStore`, `notificationService`, `authRepository` and `signupUseCase`.
  `Registrations<Dependencies>` needs a factory for every key, in both containers and in each front's
  `test/support/render-with-providers.tsx`.
- `keyValueStore` is an `InMemoryKeyValueStore` in both fronts. Its comment says login replaces it in the
  backoffice. Nothing resolves it yet.
- The backoffice has one route (`/` → `HomeView`), the `common` and `errors` namespaces, a view test for
  `HomeView`, and a single E2E (`home.spec.ts`) against the static export served by `serve out`.
- `mapHttpError` already gives `VALIDATION`, `UNAUTHORIZED`, `FORBIDDEN`, `CONFLICT`, `NETWORK` and `UNEXPECTED`.
  `errors.json` translates all of them.
- The snapshot declares no length limits on `SignupRequest`.

## Goals / Non-Goals

**Goals:**

- Establish, in the backoffice, the shape that later screens reuse: a route that only exports a view, a view
  composing a form component, a guard component, and a platform adapter behind `KeyValueStoreInterface`.
- Keep token storage inside a use case, so presentation never touches storage directly.

**Non-Goals:**

- A `SessionProvider` / `useSession` context. Nothing needs shared session state yet: the guard reads storage once
  per mount, and signup navigates away right after writing.
- A form library or schema validator. Three structural checks do not justify a dependency.
- Restructuring `HomeView` beyond wrapping it and adding one line of copy.

## Decisions

### File map

| Building block      | File                                                                                                                        |
| ------------------- | --------------------------------------------------------------------------------------------------------------------------- |
| Entity type (alias) | `core/src/domain/auth/auth.ts` (+ `Session`)                                                                                |
| Use case (changed)  | `core/src/application/use-cases/auth/signup-use-case.ts`                                                                    |
| Use case (new)      | `core/src/application/use-cases/auth/get-session-use-case.ts`                                                               |
| Barrels             | `core/src/domain/index.ts`, `core/src/application/use-cases/auth/index.ts`                                                  |
| Composition         | `core/src/application/dependencies.ts`, `backoffice/src/infrastructure/container.ts`, `app/src/infrastructure/container.ts` |
| Platform adapter    | `backoffice/src/infrastructure/local-storage-key-value-store.ts` (new); `in-memory-key-value-store.ts` (deleted)            |
| Route               | `backoffice/src/app/signup/page.tsx`                                                                                        |
| View                | `backoffice/src/presentation/auth/views/signup-view.tsx`; `backoffice/src/presentation/common/views/home-view.tsx`          |
| Component           | `backoffice/src/presentation/auth/components/signup-form.tsx`, `.../auth/components/require-session.tsx`                    |
| Translations        | `backoffice/src/i18n/{es,en}/auth.json` (new), `{es,en}/common.json`, `backoffice/src/i18n/resources.ts`                    |
| Tests               | see "Tests" below                                                                                                           |

### Storage keys and the `Session` alias

```ts
export type Session = Pick<AuthResponse, "accessToken" | "refreshToken">;
```

The tokens are stored under two keys, `auth.accessToken` and `auth.refreshToken`. They are declared once, as
exported constants in `signup-use-case.ts`, and `get-session-use-case.ts` imports them from there.

- **Two plain keys, chosen over one JSON blob.** One `auth.session` JSON value needs `JSON.parse`, which can throw on
  corrupt data, and would need a shape check. Two strings need neither.
- **`expiresIn` is not stored.** It is relative to the moment of the response and nothing reads it. Login or refresh
  can add an absolute expiry when something needs one.

### `SignupUseCase` stores the tokens

```ts
constructor(authRepository, keyValueStore: KeyValueStoreInterface, role: Role)
async execute(credentials) {
  const result = await this.authRepository.signup({ ...credentials, role: this.role });
  if (!result.ok) return result;           // nothing stored on failure
  try {
    await this.keyValueStore.set(ACCESS_TOKEN_KEY, result.value.accessToken);
    await this.keyValueStore.set(REFRESH_TOKEN_KEY, result.value.refreshToken);
  } catch {
    return fail(new DomainError("UNEXPECTED", "..."));
  }
  return result;
}
```

The `if` is pseudocode: use whatever narrowing `Result` exposes in `core/src/domain/common/result.ts`.

- **The store goes between the repository and the role, chosen over appending it.** Dependencies come first and
  configuration last, the order the login use case will also follow. Both containers change in this change anyway.
- **Catching in the use case, chosen over a non-throwing adapter.** The port's contract is `Promise<void>`, and a
  browser `localStorage` can throw (quota, blocked storage). The use case is where the invariants say failures
  become `Result`, so it catches there and never rethrows. This is not flow control in presentation.
- If the second `set` fails after the first succeeded, a lone access token stays behind. `GetSessionUseCase`
  treats a lone token as no session (see `auth-session`), so the visitor is not let in. No cleanup is attempted.

### `GetSessionUseCase`

`UseCaseInterface<void, Session | null>`, with only `keyValueStore` in its constructor. It reads both keys and
returns `ok({ accessToken, refreshToken })` when both are non-null, otherwise `ok(null)`. It catches a rejected read
and returns `fail(UNEXPECTED)`.

### Dependencies keys and registration

```ts
getSessionUseCase: UseCaseInterface<void, Session | null>;
```

Backoffice:

```ts
keyValueStore: () => new LocalStorageKeyValueStore(),
signupUseCase: (c) => new SignupUseCase(c.resolve("authRepository"), c.resolve("keyValueStore"), 2), // TRAINER
getSessionUseCase: (c) => new GetSessionUseCase(c.resolve("keyValueStore")),
```

App: the same `signupUseCase` with `1 // USER` and its existing `InMemoryKeyValueStore`, plus the same
`getSessionUseCase`. Lazy singletons, as all registrations are. `InMemoryKeyValueStore` holds its map in the
singleton, so the app's session lasts for the process.

### `LocalStorageKeyValueStore`

Same shape as the in-memory store: `async get` returns `localStorage.getItem(key)`, `async set` calls `setItem`,
`async remove` calls `removeItem`. It touches `localStorage` only inside these methods, never at module or
constructor level, because `next build` prerenders client components on the server, where `localStorage` does not
exist. Every caller runs from `useEffect` or an event handler.

### Routes and navigation

- `src/app/signup/page.tsx` imports `SignupView` and default-exports it, like `src/app/page.tsx`. With
  `output: "export"` it builds to `out/signup.html`, which `serve` serves at `/signup`.
- Navigation uses `useRouter().replace` from `next/navigation`, in `SignupView` (success → `/`) and
  `RequireSession` (no session → `/signup`). It is `replace`, not `push`, so Back does not loop through a redirect.
  Before writing it, read the Next 16 guide in `backoffice/node_modules/next/dist/docs/` for `useRouter` and
  static exports, and follow any deprecation notice.

### `RequireSession`

```tsx
<RequireSession>{children}</RequireSession>
```

A client component with local state `"checking" | "authorized"`. On mount it resolves `getSessionUseCase` through
`useContainer`. If the result is `ok` with a session, it sets `"authorized"`. Otherwise, whether no session or a
failure, it calls `router.replace("/signup")` and stays in `"checking"`. While `"checking"` it renders a centred MUI
`CircularProgress`, and `children` are rendered only when `"authorized"`. It ignores the result if it unmounts before
the use case resolves.

- **Wrapping inside the view, chosen over a route-group layout.** The invariants restrict layouts to the root
  providers. `HomeView` returns `<RequireSession>…existing content…</RequireSession>`, so the route file stays a
  bare re-export.
- **It lives in `presentation/auth/components`**, because it belongs to the `auth` area and the next guarded screens
  reuse it.

### `SignupView` and `SignupForm`

- `SignupView` is the screen. It holds the request state (`submitting`, `errorCode`), resolves `signupUseCase`,
  navigates on success, and renders the heading plus `SignupForm`.
- `SignupForm` is presentational. It holds the field values and field errors, runs the structural checks on submit,
  and calls `onSubmit({ email, password })` only when they pass. It receives `submitting` (disables the button) and
  an optional `errorMessage` (shown in an MUI `Alert` above the button).
- Fields are MUI `TextField`s (`type="email"` with `autoComplete="email"`; `type="password"` with
  `autoComplete="new-password"`), labelled so tests can query them by label. The browser's own email validation is
  off (`noValidate` on the form), so the translated message is the one shown.
- Email format check: one simple pattern, `^[^\s@]+@[^\s@]+\.[^\s@]+$`. The API is the authority, and a stricter
  regex would be a business rule in disguise.
- Values are sent as typed, with no trimming (`account-signup`: credentials are forwarded unchanged).

### Errors and translations

`SignupView` maps a failed code to a message key. `VALIDATION`, `UNAUTHORIZED` and `CONFLICT` use
`auth:signup.errors.<code>`. Every other code uses `errors:<code>`. All the copy is listed in the source brief,
"Errors And Copy", and is added verbatim to `es` and `en`. `resources.ts` registers the `auth` namespace in both
languages. No error-mapper change.

### State

| Holder                    | Holds                                   | Changed by                 | Reset                                  |
| ------------------------- | --------------------------------------- | -------------------------- | -------------------------------------- |
| `localStorage` (via port) | `auth.accessToken`, `auth.refreshToken` | `SignupUseCase` on success | never in this change (no logout)       |
| `RequireSession`          | `checking` / `authorized`               | `GetSessionUseCase` result | unmount                                |
| `SignupView`              | `submitting`, `errorCode`               | submit and its result      | unmount; `errorCode` cleared on submit |
| `SignupForm`              | field values, field errors              | typing and submit          | unmount                                |

No context provider is added.

### Tests

All tests follow the AAA convention and fake the API.

- **core** `signup-use-case.test.ts` (extended): the existing cases with an in-test `FakeKeyValueStore` added to the
  constructor; plus stores both tokens on success; stores nothing on failure; returns `UNEXPECTED` when `set`
  rejects.
- **core** `get-session-use-case.test.ts` (new): both keys present gives the session; neither key, or only one, gives
  `null`; a rejected `get` gives `UNEXPECTED`. Uses its own in-test fake store, as the existing tests use in-test
  fakes.
- **backoffice** `test/infrastructure/local-storage-key-value-store.test.ts` (new): set then get, get of a missing
  key is `null`, remove. `localStorage.clear()` runs in the test's Arrange, so tests do not depend on each other.
- **backoffice** `test/presentation/auth/views/signup-view.test.tsx` (new): structural errors and no `execute` call;
  `execute` receives the typed values; success calls `replace("/")`; one case per code (`VALIDATION`,
  `UNAUTHORIZED`, `CONFLICT`, `NETWORK`) shows its message and keeps the email; the button is disabled while
  pending.
- **backoffice** `home-view.test.tsx` (rewritten): a session shows the heading and "Has iniciado sesión."; no
  session calls `replace("/signup")` and the heading is absent; a failed read does the same; before the use case
  resolves, the progress indicator shows and the heading is absent.
- **backoffice E2E** `e2e/signup.spec.ts` (new, replaces `home.spec.ts`): intercepts `POST
http://localhost:5199/auth/signup` with `page.route`. Flows: (1) fill and submit, land on `/` with the heading,
  reload and stay; (2) `goto("/")` with fresh storage, end on `/signup` with no heading; (3) a `409` response shows
  the conflict message. The fulfilled response carries `Access-Control-Allow-Origin`, and `OPTIONS` preflights are
  fulfilled too, because the static site and the API are on different origins.

**Test-helper exceptions** (documented, as the config requires):

- `render-with-providers.tsx` in both fronts gains a do-nothing `getSessionUseCase` entry, in the style of its
  existing entries. That is the only change to it.
- View tests mock `next/navigation` with `vi.mock`, giving a `useRouter` whose `replace` is a `vi.fn()`. jsdom has
  no Next router, so this is the boundary that fits. The mock stays in each test file and is not moved into the
  shared helper.

## Risks / Trade-offs

- [Tokens in `localStorage` are readable by any script injected through XSS] → An accepted decision in the
  invariants ("Session storage"). No mitigation here.
- [The guard trusts token presence, so a revoked or expired session still shows home] → Accepted for this change
  (`backoffice-access-guard`). Home makes no authorized call, so nothing leaks. Login/refresh add reactive refresh.
- [A brief progress indicator on every load of `/`] → Needed to avoid showing protected content. Reading
  `localStorage` is near-instant.
- [This change sets backoffice UI and adapter patterns before the login slice, which the template README names as
  the first canonical slice] → The login change extends `presentation/auth`, `RequireSession` and the adapter
  instead of creating parallel ones. If it needs a different shape, it changes them in its own plan.
- [Changing `SignupUseCase`'s constructor touches the app] → Wiring and its test helper only. `pnpm typecheck`
  catches any missed registration. The role literals are a named review item.

## Migration Plan

None. No deployed users or stored data exist. Rolling back means reverting the change.
