
# Invariants

This file is the project's technical contract under the STPR method.

Use this file only for non-negotiable rules.

## 1. STPR Method Invariants

These rules describe the portable part of STPR. They should remain stable even if the stack, tool, input channel, or concrete persistence mechanism changes.

### Skeleton

- The project must have a stable technical base before requesting non-trivial generation.
- The technical base must define a minimum structure, local environment, and verification strategy so that each iteration does not start from scratch.
- If the project already exists, the project itself acts as the operating skeleton.

### Template

- The template must contain real, functional, and sufficiently representative references to guide faithful generation.
- Generation must not invent structure, naming, or patterns when an applicable canonical reference already exists.

### Plan

- Work must start from an explicit plan captured in files in this project, not only in chat context.
- The plan must be sufficient to execute the work without hidden assumptions that materially affect scope, visibility, mutability, data semantics, integration contract, or test expectations.
- If the change introduces or modifies a system-relevant identifier and that decision materially affects the implementation, the plan must state whether it can change.
- If the plan artifacts conflict with the selected reference, these invariants, or the module classification rules, planning must stop and request clarification before code generation.

#### Modeling

- Before generating code, every planned module must be classified explicitly according to its responsibility and behavior, using the taxonomy applicable to the project or current stack.
- The chosen classification must be sufficient to determine mutability, dependencies, validation shape, and the implementation pattern that applies.
- Do not classify a piece as read-only, catalog, or equivalent only because its public interface does not expose writes. If its state changes because of system behavior, that mutability must be modeled explicitly.
- Every relevant identifier supplied by external input and used in business decisions must be validated before executing the operation that depends on it.
- If one part of the system uses multiple relevant identifiers in the same operation, the plan must make clear the role of each and which ones require explicit validation.
- Do not reinterpret relation identifiers, reference ids, foreign identifiers, or lookup keys as uniqueness pre-checks for the owning piece. Those inputs are validated through their reference or existence rules, not as duplicates of another entity.
- By default, reuse a shared abstraction for externally visible identifiers or codes when the semantics are the same.
- Do not create identifier-specific abstractions unless the plan explicitly requires specific behavior, format, or semantics.
- If the system generates an identifier, it must be generated through a plan-approved abstraction, not through ad hoc composition scattered through application logic.
- Do not add preflight collision checks for system-generated identifiers unless the plan explicitly requires retry behavior.
- If a primitive value carries domain meaning, reusable validation, or business constraints, it must be modeled as a value object or equivalent domain abstraction instead of repeating primitive validations in application logic.
- If the same value object semantics are used in two or more modules, they must be moved by default to a shared `common` location instead of being duplicated per module.

### Execution

- Execution must implement what was approved in the plan without expanding scope by its own criteria.
- Execution must respect the selected reference and the active invariants.
- Execution is considered complete when it implements what was approved in the plan, respects the selected reference, complies with these invariants, and passes the verification required by the change.

#### Execution Validation

- Tests validate behavior; AI does not replace final validation.
- The required verification must be executed at the correct layer for the type of change.
- If verification fails, the work is not complete.

### Review

- Final review must check architecture, domain semantics, relevant coverage, and alignment with the plan.
- Review does not validate only functionality; it also validates sustainability and fidelity to the correct pattern.
- If recurrent deviations appear, improving the skeleton, template, invariants, or prompts must be evaluated.

### Mandatory Stop Cases

- Implementation must stop and the plan must be clarified if the plan says a module should replicate a read-only or catalog reference, but another rule indicates that its state changes internally.
- Implementation must stop and the plan must be clarified if the public interface looks read-only, but other system behaviors change that module's state.
- Implementation must stop and the plan must be clarified if a module is being called a catalog, read-only, or equivalent only because users cannot edit it directly.
- Implementation must stop and the plan must be clarified if two value objects look structurally identical and the plan does not explain why the semantic separation matters.

## 2. Architecture Invariants

The reference architecture is Clean Architecture. These rules must remain in place while that philosophy continues to be the base of the project.
CLEAN ARCHITECTURE IS A MUST

### The Dependency Rule

Source code dependencies point inward only. This is the rule the whole architecture rests on;
everything else in this section is a consequence of it.

| Project | May reference |
| --- | --- |
| `WeightTracker.Domain` | nothing |
| `WeightTracker.Application` | Domain |
| `WeightTracker.Infra` | Domain, Application |
| `WeightTracker.Api` | Domain, Application, Infra, Migrations |
| `WeightTracker.Migrations` | nothing |

