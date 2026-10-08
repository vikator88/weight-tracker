# Verification: User Name, Surname And Password As Value Objects

Local execution and test flow for this change. Run from the `api/` directory.

## Prerequisites

- .NET SDK 10 (`dotnet --version` → `10.0.x`)
- Docker, for the Postgres 17 container

```bash
docker compose -f ../infra/docker-compose.yml up -d   # Postgres on 5432
```

## 1. Build

```bash
dotnet build WeightTracker.slnx    # warnings are errors
```

Expected: `Build succeeded. 0 Warning(s) 0 Error(s)`.

## 2. Test

```bash
dotnet test WeightTracker.slnx     # needs the container up
```

Expected: **Unit 185 / Integration 54 passing**, 0 failed.
Baseline before this change was Unit 121 / Integration 54, so the unit count must never
drop below 185 and the integration count must stay at 54 — an integration delta means the
refactor leaked past the domain.

Targeted runs while iterating:

```bash
# the three new value objects
dotnet test test/WeightTracker.Unit/WeightTracker.Unit.csproj \
  --filter "FullyQualifiedName~Domain.Users"

# the modified hasher and the seed guards
dotnet test test/WeightTracker.Unit/WeightTracker.Unit.csproj \
  --filter "FullyQualifiedName~PasswordHasherTests|FullyQualifiedName~SeedDataTests"

# the login regression guard (weak-but-correct password must still authenticate)
dotnet test test/WeightTracker.Unit/WeightTracker.Unit.csproj \
  --filter "FullyQualifiedName~LoginUseCaseTests"
```

## 3. Behavior parity check

This change must not alter observable API behavior. The proof is that these suites pass
**with no edits**:

```bash
dotnet test test/WeightTracker.Integration/WeightTracker.Integration.csproj \
  --filter "FullyQualifiedName~AuthEndpointsTests|FullyQualifiedName~WorkoutScopingTests|FullyQualifiedName~WorkoutResponseShapeTests|FullyQualifiedName~OpenApiDocumentTests"
```

If any of them needs a change, stop and investigate rather than patching the test.

## 4. Template parity

```bash
for f in $(cd .stpr/template && find src test \( -name '*.cs' -o -name '*.csproj' \) \
  -not -path '*/bin/*' -not -path '*/obj/*'); do
  cmp -s ".stpr/template/$f" "$f" || echo "DRIFT: $f"
done
```

Expected: no output. This change touches no template-backed file.

## 5. Optional end-to-end seeding check

Seeding is the only place `Password.From(SeedData.Password)` runs outside tests. The
integration suite exercises it on every test via truncate-and-reseed, so this step is
only needed when changing the seeder itself.

Use a scratch database — **do not run this against `weight-tracker`**, which holds local
development data, and note that `DatabaseSeeder.Seed()` returns early if *any* user row
already exists:

```bash
docker exec myapp-postgres psql -U admin -d postgres \
  -c 'DROP DATABASE IF EXISTS "weight-tracker-seedcheck";' \
  -c 'CREATE DATABASE "weight-tracker-seedcheck";'

ASPNETCORE_ENVIRONMENT=Development \
ConnectionStrings__Default="Host=localhost;Port=5432;Database=weight-tracker-seedcheck;Username=admin;Password=admin" \
dotnet run --project src/WeightTracker.Api/WeightTracker.Api.csproj
```

Then, against the running API (it binds to `http://localhost:5000` from `launchSettings.json`):

```bash
# 200, body carries accessToken / refreshToken / expiresIn
curl -s -X POST http://localhost:5000/auth/login -H 'Content-Type: application/json' \
  -d '{"email":"user@weighttracker.test","password":"Passw0rd!"}'

# 401 - wrong password
curl -s -X POST http://localhost:5000/auth/login -H 'Content-Type: application/json' \
  -d '{"email":"user@weighttracker.test","password":"WrongPass1!"}'

# 401, NOT 400 - the login path must never run the strength rules
curl -s -X POST http://localhost:5000/auth/login -H 'Content-Type: application/json' \
  -d '{"email":"user@weighttracker.test","password":"weak"}'
```

Clean up:

```bash
docker exec myapp-postgres psql -U admin -d postgres \
  -c 'DROP DATABASE IF EXISTS "weight-tracker-seedcheck";'
```

## 6. No schema change

This change adds no migration. Confirm the count is unchanged and that no EF Core
migration was generated:

```bash
ls src/WeightTracker.Migrations/Migrations/*.cs | wc -l        # 6
find src test -iname "*ModelSnapshot.cs" -o -iname "*Designer.cs" | grep -v '/obj/'   # empty
```
