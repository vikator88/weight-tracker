## Change

Add waitlist when event is sold out

## Objective

Allow users to register interest in an event when no tickets are currently available.

## Capabilities

- Join a waitlist for a sold-out event
- View the current user's waitlist entries
- Get waitlist detail for a specific event

## Module Classification

- `waitlist` -> `transactional module` -> `.stpr/template/src/orders/`
- `events` -> `mutable aggregate` -> `.stpr/template/src/products/`
- `auth` -> `auth/authorization support` -> `src/auth/`

## Canonical Template Mapping

- `waitlist` -> `.stpr/template/src/orders/` -> nearest reference for creation plus role-filtered read flows
- `events` -> `.stpr/template/src/products/` -> nearest reference because event availability is mutable business state
- `auth` -> `src/auth/` -> existing skeleton-backed auth and protected endpoint pattern

## Business Identifiers And Mutability

- `Waitlist.waitlistCode` is a unique business identifier and is immutable after creation
- `(eventCode, userId)` is a business uniqueness rule and must be validated before persistence
- `Event.eventCode` remains immutable in this change

If a future change wants any of those identifiers to become mutable, exact persistence semantics must be defined before coding starts.

## State And Mutability

- Waitlist entries are created and stored as transactional records
- Event availability still changes through purchases and cancellations
- Event information remains publicly visible

## Business Rules

- A user cannot join the same waitlist twice for the same event
- A user can only join the waitlist if the event has no remaining availability
- A waitlist entry must belong to an existing event and an authenticated user
- If tickets become available later, the system does not reserve them automatically for waitlisted users

## Roles And Visibility

- Basic users can create and view only their own waitlist entries
- Admin users can view all waitlist entries

## Out Of Scope

- Automatic promotion from waitlist to purchase
- Email or push notifications
- Priority ordering rules beyond creation time

## Notes

- This should be modeled as a new mutable transactional module, not as a catalog
- No deviation from the mapped template references is allowed in this change unless documented explicitly
