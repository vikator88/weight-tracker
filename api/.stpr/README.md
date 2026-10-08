# STPR In This Repository

This folder contains the repository-owned artifacts for the STPR method.

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

- working code reference, copied from the repository and kept byte-identical to it
- `template/src/` for production patterns, `template/test/` for test patterns
- technical invariants in `STPR_INVARIANTS.md`
- the reference map in `template/README.md`

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

- the five projects (`Domain`, `Application`, `Infra`, `Api`, `Migrations`) and their dependency direction
- the two test projects (`WeightTracker.Unit`, `WeightTracker.Integration`)
- Postgres through `infra/docker-compose.yml`, schema owned by FluentMigrator
- local execution and verification commands

This project already exists, so **the project itself is the operating skeleton**. `src/` and `test/` are
the authority; `.stpr/template/` holds a curated copy of the files worth replicating.

Default rule:

- Existing skeleton conventions for runtime and test environments are preserved unless a plan explicitly
  overrides them. This includes DB ports, DB names, credentials, bootstrap behavior, and local execution
  assumptions.

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

Note:
the file keeps the name `STPR_INVARIANTS.md` for historical continuity.

### Canonical map

Classification follows the DDD building blocks defined in the invariants, not a module-type taxonomy.
There is no module file in this stack: a module is a feature slice across the layer projects.

| Capability | Classification | Backed by |
| --- | --- | --- |
| `Workouts` | aggregate root with value objects, multi-table persistence | template |
| `Id` / `Email` | shared value objects | template |
| `Users` | aggregate root, owns credentials | skeleton |
| `Exercises` | aggregate root, mutable (**not** a catalog) | skeleton |
| `Auth` | aggregate root plus use cases, rotating refresh tokens | skeleton |
| Repositories, mappers, `DbContext` | persistence patterns | skeleton |
| Controllers, DTOs, `ExceptionMapper` | API patterns | skeleton |
| Migrations, seed | schema and data patterns | skeleton |
| Test bootstrap, endpoint and unit test shapes | test patterns | template |

Important distinction:

- Classification can be defined in template artifacts even when the implementation reference for that
  capability lives in the skeleton.
- Use [.stpr/template/README.md](template/README.md) to determine whether a capability should be copied
  from `.stpr/template/` or read from the repository `src/` and `test/` trees.

### Keeping the template honest

`.stpr/template/src` and `.stpr/template/test` are copies. A copy that drifts is worse than no copy at all,
so check parity whenever the referenced files change:

```bash
for f in $(cd .stpr/template && find src test \( -name '*.cs' -o -name '*.csproj' \) -not -path '*/bin/*' -not -path '*/obj/*'); do
  cmp -s ".stpr/template/$f" "$f" || echo "DRIFT: $f"
done
```

## Plan

The `plan` is required before generating non-trivial changes.

This repository uses the **prompts route**:

- [.stpr/plan/prompts/prompts-project.md](plan/prompts/prompts-project.md) — for a new project
- [.stpr/plan/prompts/prompts-change.md](plan/prompts/prompts-change.md) — for an incremental change

Each file holds three prompts, used in order: discovery to plan, plan to implementation, and finalization.

Expected result:

- a guided plan precise enough to act as the execution plan
- for a new project, stored under `.stpr/work/project/`
- for an incremental change, stored under `.stpr/work/changes/<change-slug>/`
- never in `src/` or the repository root

Worked example:

- [.stpr/work/changes/workouts-getall-with-users-and-auth/](work/changes/workouts-getall-with-users-and-auth)

If a change is already clear and you want more manual control, write `plan.md` and `tasks.md` directly,
following the same section order the prompts produce. The artifacts are what matter, not how they were written.

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

Verification for any non-trivial change:

```bash
docker compose -f ../infra/docker-compose.yml up -d
dotnet build WeightTracker.slnx
dotnet test WeightTracker.slnx
```

## Golden rule

If a tool changes, disappears, or becomes too expensive, your way of working should not break.

That is why the core of the method lives in your own files:

- skeleton
- template
- plan
- review
