# Plan: Exercises CRUD

## 1. Change Summary

Exercises become manageable through the API. Anyone, authenticated or not, can list exercises and read
one by id. Only `ADMIN` users can create, fully replace or delete one. An exercise that any workout
references cannot be deleted.

The `Exercise` aggregate is aligned with the aggregate lifecycle invariants: `Create(...)` replaces
the public constructor. `Name` and `VideoUrl` become the value objects `ExerciseName` and `Url`, so
their rules live in one place. Existing endpoints keep their exact contract.

## 2. Scope

### New domain areas

None.

### Affected domain areas

| Domain area | Layers | Change |
| --- | --- | --- |
| Exercises | Domain | `ExerciseName` and `Url` value objects; `Exercise` gets `Create`, `Update`, guarded `BodyPart` and a private constructor; three new domain exceptions |
| Exercises | Application | 4 new use cases (`GetById`, `Create`, `Update`, `Delete`); `ExerciseDTO` input; `Delete` on `IExerciseRepository`; two new application exceptions |
| Exercises | Infra | `ExerciseRepository.Delete`; `ExerciseMapper`, EF config and `DatabaseSeeder` adapted to the value objects; one extra seeded exercise |
| Exercises | Api | new `ExercisesController` with 5 routes; `ExerciseRequest` / `ExerciseResponse`; `ExceptionMapper` entries |
| Workouts | Application, Infra | `IWorkoutRepository.ExistsByExerciseId` and its implementation. No behavior change. |
| Workouts | Domain, Api | compile-only adaptation: `exercise.Name` → `exercise.Name.Value` in `Workout.RemoveExercise` and `WorkoutResponse`. Output is unchanged. |
| Migrations | — | comment-only: the length comments in `CreateExercisesTable_202610080923` point at the new value object constants. No schema change. |
| `.stpr/template` | — | `Exercise.cs`, `Workout.cs`, `WorkoutTests.cs`, `IExerciseRepository.cs` and `IWorkoutRepository.cs` copies kept byte-identical to `src/` and `test/`. The `new Exercise(...)` example in `STPR_INVARIANTS.md` §4 becomes `Exercise.Create(...)`. No rule changes. |

### Explicitly out of scope

- No schema or migration change: tables, columns, lengths and FKs stay as they are.
- No behavior change to existing Workouts or Auth endpoints, roles or signup.
- No pagination, filtering or sorting on `GET /exercises`.
- Exercise names are not unique, so duplicates are accepted.
- No soft delete.
- `Workout` keeps holding `Exercise` objects instead of `Id`s. This is an existing deviation and is left as is.
- Domain events stay dormant.

## 3. Main Concepts

**`ExerciseName`** (new): `sealed record` in `Domain/Exercises`. It is trimmed, required, between 3
and 150 characters after trimming, and has no character-class restrictions. It is kept separate from
`PersonName` (trimmed, 1–100 characters) on purpose: the bounds differ, and it names a movement, not
a person.

**`Url`** (new): `sealed record` in `Domain/Exercises`. It is trimmed, absolute, written with a literal
`scheme://` prefix, contains no whitespace, uses the `http` or `https` scheme, has a host containing at
least one dot, and is at most 500 characters. The prefix and whitespace checks exist because `Uri` alone
accepts `http:\\host` and embedded spaces, and the value is stored as written. It sits in
`Domain/Exercises` because only one domain area uses it. It moves to `Domain/Common` when a second
area needs it.

**`Exercise`** (changed aggregate root):
- `Name: ExerciseName`, with a private setter.
- `VideoUrl: Url?`, with a public setter.
- `BodyPart: BodyParts`, with a public setter that rejects undefined values.
- `Create(ExerciseName, Url?, BodyParts)` generates the `Id` and sets `IsNew = true`.
- `Rehydrate(Id, ExerciseName, Url?, BodyParts)` sets `IsNew = false`.
- `Update(ExerciseName, Url?, BodyParts)` replaces all three fields.
- The constructor is private.
- `NameMaxLength` and `VideoUrlMaxLength` move to `ExerciseName.MaxLength` and `Url.MaxLength`.

**`ExerciseDTO`** (new): an Application `record(string Name, string? VideoUrl, BodyParts BodyPart)`
with no attributes. Create and Update both take it. It has no `Id`.

**`ExerciseRequest` / `ExerciseResponse`** (new): Api DTOs in `Endpoints/Exercises/Dtos`.
- `ExerciseRequest` carries structural validation only.
- `ExerciseResponse` is `{ id, name, videoUrl, bodyPart }` and is built by `static From(Exercise)`.

**New exceptions:**