- Domain must never reference Application, Infra, Api, Entity Framework, ASP.NET, or any other framework.
- Application defines the interfaces it needs (`Interfaces/`); Infra implements them. Dependency inversion
  is what keeps Application free of infrastructure.
- Adding a `ProjectReference` that points outward breaks the architecture. Treat it as a stop case.
- `Api -> Migrations` is a deliberate exception so the host can apply migrations at startup in Development.

### What "module" means here

There are no module files in this stack. A module is a **feature slice spread across the layer projects**,
named consistently in each:

```
WeightTracker.Domain/<Module>/            aggregates, entities, value objects, enums
WeightTracker.Application/UseCases/<Module>/   use cases
WeightTracker.Application/Interfaces/     repository and service contracts
WeightTracker.Infra/Repositories/         repository implementations
WeightTracker.Infra/Mappers/              aggregate mappers
WeightTracker.Api/Endpoints/<Resource>/   controller plus its Dtos folder
```

Wiring is dependency registration in `src/WeightTracker.Api/Program.cs`, not a module file.

### DDD building blocks

Classify every piece of a planned module as exactly one of these before writing code. The classification
determines mutability, who may reference it, and whether it gets a repository.

- **Aggregate root**: the entry point to a consistency boundary. Only aggregate roots get repositories and mappers.
  Extends `AggregateRoot`.
- **Entity**: has identity, lives *inside* an aggregate, is loaded and saved only through its root. Extends `Entity`.
- **Value object**: no identity, compared by value, immutable. Declared as a `record`.
- **Domain service**: domain logic that belongs to no single aggregate.
- **Application service**: orchestration reused by several use cases. Never contains business rules.

Rules that follow from the classification:

- A repository loads and saves a whole aggregate, never a fragment of one.
- One aggregate never holds a reference to another aggregate root. It holds the other root's `Id`.
- An aggregate protects its own invariants. If a rule spans two aggregates, it belongs in Application.
- Deciding something is "read-only" is a statement about its behaviour, not about its API surface. If system
  behaviour changes its state, it is mutable, whatever the endpoints expose.

### Aggregate lifecycle

- `Create(...)` is the factory for a new aggregate. It generates the `Id` and sets `IsNew = true`.
- `Rehydrate(...)` restores an aggregate from persistence. It takes the existing `Id` and sets `IsNew = false`.
- `IsNew` is load-bearing: repositories branch on it to decide insert versus update. Never set it by hand
  outside these two factories.
- Constructors stay private so an aggregate cannot be built in an invalid state.

### Domain events: dormant

`AggregateRoot` exposes `AddDomainEvent`, `DomainEvents` and `ClearDomainEvents`, and `IDomainEvent` is defined,
but **no dispatcher exists**. Events raised today would never be delivered.

- Do not call `AddDomainEvent` until a change introduces a dispatcher and the plan approves it.
- A change that wants domain events must add the dispatch mechanism as part of its own scope.

- Domain in DDD
  - Should define Rehydrate, that requires Id
  - Should have IsNew, true when using constructor, false when using rehydrate
  - If you need to create something reusable in Domain that doesn't fit into any aggregate or entity, create as DomainService
  - ValueObjects must be defined with records, not classes
- Application is creating UseCases. Don't use CQRS, use the same shape existing in GetAllExercisesUseCase.cs:
   - WhateverUseCase with one public method Execute.
   - If you need to create something reusable in Application for orchestration only, create as ApplicationService
- Infrastructure rules:
   - Use entity framework
   - Create as many EF entities as needed to represent what we have in Domain. Rule is 1 entity or aggregate should have their own table
   - Create mappers only for aggregate roots with this methods:
     - MapToEntity -> create as many EF entities as needed from Aggregate (i.e. BlogPost aggregate can have list of Comment entity)
     - MapToDomain -> read from one or more tables and use the Rehydrate method in the aggregate root to restore the aggregate status
- API rules:
   - Prefer REST shape for endpoints
   - One folder in Endpoints folder per resource
- Error handling:
   - Domain and Application must throw only custom-exceptions. We want to catch all possible exceptions in Domain/Application. This catch should: 1- log. 2- rethrown a custom exception
   - Validation lives in the layer that owns the rule. See the Validations section below
   - In API layer create a ExceptionMapper that transform all custom-exceptions to http responses with the status code that better fits. If exception mapper gets an exception not defined as custom in the previous layers, it should return a 500
