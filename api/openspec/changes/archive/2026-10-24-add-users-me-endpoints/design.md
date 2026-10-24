# Design

## Context

See proposal.md for motivation and specs/user-profile/spec.md for the behavior contract.

Relevant current state:

- `AuthorizedController` already resolves every protected request through `GetAuthenticatedUserUseCase`, which loads the stored `User` and returns `AuthenticatedUser(Id, Email, Name, Surname, Role)`. A missing user ends the request with `401` before the action runs.
- `User` has no action method that changes name or surname. `PersonName.From` already trims, enforces non-empty and max 100, and throws `InvalidPersonNameException`, which `ExceptionMapper` maps to `400`.
- `IUserRepository` has `GetById` and `Save`; `UserRepository.Save` calls `Update` when `IsNew` is false. Use cases commit through `IUnitOfWork.SaveChangesAsync`, as `UpdateExerciseUseCase` does.
- There is no `Users` controller or `UseCases/Users` folder yet.

## Goals / Non-Goals

**Goals:**
- Keep the edit inside the `User` aggregate through an action method, with input validated as value objects before mutation.
- Reuse the caller resolution that already exists instead of adding a parallel lookup.

**Non-Goals:**
- No change to `AuthorizedController`, `GetAuthenticatedUserUseCase`, the repository contract, the schema or the seed.
- No partial update (`PATCH`) semantics.

## Decisions

### Layer mapping

- **Domain**: `User.UpdateProfile(PersonName name, PersonName surname)` assigns both fields. Inputs are already-valid value objects, so the method has no other guard; it never touches `Email`, `Role`, `DateBirth`, `PasswordHash` or `CreatedAt`.
- **Application**: `UseCases/Users/UpdateMyProfileUseCase.Execute(Id userId, UpdateProfileDTO dto, CancellationToken)`:
  1. `PersonName.From(dto.Name)` and `PersonName.From(dto.Surname)` before loading or mutating anything.
  2. `IUserRepository.GetById(userId)`; if `null`, throw `AuthenticatedUserNotFoundException` (the user vanished between the filter and the action; it maps to `401` like the filter).
  3. `user.UpdateProfile(name, surname)`, `Save`, `IUnitOfWork.SaveChangesAsync`.
  4. Return the `User`, as `UpdateExerciseUseCase` returns `Exercise`.
  `UpdateProfileDTO(string Name, string Surname)` lives next to the use case.
- **API**: `Endpoints/Users/UsersController`, `[Route("users")]`, inherits `AuthorizedController`.
  - `GET me` maps `CurrentUser!` to `UserProfileResponse`.
  - `PUT me` binds `UpdateProfileRequest` (`[Required] Name`, `[Required] Surname`, `ToDto()`), calls the use case with `CurrentUser!.Id`, maps the returned `User` to `UserProfileResponse`.
  - Every catch returns `ExceptionMapper.Map(exception)`. `ProducesResponseType` lists `200`, `400` (PUT only) and `401`.
  - `UserProfileResponse(Guid Id, string Email, string Name, string Surname, string Role)` with `From(AuthenticatedUser)` and `From(User)`. Role is serialized the same way the auth responses already serialize it.
- **Infrastructure**: unchanged.
- **Wiring**: register `UpdateMyProfileUseCase` in `Program.cs` next to the other use cases.

Cross-module dependency: the Users API depends on `Application.UseCases.Auth` for `AuthenticatedUser` and `AuthenticatedUserNotFoundException`. This is the same dependency every protected controller already has.

### No separate read use case for `GET /users/me`

The brief proposed a `GetMyProfileUseCase`. It would load the same row `GetAuthenticatedUserUseCase` loaded a moment earlier for the same request and return the same fields. Mapping `CurrentUser` keeps one query per request and one source of truth for "who is the caller".
Alternative considered: a use case that takes `AuthenticatedUser` and returns it, only to satisfy "every endpoint has a use case". Rejected as a pass-through with no orchestration. If the profile later grows fields that `AuthenticatedUser` does not carry, a dedicated read use case is added then.

### `role` / `email` in the body are ignored, not rejected

The request DTO declares only `Name` and `Surname`; the JSON binder drops unknown members by default, so no extra code is needed. Rejecting them would need a custom binder or strict deserialization not used anywhere else in the API. Confirmed with the user.

### Validation order

Value objects are built before `GetById`, so invalid input returns `400` without a read, and the aggregate is never touched with invalid data. Structural absence (`null` / missing field) is caught earlier by `[Required]` and `[ApiController]` automatic `400`.

## Risks / Trade-offs

- [The response for `GET` comes from the filter's snapshot, not a fresh read] → It is read in the same request, so it cannot be stale in any observable way.
- [Two `From` factories on `UserProfileResponse`] → Both produce identical JSON; a test covers that `PUT` and a following `GET` return the same shape.
- [`UserRepository.Save` rewrites the whole row] → `Email` and `Role` are rewritten with their unchanged values; the integration tests assert they did not change.

## Migration Plan

No migration. Deploy as a normal API release; rollback is a code revert.