| Exception | Layer | HTTP |
| --- | --- | --- |
| `InvalidExerciseNameException` | Domain | 400 |
| `InvalidUrlException` | Domain | 400 |
| `InvalidBodyPartException` | Domain | 400 |
| `ExerciseNotFoundException` | Application | 404 |
| `ExerciseInUseException` | Application | 409 |

## 4. Module Classification

| Piece | Classification | Consequence |
| --- | --- | --- |
| `Exercise` | aggregate root, **mutable** | It has its own repository and mapper. It changes through `Create`, `Update`, the public setters and deletion. It is not a catalog: admins edit it. |
| `ExerciseName`, `Url` | value objects | `record`, immutable, compared by value, each with a unit test |
| `BodyParts` | enum (unchanged) | The domain checks that a value is defined, in `Create`, `Update` and the setter |
| `ExerciseDTO` | application input record | No behavior and no validation |
| `GetExerciseByIdUseCase`, `CreateExerciseUseCase`, `UpdateExerciseUseCase`, `DeleteExerciseUseCase` | use cases | One public `Execute` each |
| "Exercise referenced by a workout" | cross-aggregate rule → Application | Lives in `DeleteExerciseUseCase`, not in either aggregate |
| `Workout` | aggregate root (unchanged) | Only queried for existence |

No entity, domain service or application service is introduced.

## 5. Canonical Template Mapping

| Piece | Reference | Origin |
| --- | --- | --- |
| `Exercise` lifecycle (`Create` / `Rehydrate` / `IsNew`, private constructor) | `Workouts` | template-backed |
| `ExerciseName`, `Url` | `Email` shape; `PersonName` for trimming and length | template-backed / skeleton-backed |
| `GetExerciseByIdUseCase` | `ExerciseUseCase` (`GetAllExercisesUseCase`) | skeleton-backed |
| Write use cases (parse value objects, catch and rethrow, single commit) | `SignupUseCase` | skeleton-backed |
| `IExerciseRepository.Delete`, `ExerciseRepository` | existing `ExerciseRepository` / `WorkoutRepository` | skeleton-backed |
| `IWorkoutRepository.ExistsByExerciseId` | `IUserRepository.ExistsByEmail` | skeleton-backed |
| `ExerciseMapper` | existing `ExerciseMapper` / `WorkoutMapper` | skeleton-backed |
| `ExercisesController` | `WorkoutsController` for protected actions, `AuthController` for anonymous ones; `ExceptionMapper` in every catch | skeleton-backed |
| `ExerciseRequest` / `ExerciseResponse` | `SignupRequest` / `WorkoutResponse` | skeleton-backed |
| Seed | `DatabaseSeeder` / `SeedData` | skeleton-backed |
| Integration tests | `AuthEndpointsTests` (401 matrix), `ApiFactory` / `IntegrationTest` (bootstrap) | template-backed |
| Unit tests | `WorkoutTests` (Arrange/Act/Assert and invariants), `PersonNameTests` (value object) | template-backed / skeleton-backed |

`WorkoutScopingTests` does not apply. Every caller sees the same exercise data, and nothing depends on
role or ownership.

## 6. State And Mutability

| State | Mutability | Changed by |
| --- | --- | --- |
| `Exercise.Id` | immutable, system-generated by `Id.New()` in `Create` | — |
| `Exercise.Name` | mutable | `Create`, `Update` (admin) |
| `Exercise.VideoUrl`, `Exercise.BodyPart` | mutable | `Create`, `Update` (admin); the public setters exist but no use case in this change calls them |
| Exercise existence | mutable | create and delete (admin). Delete is blocked while any workout references the exercise. |
| `Workout` | unchanged | — |

Persistence semantics:
- PUT loads the existing aggregate, through `Rehydrate` so `IsNew = false`, then calls `Update`.
  `Save` takes the existing update branch, so a row is never duplicated.
- An unknown id fails with `404` before `Save` is reached. PUT never creates.
- Delete is a hard delete. It is staged by the repository and committed by the use case.

## 7. Endpoints And Visibility

| Method and route | Visibility | Success | Failures |
| --- | --- | --- | --- |
| `GET /exercises` | public | `200` list of `ExerciseResponse` | — |
| `GET /exercises/{id}` | public | `200` `ExerciseResponse` | `404` |
| `POST /exercises` | `ADMIN` only | `201`, `Location: /exercises/{id}`, `ExerciseResponse` | `400`, `401`, `403` |
| `PUT /exercises/{id}` | `ADMIN` only | `200` updated `ExerciseResponse` | `400`, `401`, `403`, `404` |
| `DELETE /exercises/{id}` | `ADMIN` only | `204` | `401`, `403`, `404`, `409` |

- `USER` and `TRAINER` callers get `403` on every write route and can use the read routes.
- The responses carry no role-scoped data.
- A route `{id}` that is not a GUID does not match the route, so the caller gets `404`.

