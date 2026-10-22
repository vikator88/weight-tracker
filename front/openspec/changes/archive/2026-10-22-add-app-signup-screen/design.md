# Design

## Context

See `proposal.md` (Why) and the two delta specs (behaviour).

Current state, from the sources:

- `app/src/app` holds `_layout.tsx` (`AppProviders` around a bare `Stack` and `StatusBar`) and `index.tsx`
  (re-exports `HomeView`). `app.json` has `experiments.typedRoutes: true`. `expo` is `~57.0.31`, `expo-router`
  `~57.0.29`.
- `app/src/infrastructure/container.ts` registers `signupUseCase` (role `1`, with the in-memory `keyValueStore`) and
  `getSessionUseCase`. `SignupUseCase.execute(SignupCredentials)` returns `Result<AuthResponse, DomainError>` and
  stores both tokens on success. `GetSessionUseCase.execute()` returns `Result<Session | null, DomainError>`.
- `presentation/common` holds `app-providers.tsx` (Paper → i18n → container), the `ContainerContext` trio, `theme.ts`
  (`MD3LightTheme`) and `home-view.tsx` (heading only).
- `src/i18n` has `common` and `errors` in `es` and `en`; `errors.json` translates every mapper code.
- Tests: `home-view.test.tsx`, `translation-parity.test.ts`, `container.test.ts`, and `render-with-providers.tsx`,
  whose fakes return `undefined as never`.

Section 4.3 of `.stpr/template/STPR_INVARIANTS.md` ("Routing", "Presentation") fixes the router shape, the gate, the
state-hook trio, the form conventions and the theme rule. This design applies it and does not restate it.

## Goals / Non-Goals

**Goals:**

- Establish the app's router organization and auth gate, so login and later screens only add files.
- Establish the app's first state hook (`useSession`) in the trio shape, with a contract login can extend.
- Establish the app's form view pattern.

**Non-Goals:**

- `signIn` / `signOut` on the session hook, persistent storage, token refresh. Login owns them.
- A route-tree test with `expo-router/testing-library`. The gate is checked by reading the session state, not by
  rendering the router (see Risks).
- A form library or schema validator. Three structural checks do not justify one.

## Decisions

### File map

| Building block     | File                                                                                                                             |
| ------------------ | -------------------------------------------------------------------------------------------------------------------------------- |
| Root layout, gate  | `app/src/app/_layout.tsx` (`RootLayout`, local `RootNavigator`)                                                                  |
| Group layouts      | `app/src/app/(auth)/_layout.tsx` (`AuthLayout`), `app/src/app/(app)/_layout.tsx` (`AppLayout`)                                   |
| Routes             | `app/src/app/(auth)/signup.tsx`, `app/src/app/(app)/index.tsx` (moved), `app/src/app/+not-found.tsx`                             |
| State hook         | `app/src/presentation/auth/context/session-context.ts`, `.../auth/context/session-provider.tsx`, `.../auth/hooks/use-session.ts` |
| App-wide providers | `app/src/presentation/common/components/app-providers.tsx`                                                                       |
| Views              | `app/src/presentation/auth/views/signup-view.tsx`, `.../common/views/home-view.tsx`, `.../common/views/not-found-view.tsx`       |
| Component          | `app/src/presentation/auth/components/signup-form.tsx`                                                                           |
| Translations       | `app/src/i18n/{es,en}/auth.json` (new), `{es,en}/common.json`, `app/src/i18n/resources.ts`                                       |
| Tests              | see "Tests" below                                                                                                                |

No `Dependencies` key is added and no `container.ts` changes: every use case needed is already registered.

### Router and gate

```text
app/src/app/
  _layout.tsx        RootLayout → AppProviders → RootNavigator + StatusBar
  +not-found.tsx     NotFoundView
  (auth)/_layout.tsx AuthLayout: Stack, headerShown false
  (auth)/signup.tsx  SignupView
  (app)/_layout.tsx  AppLayout: Stack, headerShown false
  (app)/index.tsx    HomeView
```

```tsx
SplashScreen.preventAutoHideAsync(); // module scope, from expo-router

function RootNavigator() {
  const { status } = useSession();
  useEffect(() => {
    if (status !== "restoring") SplashScreen.hide();
  }, [status]);
  if (status === "restoring") return null; // splash still up
  const signedIn = status === "signedIn";
  return (
    <Stack screenOptions={{ headerShown: false }}>
      <Stack.Protected guard={signedIn}>
        <Stack.Screen name="(app)" />
      </Stack.Protected>
      <Stack.Protected guard={!signedIn}>
        <Stack.Screen name="(auth)" />
      </Stack.Protected>
    </Stack>
  );
}
```

- **`Stack.Protected` over a `RequireSession` wrapper or `Redirect`.** Section 4.3 requires it. It also removes the
  guarded screens from history when the guard flips, which is what keeps `/signup` out of "back" after signup.
- **Rendering `null` while restoring.** The splash covers the screen; rendering no navigator means no route can mount
  and flash. The alternative, rendering the `Stack` with both guards false, would mount `+not-found`.
- **`(app)` first.** With both groups unguarded nothing is listed twice, and Expo Router picks the first available
  screen, so a signed-in athlete starts on `/`.

### Session state hook

```ts
type SessionStatus = "restoring" | "signedIn" | "signedOut";
interface SessionState {
  status: SessionStatus;
  errorCode: DomainErrorCode | null;
  refresh(): Promise<void>;
}
```

- `SessionProvider` resolves `getSessionUseCase` with `useContainer`, runs it on mount and on every `refresh()`:
  `ok(session)` → `signedIn`; `ok(null)` → `signedOut`; `fail(error)` → `signedOut` with `errorCode` set. It never
  navigates and never calls `SignupUseCase`. The state lives for the app process.