- Validations: Do not validate everything everywhere. Validate only where it makes sense (i.e. GetAll should not validate anything but auth stuff)
   - Validations that should live in API: required DTOs, required QueryParams, JSON format, MaxRequestSize...
   - Validations that should live in Application: Checks against database (existing user, existing email), Authorization, Permissions (not the JWT ones, but something like: can this user access to this image?, can this user perform this operation?, etc...)
   - Validations that should live in Domain: domain invariants like: cannot do checkout of an empty cart, a task cannot be changed to "inProgress" if is already "delivered", etc...
- Migrations:
   - Fluent migrator
   - migration class name shape: ActionObject_YYYYMMDDHHmm. Example: AddWorkoutsTable_202610171342
     - Timestamp is the id of the migration
       - try to use random times, not just 1200. If everybody uses the same time, migrations created the same day will conflict
   - Entities and Aggregates Id must be Guids. Other stuff can be auto-increments (i.e. logs, histories, etc...) 

## 3. Testing Invariants

- The plan must specify the verification layers required for the change.
- Every relevant business rule must be covered by tests at the appropriate layer.
- Domain tests must not depend on infrastructure or the framework.
- Integration or E2E tests do not replace domain tests.
- If a change affects observable behavior, verification covering that behavior must be added or adjusted.
- Every new value object must have a unit test unless the plan explicitly waives it.
- Missing tests required by the change invalidate completion of execution.
- Tests that exercise an endpoint authenticate only through the API. Minting a token directly, or
  writing rows to fake an authenticated caller, is not acceptable. Mocking the token service inside a
  unit test is unaffected by this rule.
- Every protected route must cover at least `401` with no token and `401` with an invalid token.
  The matrix applies per route, never once per module.
- Public routes must not carry missing-token or invalid-token cases.
- When a response depends on the caller's role or ownership, every branch must be covered, and the
  assertions must state what is excluded as well as what is returned.
- Tests must not depend on the ordering or residue of other tests. Any tier that shares a database
  starts each test from a known baseline.

## 4. Stack Invariants: DOTNET + C# + EntityFramework

The rules in this section apply to the current reference implementation in DOTNET + C# + EntityFramework.

### Auth and Login

- JWT + RefreshToken is required at API level.

### API Layer

- Controllers inject use cases only. Never ORM repositories, database entities, or infrastructure services.
- All controller handlers catch domain and application errors and delegate to `ExceptionMapper.Map(exception)`.
- Endpoint visibility must be explicit in the plan whenever API behavior is introduced or changed.
- Domain objects never cross the HTTP boundary. Every response is a DTO declared in
  `Endpoints/<Resource>/Dtos`, built by a `static From(...)` factory on the DTO itself.
- Request DTOs carry only structural validation (`[Required]`, types, sizes). Business validation
  belongs to Application or Domain.
- Every action declares its responses with `[ProducesResponseType]`, typed to the DTO it returns and
  to `ProblemDetails` for the failures the endpoint can produce. The OpenAPI document and the API
  reference are generated from these, so an undocumented action degrades both.
- Authentication is published to OpenAPI by the transformers in `WeightTracker.Api/OpenApi`. The
  built-in generator does not derive security schemes from the registered authentication, so a new
  protected endpoint gets its requirement from `[Authorize]` metadata and needs no extra wiring.

### Naming: two meanings of "entity"

The word is overloaded, so the suffix disambiguates:

- `Entity` / `AggregateRoot` in `Domain/Common` are the **DDD** base types. Domain types carry no suffix
  (`User`, `Workout`, `Exercise`).
- Types in `Infra/Entities` are **persistence rows** and always end in `Entity` (`UserEntity`, `WorkoutEntity`).
- One domain aggregate may map to several `*Entity` types, one per table.

### Identifiers

- `Id` (`WeightTracker.Domain/Common/Id.cs`) is the shared identifier of every entity and aggregate root.
- `Id.New()` is the only identifier generation path. `Guid.CreateVersion7()` and `Guid.NewGuid()` must not appear anywhere else.
- Do not introduce per-entity identifier types. Reuse `Id` unless the plan explicitly justifies a different format or semantics.
- Value objects shared by two or more modules live in `WeightTracker.Domain/Common` (`Id`, `Email`), not duplicated per module.

### Domain and Repository Boundaries

- Repositories are defined in Application Layer, in `WeightTracker.Application/Interfaces`.
- Repositories are implemented in Infrastructure layer, in `WeightTracker.Infra/Repositories`.
- Repositories never call `SaveChanges`. Committing belongs to `IUnitOfWork`.
- Aggregates reference other aggregates by `Id` only. No navigation property may cross an aggregate root boundary.