## 8. Business Rules

1. An exercise name is required, trimmed, and 3–150 characters long. → `400`
2. A video URL is optional. When present it is an absolute `http` or `https` URL whose host has a dot,
   at most 500 characters. → `400`
3. The body part must be a defined `BodyParts` value. This holds in `Create`, `Update` and the
   `BodyPart` setter. → `400`
4. PUT replaces every field. If `videoUrl` is omitted or `null`, the stored URL is cleared.
5. Duplicate exercise names are allowed.
6. An exercise referenced by at least one workout cannot be deleted. → `409`, nothing is removed.
7. Only `ADMIN` may create, update or delete. → `403` for other roles, `401` with no token or an invalid one.
8. GET, PUT or DELETE on a non-existent id → `404`.

## 9. Technical Decisions

- **Repository contract extension (approved):** `IExerciseRepository` gains
  `Task Delete(Exercise, CancellationToken)`. It is "stage a delete", next to the existing intents in
  `STPR_INVARIANTS.md` §4. It only stages the removal and never commits.
- **Existence query:** `IWorkoutRepository.ExistsByExerciseId(Id, CancellationToken)` queries
  `workout_exercises`. It lives on the Workouts repository because that aggregate owns the rows.
- **Role enforcement:** write actions use `[Authorize(Roles = nameof(Role.ADMIN))]`, which resolves
  against the existing JWT role claim. The use cases take no caller role, and ASP.NET produces the `403`.
- **Use case signatures:**
  - `GetAllExercisesUseCase.Execute(ct)` already exists and is unchanged.
  - `GetExerciseByIdUseCase.Execute(Id, ct)`
  - `CreateExerciseUseCase.Execute(ExerciseDTO, ct)`
  - `UpdateExerciseUseCase.Execute(Id, ExerciseDTO, ct)`
  - `DeleteExerciseUseCase.Execute(Id, ct)`
- **Delete order:** load the exercise (`404` if missing), check `ExistsByExerciseId` (`409` if true),
  stage `Delete`, then commit once. The FK `fk_workout_exercises_exercise`, which has no cascade,
  stays as the database backstop. If a workout attaches the exercise between the check and the commit,
  the request returns `500`. That race is accepted and not handled.
- **Enum wire format:** no global `JsonStringEnumConverter` is registered.
  - `ExerciseRequest.BodyPart` binds as its numeric value with `[EnumDataType]`, like `SignupRequest.Role`.
  - `ExerciseResponse.bodyPart` is the enum name string, like `WorkoutResponse`.
- **Column lengths:** EF configuration uses `ExerciseName.MaxLength` and `Url.MaxLength`. The values
  (150 and 500) match the existing columns.
- **Error propagation:** the write use cases do not catch and rethrow the domain exceptions raised by
  the value objects. They are already `DomainException`s with explicit mappings, so a rethrow would add nothing.
- **Rehydration:** `ExerciseMapper.MapToDomain` builds the value objects from stored strings. Failures
  are already wrapped in `PersistenceMappingException`.

## 10. Seed And Test Bootstrap

- Add one exercise that no workout references, so a successful delete can be tested:
  - `SeedData.CurlBicepsId = 00000000-0000-0000-0000-000000000014`
  - name `"Curl de Bíceps"`, `VideoUrl = null`, `BodyParts.ARMS`
- Existing seeded exercises, users, ids and credentials are unchanged. Tests authenticate as
  `admin@weighttracker.test`, `user@weighttracker.test` and `trainer@weighttracker.test`, all with
  password `Passw0rd!`.
- Tests rely on these seeded facts:
  - `Press Banca` is referenced by workouts, so deleting it returns `409`.
  - `Curl de Bíceps` is unreferenced, so deleting it returns `204`.
  - `GET /exercises` returns exactly 4 exercises.
- The bootstrap is unchanged: `exercises` is already truncated and reseeded before every test.

## 11. Implementation Constraints

- The controller injects use cases and `ExceptionMapper` only. Every action catches and delegates to
  `ExceptionMapper.Map`, and every action declares `[ProducesResponseType]` for each status in §7.
- The use cases parse `ExerciseDTO` strings into value objects. Each write use case calls
  `IUnitOfWork.SaveChangesAsync` exactly once, at the end. Read use cases never commit.
- `Id.New()` is called only inside `Exercise.Create`.
- `Url` is not added to `Domain/Common`, and no exercise-specific identifier type is created.
- `.stpr/template` copies of `Exercise.cs`, `Workout.cs` and `WorkoutTests.cs` stay byte-identical to
  their `src/` and `test/` counterparts.

## 12. Minimum Test Coverage