- It is composed innermost in `app-providers.tsx` (Paper → i18n → container → session), since it needs the container.
- `SessionContext` defaults to `{ status: "restoring", errorCode: null, refresh: async () => {} }` and `useSession`
  does not throw: AGENTS.md allows only `useContainer` to throw. A missing provider shows up as a splash that never
  hides, which the root layout's provider composition rules out.
- **`refresh()` over `signIn(session)`.** The store is the single source of truth, already written by
  `SignupUseCase`; re-reading it through `GetSessionUseCase` avoids a second copy that could disagree. Login can add
  `signIn` / `signOut` that call their use cases and then `refresh()`.

### Signup view and form

- `SignupForm` owns the field values and structural errors. On submit it trims both values, checks presence and email
  format, sets `error` on the `TextInput` and shows `HelperText type="error"` with the `auth:signup.validation.*` key;
  when valid it calls `onSubmit({ email, password })`. Props: `onSubmit`, `submitting`, `errorMessage`. The submit
  `Button` uses `loading={submitting}` and `disabled={submitting}`.
- `SignupView` owns `submitting` and the last `DomainErrorCode` in `useState` (screen-local, per section 4.3). On
  submit it runs `signupUseCase`; on `ok` it awaits `session.refresh()` and does nothing else; on `fail` it keeps the
  code. It translates the code: `VALIDATION`, `UNAUTHORIZED`, `CONFLICT` → `auth:signup.errors.<code>`, any other →
  `errors:<code>`. It wraps the form in `KeyboardAvoidingView`.
- The email field keeps its value after a failure because the form stays mounted; nothing resets it.
- Email format is a structural regex local to the form (one `@`, no spaces, a dot in the domain); the API is the
  authority.

### Home and not-found

- `HomeView` adds `common:home.signedIn` under the heading. It does not read the session: the gate already decided.
- `NotFoundView` shows `common:notFound.title` and an expo-router `Link href="/" asChild` around a Paper `Button`
  with `common:notFound.goHome`. This is the change's only navigation call.

### Errors and translations

| Code           | Where    | `es`                                                 | `en`                                                  |
| -------------- | -------- | ---------------------------------------------------- | ----------------------------------------------------- |
| `VALIDATION`   | signup   | La contraseña no cumple los requisitos de seguridad. | The password does not meet the security requirements. |
| `UNAUTHORIZED` | signup   | El email no es válido.                               | The email is not valid.                               |
| `CONFLICT`     | signup   | Ya existe una cuenta con este email.                 | An account with this email already exists.            |
| `NETWORK`      | `errors` | existing                                             | existing                                              |
| other          | `errors` | existing                                             | existing                                              |
| `UNEXPECTED`   | session  | not shown: the gate treats it as signed out          | —                                                     |

The full `auth` and `common` key list is in the source brief ("Errors And Copy"). No new error code; no mapper change.
`resources.ts` registers `auth` for both languages.

### Tests

- **Test helper exception (documented).** `render-with-providers.tsx` mounts `SessionProvider` inside
  `ContainerProvider`, mirroring `app-providers.tsx`, so views that read the session get a real provider fed by the
  container's fakes. Its default `getSessionUseCase` fake changes from `undefined as never` to `ok(null)`, because the
  provider now runs it on every render. No other helper or pattern changes.
- `test/presentation/auth/views/signup-view.test.tsx`: empty form → both field errors and `signupUseCase` not
  called; malformed email; values trimmed; short password sent; button disabled while pending; success →
  `getSessionUseCase` runs again (the refresh) and no `expo-router` call; `VALIDATION`, `UNAUTHORIZED`, `CONFLICT`,
  `NETWORK` each show their message; email kept after `CONFLICT`.
- `test/presentation/auth/context/session-provider.test.tsx`: a probe component reading `useSession` shows
  `restoring` first, then `signedIn` with a session, `signedOut` with `ok(null)`, `signedOut` with `errorCode`
  `UNEXPECTED` on `fail`, and `refresh()` re-runs the use case.
- `test/presentation/common/views/home-view.test.tsx`: also asserts "Has iniciado sesión.".
- `test/presentation/common/views/not-found-view.test.tsx`: message and link text; `expo-router`'s `Link` is mocked
  in this file to render its child.
- `translation-parity.test.ts` covers `auth` once `resources.ts` registers it.
- Every test uses `// Arrange` / `// Act` / `// Assert`, with one Act.

## Risks / Trade-offs

- [The root layout's gate has no automated test] → The decision it makes is the session status, which
  `session-provider.test.tsx` covers per branch; the layout maps it to `guard` props in two lines. Manual check from
  a running app is a task. Login decides on `expo-router/testing-library`.
- [`Stack.Protected` is client-only and SDK 57 lacks `redirectTo`] → Not needed: one screen per access state, and
  Expo Router falls back to the first available screen.
- [`SplashScreen` from `expo-router` is marked `@hidden` in its types] → It is what the Expo Router auth guide
  imports for SDK 57; if a later SDK moves it, switch to `expo-splash-screen` in the root layout only.
- [In-memory tokens are lost on restart] → Accepted until login brings persistent storage; the gate then shows signup
  again, which is correct behaviour for no session.
- [A missing `SessionProvider` fails silently as an endless splash] → Composition is fixed in `app-providers.tsx`
  and exercised by every view test through the helper.

## Migration Plan

No data or API migration. `app/src/app/index.tsx` moves to `app/src/app/(app)/index.tsx` in the same commit as the
gate, so `/` keeps resolving to one file. Rollback is a revert of the change.
