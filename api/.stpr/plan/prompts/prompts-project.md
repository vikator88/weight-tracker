# Guided Plan Workflow For New Projects

This document contains the prompts used to obtain a `Plan` for a brand new project.

This is the guided `plan` route of STPR:

- the AI asks strategically constrained questions
- the user answers
- the AI produces a compact project plan under `.stpr/work/project/plan.md`
- the AI also produces executable tasks under `.stpr/work/project/tasks.md`
- those artifacts become the Plan layer used for generation

**Template path:** In this repository, "template/" means **`.stpr/template/`**. Always use that path when reading or copying reference files.

**Source of truth:** The reference for structure and conventions is **`.stpr/template/`**. Read **`.stpr/template/README.md`** before choosing a reference module. The mandatory technical contract is **`.stpr/template/STPR_INVARIANTS.md`**.

**Expected answer style:** Final answers should be explicit enough to avoid hidden assumptions, but they do not need to repeat the same fact in multiple sections. If a decision is already unambiguous in one section, keep later sections focused on the remaining implementation-impacting facts.

## Shared Conventions

- **Template:** `.stpr/template/` contains `src/`, a curated copy of the canonical reference files from the repository `src/` tree. Seed code lives in `WeightTracker.Infra/Seed` and tests under `test/`; both are skeleton-backed. If the project includes transactional flows, define the transactional module explicitly in the plan and follow `.stpr/template/STPR_INVARIANTS.md`.
- **Canonical references:** Use `.stpr/template/README.md` to map each requested module to the canonical template reference before implementation.
- **Module classification:** A module is a feature slice across the layer projects, not a file. Before code generation, classify every piece as an `aggregate root`, `entity`, `value object`, `domain service`, or `application service`, and state whether its state is mutable and what changes it. See the DDD building blocks in `.stpr/template/STPR_INVARIANTS.md`.
- **Repositories:** Contracts live in `WeightTracker.Application/Interfaces`; implementations live in `WeightTracker.Infra/Repositories` and query through the EF `DbSet` properties of `WeightTrackerDbContext`, using `Include`/`ThenInclude` when the aggregate spans several tables. Mapping happens inside the repository. Repositories never call `SaveChanges` — `IUnitOfWork` commits. Reaching `DbContext` directly is allowed only in `WeightTracker.Infra/Seed` and the integration test bootstrap.
- **Repository contracts:** Only aggregate roots get a repository. Folder names stay plural; type names stay PascalCase singular. Keep the shapes fixed by `STPR_INVARIANTS.md`: `GetAll`, `GetAllBy...`, `GetById` returning `Task<T?>`, `ExistsBy...`, and `Save`, each taking a `CancellationToken` last. Dependent code depends on the interface in `Application/Interfaces`; registration happens in `Program.cs`.
- **Database:** PostgreSQL, provisioned by `infra/docker-compose.yml` and migrated by FluentMigrator. This is already settled by the skeleton; do not re-open it unless the plan explicitly overrides it. Integration tests run against the same engine in their own database.
- **Skeleton conventions:** If the skeleton already defines runtime or test environment conventions such as DB ports, DB names, or credentials, preserve them by default. Only change them when the plan explicitly overrides them.
- **Test layout:** `test/WeightTracker.Unit` holds domain, application and mapper tests and touches no infrastructure, mirroring the `src` folder structure (`Domain/<Module>`, `Domain/Common`, `Application/<Module>`, `Infra/Mappers`). `test/WeightTracker.Integration` holds tests that need the real database or the HTTP host.
- **Identifiers:** Use the shared `Id` value object (`WeightTracker.Domain/Common/Id.cs`) for all identifiers and externally visible codes by default. `Id.New()` is the only generation path. Only allow an identifier-specific value object when the plan explicitly requires identifier-specific format or behavior.
- **Auth contract:** If authentication exists, the plan must explicitly define the login response shape, including the final token field name.
- **Password strategy:** If credentials are persisted, the plan must explicitly state whether passwords are hashed or whether plain-text storage is an approved demo-only exception.
- **Runtime verification:** The project targets .NET 10 (`net10.0`) and needs Docker for the database. The plan should make the required local runtime explicit when build or test execution depends on a specific SDK version or `global.json` pin.

