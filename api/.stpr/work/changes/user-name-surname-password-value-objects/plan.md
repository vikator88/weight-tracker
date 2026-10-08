# Plan: User Name, Surname And Password As Value Objects

## 1. Change Summary

Refactor the `User` aggregate so `Name`, `Surname` and `PasswordHash` stop being raw `string`
properties and become domain value objects. No observable behavior changes: no endpoint, DTO,
response shape, status code, database schema or seeded credential is modified.

The change also introduces a `Password` value object carrying the password-strength rules. It is
deliberately **not** the type of `User.PasswordHash` — the aggregate holds a PBKDF2 hash, never a
plaintext, so strength rules belong at the hashing boundary (`IPasswordHasher.Hash`), not on the
stored value. Applying strength rules to the stored value would reject every row on rehydration.

Outcome: the length and strength rules that were implicit (`User.NameMaxLength` constants, and
nothing at all for password strength) become reusable, unit-tested domain abstractions.

## 2. Scope

### New modules

None.

### Existing modules affected

| Module | Change |
| --- | --- |
| `Domain/Users` | three new value objects; `User` retyped |
| `Domain/Exceptions` | three new `DomainException` subclasses |
| `Application/Interfaces` | `IPasswordHasher` signature retyped |
| `Infra` | `UserMapper`, `PasswordHasher`, `DatabaseSeeder` adapted |
| `Api` | `ExceptionMapper` gains explicit mappings for the new exceptions |
| `Migrations` | comment-only update to `CreateUsersTable_202610080917` (no schema change) |
| `test/WeightTracker.Unit` | three new value object test classes; existing `User`, mapper and login tests retyped |
| `test/WeightTracker.Integration` | `UserRepositoryTests` assertions retyped |

### Explicitly out of scope

- Any API change: no new endpoint, no DTO change, no response shape or status code change on
  existing routes. `POST /auth/login` keeps its exact contract.
- User registration, profile update, or change-password endpoints. `Password` therefore has only
  two call sites today (`DatabaseSeeder` and `IPasswordHasher.Hash`); it exists so the rule has one
  home when a write path arrives.
- Database schema: no migration is added. Column names, types and lengths stay exactly as they are.
- `Exercise.Name` (max 150) stays a `string`. Different module, different limit, different
  semantics — it is not folded into `PersonName`.
- `Email` and `Id` are untouched, so `Domain/Common` and its `.stpr/template/` copies are untouched.
- Re-hashing or rotating seeded credentials. `Passw0rd!` already satisfies the new strength rules.
- Domain event dispatching stays dormant.

## 3. Main Concepts

**`PersonName`** — `sealed record` in `Domain/Users`. Wraps a person's given name or family name.
Trims surrounding whitespace, rejects empty-after-trim, max 100 characters. One type for both
`Name` and `Surname`: the rules are identical, and two structurally identical value objects without
a stated semantic difference is a Mandatory Stop Case under the invariants.

**`PasswordHash`** — `sealed record` in `Domain/Users`. Wraps the already-hashed credential stored
on the aggregate. Validates non-empty and max 500 characters, aligned with `users.password_hash`.
Carries **no** strength rules: it describes a hash, not a plaintext.

**`Password`** — `sealed record` in `Domain/Users`. Wraps a plaintext password at the moment it
enters the system, and is the only place the strength rules live. Never stored, never held by an
aggregate, never logged.

**`User` (changed)** — `Name` and `Surname` become `PersonName`; `PasswordHash` becomes
`PasswordHash`. `Create` and `Rehydrate` parameter types change accordingly. The `NameMaxLength`,
`SurnameMaxLength` and `PasswordHashMaxLength` constants move off the aggregate and onto the value
objects that now own those limits.

## 4. Module Classification

| Piece | Classification | Reason |
| --- | --- | --- |
| `PersonName` | value object | no identity, compared by value, immutable, `record` |
| `PasswordHash` | value object | same |
| `Password` | value object | same; transient, never persisted |
| `User` | aggregate root (unchanged) | still owns credentials and profile state |
| `IPasswordHasher` / `PasswordHasher` | application contract + infrastructure service | hashing stays an infrastructure concern; the contract is retyped, not relocated |

No aggregate, entity, domain service or application service is introduced. The classification of
every existing piece is unchanged.

All three value objects live in `Domain/Users`, not `Domain/Common`: the invariants move a value
object to `Common` only when **two or more modules** share its semantics, and only the `Users`
module uses these. Keeping them out of `Common` also leaves the template-backed
`Domain/Common` files byte-identical, so no `.stpr/template/` parity work is incurred.

## 5. Canonical Template Mapping

