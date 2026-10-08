# Plan: Workouts GetAll With Users And Auth

## 1. Change Summary

Deliver the first end-to-end vertical slice of the API: `GET /workouts`, authenticated and role-aware.

To reach it this change also stands up the layers the slice depends on, all of which are currently empty:

- a `User` aggregate with a `Role` (`USER`, `TRAINER`, `ADMIN`) and persisted credentials,
- JWT + RefreshToken authentication (`POST /auth/login`, `POST /auth/refresh`) as required by the stack invariants,
- the ownership link on `Workout` (one owner, one optional trainer),
- the full persistence layer: DbContext, EF entities, mappers, repositories, FluentMigrator migrations against Postgres,
- `ExceptionMapper` in the API,
- a shared `Id` value object adopted across the whole domain,
- a seeded dataset and a new integration test project.

Outcome: an authenticated caller lists the workouts they are entitled to see, with the full exercise/set prescription nested in the response.

## 2. Scope

### New modules

| Module | Layers |
| --- | --- |
| `Users` | Domain, Application, Infra, Migrations |
| `Auth` (RefreshToken + login/refresh) | Domain, Application, Infra, Api, Migrations |
| `Workouts` | Infra, Api |
| `Exercises` | Infra, Migrations (persistence only, no endpoint) |

### Existing modules affected

| Module | Change |
| --- | --- |
| `Domain/Common` | `Entity.Id` becomes the new `Id` value object; value equality added; new `Id` and `Email` value objects |
| `Domain/Workouts` | `Workout` gains `UserId` and nullable `TrainerId`; public accessors for date and exercises |
| `Domain/Exercises` | `Exercise` migrates to `Id`; gains `Rehydrate` and `IsNew` |
| `Domain/Exceptions` | `DomainException` base introduced; existing exception rebased |
| `Application/Interfaces` | repository contracts retyped to `Id`; new contracts added |
| `Api` | `Program.cs` wiring, first controllers, `ExceptionMapper`, auth pipeline |
| `test/WeightTracker.Unit` | placeholder test replaced by real coverage |

### Explicitly out of scope

- Workout create / update / delete endpoints, and `GET /workouts/{id}`
- Exercises endpoints (table and seed ship; no controller)
- User management and registration endpoints
- Password reset, email verification, logout / token-revocation endpoint
- Domain event dispatching (`AddDomainEvent` stays unconsumed)
- CI pipeline

## 3. Main Concepts

**`User`** — aggregate root. `Id`, `Email`, `Name`, `Surname`, `DateBirth` (`DateOnly`), `Role`, `PasswordHash`. Created through `User.Create(...)` / `User.Rehydrate(...)`. The domain receives an already-hashed password; hashing is an infrastructure concern behind `IPasswordHasher`.

**`Role`** — enum: `USER = 1`, `TRAINER = 2`, `ADMIN = 3`.

**`RefreshToken`** — aggregate root under `Domain/Auth`. `Id`, `UserId`, `TokenHash`, `ExpiresAt`, `RevokedAt?`, `ReplacedByTokenId?`, `CreatedAt`. Only the hash is persisted; the raw token exists in the HTTP response only.

**`Id`** — shared value object in `Domain/Common`, `sealed record` wrapping a `Guid`. `Id.New()` is the single system generator (`Guid.CreateVersion7()`); `Id.From(Guid)` rejects `Guid.Empty`. Replaces every inline `Guid.CreateVersion7()` call.

**`Email`** — shared value object in `Domain/Common`. Normalizes to lowercase, validates shape, max 254 chars. Equality is case-insensitive.

**`Workout` (changed)** — gains immutable `UserId` and nullable `TrainerId`, plus public read accessors for `WorkoutDate` and `Exercises`.

## 4. Module Classification

| Module | Classification | Reason |
| --- | --- | --- |
| `Users` | mutable aggregate | owns credentials and profile state that changes over time |
| `Workouts` | mutable aggregate | `AddExercise` / `RemoveExercise` mutate aggregate state |
| `Exercises` | mutable aggregate | **not** a catalog: public constructor, settable `VideoUrl`/`BodyPart`, and `IExerciseRepository.Save` already exist. Per the invariants, GET-only API exposure is not evidence of a catalog. |
| `Auth` (`RefreshToken`, login/refresh use cases, token service) | auth/authorization support | issues, rotates and revokes credentials |
| `Domain/Common` | shared domain primitives | not a module type; hosts `Id`, `Email`, `Entity`, `AggregateRoot` |

No transactional module is introduced. Login and refresh each write a single aggregate under one `IUnitOfWork.SaveChanges()`.

