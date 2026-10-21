# STPR In This Repository

This folder contains the front's repository-owned artifacts for the STPR method. It covers the three workspaces of
`front/` (`core`, `backoffice`, `app`) and is independent from the API's STPR in `api/.stpr/`.

STPR means:

- `Skeleton`
- `Template`
- `Plan`
- `Review`

It is not a prompt.
It is not a specific tool.

It is a way to control AI-assisted development without delegating architecture, planning, or validation.

## Goal

Use AI to replicate validated patterns, not to invent architecture from scratch.

Core principle:

- human decides
- AI executes
- tests and review validate

## Structure

`template/`

- working code reference, copied from the workspaces and kept byte-identical to them
- copies keep their path relative to `front/` (`template/core/...`, `template/backoffice/...`, `template/app/...`)
- technical invariants in `STPR_INVARIANTS.md`
- the reference map in `template/README.md`

`plan/openspec/`

- the change brief template for the OpenSpec route, and the filled briefs per change

`plan/prompts/`

- prompts for obtaining a plan with AI assistance
- the AI asks using a constrained structure
- the answers generate STPR work artifacts under `.stpr/work/`

`review/`

- the manual review checklist and the audit prompt

`work/`

- the plan and tasks produced for each change

## Skeleton

The `skeleton` is the reusable project base:

- the pnpm + Turborepo monorepo and its three workspaces
- `core` with its `domain` / `application` / `infrastructure` layers, the dependency rule enforced by `tsconfig`,
  and the API contract snapshot it generates types from
- `backoffice` (Next.js static SPA, MUI) and `app` (Expo, React Native Paper), each with its own composition root,
  platform adapters and translations
- Prettier, ESLint, the test tiers, and the verification commands

The front already exists, so **the project itself is the operating skeleton**. The workspace sources are the
authority; `.stpr/template/` holds a curated copy of the files worth replicating.

Default rule:

- Existing skeleton conventions are preserved unless a plan explicitly overrides them. This includes environment
  variable names, the API base URL fallback, package manager, and local execution assumptions.

## Template

The `template` is the highest-value context for the AI because it contains real code the model can replicate.

What matters most here:

- working examples
- naming
- layer structure
- tests

Less important:

- long theoretical explanations

Main path:

- [.stpr/template/README.md](template/README.md) — the reference map; read this before choosing a reference

Technical contract:

- [.stpr/template/STPR_INVARIANTS.md](template/STPR_INVARIANTS.md)

The template is populated by the first canonical slice, **login**. Until then the map marks its entries `pending`.

### Keeping the template honest

`.stpr/template/` copies are only useful while they match the sources. A copy that drifts is worse than no copy at
all, so check parity from `front/` whenever the referenced files change:

```bash
for f in $(cd .stpr/template && find core backoffice app -type f 2>/dev/null); do
  cmp -s ".stpr/template/$f" "$f" || echo "DRIFT: $f"
done
```

## Plan

The `plan` is required before generating non-trivial changes.

This repository supports two routes. Both produce the Plan layer; pick one per change.

**OpenSpec route** (direct): you write the plan brief yourself, and OpenSpec turns it into proposal, specs, design and
tasks.

1. Copy [.stpr/plan/openspec/proposal-change-template.md](plan/openspec/proposal-change-template.md) to
   `.stpr/plan/openspec/proposal-change-<change-slug>.md` and fill it in.
2. Run `/opsx:propose` with that brief. `openspec/config.yaml` binds the artifacts to the template, the invariants
   and `AGENTS.md`.
3. Review the artifacts in `openspec/changes/<change-slug>/`, then `/opsx:apply` and, once verified, `/opsx:archive`.

**Prompts route** (guided): the AI asks a constrained set of questions and writes the plan.

- [.stpr/plan/prompts/prompts-change.md](plan/prompts/prompts-change.md) — three prompts, used in order: discovery to
  plan, plan to implementation, and finalization
- artifacts go to `.stpr/work/changes/<change-slug>/` (`plan.md`, `tasks.md`)

There is no new-project route on either path: the front already exists and is extended only through changes.

Expected result, whatever the route:

- a plan precise enough to act as the execution plan
- stored under `openspec/changes/<change-slug>/` (OpenSpec) or `.stpr/work/changes/<change-slug>/` (prompts)
- never in a workspace `src/` or in `front/` root

A change that also needs the API is planned twice: the API part in `api/` with its own STPR, the front part here.

If a change is already clear and you want more manual control, write `plan.md` and `tasks.md` directly, following
the same section order the prompts produce. The artifacts are what matter, not how they were written.

## Review

`Review` is the final control layer.

It should verify:

- fidelity to the template
- respect for invariants
- relevant test coverage
- absence of unnecessary drift

Resources:

- [.stpr/review/CHECKLIST_REVISION.md](review/CHECKLIST_REVISION.md) — manual checklist
- [.stpr/review/prompt.md](review/prompt.md) — audit prompt for the method itself
- [AGENTS.md](../AGENTS.md) — operational guide, testing policy, local verification

Verification for any non-trivial change, from `front/`:

```bash
pnpm typecheck && pnpm lint && pnpm test
```

## Golden rule

If a tool changes, disappears, or becomes too expensive, your way of working should not break.

That is why the core of the method lives in your own files:

- skeleton
- template
- plan
- review
