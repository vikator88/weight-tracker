# Tasks

No core task: every use case and `Dependencies` key this change needs already exists. No backoffice task.

## 1. App translations

- [x] 1.1 Create `app/src/i18n/{es,en}/auth.json` with every `signup.*` key and its copy from the source brief, add `home.signedIn`, `notFound.title` and `notFound.goHome` to both `common.json`, and register `auth` in `app/src/i18n/resources.ts` for both languages; verify `test/i18n/translation-parity.test.ts` passes

## 2. App session state hook

- [x] 2.1 Create `app/src/presentation/auth/context/session-context.ts` (`SessionState`, default `restoring` with a no-op `refresh`), `session-provider.tsx` (`SessionProvider`: runs `getSessionUseCase` on mount and on `refresh()`, maps `ok(session)` / `ok(null)` / `fail` as in `design.md`, never navigates) and `app/src/presentation/auth/hooks/use-session.ts` (`useSession`, does not throw), following the `ContainerContext` trio; verify with `pnpm --filter app typecheck`
- [x] 2.2 Compose `SessionProvider` innermost in `app/src/presentation/common/components/app-providers.tsx`; verify with the app typecheck
- [x] 2.3 In `app/test/support/render-with-providers.tsx`, mount `SessionProvider` inside `ContainerProvider` and change the default `getSessionUseCase` fake to return `ok(null)` (the documented exception in `design.md`); verify `home-view.test.tsx` still passes
- [x] 2.4 Add `app/test/presentation/auth/context/session-provider.test.tsx` with a probe reading `useSession`: `restoring` first, `signedIn` with a session, `signedOut` with `ok(null)`, `signedOut` with `errorCode` `UNEXPECTED` on `fail`, and `refresh()` re-running the use case; AAA with one Act each; verify with `pnpm --filter app test`

## 3. App views and components

- [x] 3.1 Create `app/src/presentation/auth/components/signup-form.tsx` (Paper `TextInput` with `error` + `HelperText type="error"`, trim then presence and email-format checks, email keyboard with autocapitalization off, secure password, `Button` with `loading` and `disabled` while submitting, `StyleSheet.create` at the bottom, no colour literal); verify with the app typecheck and lint
- [x] 3.2 Create `app/src/presentation/auth/views/signup-view.tsx` (`KeyboardAvoidingView`, `submitting` and last error code in `useState`, runs `signupUseCase`, awaits `session.refresh()` on `ok`, maps `VALIDATION` / `UNAUTHORIZED` / `CONFLICT` to `auth:signup.errors.*` and other codes to `errors:<code>`, no `router` call); verify with the app typecheck
- [x] 3.3 Add `app/test/presentation/auth/views/signup-view.test.tsx` covering every case listed in `design.md` ("Tests"), with `signupUseCase` and `getSessionUseCase` faked through `renderWithProviders`; AAA with one Act each; verify with `pnpm --filter app test`
- [x] 3.4 Add the `common:home.signedIn` message to `app/src/presentation/common/views/home-view.tsx` and assert it in `home-view.test.tsx`; verify with `pnpm --filter app test`
- [x] 3.5 Create `app/src/presentation/common/views/not-found-view.tsx` (`common:notFound.title`, `Link href="/" asChild` around a Paper `Button`) and `app/test/presentation/common/views/not-found-view.test.tsx` (message and link text, `Link` mocked in the file); verify with `pnpm --filter app test`

## 4. App routes and auth gate

- [x] 4.1 Move `app/src/app/index.tsx` to `app/src/app/(app)/index.tsx` and add `app/src/app/(auth)/signup.tsx` and `app/src/app/+not-found.tsx`, each exactly an import of the view and `export default`; verify `find app/src/app -name index.tsx` lists only `(app)/index.tsx`
- [x] 4.2 Add `app/src/app/(auth)/_layout.tsx` (`AuthLayout`) and `app/src/app/(app)/_layout.tsx` (`AppLayout`), each a `Stack` with `headerShown: false` and no other hook or logic; verify with the app typecheck
- [x] 4.3 Rewrite `app/src/app/_layout.tsx`: `SplashScreen.preventAutoHideAsync()` at module scope, `RootLayout` rendering `AppProviders` around a local `RootNavigator` and `StatusBar`, and `RootNavigator` returning `null` while `restoring`, hiding the splash once the status is known, and rendering the `Stack` with `Stack.Protected guard={signedIn}` around `(app)` and `guard={!signedIn}` around `(auth)`, as in `design.md`; verify with the app typecheck (typed routes regenerate) and lint
- [x] 4.4 Check from a running app (`pnpm dev:app`): launch with no session shows signup; a valid signup with the API mocked or running shows home and back does not return to signup; an unknown deep link shows the not-found screen; record the result in the change notes

## 5. Review

- [x] 5.1 Review the app against `design.md` and `.stpr/template/STPR_INVARIANTS.md` section 4.3 ("Routing", "Presentation") and `.stpr/review/CHECKLIST_REVISION.md`: route files are two statements, layouts hold only a navigator, no `Redirect` / `router.replace`, state trio shape, no `throw` outside `useContainer`, kebab-case files, every string in `es` and `en`, no colour literal; verify by reading `git diff -- app/`
- [x] 5.2 Confirm `core/`, `backoffice/` and `.stpr/template/` code copies are untouched and the template drift check prints nothing; verify with `git diff --stat -- core backoffice` and the drift loop in `AGENTS.md`
- [x] 5.3 Run `pnpm typecheck && pnpm lint && pnpm test` from `front/`; verify it passes (no `pnpm test:e2e`: no backoffice flow changes)

## Notes

- 4.4: the manual check on a running app was reported completed by the maintainer on 2026-10-22. No further detail was recorded.