## 5. Canonical Template Mapping

| Capability | Reference | Origin |
| --- | --- | --- |
| `User` aggregate | `.stpr/template/src/WeightTracker.Domain/Workouts/Workout.cs` (Create / Rehydrate / IsNew shape) | template-backed |
| `RefreshToken` aggregate | same as above | template-backed |
| `GetAllWorkoutsUseCase` | `src/WeightTracker.Application/UseCases/Exercises/GetAllExercisesUseCase.cs` | skeleton-backed |
| `LoginUseCase`, `RefreshTokenUseCase` | same single-`Execute` shape as above | skeleton-backed |
| Repository contracts | `src/WeightTracker.Application/Interfaces/IExerciseRepository.cs` | skeleton-backed |

**No canonical reference exists** for: EF entities, `DbContext`, mappers, repository implementations, `UnitOfWork`, controllers, `ExceptionMapper`, FluentMigrator migrations, seed, and integration test bootstrap. This change establishes them as new local patterns, governed only by `.stpr/template/STPR_INVARIANTS.md`. The first file of each kind becomes the anchor for later changes, and `.stpr/template/README.md` must be updated to register them.

> Note: `.stpr/template/README.md` currently describes `Workouts` as a "template-backed catalog reference". `Workouts` is a mutable aggregate. The wording is inconsistent with the invariants and should be corrected when the README is updated.

## 6. State And Mutability

| State | Rule |
| --- | --- |
| `Workout.UserId` | immutable after creation |
| `Workout.TrainerId` | set at creation, nullable; no mutation operation in this change |
| `Workout` exercises/sets | mutable via `AddExercise` / `RemoveExercise` |
| `User.Email` | unique business identifier, immutable in this change (no update endpoint) |
| `User.PasswordHash` | mutable in principle; no operation changes it in this change |
| `RefreshToken.RevokedAt` / `ReplacedByTokenId` | mutable exactly once, via `Revoke(...)`. Revoking an already-revoked token is a domain error. |
| `Exercise` | mutable aggregate; no mutation operation reached by this change |

`RefreshToken` state changes through system behavior (rotation on refresh), not user editing. It is modeled as a mutable aggregate for that reason.

## 7. Endpoints And Visibility

| Method | Route | Visibility | Notes |
| --- | --- | --- | --- |
| `POST` | `/auth/login` | public | body `{ email, password }` |
| `POST` | `/auth/refresh` | public | body `{ refreshToken }`; rotates the token |
| `GET` | `/workouts` | authenticated, role-scoped | see scoping rule below |

**`GET /workouts` scoping** (enforced in Application, not in the controller):

| Caller role | Returns |
| --- | --- |
| `USER` | workouts where `UserId == callerId` |
| `TRAINER` | workouts where `UserId == callerId` **or** `TrainerId == callerId` |
| `ADMIN` | all workouts |

**Login response** — exact field names:

```json
{ "accessToken": "<jwt>", "refreshToken": "<opaque>", "expiresIn": 900 }
```

`expiresIn` is seconds. `/auth/refresh` returns the same shape.

**Response shape of `GET /workouts`:**

```json
[{ "id": "...", "workoutDate": "2026-10-01",
   "userId": "...", "trainerId": null,
   "exercises": [{ "exerciseId": "...", "name": "Press Banca", "bodyPart": "CHEST",
                   "sets": [{ "count": 4, "target": { "type": "Reps", "value": 10 } }] }] }]
```

`target.type` is `Reps` | `Duration` | `MaxReps`; `MaxReps` carries no `value`.

## 8. Business Rules

| Rule | Layer | Enforcement |
| --- | --- | --- |
| A workout belongs to exactly one user | Domain | `UserId` required by `Workout.Create` |
| A workout has at most one trainer | Domain | `TrainerId` nullable |
| Trainer cannot be the workout owner | Domain | `Workout.Create` throws `TrainerCannotBeWorkoutOwnerException` |
| `TrainerId` must reference an existing user with role `TRAINER` or `ADMIN` | Application | requires a DB lookup, so it cannot live in Domain. No write endpoint reaches it in this change; the check is implemented on `IUserRepository` and covered by a unit test for the future write path. |
| A caller only sees workouts they own, train, or administer | Application | role switch in `GetAllWorkoutsUseCase` |
| Email is unique across users | Application + DB | unique index on `users.email`; `IUserRepository.ExistsByEmail` |
| Email must be well-formed | Domain | `Email` value object |
| An identifier can never be `Guid.Empty` | Domain | `Id.From` |
| Unknown email or wrong password is indistinguishable to the caller | Application | both throw `InvalidCredentialsException` → 401 |
| A refresh token is usable only while unexpired and unrevoked | Domain | `RefreshToken.IsActive(utcNow)` |
| Refreshing rotates: the presented token is revoked and linked to its replacement | Application + Domain | `RefreshToken.Revoke(replacementId)`; re-presenting a revoked token throws `InvalidRefreshTokenException` → 401 |
| Removing an exercise not in the workout is an error | Domain | existing `ExerciseNotInWorkoutException` |

