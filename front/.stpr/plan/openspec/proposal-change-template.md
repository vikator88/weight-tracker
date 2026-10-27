# Change Proposal Template

Use this template to define the `Plan` for a change in the `front/` monorepo (`core`, `backoffice`, `app`).

Use this file as the source brief before asking the AI to create an OpenSpec proposal (`/opsx:propose`). Copy it to
`.stpr/plan/openspec/proposal-change-<change-slug>.md`, fill it in, and pass that file to the proposal.

This is the direct `openspec` route of STPR:

- you define the change plan yourself
- the AI works from a tighter and more explicit plan
- the resulting OpenSpec artifacts live in `openspec/changes/<change-slug>/`

The proposal should be compact and functional-first.
Keep technical detail only when it changes architecture, the API contract, held state, or test strategy.

Before filling it:

- read `.stpr/template/README.md` to choose the canonical reference for each affected piece
- read `.stpr/template/STPR_INVARIANTS.md` to confirm the building blocks and the stop cases

A change that also needs the API is two proposals: one in `api/` for the API, and this one for the front. This one
only names the API contract it depends on.

## Change

<Short name or one-line description of the change>

## Objective

<What problem this change solves and why it matters now>

## Capabilities

- <Capability 1>
- <Capability 2>
- <Capability 3>

## Fronts And Roles

- backoffice -> <lands here: yes / no> -> <roles: ADMIN, TRAINER> -> <what each role does here>
- app -> <lands here: yes / no> -> <roles: USER> -> <what the athlete does here>

State how the behaviour differs between fronts and between roles. For role-dependent screens, state what each
role must NOT see.

## Classification

- <Piece> -> <entity type | repository contract | platform port | use case | repository implementation | platform adapter | route | view | atom | molecule | organism | state hook> -> <workspace and folder>
- <Piece> -> <building block> -> <workspace and folder>

Use the building blocks and the workspace layout in `.stpr/template/STPR_INVARIANTS.md`. Use cases and repositories
shared by both fronts live once in `core`.

## Canonical Template Mapping

- <Piece> -> <canonical template reference path> -> <reason this is the nearest reference>
- <Piece> -> <canonical template reference path> -> <reason this is the nearest reference>

Every affected piece must name its canonical reference explicitly. UI pieces take their reference from the same front,
never from the other one, and name their atomic level. If the reference is `pending` in `.stpr/template/README.md`, say so here: this change
then establishes the pattern.

## Screens And Navigation

- <front> -> <route> -> <view> -> <how the user gets here> -> <where the user goes on success / on each failure>

## API Contract

- <METHOD /route> -> <in `core/openapi/v1.json`: yes / no> -> <request and response types used>

If an endpoint is missing, name the API change that provides it and state whether this change refreshes the snapshot
(`openapi:refresh` + `openapi:generate`). Never plan a hand-written domain type.

## Held State

- <Provider or hook> -> <what it holds> -> <who changes it> -> <when it resets: logout, screen exit, refresh, ...>

Write `None` if the change holds no state.

## Forms And Structural Validation

- <Form> -> <field> -> <required / format / maximum length, mirroring the API's declared limit>

Only structural validation belongs here. Business rules belong to the API: if the change needs one, it is an API
change, and this front proposal must stop until it exists.

## Errors And Copy

- <Failure the user can hit> -> <`DomainError` code> -> <what the screen shows>
- <Copy key> -> <es> -> <en, or "to be proposed">

State whether a new error code is needed in the HTTP error mapper. Spanish is the default language. Every key ships
in `es` and `en`.

## Platform Capabilities And Dependencies

- <Capability> -> <port in `core/src/application/ports`> -> <backoffice adapter> -> <app adapter> -> <app needs a development build: yes / no>
- <Package> -> <workspace> -> <why no existing dependency covers it>

Write `None` if the change adds neither.

## Out Of Scope

- <What this version will not include>
- <What should be deferred to a later change>
- <What must not change>

## Notes

- <Optional implementation hints or domain terms>
- <Verification tiers this change requires: core unit, component per front, backoffice E2E (and which flows)>
- <Resolve any conflict with a stop case in `STPR_INVARIANTS.md` before asking for implementation>
- <Document any allowed deviation from the mapped template reference; if none, state that no deviations are allowed>