| Capability | Reference | Origin |
| --- | --- | --- |
| `PersonName`, `PasswordHash`, `Password` | `.stpr/template/src/WeightTracker.Domain/Common/Email.cs` | **template-backed** |
| New domain exceptions | `src/WeightTracker.Domain/Exceptions/InvalidEmailException.cs` | skeleton-backed |
| `User` aggregate shape | `.stpr/template/src/WeightTracker.Domain/Workouts/Workout.cs` | **template-backed** |
| `UserMapper` adaptation | `src/WeightTracker.Infra/Mappers/UserMapper.cs` (itself the local anchor) | skeleton-backed |
| `ExceptionMapper` entries | `src/WeightTracker.Api/Common/ExceptionMapper.cs` | skeleton-backed |
| Value object unit tests | `test/WeightTracker.Unit/Domain/Common/EmailTests.cs` | skeleton-backed |

`Email` is the canonical value object reference and must be replicated exactly: `sealed record`,
private constructor, public `Value`, static `From(string?)` factory that throws a dedicated
`DomainException` subclass, `ToString()` returning the wrapped value. No new reference pattern is
established by this change, so `.stpr/template/README.md` needs no new entry.

## 6. State And Mutability

| State | Rule |
| --- | --- |
| `User.Name` | immutable after construction; type changes `string` → `PersonName`. No operation mutates it before or after this change. |
| `User.Surname` | same as `Name` |
| `User.PasswordHash` | immutable in practice; type changes `string` → `PasswordHash`. Mutable in principle, but no operation in the system changes it — unchanged from today. |
| `PersonName` / `PasswordHash` / `Password` | immutable by construction (`record`, private constructor, no setters) |
| `Password` instances | transient. Constructed at the hashing boundary, consumed immediately, never assigned to aggregate state and never persisted. |

No mutability classification changes. No piece becomes mutable or read-only as a result of this
change.

## 7. Endpoints And Visibility

**No endpoint is added, removed, or changed.** The visibility matrix of the API is identical before
and after this change:

| Method | Route | Visibility | Changed? |
| --- | --- | --- | --- |
| `POST` | `/auth/login` | public | no — request and response contract byte-identical |
| `POST` | `/auth/refresh` | public | no |
| `GET` | `/workouts` | authenticated, role-scoped | no |

No DTO reads `User.Name`, `User.Surname` or `User.PasswordHash` — verified across
`Endpoints/`, `JwtTokenService` and `AuthResponse` — so the retyping cannot reach the HTTP boundary.

**Critical constraint:** `LoginUseCase.Execute(string email, string password, CancellationToken)`
keeps its signature. The submitted password is **not** parsed through `Password`. A correct but
weak password must continue to return `401 Invalid credentials`, never a `400` strength error —
validating strength on the login path would both change behavior and disclose which stored
passwords are weak.

## 8. Business Rules

| Rule | Layer | Enforcement | New? |
| --- | --- | --- | --- |
| A person name is non-empty after trimming and at most 100 characters | Domain | `PersonName.From` throws `InvalidPersonNameException` | new (was an unenforced constant) |
| A person name is stored with surrounding whitespace removed | Domain | `PersonName.From` normalizes | new |
| A stored password hash is non-empty and at most 500 characters | Domain | `PasswordHash.From` throws `InvalidPasswordHashException` | new (was an unenforced constant) |
| A plaintext password is at least 8 characters | Domain | `Password.From` throws `InvalidPasswordException` | new |
| A plaintext password contains at least one uppercase letter | Domain | same | new |
| A plaintext password contains at least one digit | Domain | same | new |
| A plaintext password contains at least one special character (any non-alphanumeric) | Domain | same | new |
| A plaintext password is at most 128 characters | Domain | same | new — see assumption in §9 |
| Password strength is **not** evaluated when verifying credentials | Application | `LoginUseCase` passes the raw string to `IPasswordHasher.Verify` | unchanged behavior, stated to prevent regression |
| A stored hash that fails `PasswordHash` validation is a persistence fault, not caller input | Infra | `UserMapper.MapToDomain` already wraps `DomainException` into `PersistenceMappingException` → 500, logged | unchanged mechanism |

Every other business rule in the system is untouched.

## 9. Technical Decisions

**Password modeling — two value objects, not one.** `User.PasswordHash` is rehydrated directly from
the `users.password_hash` column, which holds a PBKDF2 output. A single strength-validating
`Password` type on that field would throw on every `MapToDomain` call. The responsibilities are
therefore split: `PasswordHash` types the stored value, `Password` types the plaintext at the
boundary. This keeps the aggregate's documented contract intact — it never sees a plaintext.

**`IPasswordHasher` contract change.** The interface lives in `Application/Interfaces` and
Application references Domain, so domain types in its signature respect the Dependency Rule.

```csharp
PasswordHash Hash(Password plainPassword);
bool Verify(string plainPassword, PasswordHash passwordHash);
```

