# Tasks: User Name, Surname And Password As Value Objects

Execute in order. Each phase ends with `dotnet build WeightTracker.slnx` (warnings are errors);
phases 2 onward also run the affected tests.
Reference: `plan.md` in this folder, `.stpr/template/STPR_INVARIANTS.md`, `.stpr/template/README.md`, `AGENTS.md`.

Expect the build to be **red from task 6 until task 17**. The aggregate is retyped before its
consumers are adapted; that is intentional, and the compiler is the worklist for finding every call
site. Do not patch consumers by calling `.Value` to silence errors — adapt them as specified.

---

## Phase 0 — Prerequisites

1. Start Postgres: `docker compose -f ../infra/docker-compose.yml up -d`. Verify it listens on 5432.
2. Confirm `dotnet --version` resolves to 10.0.x, then run `dotnet build WeightTracker.slnx` and
   `dotnet test WeightTracker.slnx` on the untouched tree. **Record the passing test count** — it is
   the baseline that must not drop.
3. Run the `.stpr/template/` parity check from `AGENTS.md` and confirm it reports no drift before any
   edit.

## Phase 1 — Domain exceptions

4. Add `src/WeightTracker.Domain/Exceptions/InvalidPersonNameException.cs`, deriving from
   `DomainException`, replicating `InvalidEmailException`. Message: `'{value}' is not a valid person name`.
5. Add `src/WeightTracker.Domain/Exceptions/InvalidPasswordHashException.cs`, deriving from
   `DomainException`. The message must **not** include the offending value — it is credential
   material. Use a fixed message such as `The stored password hash is not valid`.
6. Add `src/WeightTracker.Domain/Exceptions/InvalidPasswordException.cs`, deriving from
   `DomainException`. The message must **not** include the plaintext. State the unmet rule only,
   e.g. `The password does not meet the strength requirements`.

## Phase 2 — Value objects

Replicate `.stpr/template/src/WeightTracker.Domain/Common/Email.cs` exactly: `sealed record`,
private constructor, public `Value`, static `From(string?)`, `ToString()`. All three files go in
`src/WeightTracker.Domain/Users/`.

7. Add `PersonName.cs` — `public const int MaxLength = 100`. `From` trims, throws
   `InvalidPersonNameException` on null/empty/whitespace-only and on length > `MaxLength` after
   trimming. No character-class validation. `ToString()` returns `Value`.
8. Add `PasswordHash.cs` — `public const int MaxLength = 500`. `From` throws
   `InvalidPasswordHashException` on null/empty/whitespace-only and on length > `MaxLength`. **No
   trimming and no strength rules.** `ToString()` returns `Value`.
9. Add `Password.cs` — `public const int MinLength = 8`, `public const int MaxLength = 128`. `From`
   throws `InvalidPasswordException` unless the value is non-null, within the length bounds, and
   contains at least one uppercase letter, one digit and one character that is neither a letter nor
   a digit. **Do not trim**: leading and trailing whitespace is significant. Override `ToString()`
   to return a masked constant such as `"********"`, never `Value`.
10. Write `test/WeightTracker.Unit/Domain/Users/PersonNameTests.cs` — valid name; exactly 100 chars
    accepted; 101 rejected; null, empty and whitespace-only rejected; surrounding whitespace trimmed;
    value equality; `ToString()` returns the value. Arrange/Act/Assert labels, one Act per test.
11. Write `test/WeightTracker.Unit/Domain/Users/PasswordHashTests.cs` — a representative PBKDF2 hash
    accepted; exactly 500 chars accepted; 501 rejected; null, empty and whitespace-only rejected;
    value equality. Include an explicit test that `PasswordHash.From("hash")` **succeeds**, naming it
    so the intent is clear (no strength rules on a stored hash).
12. Write `test/WeightTracker.Unit/Domain/Users/PasswordTests.cs` — valid password accepted; rejected
    when shorter than 8, when missing an uppercase letter, when missing a digit, when missing a
    special character, when longer than 128, and when null or empty; leading and trailing whitespace
    preserved in `Value`; `ToString()` does not contain the plaintext.
