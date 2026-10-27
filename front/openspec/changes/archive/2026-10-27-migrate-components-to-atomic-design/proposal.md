# Proposal

Source brief: `.stpr/plan/openspec/proposal-change-migrate-components-to-atomic-design.md`.

## Why

`STPR_INVARIANTS.md` now classifies presentation components as atoms, molecules and organisms, fixes the import
direction, and keeps UI-library widgets out of everything above atoms. The existing components predate it:
`common/components` mixes a 29-line search box with the whole app shell, and every form, menu and view imports MUI or
React Native Paper widgets directly. Migrating now, while the fronts hold 19 backoffice and 3 app components, is cheap;
the next screens (workouts) would otherwise repeat the raw markup.

## What Changes

- Create `components/{molecules,organisms}` per area and `common/components/{atoms,molecules,organisms}` in each front,
  and move every component to the folder of its level. Providers and gates stay at the root of `components/`.
- Extract atoms that wrap the library widgets used by two or more files, and make molecules, organisms and views import
  those atoms. Structural widgets used by a single organism stay in that organism (decision: shared widgets only).
- Amend `STPR_INVARIANTS.md` (the two gaps found while surveying): `Container` joins the layout primitives allowed at any
  level, and providers may import the library's theme and cache providers (`ThemeProvider`, `CssBaseline`,
  `AppRouterCacheProvider`, `PaperProvider`); also state that the exception for single-use structural widgets in
  organisms is allowed.
- Move the component tests to mirror the new paths; no assertion changes.
- No behaviour, copy, route, state, API, dependency or core change. **Not breaking.**

## Capabilities

### New Capabilities

None.

### Modified Capabilities

None. This is a structural refactor with no change to observable behaviour, so the change sets `skip_specs: true`.

## Impact

- Code: `backoffice/src/presentation/**/components`, the four backoffice views and the two app views (`home`, `not-found`) that import
  library widgets, and the matching tests under `backoffice/test/presentation` (`app/test` has no component tests to move).
- Docs: `.stpr/template/STPR_INVARIANTS.md` (the amendments above).
- Canonical references: atoms, molecules and organisms are `pending` in `.stpr/template/README.md`; the first migrated
  files become the reference, promoting them is a separate step. UI references come from the same front only.
- Roles: no visible change for `ADMIN`, `TRAINER` (backoffice) or `USER` (app); nothing is shown or hidden differently.
- Out of scope: new screens, theme redesign, new dependencies, sharing atoms across fronts, deduplicating the app's two
  forms, and moving or changing any provider.
