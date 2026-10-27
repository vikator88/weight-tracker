# Design

Refines the brief `.stpr/plan/openspec/proposal-change-migrate-components-to-atomic-design.md`. The rules come from
`.stpr/template/STPR_INVARIANTS.md`; this maps the existing files to them.

## Context

Surveyed read-only: 19 backoffice and 3 app components, 6 views that import widgets, 8 backoffice component tests.
Findings that shaped the plan:

- Views also import widgets (backoffice `Container`, `Typography`, `Link`; app `Button`, `Text`, `HelperText`), so views
  migrate too.
- `app-shell` guards the session, redirects onboarding and composes the drawer and header: it is an **organism**, used by
  layouts, not a view (resolves the brief's open question).
- `language-menu` and `user-menu` call use cases through the container; `profile-name-fields` translates and reads core
  validation constants. Those are allowed at their level (organism and molecule respectively; the invariants forbid i18n
  only in atoms).
- `app-providers` and `current-user-provider`/`session-provider`/`require-session` are composition, not atomic.

## Decisions

- **Atom rule: a widget becomes an atom when two or more component or view files use it.** The final list is computed by
  grep in task 1.1; expected backoffice atoms: `alert`, `button`, `text-field`, `typography`, `link`, `icon-button`,
  `skeleton`, `menu`, `menu-item`, `list-subheader`, `toolbar`, `list-item-icon`, `list-item-text`. Expected app atoms:
  `button`, `text`, `helper-text`, `text-input`. Widgets and icons used by a single component (`Drawer`, `AppBar`, `Dialog*`, `List`,
  `ListItemButton`, `Avatar`, `Badge`, `Divider`, `CircularProgress`, `InputAdornment`, icons) are imported directly there.
- Atoms are thin: they forward props to the widget, set project defaults only through the theme or `sx` theme tokens, take
  text by props, and contain no hooks or `t()`. Type the props as the widget's own props so call sites do not change.
- **Layout primitives** at any level: `Box`, `Stack`, `Grid`, `Container` (added), and the app's `View`, `Portal`,
  `KeyboardAvoidingView`.
- **Folders:** `common/components/atoms` (atoms only there).

| Piece                                                                                                  | Level               | Destination                                     |
| ------------------------------------------------------------------------------------------------------ | ------------------- | ----------------------------------------------- |
| backoffice `alerts-menu`, `language-menu`, `user-menu`, `app-header`, `navigation-drawer`, `app-shell` | organism            | `common/components/organisms`                   |
| backoffice `header-search`, `load-error`, `home-skeleton`                                              | molecule            | `common/components/molecules`                   |
| backoffice `login-form`, `signup-form`                                                                 | organism            | `auth/components/organisms`                     |
| backoffice `profile-form` / `profile-name-fields`                                                      | organism / molecule | `profile/components/organisms` / `molecules`    |
| backoffice `onboarding-dialog` / `name-step`                                                           | organism / molecule | `onboarding/components/organisms` / `molecules` |
| backoffice `app-providers`, `current-user-provider`, `session-provider`, `require-session`             | provider/gate       | unchanged place                                 |
| app `login-form`, `signup-form`                                                                        | organism            | `auth/components/organisms`                     |
| app `app-providers`                                                                                    | provider            | unchanged place                                 |

- Public file names and default export names do not change, so only import paths move. Relative imports inside moved
  files gain one `../` level.
- Tests mirror `src`: the five organism tests and `onboarding-dialog.test.tsx` move to the `organisms/` folder, with their
  relative `support/render-with-providers` import deepened by one level. Providers' tests stay. No assertions change, and
  no new test is required beyond them (atoms have no behaviour).
- No `Dependencies`, container, core, i18n or error-code change.

## Risks

- A thin atom that silently drops a prop changes rendering. Mitigation: type atoms as the widget's props and spread them;
  the existing component tests and E2E flows (login, signup, profile) catch regressions.
- Moving files breaks `@/` and relative imports. Mitigation: move one front at a time, typecheck after each group.
- The app has no E2E tier, so its forms are verified by the view tests only.
