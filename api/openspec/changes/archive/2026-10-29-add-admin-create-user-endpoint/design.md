# Design

## Context

See proposal.md for motivation and specs/user-administration/spec.md for the behaviour. Relevant current state:

- `SignupUseCase` already creates users: parses `Email` and `Password`, checks `ExistsByEmail`, hashes, calls
  `User.Create`, saves, then issues tokens. It maps `InvalidEmailException` to `InvalidCredentialsException`
  (`401`) to avoid user enumeration, and rejects `ADMIN` with `AdminSignupNotAllowedException`.
- `User.Create` already accepts every field this change needs; no domain change is required.
- `UsersController` (`/users`) inherits `AuthorizedController`. `RequireRoleAttribute` exists and
  `AuthorizedController` already enforces it after resolving the caller (`401` first, then `403`), but no action
  uses it yet.
- `UserProfileResponse` has no `DateBirth`.

## Goals / Non-Goals

**Goals:**
- One use case and one action, built from the existing pieces, with no new exception types and no mapper changes.

**Non-Goals:**
- Sharing code between `SignupUseCase` and `CreateUserUseCase` (no extracted helper).
- Making `DateBirth` nullable in the domain or the database.

## Decisions

**Layer mapping**
- Domain: no change. `User.Create`, `Email`, `Password`, `PersonName`, `Role` are reused.
- Application: `CreateUserUseCase` in `UseCases/Users`, with a `CreateUserDTO` input record (`Email`, `Password`,
  `Role`, `Name?`, `Surname?`, `DateBirth?`), following `UpdateProfileDTO`. Depends on `IUserRepository`,
  `IPasswordHasher`, `IClock`, `IUnitOfWork`. Returns the created `User`.
- Api: `UsersController.Create` (`[HttpPost]`, `[RequireRole(Role.ADMIN)]`), `CreateUserRequest` and
  `CreatedUserResponse` in `Endpoints/Users/Dtos`. Registration of the use case in `Program.cs`.
- Infrastructure: none. `UserRepository.Save` already inserts when `IsNew == true`.

**Authorization with `[RequireRole(Role.ADMIN)]`, not a check in the use case.** The attribute is the existing
mechanism and guarantees `401` before `403` as `authenticated-caller` requires. The use case stays role-agnostic.
Alternative: pass the caller to the use case and throw `UnauthorizedOperationException`, as some workout use cases
do for ownership. Rejected: here access depends on the caller's role only, not on the target resource.

**Let domain exceptions surface unchanged.** `InvalidEmailException`, `PasswordNotStrongEnoughException`,
`InvalidPersonNameException` and `EmailAlreadyExistException` are already mapped to `400`/`400`/`400`/`409`.
The use case catches `InvalidPasswordException` and rethrows `PasswordNotStrongEnoughException`, as signup does, so
the message matches. It does not translate `InvalidEmailException` to `InvalidCredentialsException`: the caller
is an authenticated admin, and enumeration protection does not apply.

**Optional fields.** `null` `Name`/`Surname` -> `PersonName.Empty()`; a given value -> `PersonName.From`, which
trims and rejects empty, whitespace-only or over-length values. `null` `DateBirth` -> `DateOnly.MinValue`. Same
unset values as signup, so stored users stay uniform.

**Validation order.** Parse every value object first, then `ExistsByEmail`, then hash and save. Invalid input
never reaches the database and never pays for hashing.

**New response record instead of extending `UserProfileResponse`.** `CreatedUserResponse(Id, Email, Name,
Surname, DateBirth, Role)` with `From(User)`. Adding `DateBirth` to `UserProfileResponse` would change
`GET /users/me` and `PUT /users/me`, which the `user-profile` spec says do not return the date of birth.

**`201` via `StatusCode(201, ...)`**, as `CreateSuperSeries` does. There is no `GET /users/{id}` for
`CreatedAtAction` to point to.

**Request DTO.** `Email`, `Password` `[Required]`; `Role` `[Required]` + `[EnumDataType(typeof(Role))]` like
`SignupRequest`, but nullable so a missing role is a `400` instead of silently defaulting; `Name`, `Surname`
`string?`; `DateBirth` `DateOnly?`.

## Risks / Trade-offs

- [Admins can create other admins with no extra safeguard] -> Intended by the request; out of scope to add
  approval or audit.
- [`dateBirth` `0001-01-01` is a sentinel visible to clients] -> Same as signup today; making it nullable is a
  separate change with a migration.
- [Creation logic duplicated with `SignupUseCase`] -> Accepted to keep each use case self-contained, as the
  template does; revisit if a third creation path appears.

## Migration Plan

None. No schema change. Deploy and roll back as a normal API release.
