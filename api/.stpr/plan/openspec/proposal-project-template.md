# Project Proposal Template

Use this file to define the `Plan` for a brand new project created from this skeleton.

This is the direct `openspec` route of STPR:

- you write the plan more explicitly
- the AI does less discovery and less interpretation

This template is for defining project-level decisions before or together with the first implementation-oriented proposal.

The proposal should be compact and functional-first.
Keep technical detail only when it changes architecture, persistence semantics, mutation rules, or test strategy.

Before filling it:

- read `.stpr/template/README.md` to choose the canonical reference module for each planned module
- read `.stpr/template/STPR_INVARIANTS.md` to confirm mutability and modeling rules

## Project Name

<Project name>

## Project Description

<What the system is for and what domain it serves>

## Initial Scope

- <Main capability 1>
- <Main capability 2>
- <Main capability 3>

## Main Concepts

- <Concept / entity 1>
- <Concept / entity 2>
- <Concept / entity 3>

## Module Classification

- <Module> -> <read-only catalog | mutable aggregate | transactional module | auth/authorization support> -> <template reference path>
- <Module> -> <read-only catalog | mutable aggregate | transactional module | auth/authorization support> -> <template reference path>

Clarify here whether a module is truly catalog-like or only publicly read-only.
Prefer canonical references from `.stpr/template/README.md`.

## Canonical Template Mapping

- <Capability or module> -> <canonical template reference path> -> <reason this is the nearest reference>
- <Capability or module> -> <canonical template reference path> -> <reason this is the nearest reference>

Every planned capability must name its canonical template reference explicitly.

## State And Mutability

- <What business state changes over time>
- <What is read-only or catalog-like>
- <Which module exposes only GET but still changes internally, if any>

## Business Rules

- <Rule 1>
- <Rule 2>
- <Rule 3>

## Roles And Visibility

- <Role 1 and what it can do>
- <Role 2 and what it can do>

## Initial Technical Decisions

- Main database: <MySQL or PostgreSQL>
- E2E database: <MySQL or PostgreSQL>
- Acceptance tests: <SQLite in-memory or other>
- Authentication: <yes / no>
- Login response token field: <accessToken | other explicit value>
- Password storage strategy: <hashed | plain-text demo-only explicitly approved>
- Required local runtime for verification when relevant: <for example Node.js 20 + `nvm use`>
- <Only include extra technical decisions when they materially affect persistence, validation boundaries, or test execution>

## Business Identifiers And Mutability

- <Entity.identifier> is unique and immutable after creation
- <Entity.identifier> is unique and mutable only if exact persistence semantics are defined explicitly here
- <If a module has more than one unique business identifier, list each one and whether it is mutable or immutable>

If a future change wants any listed identifier to become mutable, exact persistence semantics must be defined before coding starts.

## Non-Negotiable Implementation Rules

- Do not introduce implementation patterns that are not present in the mapped template reference unless an explicit later proposal authorizes the deviation
- Validate every unique business identifier in the use case before calling repository persistence methods
- Validate every catalog relation with `existsById()` in create and update flows
- If a mutable aggregate ever allows changing a unique business identifier, the plan must define exact repository persistence semantics before coding starts
- Do not change the template test helper/request style unless an environment-specific limitation is documented explicitly as a temporary exception

## Required Review Checkpoints

- Before coding each capability, state the mapped template reference explicitly
- Before closing each capability, compare controller, module, repository, use case, and tests against the mapped template reference
- If the generated code needs a deviation from the mapped template, stop and record the reason in the change artifacts before continuing

## Seed Data

- <What base data should exist for development and tests>

## Out Of Scope

- <What this first version will not include>
- <What is intentionally deferred>

## Notes

- <Optional domain terminology, invariants, or important clarifications>
- <Any conflict such as "replicate categories" vs "state changes internally" must be resolved here before implementation>
- <Summarize unique identifiers, relation semantics, or technical primary keys only when they materially affect implementation>
- <Use the shared `Id` value object for all business identifiers and externally visible codes by default; if any identifier does not use `Id`, justify the exception explicitly>
- <For each system-generated business identifier, state that generation uses `Id` unless an explicit exception is approved here>
- <If auth exists, state the exact login response contract and token field name explicitly>
- <If plain-text passwords are allowed as a demo-only exception, state that explicitly here; otherwise assume hashed passwords>
- <Document any allowed deviation from the mapped template reference; if none, state that no deviations are allowed>
