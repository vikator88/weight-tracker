# Change Proposal Template

Use this template to define the `Plan` for a change on a project that already exists conceptually.

Use this file as the source brief before asking Codex to create an OpenSpec proposal.
Do not use it for the first proposal of a brand new project from the skeleton; use `project-proposal-template.md` for that.

This is the direct `openspec` route of STPR:

- you define the change plan yourself
- the AI works from a tighter and more explicit plan

The proposal should be compact and functional-first.
Keep technical detail only when it changes architecture, persistence semantics, mutation rules, or test strategy.

Before filling it:

- read `.stpr/template/README.md` to choose the canonical reference module for each affected module
- read `.stpr/template/STPR_INVARIANTS.md` to confirm mutability and modeling rules

## Change

<Short name or one-line description of the change>

## Objective

<What problem this change solves and why it matters now>

## Capabilities

- <Capability 1>
- <Capability 2>
- <Capability 3>

## Module Classification

- <Affected module> -> <read-only catalog | mutable aggregate | transactional module | auth/authorization support> -> <template reference path>
- <Affected module> -> <read-only catalog | mutable aggregate | transactional module | auth/authorization support> -> <template reference path>

State explicitly whether each affected module is truly catalog-like or only publicly read-only.
Prefer canonical references from `.stpr/template/README.md`.

## Canonical Template Mapping

- <Affected capability or module> -> <canonical template reference path> -> <reason this is the nearest reference>
- <Affected capability or module> -> <canonical template reference path> -> <reason this is the nearest reference>

Every affected capability must name its canonical template reference explicitly.

## State And Mutability

- <What business state changes when the system is used>
- <What data is read-only or catalog-like>
- <Which module exposes only GET but still changes internally, if any>

## Business Rules

- <Rule 1>
- <Rule 2>
- <Rule 3>

## Roles And Visibility

- <Who can do what>
- <Any access restrictions or visibility rules>

## Business Identifiers And Mutability

- <Entity.identifier> is a unique business identifier and is immutable after creation
- <Entity.identifier> is unique and mutable only if exact persistence semantics are defined explicitly here
- <If this change affects more than one unique business identifier, list each one and whether it is mutable or immutable>

If a future change wants any listed identifier to become mutable, exact persistence semantics must be defined before coding starts.

## Out Of Scope

- <What this version will not include>
- <What should be deferred to a later change>

## Notes

- <Optional implementation hints or domain terms>
- <Summarize unique identifiers, relation semantics, or technical primary keys only when they materially affect this change>
- <Use the shared `Id` value object for all new or changed business identifiers and externally visible codes by default; if any identifier does not use `Id`, justify the exception explicitly>
- <For each system-generated business identifier affected by this change, state that generation uses `Id` unless an explicit exception is approved here>
- <Resolve any conflict such as "replicate categories" vs "state changes internally" before asking for implementation>
- <Document any allowed deviation from the mapped template reference; if none, state that no deviations are allowed>
