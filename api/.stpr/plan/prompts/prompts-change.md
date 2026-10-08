# Guided Plan Workflow For Incremental Changes

Use these prompts when the project already exists conceptually and you want to obtain a compact `Plan` for a change through the guided `plan` route.

**Template path:** In this repository, "template/" means **`.stpr/template/`**. Always use that path when reading or copying reference files.

**Source of truth:** The reference for structure and conventions is **`.stpr/template/`**. Read **`.stpr/template/README.md`** before choosing a reference module. The mandatory technical contract is **`.stpr/template/STPR_INVARIANTS.md`**.

**Expected answer style:** Final answers should be explicit enough to avoid hidden assumptions, but they do not need to restate the same decision in multiple sections when one section already makes it implementation-safe.

## Shared Conventions

- **Template:** `.stpr/template/` contains `src/`, a curated copy of the canonical reference files from the repository `src/` tree. Seed code lives in `WeightTracker.Infra/Seed` and tests under `test/`; both are skeleton-backed. If the change introduces transactional flows, define the transactional module explicitly in the plan and follow `.stpr/template/STPR_INVARIANTS.md`.
- **Canonical references:** Use `.stpr/template/README.md` to map each affected module to the canonical template reference before implementation.
- **Module classification:** A module is a feature slice across the layer projects, not a file. Before code generation, classify every affected piece as an `aggregate root`, `entity`, `value object`, `domain service`, or `application service`, and state whether its state is mutable and what changes it. See the DDD building blocks in `.stpr/template/STPR_INVARIANTS.md`.
- **Repositories:** Contracts live in `WeightTracker.Application/Interfaces`; implementations live in `WeightTracker.Infra/Repositories` and query through the EF `DbSet` properties of `WeightTrackerDbContext`. Mapping happens inside the repository. Repositories never call `SaveChanges` — `IUnitOfWork` commits. Reaching `DbContext` directly is allowed only in `WeightTracker.Infra/Seed` and the integration test bootstrap.
- **Repository contracts:** Only aggregate roots get a repository. Folder names stay plural; type names stay PascalCase singular. Keep the shapes fixed by `STPR_INVARIANTS.md`: `GetAll`, `GetAllBy...`, `GetById` returning `Task<T?>`, `ExistsBy...`, and `Save`, each taking a `CancellationToken` last. Dependent code depends on the interface in `Application/Interfaces`; registration happens in `Program.cs`.
- **Skeleton conventions:** Preserve existing runtime and test environment conventions by default. Only override them when the change explicitly modifies them.
- **Identifiers:** Use the shared `Id` value object (`WeightTracker.Domain/Common/Id.cs`) for all affected identifiers and externally visible codes by default. `Id.New()` is the only generation path. Only allow an identifier-specific value object when the change explicitly requires identifier-specific format or behavior.
- **Auth contract:** If the change touches authentication behavior, the plan must explicitly define the final login response shape, including the token field name.
- **Password strategy:** If the change touches persisted credentials, the plan must explicitly state whether passwords are hashed or whether plain-text storage remains an approved demo-only exception.
- **Runtime verification:** The project targets .NET 10 (`net10.0`). If the change alters the required SDK, a `global.json` pin, or the Docker prerequisites, the plan must say so explicitly.

## Prompt 1: Change Discovery To Plan

