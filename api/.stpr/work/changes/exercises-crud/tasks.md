# Tasks: Exercises CRUD

Do the tasks in order. Each phase ends with `dotnet build WeightTracker.slnx`, where warnings are errors.
From Phase 2 onwards, also run the affected tests.

References: `plan.md` in this folder, `.stpr/template/STPR_INVARIANTS.md`, `.stpr/template/README.md`
and `AGENTS.md`.

The build is expected to be **red from task 12 until task 22**. `Exercise` is retyped before the code
that uses it is adapted, and the compiler errors are the list of call sites to fix. Adapt each call
site as specified. Do not silence errors with ad hoc `.Value` calls in unrelated places.

---

## Phase 0 — Prerequisites

1. Start Docker. Check that `dotnet --version` reports 10.0.x.
2. Run `dotnet build WeightTracker.slnx` and `dotnet test WeightTracker.slnx` on the untouched tree.
   **Record the passing test count.** It is the baseline, and the count must not drop.
3. Run the `.stpr/template` parity check from `AGENTS.md`. It must report no drift before you edit anything.

## Phase 1 — Domain exceptions

Copy the shape of `InvalidPersonNameException` for each of these. Put them in `src/WeightTracker.Domain/Exceptions/`.

4. Add `InvalidExerciseNameException(string value)` with the message `'{value}' is not a valid exercise name`.
5. Add `InvalidUrlException(string value)` with the message `'{value}' is not a valid url`.
6. Add `InvalidBodyPartException(int value)` with the message `'{value}' is not a known body part`.

## Phase 2 — Value objects

Copy `.stpr/template/src/WeightTracker.Domain/Common/Email.cs`: a `sealed record` with a private
constructor, a public `Value`, a static `From(string?)` and `ToString()`. Put both files in
`src/WeightTracker.Domain/Exercises/`.

7. Add `ExerciseName.cs` with `MinLength = 3` and `MaxLength = 150`. `From` trims the input. It throws
   `InvalidExerciseNameException` for null, empty or whitespace-only input, and when the trimmed
   length is outside 3–150. Add an XML doc explaining why it is not `PersonName`, as in plan §3.
8. Add `Url.cs` with `MaxLength = 500`. `From` trims the input. It throws `InvalidUrlException` when:
   - the value is null or whitespace
   - it is longer than 500 characters
   - `Uri.TryCreate(value, UriKind.Absolute, ...)` fails
   - the scheme is not `http` or `https`
   - the host has no `.`
9. Add `test/WeightTracker.Unit/Domain/Exercises/ExerciseNameTests.cs`, copying the structure of
   `PersonNameTests`. Cover the cases in plan §12.
10. Add `test/WeightTracker.Unit/Domain/Exercises/UrlTests.cs` and cover the cases in plan §12.
11. **Review (value objects):** check plan §13 "Value objects", and that every test follows
    Arrange/Act/Assert.

## Phase 3 — Exercise aggregate

12. Rework `src/WeightTracker.Domain/Exercises/Exercise.cs`:
    - Remove `NameMaxLength` and `VideoUrlMaxLength`.
    - `Name` becomes `ExerciseName { get; private set; }`.
    - `VideoUrl` becomes `Url? { get; set; }`.
    - `BodyPart` stays a public property with a backing field. Its setter throws
      `InvalidBodyPartException` when `Enum.IsDefined` is false.
    - Make the constructor `private Exercise(Id id, ExerciseName name, Url? videoUrl, BodyParts bodyPart, bool isNew)`.
      The only things that call it are:
      - `public static Exercise Create(ExerciseName name, Url? videoUrl, BodyParts bodyPart)`, with `Id.New()` and `isNew: true`.
      - `public static Exercise Rehydrate(Id id, ExerciseName name, Url? videoUrl, BodyParts bodyPart)`, with `isNew: false`.
    - Add `public void Update(ExerciseName name, Url? videoUrl, BodyParts bodyPart)`. It assigns all
      three fields. `BodyPart` goes through the guarded setter.
    - Do not call `AddDomainEvent`.