`Verify` keeps `string` for the submitted plaintext deliberately: the caller is the login path,
which must not strength-validate (see §7). The service stays synchronous and token-free, per the
invariants on CPU-bound services.

**Constant ownership.** `User.NameMaxLength`, `User.SurnameMaxLength` and
`User.PasswordHashMaxLength` are deleted and replaced by `PersonName.MaxLength = 100` and
`PasswordHash.MaxLength = 500`. The alignment comments in `CreateUsersTable_202610080917` are
repointed at the new owners. The migration body is **not** modified — it is already applied, and the
schema is unchanged.

**Assumption, stated explicitly:** `Password.MaxLength = 128` was not requested. It is added as a
bound on PBKDF2 input so an unbounded plaintext cannot be fed to the hasher. If unwanted, drop the
constant and its test; nothing else depends on it.

**`PersonName` rules — length only, as specified.** No character-class restriction is applied: names
may contain digits, punctuation, or any script. Only trimming and length are enforced.

**Password special-character definition.** A special character is any character that is neither a
letter nor a digit. Whitespace counts as special and is **not** trimmed — unlike `PersonName`,
leading and trailing whitespace is significant in a password and must survive to the hasher.

**No persistence change.** `UserEntity` keeps `string` properties, `WeightTrackerDbContext`
configuration is untouched, and no FluentMigrator migration is added. `UserMapper` absorbs the
conversion in both directions (`user.Name.Value` outbound, `PersonName.From(entity.Name)` inbound),
which keeps mapping inside the mapper as the invariants require. `UserRepository.Save` operates on
`UserEntity` primitives and needs no change.

**Exception mapping.** `ExceptionMapper` already routes unmatched `DomainException` to 400, but the
invariants require an explicit mapping per custom exception. `InvalidPersonNameException`,
`InvalidPasswordException` and `InvalidPasswordHashException` are added to the existing 400 arm
alongside `InvalidEmailException`. No status code observable today changes, because no endpoint can
currently raise them.

**No new packages, no runtime change.** Every project already targets `net10.0`; SDK 10 and Docker
requirements are unchanged.

## 10. Seed And Test Bootstrap

Seeded records, identifiers and credentials are **unchanged**. `SeedData.Password` stays
`"Passw0rd!"`, and the three seeded users keep ids `...0001` / `...0002` / `...0003` with emails
`user@`, `trainer@`, `admin@weighttracker.test`.

`Passw0rd!` already satisfies every new strength rule — 9 characters, uppercase `P`, digit `0`,
special `!` — so `Password.From(SeedData.Password)` succeeds and no credential needs rotating. This
must be asserted by a test rather than assumed, because a future edit to `SeedData.Password` that
violated the rules would break seeding at runtime rather than at compile time.

`DatabaseSeeder` changes only in how it calls the hasher and builds users:
`_passwordHasher.Hash(Password.From(SeedData.Password))`, and `BuildUser` takes `PersonName`
arguments. The seeder stays idempotent and keeps running only in Development and from the
integration fixture.

The integration bootstrap (`ApiFactory`, `IntegrationTest`, truncate-and-reseed baseline,
`weight-tracker-test` database) is untouched.

## 11. Implementation Constraints

- The three value objects replicate `Email` exactly: `sealed record`, private constructor, public
  `Value`, static `From(string?)`, dedicated `DomainException` subclass, `ToString()` returning
  `Value`. No `class`, no public constructor, no `TryFrom` unless a caller needs it.
- All three live in `Domain/Users`. Do not add them to `Domain/Common` — that would break the
  single-module rule and force `.stpr/template/` parity updates.
- `Password` must never be assigned to aggregate state, persisted, serialized or logged. Its
  `ToString()` must **not** return the plaintext; it returns a masked constant.
- `LoginUseCase` must not construct a `Password`. Its signature and its
  `InvalidCredentialsException` behavior are frozen by this change.
- No FluentMigrator migration and no EF Core migration. The schema does not change.
- Mapping stays inside `UserMapper`; no value object conversion leaks into use cases, repositories
  or controllers.
- `UserMapper.MapToDomain` keeps its existing `catch (DomainException)` → `PersistenceMappingException`
  wrapper, which must now also cover `PersonName` and `PasswordHash` failures.
- Warnings are errors. Deleting the `User.*MaxLength` constants must not leave a stale reference.
- `.stpr/template/src` and `.stpr/template/test` must remain byte-identical to their `src`/`test`
  counterparts. This change touches no template-backed file; the parity check must still be run.

## 12. Minimum Test Coverage

**Domain unit tests** — `test/WeightTracker.Unit/Domain/Users/`, no infrastructure, no framework:

- `PersonNameTests`: accepts a valid name; accepts exactly 100 characters; rejects 101; rejects
  `null`, empty and whitespace-only; trims surrounding whitespace; two instances with the same value
  are equal; `ToString()` returns the value.