```text
I want to propose a change for an existing project built from this Clean Architecture skeleton.

Ask me only the questions needed to define the change.

You must ask about:

1. Change name
2. Change objective — what should be true after this change that is not true today
3. Scope — which domain areas are affected (Users, Auth, Workouts) and which layers each one
   touches, and whether a new domain area is introduced
4. Main concepts introduced or changed — aggregates, entities, value objects, enums, or changed
   fields on existing ones
5. Business rules introduced or changed, and what state they make mutable or immutable
6. API impact — whether observable API behavior changes at all; if it does, the exact method and
   route per affected operation, the visibility of each (public / authenticated / role-restricted),
   and for role- or ownership-scoped responses what each role must NOT see
7. Persistence and identifiers — only if the change stores or alters stored data: new or changed
   tables and columns, whether existing rows need migrating, and for any new business identifier
   whether it uses the shared `Id`, is system-generated or client-supplied, and is mutable or
   immutable. If mutable, the persistence semantics that keep an update from duplicating a row.
8. Seed, credentials and environment — only if the change materially modifies or depends on them:
   the exact seeded records and credentials the tests assert on; the exact login response field
   name for the token if auth changes; whether passwords are hashed or plain-text storage is an
   explicitly approved demo-only exception if credential storage changes; the required .NET SDK
   version or `global.json` pin if local execution changes
9. Out of scope — what this change deliberately does not do, and what must not change

Ask conditional items only when they apply, and skip them silently otherwise. Where the skeleton
already fixes a default — the shared `Id`, hashed passwords, PostgreSQL, .NET 10 — propose the
default and ask me only to confirm it, rather than asking me to author it. The classification of
each affected piece and the canonical reference for each affected domain area are yours to derive
from `.stpr/template/README.md` and `STPR_INVARIANTS.md`; state them back in sections 4 and 5 of
the plan rather than asking me for them. If two reasonable readings of my answer would lead to
materially different work, ask me which one, with the options and their consequences, before
producing the artifacts.

Do not ask again about project-wide decisions such as database, authentication, or global architecture unless this change modifies them.

After I answer, generate two artifacts in English:

1. `.stpr/work/changes/<change-slug>/plan.md`
2. `.stpr/work/changes/<change-slug>/tasks.md`

`.stpr/work/changes/<change-slug>/plan.md` must be compact and functional-first.
Keep technical detail only when it changes architecture, persistence semantics, mutation rules, or test strategy.

`.stpr/work/changes/<change-slug>/plan.md` must use these sections, in this order:
1. Change Summary
2. Scope
3. Main Concepts
4. Module Classification
5. Canonical Template Mapping
6. State And Mutability
7. Endpoints And Visibility
8. Business Rules
9. Technical Decisions
10. Seed And Test Bootstrap
11. Implementation Constraints
12. Minimum Test Coverage
13. Review Checkpoints

If the change explicitly modifies environment conventions, auth contract, credential strategy, or local execution prerequisites, include those details inside `Technical Decisions` or `Implementation Constraints` only when they are materially changed by this change.

`.stpr/work/changes/<change-slug>/tasks.md` must:
- be ordered
- be implementation-oriented
- include test tasks
- include seed/bootstrap tasks when applicable
- include a review task per capability
- keep operational detail out of `.stpr/work/changes/<change-slug>/plan.md`

Important rules:
- Use `.stpr/template/README.md`, `.stpr/template/STPR_INVARIANTS.md`, and `AGENTS.md` as the source of truth.
- Do not restate or redefine architecture already fixed by those files.
- Ask for clarification if the answers conflict with the template, invariants, or module classification rules.
- Ask for clarification if the answers conflict with existing skeleton environment conventions and do not explicitly authorize an override.
- Produce `.stpr/work/changes/<change-slug>/plan.md` and `.stpr/work/changes/<change-slug>/tasks.md` with no hidden assumptions.
- Keep `.stpr/work/changes/<change-slug>/plan.md` compact and move execution detail to `.stpr/work/changes/<change-slug>/tasks.md`.
- Do not create these work artifacts in `src/` or in the repository root.
```

## Prompt 2: Change Plan To Implementation