Action-level protection: `Revoke` guards the *act* of revoking (throws when already revoked) rather than only the resulting state; `Workout.Create` rejects the owner/trainer collision at construction rather than leaving it to persistence.

## 9. Technical Decisions

**Identifier strategy.** A shared `Id` value object is introduced in `Domain/Common` and adopted everywhere. `Entity.Id` changes from `Guid` to `Id`, and `Exercise` and `Workout` migrate to it. `IExerciseRepository.GetById` and `IWorkoutRepository.GetById` change signature from `Guid` to `Id`. `Id.New()` is the only generation path; no ad hoc `Guid.CreateVersion7()` remains in aggregates. No preflight collision checks are added for generated ids. Persisted as `uuid`.

**Entity value equality — required fix.** `Workout._exercises` is a `Dictionary<Exercise, List<SetPrescription>>`. `Exercise` is a class, so `ContainsKey` currently uses reference equality: an `Exercise` rehydrated from the DB would never match an equal one already in the dictionary, silently duplicating entries. `Entity` must therefore override `Equals`/`GetHashCode` on `(concrete type, Id)`. This is a prerequisite for correct mapping, not an optional cleanup.

**Aggregate references.** `Workout` references `User` by `Id` only — no navigation property to the `User` aggregate, in both domain and EF entities (the EF side keeps FK columns and constraints without object navigation into another aggregate root).

**Persistence.** Postgres 17 via `infra/docker-compose.yml`. FluentMigrator owns the schema; EF Core maps onto it and never generates it. snake_case tables and columns, declared explicitly in EF configuration so the two stay in lockstep.

| Table | Key columns |
| --- | --- |
| `users` | `id` uuid PK, `email` varchar(254) **unique**, `name`, `surname`, `date_birth` date, `role` int, `password_hash` varchar(500), `created_at` timestamptz |
| `exercises` | `id` uuid PK, `name` varchar(150), `video_url` varchar(500) null, `body_part` int |
| `workouts` | `id` uuid PK, `user_id` uuid FK→`users`, `trainer_id` uuid null FK→`users`, `workout_date` date |
| `workout_exercises` | `id` uuid PK, `workout_id` FK→`workouts` cascade, `exercise_id` FK→`exercises`, `position` int, unique `(workout_id, exercise_id)` |
| `workout_exercise_sets` | `id` uuid PK, `workout_exercise_id` FK→`workout_exercises` cascade, `position` int, `count` int, `target_type` varchar(20), `target_value` int null |
| `refresh_tokens` | `id` uuid PK, `user_id` FK→`users` cascade, `token_hash` varchar(128) **unique**, `expires_at`, `revoked_at` null, `replaced_by_token_id` uuid null, `created_at` |

`SetTarget` is a closed hierarchy persisted as a discriminator pair: `target_type` ∈ {`Reps`, `Duration`, `MaxReps`} with `target_value` null only for `MaxReps`. `position` columns preserve ordering, since the domain dictionary and list carry an implied order the DB would otherwise lose.

All string column lengths match the value object limits.

**Mappers.** One per aggregate root (`UserMapper`, `WorkoutMapper`, `ExerciseMapper`, `RefreshTokenMapper`), each exposing `MapToEntity` and `MapToDomain`. `WorkoutMapper.MapToEntity` produces the `workouts` + `workout_exercises` + `workout_exercise_sets` rows; `MapToDomain` reads all three and calls `Workout.Rehydrate`. Mapping happens inside repositories.

**Error handling.** Two abstract bases: `DomainException` and `ApplicationLayerException` (named to avoid colliding with `System.ApplicationException`). `ExceptionMapper` in the API maps:

| Exception | Status |
| --- | --- |
| `InvalidEmailException`, `InvalidIdException`, `TrainerCannotBeWorkoutOwnerException` | 400 |
| `InvalidCredentialsException`, `InvalidRefreshTokenException` | 401 |
| `UnauthorizedOperationException` | 403 |
| `UserNotFoundException`, `ExerciseNotInWorkoutException` | 404 |
| unmatched `DomainException` | 400 |
| unmatched `ApplicationLayerException` | 500, logged |
| anything else | 500, logged |

