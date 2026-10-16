# Project Name

events-ticketing-api

## Project Description

NestJS API for an event ticketing system with authentication, event browsing, ticket purchases, and role-based visibility over purchases.

## Initial Scope

- List and retrieve public event information
- Authenticate users and expose user profile
- Create ticket purchases for authenticated users
- Retrieve purchases with role-based visibility

## Main Concepts

- UserRole
- User
- EventType
- Event
- Purchase

## State And Mutability

- Ticket availability changes when purchases are completed
- Purchases are created as transactional records
- Event types and user roles are read-only catalogs
- Events are publicly visible, but their business state is mutable if availability is part of the same aggregate

## Business Rules

- A user cannot buy more tickets than the available capacity for an event
- Purchase quantity must be a positive integer
- An event cannot be created with an event date in the past
- A basic user can only view their own purchases
- An admin can view all purchases
- New users created from the API receive the basic role

## Roles And Visibility

- Public users can access welcome, login, and public catalog endpoints
- Basic users can view their own profile and their own purchases
- Admin users can create users and access all purchases
- Only admin users can create, update, or delete events

## Initial Technical Decisions

- Main database: MySQL
- E2E database: MySQL
- Acceptance tests: SQLite in-memory
- Authentication: yes, JWT
- Login response token field: `accessToken`
- Password storage strategy: plain-text demo-only explicitly approved for the example project
- Required local runtime for verification: Node.js 20 with `nvm use` when the current shell is on another version

## Canonical Template Mapping

- `user-roles` uses skeleton-backed `src/user-roles/` as `read-only catalog`
- `event-types` replicates `.stpr/template/src/categories/` as `read-only catalog`
- `events` replicates `.stpr/template/src/products/` as `mutable aggregate`
- `purchases` replicates `.stpr/template/src/orders/` adapted to the `purchases` domain name as `transactional module`
- `auth` uses skeleton-backed `src/auth/` constrained by `.stpr/template/STPR_INVARIANTS.md`
- `users` uses skeleton-backed `src/users/` as `mutable aggregate`

## Business Identifiers And Mutability

- `user-roles.roleId` is unique and immutable after creation
- `event-types.eventTypeUid` is unique and immutable after creation
- `events.eventCode` is unique and immutable after creation unless a later change proposal explicitly defines update semantics
- `purchases.purchaseCode` is unique and immutable after creation
- `users.userId` and `users.username` are unique business identifiers and must both be validated before persistence

## Non-Negotiable Implementation Rules

- Do not introduce implementation patterns that are not present in the mapped template reference unless a later change proposal explicitly authorizes the deviation
- Validate every unique business identifier in the use case before calling repository persistence methods
- Validate every catalog relation with `existsById()` in create and update flows
- If a mutable aggregate ever allows changing a unique business identifier, the plan must define exact repository persistence semantics before coding starts
- Do not change the template test helper/request style unless an environment-specific limitation is documented explicitly as a temporary exception

## Required Review Checkpoints

- Before coding each capability, state the mapped template reference explicitly
- Before closing each capability, compare controller, module, repository, use case, and tests against the mapped template reference
- If the generated code needs a deviation from the mapped template, stop and record the reason in the change artifacts before continuing

## Seed Data

- User roles: admin and basic
- One admin user for development and tests
- Event types for the catalog
- No sample events in the baseline seed unless the plan explicitly requires them for bootstrap

## Out Of Scope

- Payment gateway integration
- Refunds and cancellations
- Seat maps or seat selection
- QR generation and ticket validation

## Notes

- If event availability must change as part of purchases, do not model `Event` as a read-only catalog.
- If `eventCode` ever needs to become mutable, that must be defined in a later change proposal before implementation.
- For protected catalog endpoints, acceptance should include both `401` variants plus `404` for missing `GET by id`.
- For CRUD protected endpoints, acceptance should include the `401` matrix per protected route, not only per module in aggregate.
- Keep project-level decisions here and use `change-proposal-template.md` for later incremental changes.