13. Add to `PasswordTests` a case asserting `Password.From("Passw0rd!")` succeeds, named so it reads
    as the guard it is (the seeded credential must satisfy the rules). Keep the literal in the test
    rather than referencing `SeedData` — a domain test must not depend on infrastructure.
14. Run `dotnet test test/WeightTracker.Unit` filtered to the three new classes. They must pass
    before the aggregate is touched.

## Phase 3 — User aggregate

15. Retype `src/WeightTracker.Domain/Users/User.cs`: `Name` and `Surname` become `PersonName`,
    `PasswordHash` becomes `PasswordHash`. Update the private constructor, `Create` and `Rehydrate`
    parameter types to match.
16. Delete `User.NameMaxLength`, `User.SurnameMaxLength` and `User.PasswordHashMaxLength`. Keep the
    class-level `<remarks>` stating the aggregate never sees a plain-text password, and extend it to
    note that the stored value is typed `PasswordHash`.
17. Update `test/WeightTracker.Unit/Domain/Users/UserTests.cs` to construct `PersonName` and
    `PasswordHash` arguments. Assertions compare value objects (`user.Name.Should().Be(PersonName.From("Ada"))`)
    rather than unwrapping to strings. Do not remove any existing test case.

## Phase 4 — Application contract

18. Retype `src/WeightTracker.Application/Interfaces/IPasswordHasher.cs`:
    `PasswordHash Hash(Password plainPassword);` and `bool Verify(string plainPassword, PasswordHash passwordHash);`.
    Keep it synchronous and token-free. Add an XML comment on `Verify` explaining why the first
    parameter stays `string`: the login path must not strength-validate a submitted password.
19. Verify `src/WeightTracker.Application/UseCases/Auth/LoginUseCase.cs` compiles **unchanged**. Its
    `Execute(string email, string password, CancellationToken)` signature is frozen and it must not
    construct a `Password`. The only thing that changed is that `user.PasswordHash` is now a
    `PasswordHash` flowing into `Verify`.
20. Confirm no other use case or application service references `User.Name`, `User.Surname` or
    `User.PasswordHash`. If one does, adapt it without changing its public signature.

## Phase 5 — Infrastructure

21. Update `src/WeightTracker.Infra/Services/PasswordHasher.cs` to the new signature:
    `Hash` takes `Password` and returns `PasswordHash.From(_hasher.HashPassword(HashedSubject, plainPassword.Value))`;
    `Verify` takes `PasswordHash` and reads `passwordHash.Value`. Keep the existing
    `FormatException` guard and the empty-input guard — a malformed stored hash must still fail
    verification rather than crash.
22. Update `src/WeightTracker.Infra/Mappers/UserMapper.cs`: `MapToEntity` writes `user.Name.Value`,
    `user.Surname.Value`, `user.PasswordHash.Value`; `MapToDomain` wraps `PersonName.From(entity.Name)`,
    `PersonName.From(entity.Surname)`, `PasswordHash.From(entity.PasswordHash)`. The existing
    `catch (DomainException)` → `PersistenceMappingException` wrapper already covers the new throws —
    confirm it is not narrowed.
23. Confirm `src/WeightTracker.Infra/Entities/UserEntity.cs` and
    `src/WeightTracker.Infra/Persistence/WeightTrackerDbContext.cs` need **no change**. The EF entity
    keeps `string` properties and the configuration keeps its explicit lengths. If either needs a
    change, stop — it means a value object leaked into persistence.
24. Confirm `src/WeightTracker.Infra/Repositories/UserRepository.cs` needs **no change**. `Save`
    copies `UserEntity` primitives onto the tracked entity and never touches a value object.
25. Update `src/WeightTracker.Infra/Seed/DatabaseSeeder.cs`: hash once via
    `_passwordHasher.Hash(Password.From(SeedData.Password))`, and change `BuildUser` to take
    `PersonName` parameters (or to wrap its string arguments internally — pick one and apply it
    consistently). Seeded names, emails, ids, roles and birthdates are unchanged.