- `PasswordHashTests`: accepts a representative PBKDF2 hash; accepts exactly 500 characters; rejects
  501; rejects `null`, empty and whitespace-only; **accepts a weak-looking value such as `"hash"`**,
  proving strength rules are absent; value equality.
- `PasswordTests`: accepts a valid password; rejects shorter than 8; rejects missing uppercase;
  rejects missing digit; rejects missing special character; rejects longer than 128; rejects `null`
  and empty; preserves leading and trailing whitespace; `ToString()` does not reveal the plaintext.
- `PasswordTests` additionally asserts `Password.From(SeedData.Password)` succeeds — guarding the
  seeded credential against a future strength-rule or seed change. (If referencing `SeedData` from a
  domain test is undesirable, duplicate the literal `"Passw0rd!"` and name the test accordingly.)

**Updated existing unit tests:**

- `UserTests`: retyped to pass `PersonName` and `PasswordHash`; assertions compare value objects.
  Coverage is not reduced — `Create` sets `IsNew = true`, `Rehydrate` sets `IsNew = false` and
  restores every field, `CanTrain()` branches.
- `AggregateMapperTests`: the `User` round-trip must still assert `Name`, `Surname` and
  `PasswordHash` survive `MapToEntity` → `MapToDomain`, now as value objects.
- `LoginUseCaseTests`: `Mock<IPasswordHasher>` setups retyped to the new `Verify(string, PasswordHash)`
  signature. **Every existing branch must still be covered**: unknown email, malformed email and
  wrong password all throw `InvalidCredentialsException`; success returns tokens and persists exactly
  one refresh token.
- A new `LoginUseCaseTests` case: a correct password that would fail `Password` strength rules still
  authenticates successfully. This is the regression guard for §7.

**Infrastructure unit tests:**

- `UserMapper` round-trip, covered by the updated `AggregateMapperTests`.
- A mapper test asserting that a stored `users.name` or `users.password_hash` value violating the
  value object rules surfaces as `PersistenceMappingException`, not a raw `DomainException`.

**Integration tests** (real Postgres, migrations applied, seeded):

- `UserRepositoryTests` assertions retyped to the value objects (`user.Surname.Value`,
  `user.PasswordHash.Value`), keeping the existing check that the stored hash is not
  `SeedData.Password`.
- Seeding succeeds end to end against the real database — covered by the existing truncate-and-reseed
  baseline, which now exercises `Password.From(SeedData.Password)` on every test.
- The existing auth and workout suites must pass **unmodified**. That they need no edits is itself
  the evidence that no behavior changed; any required edit to `AuthEndpointsTests`,
  `WorkoutScopingTests`, `WorkoutResponseShapeTests` or `OpenApiDocumentTests` is a signal that the
  change leaked past the domain and must be investigated, not patched.

**Verification.** This change touches domain, mappers, seed and an auth-adjacent contract, so the
full `dotnet test WeightTracker.slnx` runs with the Postgres container up before it is considered
done. The `.stpr/template/` parity check runs as well.

## 13. Review Checkpoints

| # | Capability | Checkpoint |
| --- | --- | --- |
| 1 | `PersonName` | One value object serves both `Name` and `Surname`; no separate `Name`/`Surname` types exist; replicates the `Email` shape; lives in `Domain/Users`, not `Domain/Common` |
| 2 | `PasswordHash` | Types `User.PasswordHash`; enforces only non-empty and ≤500; contains **no** strength rules; rehydration of a real PBKDF2 hash succeeds |
| 3 | `Password` | Carries the strength rules; never assigned to aggregate state, persisted or logged; `ToString()` masks the plaintext |
| 4 | Login behavior | `LoginUseCase.Execute` signature unchanged; does not construct `Password`; a correct-but-weak password still yields 401, proven by a test |
| 5 | `User` aggregate | `Create`/`Rehydrate`/`IsNew` shape preserved; the three `*MaxLength` constants are gone with no stale references; aggregate still never holds a plaintext |
| 6 | Persistence | No migration added; `UserEntity` and `WeightTrackerDbContext` unchanged; conversion happens only inside `UserMapper`; column lengths still match the value object constants |
| 7 | Error handling | All three new exceptions derive from `DomainException` and have explicit `ExceptionMapper` entries; stored-value failures surface as `PersistenceMappingException` → 500 |
| 8 | Seed | Seeded records, ids and credentials unchanged; `Passw0rd!` passes the strength rules and a test proves it |
| 9 | Behavior parity | The API suites pass with no edits; no DTO, route, status code or response shape changed |
| 10 | Tests | Each new value object has its own unit test class; no existing test coverage was reduced while retyping |
| 11 | Template | `.stpr/template/` parity check reports no drift; `.stpr/template/README.md` needs no new entry, and none was added |