```text
Based on `.stpr/work/changes/<change-slug>/plan.md` and `.stpr/work/changes/<change-slug>/tasks.md`, implement the approved change in the existing .NET project using the current project structure and the STPR template as reference.

Before writing code:
1. Read `.stpr/work/changes/<change-slug>/plan.md` first and treat it as the source of execution decisions.
2. Read `.stpr/work/changes/<change-slug>/tasks.md` second and implement in task order.
3. State the mapping as:
   Module -> template reference -> module type -> tests to create
4. Stop if any classification conflicts with the stated mutability rules, or if a change would make a dependency point outward.

Mandatory implementation rules:
- Follow `.stpr/template/` and `.stpr/template/STPR_INVARIANTS.md`.
- Use `.stpr/template/README.md` to select the canonical reference module for each affected module before modifying code.
- Treat the declared template mapping in `.stpr/work/changes/<change-slug>/plan.md` as mandatory unless the plan explicitly documents an allowed deviation.
- Respect the architecture and modeling invariants from `.stpr/template/STPR_INVARIANTS.md`.
- Implement the change by creating and modifying the necessary files in `src/`, `test/`, and `WeightTracker.Infra/Seed` when the plan requires them.
- Complete the approved change end-to-end; do not stop at describing the approach unless a clarification stop case is reached.
- Repository contracts live in `WeightTracker.Application/Interfaces`; implementations live in `WeightTracker.Infra/Repositories`.
- Select the canonical reference for each module through `.stpr/template/README.md`. If no reference exists yet for a capability, say so explicitly in the plan and establish the pattern, rather than inventing one silently.
- Add unit coverage in `test/WeightTracker.Unit` and integration coverage in `test/WeightTracker.Integration` as required by `.stpr/work/changes/<change-slug>/plan.md`.
- Keep string column lengths aligned with value object limits, in both the FluentMigrator migration and the EF configuration.
- Do not treat public `GET`-only access as evidence that state is immutable.
- Validate every affected unique business identifier declared in `.stpr/work/changes/<change-slug>/plan.md` before persistence.
- Use the shared `Id` value object for affected business identifiers and externally visible codes unless `.stpr/work/changes/<change-slug>/plan.md` explicitly documents an allowed exception.
- If an affected business identifier is system-generated, generate it through `Id.New()`, never with ad hoc `Guid.NewGuid()` / `Guid.CreateVersion7()` calls or string assembly inside a use case.
- If a unique business identifier is mutable, define and implement exact persistence semantics before coding so updates preserve single-row identity and do not create duplicate business entities.
- Call out new tests required before or while implementing, and then add them in code.
- Call out any design conflicts or invariant risks before coding.
- Use `.stpr/work/changes/<change-slug>/tasks.md` for execution order and review checkpoints, not for inventing new architecture.
- Run the required verification for the affected change before considering the implementation complete.

Do not invent patterns if the template already provides a valid reference.
```

## Prompt 3: Change Finalization

Use this prompt after implementation.

```text
Finalize the implemented change for the existing .NET project.

Review the change against:
1. `.stpr/work/changes/<change-slug>/plan.md`
2. `.stpr/work/changes/<change-slug>/tasks.md`
3. `.stpr/template/`
4. `.stpr/template/STPR_INVARIANTS.md`

Tasks:
- Complete missing tests required by the change
- Fix deviations from template structure in the affected modules
- Fix domain validation, persistence, and repository issues introduced or exposed by the change
- Ensure every controller handler in the affected modules delegates its catch to `ExceptionMapper.Map(exception)`, and that every new custom exception has an explicit status mapping
- Ensure state declared immutable in the plan is in fact never mutated
- Ensure nothing that owns mutable business state was treated as immutable, and that only aggregate roots were given repositories
- Ensure domain-significant primitives introduced by the change are not left as ad hoc checks in use cases
- Ensure canonical template mappings were actually followed module by module for every affected module
- Ensure all new or changed unique business identifiers are validated before persistence
- Ensure any auth response modified by the change matches the approved plan exactly
- Ensure any credential or password storage behavior modified by the change matches the approved plan exactly, or report the mismatch as a gap explicitly
- Ensure `.stpr/work/changes/<change-slug>/tasks.md` is fully covered or mark any remaining gaps explicitly
- Prepare the affected verification flow for local execution and test runs

Important boundaries:
- Focus on the implemented change and the affected modules, not on redesigning the whole project or the STPR method itself.
- If the change exposed pre-existing project issues outside scope, mention them briefly as residual notes only when they materially affect the implemented change.
- Do not turn this step into a meta-review of prompts, checklist files, or template governance.

Return:
- The fixes made
- The remaining gaps, if any
- The residual risks
```

## Note

This guided `plan` workflow is one way to create the Plan layer of STPR.

If the change is already clear and you want more direct control, write `.stpr/work/changes/<change-slug>/plan.md` and `tasks.md` directly, following the same section order.