## Prompt 1: Project Discovery To Plan

Use this prompt to discover and define a brand new project.

```text
I want to create a .NET project using this Clean Architecture skeleton.

Ask me only the questions required to fully define the project.

You must ask about:

1. Project name
2. Project description
3. Scope of the project:
   - planned modules
   - main concepts
4. Main entities and their main fields
5. Relationships between entities, including whether each persisted relation joins by DB primary key or by a unique business identifier
6. Required endpoints or operations
7. Business rules
8. Any database decision that departs from the skeleton default (PostgreSQL via `infra/docker-compose.yml`, schema owned by FluentMigrator)
9. Required value objects
10. Specific use cases beyond basic CRUD
11. Environment configuration, including the final environment variables to use and whether runtime/E2E env conventions from the skeleton are preserved or overridden
12. Seed data and test bootstrap expectations, including exact seeded records and fixed credentials when tests depend on seeded users
13. Classification of each planned piece: aggregate root, entity, value object, domain service, or application service
14. Canonical template reference for each planned capability or module
   - state whether each reference is template-backed or skeleton-backed when relevant
15. Unique business identifiers for each main entity and whether each one is mutable or immutable
   - whether each one is client-supplied or system-generated
16. Technical primary keys for each main entity when they differ from business identifiers
17. Whether mutable business state such as availability, stock, or counters is persisted state or derived read state
18. Endpoint visibility matrix: which endpoints are public and which are protected
19. Test environment strategy:
   - whether integration tests reuse the skeleton DB convention or override it
   - the exact integration test database name
   - confirm the integration bootstrap creates, migrates, truncates and reseeds that database without manual setup
20. Identifier generation strategy:
   - confirm that business identifiers and externally visible codes use the shared `Id` value object by default
   - if any identifier is system-generated, state whether generation uses `Id`
   - if any identifier does not use `Id`, justify the exception explicitly
21. Authentication contract and credential strategy:
   - exact login response field name for the token
   - whether persisted passwords are hashed or plain-text demo-only
   - if plain-text is allowed, authorize the exception explicitly
22. Local execution prerequisites when relevant:
   - exact required .NET SDK version if build/tests depend on it
   - any required `global.json` pin or other local runtime step

After I answer, generate two artifacts in English:

1. `.stpr/work/project/plan.md`
2. `.stpr/work/project/tasks.md`

`.stpr/work/project/plan.md` must be compact, functional-first, and easy to review quickly.
Keep technical detail only when it changes architecture, persistence semantics, validation responsibilities, or test strategy.

`.stpr/work/project/plan.md` must use these sections, in this order:
1. Project Summary
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

`.stpr/work/project/tasks.md` must:
- be ordered
- be implementation-oriented
- include test tasks
- include seed/bootstrap tasks when applicable
- include a review task per capability
- avoid restating architecture that already belongs in `.stpr/work/project/plan.md`
- hold the operational detail that would make `.stpr/work/project/plan.md` noisy

Important rules:
- Use `.stpr/template/README.md`, `.stpr/template/STPR_INVARIANTS.md`, and `AGENTS.md` as the source of truth.
- Do not restate or redefine architecture already fixed by those files.
- Ask for clarification if the answers conflict with the template, invariants, or module classification rules.
- Ask for clarification if the answers conflict with existing skeleton environment conventions and do not explicitly authorize an override.
- Produce `.stpr/work/project/plan.md` and `.stpr/work/project/tasks.md` with no hidden assumptions.
- Keep `.stpr/work/project/plan.md` compact and move execution detail to `.stpr/work/project/tasks.md`.
- Do not create these work artifacts in `src/` or in the repository root.
```

## Prompt 2: Plan And Tasks To Generation

Use this prompt after Prompt 1.