26. Confirm `src/WeightTracker.Infra/Seed/SeedData.cs` needs **no change**. `Password` stays
    `"Passw0rd!"`.

## Phase 6 — API

27. Add `InvalidPersonNameException`, `InvalidPasswordException` and `InvalidPasswordHashException`
    to the existing 400 arm of `src/WeightTracker.Api/Common/ExceptionMapper.cs`, alongside
    `InvalidEmailException`. The `DomainException` catch-all already returns 400, but the invariants
    require an explicit mapping per custom exception.
28. Confirm `src/WeightTracker.Api/Program.cs` needs **no change** — no registration is added or
    retyped.
29. Confirm no DTO under `src/WeightTracker.Api/Endpoints/` references `User.Name`, `User.Surname` or
    `User.PasswordHash`. If one does, stop and reassess: the plan asserts the HTTP surface is untouched.

## Phase 7 — Migrations

30. In `src/WeightTracker.Migrations/Migrations/CreateUsersTable_202610080917.cs`, repoint the three
    alignment comments from `User.NameMaxLength` / `User.SurnameMaxLength` / `User.PasswordHashMaxLength`
    to `PersonName.MaxLength` (×2) and `PasswordHash.MaxLength`. **Comments only** — the migration
    body, column names, types and lengths must stay byte-identical, and no new migration is created.
31. Confirm no EF Core migration was generated anywhere in the tree.

## Phase 8 — Test adaptation

32. Update `test/WeightTracker.Unit/Infra/Mappers/AggregateMapperTests.cs`: build the `User` with
    value objects and assert the round-trip preserves `Name`, `Surname` and `PasswordHash` as value
    objects. Do not reduce what the test asserts.
33. Add to `AggregateMapperTests` a case asserting that a `UserEntity` whose `Name` is empty (or whose
    `PasswordHash` is empty) surfaces from `MapToDomain` as `PersistenceMappingException`, not as a
    raw `DomainException`.
34. Update `test/WeightTracker.Unit/Application/Auth/LoginUseCaseTests.cs`: retype the
    `Mock<IPasswordHasher>` setups to `Verify(It.IsAny<string>(), It.IsAny<PasswordHash>())` and build
    seeded `User` instances with value objects. **Every existing case must survive**: unknown email,
    malformed email, wrong password, and the success path persisting exactly one refresh token.
35. Add to `LoginUseCaseTests` the regression guard from plan §7: a user authenticating with a
    correct password that would fail `Password` strength rules (for example `"weak"`) succeeds and
    returns tokens. This proves the login path does not strength-validate.
36. Update `test/WeightTracker.Integration/Repositories/UserRepositoryTests.cs`: retype assertions to
    `user.Surname.Value` / `user.PasswordHash.Value`, keeping the existing check that the stored hash
    differs from `SeedData.Password`.
37. Run `dotnet test test/WeightTracker.Unit`. All unit tests must pass, and the count must be the
    Phase 0 baseline **plus** the new cases — never lower.

## Phase 9 — Verification

38. With the Postgres container up, run the full `dotnet test WeightTracker.slnx`.
39. Confirm `test/WeightTracker.Integration/Auth/AuthEndpointsTests.cs`,
    `Workouts/WorkoutScopingTests.cs`, `Workouts/WorkoutResponseShapeTests.cs`,
    `Migrations/MigrationsTests.cs` and `OpenApi/OpenApiDocumentTests.cs` pass **with zero edits**.
    If any required an edit, stop and investigate: the refactor leaked past the domain.
40. Run `dotnet run --project src/WeightTracker.Api` in Development against the dev database and
    confirm seeding still succeeds — this is the only place `Password.From(SeedData.Password)` runs
    outside tests.
41. Re-run the `.stpr/template/` parity check. It must report no drift; this change touches no
    template-backed file.
42. Confirm `.stpr/template/README.md` was **not** modified. No new canonical reference is introduced.

## Phase 10 — Review

One checkpoint per capability, from plan §13. Review against the plan, not against the diff alone.