**Auth mechanics.** Access token: JWT, HS256, 15 minutes, claims `sub` (user id), `email`, `role` (as `ClaimTypes.Role` so `[Authorize(Roles = ...)]` works). Refresh token: 32 random bytes, base64url, 30 days, stored only as a SHA-256 hash, rotated and revoked on every use. Passwords hashed with ASP.NET Core `PasswordHasher<T>` (PBKDF2) behind `IPasswordHasher` — **no plain-text storage anywhere, and no demo-only exception is requested or approved**.

**No ambient caller state.** `GetAllWorkoutsUseCase.Execute(Id callerId, Role callerRole)` takes the caller explicitly. The controller reads claims and passes them. No `ICurrentUser` abstraction, which keeps Application free of request-scoped infrastructure.

**Configuration.** New `appsettings.json` keys:

```json
"ConnectionStrings": { "Default": "Host=localhost;Port=5432;Database=weight-tracker;Username=admin;Password=admin" },
"Jwt": { "Issuer": "weight-tracker", "Audience": "weight-tracker-api", "SigningKey": "", "AccessTokenMinutes": 15, "RefreshTokenDays": 30 }
```

`SigningKey` stays empty in `appsettings.json`; a development-only key lives in `appsettings.Development.json`, and startup fails fast when the key is missing or shorter than 32 bytes outside Development. Production supplies `Jwt__SigningKey` via environment.

**Runtime.** No change: every project already targets `net10.0` and SDK 10.0.401 is installed. No Node.js involved.

**New packages.** Infra: `Npgsql.EntityFrameworkCore.PostgreSQL`, `Microsoft.Extensions.Identity.Core`, `Microsoft.IdentityModel.JsonWebTokens`. Api: `Microsoft.AspNetCore.Authentication.JwtBearer`. Integration tests: `Microsoft.AspNetCore.Mvc.Testing`, `Npgsql`, `FluentMigrator.Runner.Postgres`.

## 10. Seed And Test Bootstrap

Seeding lives in `WeightTracker.Infra/Seed/DatabaseSeeder.cs`, is idempotent, and runs only in Development and from the integration test fixture — never in production. Ids are fixed so tests can assert on them. Password hashes are computed at seed time through `IPasswordHasher`, never hardcoded.

**Users** — all with password `Passw0rd!`:

| Email | Role | Id |
| --- | --- | --- |
| `user@weighttracker.test` | `USER` | `00000000-0000-0000-0000-000000000001` |
| `trainer@weighttracker.test` | `TRAINER` | `00000000-0000-0000-0000-000000000002` |
| `admin@weighttracker.test` | `ADMIN` | `00000000-0000-0000-0000-000000000003` |

**Exercises:** `Press Banca` (CHEST, `...0011`), `Sentadilla` (LEGS, `...0012`), `Plancha` (BACK, `...0013`).

**Workouts:**

| Id | Owner | Trainer | Date | Content |
| --- | --- | --- | --- | --- |
| `...0021` | user | — | 2026-10-01 | Press Banca 4 × 10 reps |
| `...0022` | user | trainer | 2026-10-03 | Sentadilla 5 × 5 reps; Plancha 3 × 40 s |
| `...0023` | trainer | — | 2026-10-05 | Press Banca 3 × max |

This dataset makes the three scoping outcomes distinguishable: `USER` sees 2 workouts, `TRAINER` sees 2 (one owned, one trained), `ADMIN` sees 3.

**Integration bootstrap.** A new `test/WeightTracker.Integration` project runs against database `weight-tracker-test` on the Postgres container from `infra/docker-compose.yml` — a separate database, so local development data is never wiped. The fixture applies FluentMigrator migrations, truncates all tables between tests, re-seeds, and drives the API through `WebApplicationFactory` with the connection string and JWT signing key overridden.

## 11. Implementation Constraints

- Controllers inject use cases only, and every handler wraps its call in `try/catch` delegating to `ExceptionMapper.Map(exception)`.
- Domain and Application throw only custom exceptions; any caught third-party exception is logged and rethrown as a custom one.
- One EF entity per table; mapping happens inside repositories, never in use cases or controllers.
- FluentMigrator is the only schema authority. EF migrations must not be generated. Migration class names follow `ActionObject_YYYYMMDDHHmm` with varied minute values.
- `Id` is the only identifier abstraction. No per-entity id value objects, and no `Guid.CreateVersion7()` outside `Id.New()`.
- `Email` and `Id` are shared in `Domain/Common` because both are used by more than one module.
- Validation placement: structural (required DTO fields, JSON shape) in the API; existence, uniqueness and authorization in Application; invariants in Domain. `GET /workouts` performs no input validation beyond authentication and scoping.
- `Workout` is referenced by `Id` only; no cross-aggregate navigation properties.
- Secrets are never committed outside the development-only signing key.