13. In `src/WeightTracker.Domain/Workouts/Workout.cs`, `RemoveExercise` passes `exercise.Name.Value`
    to `ExerciseNotInWorkoutException`. Change nothing else.
14. Add `test/WeightTracker.Unit/Domain/Exercises/ExerciseTests.cs`, copying the structure of
    `WorkoutTests`. Cover the `Exercise` cases in plan §12.
15. Retype the existing unit tests that build `Exercise` instances:
    - `test/WeightTracker.Unit/Domain/Workouts/WorkoutTests.cs`: change `new Exercise(...)` to
      `Exercise.Create(ExerciseName.From(...), null, ...)`, and pass value objects to `Rehydrate`.
    - `test/WeightTracker.Unit/Domain/Common/EntityEqualityTests.cs`
    - `test/WeightTracker.Unit/Infra/Mappers/AggregateMapperTests.cs`
    - `test/WeightTracker.Unit/Infra/Mappers/WorkoutMapperTests.cs`

    Change `"https://video"` to a URL with a dot in the host, such as `"https://videos.example.com/plancha"`.
    Update the matching assertions to compare value objects, or `.Value`.
16. Copy the new `Exercise.cs`, `Workout.cs` and `WorkoutTests.cs` into their `.stpr/template/`
    counterparts byte for byte.
17. In the Arrange/Act/Assert example in `.stpr/template/STPR_INVARIANTS.md` §4, change
    `new Exercise("Peso Muerto", BodyParts.BACK)` to
    `Exercise.Create(ExerciseName.From("Peso Muerto"), null, BodyParts.BACK)`. No rule text changes.
18. **Review (Exercise aggregate):** check plan §13 "Exercise aggregate". Run the parity check.

## Phase 4 — Persistence

19. `src/WeightTracker.Infra/Mappers/ExerciseMapper.cs`:
    - `MapToEntity` writes `Name.Value` and `VideoUrl?.Value`.
    - `MapToDomain` builds `ExerciseName.From`, uses `Url.From` only when the stored value is not
      null, and keeps `MapBodyPart`. The existing catch already wraps the new domain exceptions.
20. `WeightTrackerDbContext`: replace the literal lengths `150` and `500` on `ExerciseEntity` with
    `ExerciseName.MaxLength` and `Url.MaxLength`.
21. `CreateExercisesTable_202610080923`: point the two length comments at `ExerciseName.MaxLength` and
    `Url.MaxLength`. This is a comment-only change.
22. Adapt the Exercise call sites that are still failing to compile:
    - `src/WeightTracker.Infra/Seed/DatabaseSeeder.cs`: pass `ExerciseName.From(...)` to `Rehydrate`.
    - `src/WeightTracker.Api/Endpoints/Workouts/Dtos/WorkoutResponse.cs`: use `exercise.Name.Value`.
      The output must stay identical.
    - `test/WeightTracker.Integration/Repositories/WorkoutRepositoryTests.cs`

    The build must be green after this task.
23. Add `public Task Delete(Exercise exercise, CancellationToken cancellationToken);` to
    `IExerciseRepository`. Add an XML doc saying it stages only.
24. Implement `ExerciseRepository.Delete`:
    - If the entity is tracked, remove the tracked entity.
    - Otherwise, attach `ExerciseMapper.MapToEntity(exercise)` and remove it.
    - Never call `SaveChanges`.
25. Add `public Task<bool> ExistsByExerciseId(Id exerciseId, CancellationToken cancellationToken);`
    to `IWorkoutRepository`. Implement it in `WorkoutRepository` as an `AnyAsync` over
    `WorkoutExercises` where `ExerciseId == exerciseId.Value`.
26. Integration repository tests:
    - `WorkoutRepositoryTests`: `ExistsByExerciseId` is true for `PressBancaId` and false for `CurlBicepsId`.
      This depends on the seed in task 35, so run it after Phase 6.
    - New `test/WeightTracker.Integration/Repositories/ExerciseRepositoryTests.cs`: `Delete`, then
      commit through `IUnitOfWork`, then `GetById` returns `null`.
27. Run the existing `ExerciseMapper` round-trip tests, including the null URL case. All must pass.
28. **Review (persistence):** check plan §13 "Persistence".

