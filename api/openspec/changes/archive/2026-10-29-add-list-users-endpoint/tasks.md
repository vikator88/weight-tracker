# Tasks

## 1. Application and persistence

- [x] 1.1 Add `GetAll(CancellationToken)` to `IUserRepository` and implement it in `UserRepository`: all users ordered by `Email` ascending, no tracking, mapped through the existing user mapper, following `WorkoutRepository.GetAll`. Verify that `dotnet build WeightTracker.slnx` succeeds.
- [x] 1.2 Add `TrainerAthletesNotImplementedException` in `Application/Exceptions`, following the existing application exceptions, with a message stating that listing a trainer's athletes is not implemented yet. Verify that the solution builds.
- [x] 1.3 Add `GetAllUsersUseCase` in `Application/UseCases/Users` with `Execute(Id callerId, Role callerRole, CancellationToken)`, following `GetAllWorkoutsUseCase`: `ADMIN` returns `GetAll`; `TRAINER` throws `TrainerAthletesNotImplementedException`; any other role throws `UnauthorizedOperationException`. XML docs with `<exception>` entries and a note on why `callerId` is kept. Verify that the solution builds.
- [x] 1.4 Register `GetAllUsersUseCase` in `Program.cs` next to `CreateUserUseCase`. Verify that the integration host resolves it once the controller uses it (task 2.4).
- [x] 1.5 Add `test/WeightTracker.Unit/Application/Users/GetAllUsersUseCaseTests.cs`, following `GetAllWorkoutsUseCaseTests` and Arrange/Act/Assert: `ADMIN` returns exactly what the repository returns; `TRAINER` throws `TrainerAthletesNotImplementedException` and never calls the repository; `USER` throws `UnauthorizedOperationException` and never calls the repository. Verify that `dotnet test test/WeightTracker.Unit` passes.
- [x] 1.6 Review `GetAllUsersUseCase` and `UserRepository.GetAll` against `GetAllWorkoutsUseCase`, `WorkoutRepository` and `STPR_INVARIANTS.md`: mapping inside the repository, no EF entity past it, `CancellationToken` passed through, no unit of work for a read. Record any deviation, or confirm there is none.

## 2. API

- [x] 2.1 Map `TrainerAthletesNotImplementedException` to `501 Not Implemented` in `ExceptionMapper`, and add it to `test/WeightTracker.Integration/Common/ExceptionMapperTests.cs` in the existing style. Verify that the exception mapper tests pass.
- [x] 2.2 Add `UsersController.GetAll` (`[HttpGet]`, `[RequireRole(Role.ADMIN, Role.TRAINER)]`) passing `CurrentUser!.Id` and `CurrentUser.Role`, returning `Ok(users.Select(UserProfileResponse.From).ToList())`, with `ProducesResponseType` for `200` (`IReadOnlyList<UserProfileResponse>`), `401`, `403` and `501` (`ProblemDetails`), and the catch through `ExceptionMapper.Map`. Update the controller summary to mention the list. Verify that the solution builds.
- [x] 2.3 Add `test/WeightTracker.Integration/Users/ListUsersEndpointTests.cs`, following `CreateUserEndpointTests`, one test per scenario in `specs/user-administration/spec.md`, each with exactly one Act and the status asserted before the body is read: `401` without token and with an invalid token; `403` for the seeded `USER` with no user data in the body; `501` for the seeded `TRAINER` with no user data in the body; `200` for the seeded `ADMIN` listing the three seeded users (one per role, including the caller); a user created with `POST /users` in Arrange appears in the list returned by the Act; items ordered by email ascending; every item has exactly `id`, `email`, `name`, `surname`, `role`. Verify that the integration tests pass with Docker running.
- [x] 2.4 Add `GET /users` to `test/WeightTracker.Integration/OpenApi/OpenApiDocumentTests.cs` in the style of the `POST /users` entry (secured, declares `200`, `401`, `403`, `501`). Verify that the OpenAPI tests pass.
- [x] 2.5 Review the action against the template `WorkoutsController.GetAll` and `RequireRoleAttribute`'s contract: `401` before `403`, no logic in the controller, no `try`/`catch` bypassing `ExceptionMapper`. Confirm `GET /users/me`, `PUT /users/me` and `POST /users` behaviour is unchanged.

## 3. Verification

- [x] 3.1 Run `dotnet build WeightTracker.slnx` and `dotnet test WeightTracker.slnx` with Docker running. Verify that everything passes with no warnings and that the `.stpr/template` parity loop from `AGENTS.md` prints no `DRIFT`.

## Workflow follow-up

- Archive the change, syncing the `user-administration` delta into the main spec.
- Refresh the front's OpenAPI snapshot in a separate `front/` plan.
