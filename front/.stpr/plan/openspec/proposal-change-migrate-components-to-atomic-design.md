# Change Proposal: Migrate Components To Atomic Design

Source brief for `/opsx:propose`. Artifacts go to `openspec/changes/migrate-components-to-atomic-design/`.

## Change

Move every existing presentation component of `backoffice` and `app` into the atomic levels that `STPR_INVARIANTS.md`
now requires (`atoms`, `molecules`, `organisms`), extracting the atoms that wrap MUI and React Native Paper. No behaviour,
copy or route changes.

## Objective

The invariants now classify components by level, fix the import direction, and let only atoms import the UI library's
widgets. The existing components were written before that rule: `common/components` mixes tiny pieces
(`language-menu.tsx`) with whole sections (`app-shell.tsx`) and providers. This change brings the code in line, so the
next screens reuse atoms and molecules instead of repeating MUI and Paper markup.

## Capabilities

- Every component lives in the folder of its level; providers and gates stay at the root of `components/`.
- A first set of atoms wraps the library widgets the forms and menus already use.
- Views, organisms and molecules import atoms instead of MUI / Paper widgets (layout primitives stay allowed).
- Behaviour and screens stay identical: existing tests pass with updated import paths only.

## Fronts And Roles

- backoffice -> lands here: yes -> roles: `ADMIN`, `TRAINER` -> no visible change for either.
- app -> lands here: yes -> roles: `USER` -> no visible change.

No role-dependent behaviour changes, and nothing is hidden or shown differently.

## Classification

Proposed levels (the plan must confirm each before implementation):

backoffice `common/components`:

- `alerts-menu`, `language-menu`, `user-menu` -> organism (built on a shared menu molecule when two or more share markup)
- `header-search` -> molecule
- `load-error`, `home-skeleton` -> molecule
- `app-header`, `navigation-drawer` -> organism
- `app-shell` -> organism (page frame used by views)
- `app-providers`, `current-user-provider` -> provider, stays at the root of `components/`

backoffice `auth`: `login-form`, `signup-form` -> organism; `require-session`, `session-provider` -> gate/provider, stay at the root.

backoffice `profile`: `profile-form` -> organism; `profile-name-fields` -> molecule.

backoffice `onboarding`: `onboarding-dialog` -> organism; `name-step` -> molecule.

app: `auth/components/login-form`, `signup-form` -> organism; `common/components/app-providers` -> provider.

Candidate atoms to extract, named from what the components above already use (confirm by reading them): a themed text
field with helper/error text, a submit button with loading state, a text/heading wrapper, and an icon button.

## Canonical Template Mapping

- Atom, molecule, organism -> `pending` in `.stpr/template/README.md` -> this change establishes the pattern; the first
  migrated files become the reference, and promoting them to `.stpr/template/` is a separate step.
- Each front takes its references from the same front only; the other front is never a UI reference.

## Screens And Navigation

None. No route or view behaviour changes; views change only their imports.

## API Contract

None. No endpoint, use case or core change.

## Held State

None. No provider is added, moved out of its place or changed.

## Forms And Structural Validation

None changed. Forms keep their fields and structural validation; only the widgets they render come from atoms.

## Errors And Copy

None. No error code and no copy key changes.

## Platform Capabilities And Dependencies

None. No port, adapter or package.

## Out Of Scope

- New screens, new behaviour, theme redesign, new dependencies.
- Moving `app-providers` or any provider into a different folder or changing its contents.
- Storybook or any visual-regression tooling.
- Promoting the migrated files to `.stpr/template/`.
- Sharing atoms between `backoffice` and `app`.

## Notes

- Do it one front per commit, atoms first, then molecules, then organisms, then views.
- Tests mirror `src`: move any component test to the new path, and keep every test passing with import changes only.
  Add a test for a molecule or organism only where it has behaviour no view test covers.
- Verification: `pnpm typecheck && pnpm lint && pnpm test` from `front/`, plus `pnpm test:e2e` as the migration touches
  the backoffice login, signup and profile flows.
- Review: grep each front for MUI / Paper widget imports outside `atoms/` and for any import that points upward.
- Open question for the plan: whether `app-shell` is an organism or belongs to the view's page layout. Decide when
  reading it.
- No deviation from the invariants is allowed.