## Phase 5 — Application

29. Add `src/WeightTracker.Application/UseCases/Exercises/ExerciseDTO.cs`:
    `public sealed record ExerciseDTO(string Name, string? VideoUrl, BodyParts BodyPart);`
    with no attributes.
30. Add the Application exceptions to `src/WeightTracker.Application/Exceptions/`, each deriving from
    `ApplicationLayerException`:
    - `ExerciseNotFoundException(Id id)`
    - `ExerciseInUseException(Id id)`
31. Add the use cases. Copy the shape of `GetAllExercisesUseCase`: one public `Execute`, with `ct` last.
    For the parse and rethrow pattern, follow `SignupUseCase`.
    - `GetExerciseByIdUseCase.Execute(Id id, ct)`: `GetById`; if it returns null, throw
      `ExerciseNotFoundException`.
    - `CreateExerciseUseCase.Execute(ExerciseDTO dto, ct)`:
      1. Parse the value objects. Parse `Url` only when `VideoUrl` is not null.
      2. `Exercise.Create`
      3. `Save`
      4. `SaveChangesAsync` once
      5. Return the `Exercise`.
    - `UpdateExerciseUseCase.Execute(Id id, ExerciseDTO dto, ct)`:
      1. `GetById`; if it returns null, throw `ExerciseNotFoundException`.
      2. Parse the value objects.
      3. `Update`
      4. `Save`
      5. `SaveChangesAsync` once
      6. Return the `Exercise`.
    - `DeleteExerciseUseCase.Execute(Id id, ct)`:
      1. `GetById`; if it returns null, throw `ExerciseNotFoundException`.
      2. Call `IWorkoutRepository.ExistsByExerciseId`; if it returns true, throw `ExerciseInUseException`.
      3. `Delete`
      4. `SaveChangesAsync` once
32. Register the four use cases in `src/WeightTracker.Api/Program.cs`, next to `GetAllExercisesUseCase`.
33. Add unit tests under `test/WeightTracker.Unit/Application/Exercises/`, one file per use case.
    Cover the cases in plan §12, with mocked repositories and `IUnitOfWork`. `Verify` the commit count
    and the absence of `Save` or `Delete` on failure paths.
34. **Review (use cases):** check plan §13 "Use cases".

## Phase 6 — Seed

35. Add `SeedData.CurlBicepsId = new("00000000-0000-0000-0000-000000000014")`.
36. In `DatabaseSeeder.SeedExercises`, add
    `Exercise.Rehydrate(Id.From(SeedData.CurlBicepsId), ExerciseName.From("Curl de Bíceps"), null, BodyParts.ARMS)`.
    Do not reference it from any seeded workout.
37. Add `SeedData.CurlBicepsId` to the id list in `test/WeightTracker.Unit/Infra/Seed/SeedDataTests.cs`.
38. **Review (seed):** check plan §13 "Seed". Make sure no existing seeded id or credential changed.

## Phase 7 — API

39. Add `src/WeightTracker.Api/Endpoints/Exercises/Dtos/ExerciseRequest.cs`. Copy the shape of `SignupRequest`:
    - `[Required] string Name`
    - `string? VideoUrl`
    - `[Required][EnumDataType(typeof(BodyParts))] BodyParts BodyPart`
    - a `ToDto()` method, or equivalent, that returns `ExerciseDTO`
40. Add `ExerciseResponse.cs` in the same folder:
    - `Guid Id`
    - `string Name`
    - `string? VideoUrl`
    - `string BodyPart`, as the enum name
    - `static From(Exercise)`
41. Add `src/WeightTracker.Api/Endpoints/Exercises/ExercisesController.cs` with `[Route("exercises")]`.
    Inject only the 5 use cases and `ExceptionMapper`.
    - `GET` and `GET {id:guid}`: `[AllowAnonymous]`
    - `POST`, `PUT {id:guid}` and `DELETE {id:guid}`: `[Authorize(Roles = nameof(Role.ADMIN))]`
    - `POST` returns `CreatedAtAction(nameof(GetById), ...)`.
    - Every action wraps its body in try/catch and calls `ExceptionMapper.Map`.
    - Every action declares `[ProducesResponseType]` for each status in plan §7: the DTO for success,
      `ValidationProblemDetails` for structural `400`, and `ProblemDetails` for the others.