**Unit tests (`test/WeightTracker.Unit`):**
- `ExerciseNameTests`: trims; accepts lengths 3 and 150 after trimming; rejects null, whitespace,
  2 characters and 151 characters.
- `UrlTests`: accepts http and https; trims; rejects a relative URL, `ftp://`, a host with no dot
  (`https://video`) and 501 characters.
- `ExerciseTests`:
  - `Create` sets `IsNew = true` and a new `Id`.
  - `Rehydrate` sets `IsNew = false` and keeps the `Id`.
  - `Update` replaces all fields, including clearing the URL.
  - `Create`, `Update` and the `BodyPart` setter reject an undefined body part.
- `ExerciseMapper` also rejects a stored name or URL that breaks the domain rules
  (`PersistenceMappingException`).
- `ExceptionMapper` status of the five new exceptions. This sits in `test/WeightTracker.Integration/Common`
  because that project references the Api layer. It needs no database.
- Use case tests, with mocked repositories and `IUnitOfWork`:
  - Create stages and commits once.
  - Update returns `404` (no `Save`, no commit) for an unknown id; otherwise it updates and commits.
  - Delete for an unknown id fails with no commit.
  - Delete of a referenced exercise throws `ExerciseInUseException` with no `Delete` and no commit.
  - Delete of an unreferenced exercise stages and commits once.
  - GetById returns `404` for an unknown id.
- `ExerciseMapper` round-trip with value objects, including a `null` URL. Existing mapper, equality and
  workout tests are retyped.

**Integration tests (`test/WeightTracker.Integration`):**
- One matrix per route:
  - `GET /exercises` (anonymous): `200` with exactly the 4 seeded exercises. Public, so no token cases.
  - `GET /exercises/{id}` (anonymous): `200`, `404` for an unknown id, and `400` for the empty GUID
    (`Id.From` rejects it). Public, so no token cases.
  - `POST /exercises`:
    - `401` with no token and with an invalid token
    - `403` as USER and as TRAINER
    - `201` as ADMIN, checking `Location` and the body, then a follow-up GET finds the exercise
    - `400` for a missing name (structural)
    - `400` for a 2-character name and for an invalid URL (domain)
  - `PUT /exercises/{id}`:
    - `401` with no token and with an invalid token
    - `403` as USER and as TRAINER
    - `200` as ADMIN, replacing every field and clearing `videoUrl`
    - `404` for an unknown id, with no row created
    - `400` for invalid input
  - `DELETE /exercises/{id}`:
    - `401` with no token and with an invalid token
    - `403` as USER and as TRAINER
    - `204` on `Curl de Bíceps`, after which GET returns `404`
    - `404` for an unknown id
    - `409` on `Press Banca`, which is still returned by GET afterwards
- Repository tests:
  - `ExerciseRepository.Delete` removes the row after commit.
  - `WorkoutRepository.ExistsByExerciseId` returns true for `Press Banca` and false for `Curl de Bíceps`.
- OpenAPI tests:
  - The anonymous GET routes have no security requirement.
  - The write routes have one.
  - Every route documents its responses.
- Regression: the existing Workouts and Auth suites stay green, and the Workouts response shape is unchanged.

**Verification:** run the full `dotnet test WeightTracker.slnx` with Docker running. The passing test
count must not fall below the baseline.

## 13. Review Checkpoints

- **Value objects:** `record` with `From`; bounds as in §8; `ExerciseName` and `PersonName` kept
  separate for the stated reason; `Url` in `Domain/Exercises`.
- **Exercise aggregate:**
  - The constructor is private, and `Id.New()` is called only in `Create`.
  - `IsNew` is set only by `Create` and `Rehydrate`.
  - The `BodyPart` setter is guarded.
  - No `AddDomainEvent` call.
- **Persistence:**
  - `Delete` only stages. `ExistsByExerciseId` sits on the Workouts repository.
  - The EF lengths use the value object constants.
  - There is no new migration, and the migration change is comment-only.
  - The mapper round-trips.
- **Use cases:** each has a single `Execute` with `ct` last. Each write use case commits once at the
  end. The cross-aggregate delete rule is in Application.
- **API:**
  - The visibility matches §7: `[AllowAnonymous]` on the reads, the `ADMIN` role on the writes.
  - Every catch goes through `ExceptionMapper`, and all five new exceptions are mapped.
  - Responses are DTOs built with `From`.
  - Every status is declared with `[ProducesResponseType]`.
- **Seed:** the new exercise uses a fixed id, is built through the domain, and no workout references it.
  `SeedDataTests` covers the new id.
- **Tests:** the matrix in §12 is complete for each route, and every test follows Arrange/Act/Assert.
- **Template parity:** the check in `AGENTS.md` reports no drift.