43. **Review `PersonName`** — one type for both fields; no separate `Name`/`Surname` types; `Email`
    shape replicated; located in `Domain/Users`, not `Domain/Common`; unit-tested.
44. **Review `PasswordHash`** — types the aggregate field; non-empty and ≤500 only; grep the file for
    any strength check and confirm there is none; a real PBKDF2 hash round-trips.
45. **Review `Password`** — grep the solution for `Password` usages and confirm it appears only in
    `IPasswordHasher.Hash`, `PasswordHasher.Hash`, `DatabaseSeeder` and its own tests. Confirm it is
    never a field, never serialized, never logged, and that `ToString()` masks.
46. **Review login behavior** — `LoginUseCase` diff is empty; no `Password.From` on the login path;
    the weak-but-correct password test passes.
47. **Review `User`** — Create/Rehydrate/IsNew shape intact; the three `*MaxLength` constants are
    gone with no stale references anywhere; the aggregate still never holds a plaintext.
48. **Review persistence** — no migration added, no EF migration generated; `UserEntity`,
    `WeightTrackerDbContext` and `UserRepository` diffs are empty; conversion lives only in
    `UserMapper`; column lengths still match `PersonName.MaxLength` and `PasswordHash.MaxLength`.
49. **Review error handling** — all three exceptions derive from `DomainException` and have explicit
    `ExceptionMapper` entries; neither password exception message leaks credential material;
    stored-value failures surface as `PersistenceMappingException` → 500, logged.
50. **Review seed** — seeded records, ids and credentials unchanged; the `Passw0rd!` strength guard
    test exists and passes; seeding verified against the real database in task 40.
51. **Review behavior parity** — no DTO, route, status code or response shape changed; the API suites
    passed unmodified.
52. **Review tests** — three new value object test classes exist; no existing case was deleted while
    retyping; the final test count exceeds the Phase 0 baseline.
53. Run the `AGENTS.md` quality checklist end to end before declaring the change done.

## Optional

54. The `Password.MaxLength = 128` bound is an assumption flagged in plan §9, not a stated
    requirement. Confirm with the requester, or drop the constant and its test.

---

## Completion Status

Recorded at finalization. Verification flow: `verification.md` in this folder.

| Phase | Tasks | Status |
| --- | --- | --- |
| 0 — Prerequisites | 1–3 | done (baseline Unit 121 / Integration 54, parity clean) |
| 1 — Domain exceptions | 4–6 | done |
| 2 — Value objects | 7–14 | done |
| 3 — User aggregate | 15–17 | done |
| 4 — Application contract | 18–20 | done |
| 5 — Infrastructure | 21–26 | done |
| 6 — API | 27–29 | done |
| 7 — Migrations | 30–31 | done (comments only; no schema change) |
| 8 — Test adaptation | 32–37 | done |
| 9 — Verification | 38–42 | done |
| 10 — Review | 43–53 | done |
| Optional | 54 | **open — awaiting decision** |

### Added during finalization, beyond the original task list

- `test/WeightTracker.Unit/Infra/Seed/SeedDataTests.cs` — binds the strength guard to the
  real `SeedData.Password` constant. Plan §10 requires the seeded credential to be
  "asserted by a test rather than assumed"; task 13 satisfied the letter of that with a
  hardcoded literal, which would not fail if `SeedData.Password` were weakened. Verified
  by temporarily setting it to `"weak"` and confirming the test goes red.
- `test/WeightTracker.Unit/Infra/Services/PasswordHasherTests.cs` — covers the hasher,
  whose signature this change retyped and whose empty-hash guard it removed. Includes the
  corrupt-stored-hash path, which now relies on `PasswordHash` plus the `FormatException`
  catch rather than the deleted guard.

### Remaining gap

**Task 54** — `Password.MaxLength = 128` is an assumption flagged in plan §9, not a stated
requirement. It is implemented and tested. Drop the constant and its two test cases
(`From_ShouldAcceptAPasswordOfExactlyMaxLength`, `From_ShouldRejectAPasswordLongerThanMaxLength`)
if unwanted.
