# Tasks

## 1. Domain

- [x] 1.1 Add `User.UpdateProfile(PersonName name, PersonName surname)` in `src/WeightTracker.Domain/Users/User.cs` with an XML summary; verify `dotnet build WeightTracker.slnx` succeeds
- [x] 1.2 Add unit tests to `test/WeightTracker.Unit/Domain/Users/UserTests.cs` (Arrange/Act/Assert): name and surname replaced; `Id`, `Email`, `Role`, `DateBirth`, `PasswordHash`, `CreatedAt` and `IsNew` unchanged; verify `dotnet test test/WeightTracker.Unit` passes
- [x] 1.3 Review `UpdateProfile` against the `Workouts` aggregate action methods and `Exercise.Update`; verify it takes value objects, mutates nothing else and adds no `Guid` generation

## 2. Application

- [x] 2.1 Add `UpdateProfileDTO` and `UpdateMyProfileUseCase` in `src/WeightTracker.Application/UseCases/Users`: build `PersonName`s first, `GetById`, throw `AuthenticatedUserNotFoundException` when missing, `UpdateProfile`, `Save`, `IUnitOfWork.SaveChangesAsync`, return the `User`; verify it builds
- [x] 2.2 Register `UpdateMyProfileUseCase` in `src/WeightTracker.Api/Program.cs`; verify the API resolves it (covered by group 3 integration tests)
- [x] 2.3 Add `test/WeightTracker.Unit/Application/Users/UpdateMyProfileUseCaseTests.cs` mirroring `UpdateExerciseUseCaseTests` (mocked `IUserRepository` and `IUnitOfWork`): success saves and commits once; invalid name or surname throws `InvalidPersonNameException` and never calls `GetById`, `Save` or commit; missing user throws `AuthenticatedUserNotFoundException` and never saves; verify `dotnet test test/WeightTracker.Unit` passes
- [x] 2.4 Review `UpdateMyProfileUseCase` against `UpdateExerciseUseCase`; verify naming, field style, XML `<exception>` docs and that no business rule lives outside the aggregate or value objects

## 3. API endpoints and integration tests

- [x] 3.1 Add `UpdateProfileRequest` (`[Required] Name`, `[Required] Surname`, `ToDto()`) and `UserProfileResponse` (`Id`, `Email`, `Name`, `Surname`, `Role`, `From(AuthenticatedUser)`, `From(User)`) in `src/WeightTracker.Api/Endpoints/Users/Dtos`, following `ExerciseRequest` / `ExerciseResponse`; verify it builds
- [x] 3.2 Add `UsersController` (`[Route("users")]`, inherits `AuthorizedController`) with `GET me` (maps `CurrentUser`) and `PUT me` (calls the use case with `CurrentUser!.Id`), `ProducesResponseType` for `200`/`400`/`401`, every catch through `ExceptionMapper.Map`; verify both routes appear in `/openapi/v1.json`
- [x] 3.3 Add `test/WeightTracker.Integration/Users/UserProfileEndpointTests.cs` using `AuthenticatedClientFor` and `SeedCredentials`, covering `GET /users/me`: `401` no token, `401` invalid token, `200` with own `id`/`email`/`name`/`surname`/`role` for `USER`, `TRAINER` and `ADMIN`, no `password`/`passwordHash`/`dateBirth`/`createdAt` in the body; verify they pass
- [x] 3.4 Extend the same file for `PUT /users/me`: `401` no token, `401` invalid token, `401` for a removed user (`Factory.RemoveUser`), `200` with new values and read-back via `GET /users/me`, trimming, `400` for missing field, blank value and 101-char value with the stored profile unchanged, `role: ADMIN` in body ignored, different `email` in body ignored and login with the original email still works, another seeded user's profile unchanged; verify they pass
- [x] 3.5 Update `OpenApiDocumentTests` only if it asserts the full route list; verify it passes
- [x] 3.6 Review the controller against `WorkoutsController` and the tests against `AuthEndpointsTests` and `ExerciseUpdateEndpointTests`; verify each test has labelled Arrange/Act/Assert with exactly one Act, and the per-route `401` matrix is present for both routes

## 4. Verification

- [x] 4.1 Run the `.stpr/template` drift check from `AGENTS.md`; verify it prints no `DRIFT:` line
- [x] 4.2 Run `dotnet build WeightTracker.slnx` and the full `dotnet test WeightTracker.slnx` with Docker running; verify zero warnings and all tests green

## Workflow follow-up

- Archive the change after review, which creates `openspec/specs/user-profile`.