42. Add the five new exceptions to `ExceptionMapper`:
    - `400`: `InvalidExerciseNameException`, `InvalidUrlException`, `InvalidBodyPartException`
    - `404`: `ExerciseNotFoundException`
    - `409`: `ExerciseInUseException`
43. **Review (API):** check plan §13 "API". Open `/scalar` in Development: the reads show no lock and
    the writes do.

## Phase 8 — Integration tests

Copy `AuthEndpointsTests` for the `401` matrix, and use `AuthenticatedClientFor(...)` with
`SeedCredentials` for every authenticated call. Do not mint tokens and do not write rows directly.

44. Add `test/WeightTracker.Integration/Exercises/ExerciseReadEndpointsTests.cs` for the
    `GET /exercises` and `GET /exercises/{id}` cases in plan §12. Do not add token cases, since the
    routes are public. Assert the exact set of 4 seeded ids.
45. Add `ExerciseCreateEndpointTests.cs` for the full `POST /exercises` matrix in plan §12.
46. Add `ExerciseUpdateEndpointTests.cs` for the full `PUT /exercises/{id}` matrix. Assert that the
    `404` case created no row (a follow-up GET still returns `404`). Assert that `videoUrl` was cleared.
47. Add `ExerciseDeleteEndpointTests.cs` for the full `DELETE /exercises/{id}` matrix. After the `409`,
    `Press Banca` is still returned by GET. After the `204`, `Curl de Bíceps` returns `404`.
48. Extend `OpenApiDocumentTests`:
    - Add `/exercises` and `/exercises/{id}` GET to the anonymous-operation theory.
    - Add a security-requirement assertion for POST, PUT and DELETE.
    - Add an `Exercises_ShouldDocumentItsResponses` case.
49. **Review (tests):** every route in plan §7 has its full matrix. Every test follows
    Arrange/Act/Assert with exactly one Act. No test depends on another test's writes.

## Phase 9 — Verification and final review

50. Run `dotnet build WeightTracker.slnx` and `dotnet test WeightTracker.slnx` with Docker running.
    Everything must pass, and the passing count must be at least the baseline from task 2 plus the
    new tests.
51. Run the `.stpr/template` parity check. It must report no drift.
52. Grep `src` and `test` for `Guid.NewGuid`, `Guid.CreateVersion7` and `new Exercise(`. None may
    have been added outside the existing allowed places.
53. Go through the `AGENTS.md` quality checklist and every item in plan §13. Optionally, run
    `.stpr/review/CHECKLIST_REVISION.md`.

---

## Completion status

All 53 tasks are done. The full `dotnet test WeightTracker.slnx` passes with Docker running:
270 unit and 125 integration tests, against a baseline of 208 and 70.

Deviations from the task text, all reflected in `plan.md`:

- **Task 3 / 51 (parity):** the check already reported drift on `test/WeightTracker.Integration/Auth/AuthEndpointsTests.cs`
  before this change started, because the signup commit extended the `src` file without refreshing the template
  copy. It is unrelated to this change and was not touched. Every file this change touched is identical to its copy.
- **Task 16:** the template also holds copies of `IExerciseRepository.cs` and `IWorkoutRepository.cs`. They changed,
  so they were synced too.
- **Task 24:** `ExerciseRepository.Delete` removes the tracked entity, or the mapped entity when none is tracked.
  It does not attach first.
- **Task 31:** the write use cases let value object exceptions propagate instead of catching and rethrowing.
- **Task 35–38 (seed):** done together with Phase 4 because the repository test needed the seeded id.
- **Task 41:** authorization attributes are on each action, not the class. A class-level `[AllowAnonymous]`
  would override the action-level `[Authorize(Roles = ...)]`.
- **Added after review:** `Url` rejects whitespace and a missing `scheme://`; mapper tests for stored invalid
  values; `ExceptionMapperTests` for the five new exceptions; `GET` with an empty GUID returns `400`.

Remaining gaps: none against the plan.