### Repository contracts

Only aggregate roots get a repository. Keep these shapes so every repository reads the same:

| Intent | Signature |
| --- | --- |
| list | `Task<IEnumerable<T>> GetAll(CancellationToken)` |
| filtered list | `Task<IEnumerable<T>> GetAllBy<Criteria>(..., CancellationToken)` |
| single | `Task<T?> GetById(Id, CancellationToken)` |
| existence | `Task<bool> ExistsBy<Criteria>(..., CancellationToken)` |
| stage a write | `Task Save(T, CancellationToken)` |

- Repositories take and return **domain types only**. EF entities must never leak past the repository.
- "Not found" is `null`, never an exception. The caller decides whether that is an error.
- `Save` stages both inserts and updates, branching on `IsNew`. It never commits.

### Unit of work

- `IUnitOfWork` is the transaction boundary. It is scoped per request, sharing one `DbContext`
  with every repository resolved in that scope.
- The **use case** calls `SaveChangesAsync` exactly once, at the end of a successful operation.
  Controllers and repositories never call it.
- A use case that throws before committing leaves nothing persisted; the scoped `DbContext` is discarded.
- Several repositories writing in one use case commit together through that single call.

### Asynchrony and cancellation

- Every method that performs I/O is async and takes a `CancellationToken` as its last parameter.
  The parameter is required, not optional, so omissions fail to compile.
- Controllers accept `CancellationToken` as an action parameter; ASP.NET Core binds it to the request's
  abort token automatically. They pass it to the use case, which passes it to repositories and EF.
- Pure CPU-bound services (`IPasswordHasher`, `ITokenService`, `IClock`) stay synchronous and take no token.

### Persistence and Mapping

- Wiring EF entities with Domain requires Mappers in infrastructure layer. Mapping must occur inside the repositories
- FluentMigrator owns the schema. Never generate EF Core migrations, and never let EF create or alter tables.
- The EF model configuration must mirror the migrations exactly: same table names, same column names, same lengths.
- String column lengths must match the limits declared on the corresponding value object or aggregate constant.
- Every key is configured `.ValueGeneratedNever()`, because identifiers come from `Id.New()` and never from the database.
  Without it EF treats an already-populated key as an existing row and silently turns inserts into updates.
- A foreign key that crosses an aggregate boundary is declared with `HasOne<T>().WithMany()` and no navigation property,
  so EF orders writes to satisfy the real constraints while the aggregate boundary stays intact.
- Direct `DbContext` access outside a repository is allowed only in `WeightTracker.Infra/Seed` and in the integration test bootstrap.

### Errors

- `DomainException` (`WeightTracker.Domain/Exceptions`) is the base of every custom Domain exception.
- `ApplicationLayerException` (`WeightTracker.Application/Exceptions`) is the base of every custom Application exception.
- `ExceptionMapper` (`WeightTracker.Api/Common`) owns the exception-to-HTTP mapping and is the only place that decides status codes.
- Every custom exception must have an explicit mapping. Anything unmapped returns 500 and is logged.

### Tests

- `test/WeightTracker.Unit`: domain, application and mapper tests. No database, no HTTP, no framework host.
- `test/WeightTracker.Integration`: real Postgres from `infra/docker-compose.yml`, API driven through `WebApplicationFactory`.
- The integration bootstrap owns its own database and must create, migrate, truncate and reseed it without manual setup.

#### Arrange / Act / Assert

Every test body is split into labelled phases, separated by a blank line:

```csharp
[Fact]
public void RemoveExercise_ShouldRejectAnExerciseNotInTheWorkout()
{
    // Arrange
    var workout = Workout.Create(Id.New(), null, WorkoutDate);

    // Act
    var act = () => workout.RemoveExercise(new Exercise("Peso Muerto", BodyParts.BACK));

    // Assert
    act.Should().Throw<ExerciseNotInWorkoutException>();
}
```

- `// Act` marks exactly one statement: the behaviour under test. If the test needs two acts,
  it is testing two things and should be split.
- When constructing the subject *is* the behaviour under test, use a single `// Arrange & Act` label.
- The method named in the test name belongs in the Act phase, not buried inside an assertion.
- Exception tests put the `var act = () => ...` lambda in the Act phase and the `Should().Throw<T>()` in Assert.
- Mock `Setup` calls are Arrange; `Verify` calls are Assert.