```text
Based on `.stpr/work/project/plan.md` and `.stpr/work/project/tasks.md`, generate a complete .NET project following Clean Architecture.

Before writing code:
1. Read `.stpr/work/project/plan.md` first and treat it as the source of execution decisions.
2. Read `.stpr/work/project/tasks.md` second and implement in task order.
3. State the mapping as:
   Module -> template reference -> module type -> tests to create
4. Stop if any classification conflicts with the stated mutability rules, or if a change would make a dependency point outward.

Mandatory implementation rules:
- Follow `.stpr/template/` and `.stpr/template/STPR_INVARIANTS.md`.
- Use `.stpr/template/README.md` to select the canonical reference module before generating code.
- Treat the declared template mapping in `.stpr/work/project/plan.md` as mandatory unless the plan explicitly documents an allowed deviation.
- Respect the architecture and modeling invariants from `.stpr/template/STPR_INVARIANTS.md`.
- Repository contracts live in `WeightTracker.Application/Interfaces`; implementations live in `WeightTracker.Infra/Repositories`.
- Select the canonical reference for each module through `.stpr/template/README.md`. If no reference exists yet for a capability, say so explicitly in the plan and establish the pattern, rather than inventing one silently.
- Add unit coverage in `test/WeightTracker.Unit` and integration coverage in `test/WeightTracker.Integration` as required by `.stpr/work/project/plan.md`.
- Keep string column lengths aligned with value object limits, in both the FluentMigrator migration and the EF configuration.
- Do not treat public `GET`-only access as evidence that state is immutable.
- Validate every unique business identifier declared in `.stpr/work/project/plan.md` before persistence.
- Use the shared `Id` value object for business identifiers and externally visible codes unless `.stpr/work/project/plan.md` explicitly documents an allowed exception.
- Match the auth response contract from `.stpr/work/project/plan.md` exactly when auth exists; do not inherit an older skeleton token field name implicitly.
- If the plan does not explicitly authorize plain-text password storage as a demo-only exception, do not implement runtime auth with plain-text passwords.
- If a business identifier is system-generated, generate it through `Id.New()`, never with ad hoc `Guid.NewGuid()` / `Guid.CreateVersion7()` calls or string assembly inside a use case.
- If a unique business identifier is mutable, preserve single-row identity and do not create a duplicate business entity during update flows.
- Use `.stpr/work/project/tasks.md` for execution order and review checkpoints, not for inventing new architecture.

Do not invent patterns if the template already provides a valid reference.
```

## Prompt 3: Project Finalization

Use this prompt after generation.

```text
Finalize the generated .NET project.

Review the project against:
1. `.stpr/work/project/plan.md`
2. `.stpr/work/project/tasks.md`
3. `.stpr/template/`
4. `.stpr/template/STPR_INVARIANTS.md`

Tasks:
- Complete missing tests
- Fix deviations from template structure
- Fix domain validation and repository issues
- Ensure every controller handler delegates its catch to `ExceptionMapper.Map(exception)`, and that every custom exception has an explicit status mapping
- Ensure state declared immutable in the plan is in fact never mutated
- Ensure domain-significant primitives are not left as ad hoc checks in use cases
- Ensure canonical template mappings were actually followed module by module
- Ensure all declared unique business identifiers are validated before persistence
- Ensure the auth response contract matches the approved plan exactly when auth exists
- Ensure password storage behavior matches the approved plan or report it as a gap explicitly
- Ensure `.stpr/work/project/tasks.md` is fully covered or mark any remaining gaps explicitly
- Prepare the project for local execution and test runs (`dotnet build WeightTracker.slnx`, `dotnet test WeightTracker.slnx` with the database container running)

Important boundaries:
- Focus on the current project output, not on redesigning the STPR method itself.
- If you detect a process weakness, mention it briefly as a residual note only if it directly affected the current result.
- Do not turn this step into a meta-review of prompts, checklist files, or template governance.

Return:
- The fixes made
- The remaining gaps, if any
- The residual risks
```

## Note

This guided `plan` workflow is one way to create the Plan layer of STPR.