## 12. Minimum Test Coverage

**Domain unit tests** (no infrastructure, no framework):

- `Id`: `New()` yields a non-empty v7 Guid; `From(Guid.Empty)` throws; value equality.
- `Email`: accepts valid, rejects malformed, empty, and over-length; normalizes case; case-insensitive equality.
- `Entity`: two instances of the same type with the same `Id` are equal; different types with the same `Id` are not.
- `User`: `Create` sets `IsNew = true`; `Rehydrate` sets `IsNew = false` and restores every field.
- `Workout`: `Create` with `trainerId == userId` throws; `Create` with a null trainer succeeds; `AddExercise` appends to an existing exercise rather than duplicating it when the `Exercise` is a distinct instance with the same `Id`; `RemoveExercise` on an absent exercise throws `ExerciseNotInWorkoutException`; `Rehydrate` restores exercises and sets.
- `RefreshToken`: active when unexpired and unrevoked; inactive when expired; `Revoke` records timestamp and replacement; revoking twice throws.

**Application unit tests** (mocked repositories):

- `GetAllWorkoutsUseCase` dispatches to the correct repository method per role, for all three roles.
- `LoginUseCase`: unknown email and wrong password both throw `InvalidCredentialsException`; success returns tokens and persists exactly one refresh token.
- `RefreshTokenUseCase`: unknown, expired, and revoked tokens each throw `InvalidRefreshTokenException`; success revokes the presented token, links the replacement, and returns a new pair.

**Infrastructure tests:** `WorkoutMapper` round-trip (`MapToEntity` → `MapToDomain`) preserves exercises, set counts, ordering, and every `SetTarget` variant including `MaxReps`.

**Integration tests** (real Postgres, migrations applied, seeded):

- `GET /workouts` without a token → 401.
- `POST /auth/login` with a seeded user → 200 carrying `accessToken`, `refreshToken`, `expiresIn`.
- `POST /auth/login` with a wrong password → 401.
- `GET /workouts` as `USER` → exactly workouts `...0021` and `...0022`.
- `GET /workouts` as `TRAINER` → exactly `...0022` and `...0023`.
- `GET /workouts` as `ADMIN` → all three.
- Response body matches the documented shape, including nested sets and the `MaxReps` target without a `value`.
- `POST /auth/refresh` returns a new pair; re-presenting the old refresh token → 401.
- Migrations apply cleanly from an empty database.

## 13. Review Checkpoints

| # | Capability | Checkpoint |
| --- | --- | --- |
| 1 | Shared identifiers | `Id` and `Email` live in `Domain/Common`; no `Guid.CreateVersion7()` survives outside `Id.New()`; no per-entity id value object exists |
| 2 | `User` aggregate | Create/Rehydrate/IsNew match the `Workout` reference; no plain-text password path exists |
| 3 | `Workout` ownership | `UserId` immutable; owner/trainer collision rejected in Domain; no navigation property into `User` |
| 4 | Persistence | FluentMigrator owns the schema; one EF entity per table; column lengths match value object limits; snake_case names agree between migration and EF configuration |
| 5 | Mapping | Mapping occurs only inside repositories; `WorkoutMapper` round-trips the full aggregate including set ordering |
| 6 | Auth | Login response field names are exactly `accessToken` / `refreshToken` / `expiresIn`; refresh rotates and revokes; only token hashes are persisted |
| 7 | `GET /workouts` | Scoping enforced in Application, not in the controller; controller injects use cases only and routes all errors through `ExceptionMapper` |
| 8 | Error handling | Every custom exception has an explicit status mapping; unmapped exceptions return 500 and are logged |
| 9 | Classification | `Exercises` is not implemented as a read-only catalog |
| 10 | Tests | Every rule in §12 is covered at the stated layer and the full suite passes |
| 11 | Template | `.stpr/template/README.md` registers the new canonical references introduced here |

**Residual note, not in scope:** `Domain/Exceptions/ExerciseNotInWorkputException.cs` is misspelled in both `src/` and `.stpr/template/`. The type inside is spelled correctly. Renaming the file is tracked as an optional task.
