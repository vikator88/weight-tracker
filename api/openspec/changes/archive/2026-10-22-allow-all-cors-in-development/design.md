# Design

## Context

See proposal.md for the motivation and specs/development-host-access/spec.md for the requirements.

Current state:

- `Program.cs` registers no CORS services. Its pipeline is: an `IsDevelopment()` block (Scalar, startup migration, seed), then `UseHttpsRedirection()`, `UseAuthentication()`, `UseAuthorization()` and `MapControllers()`. `UseRouting()` is not called explicitly; `WebApplication` adds it at the start of the pipeline.
- The integration host (`ApiFactory`) runs with `UseEnvironment("Testing")` on an in-memory `TestServer`, so it never exercises Development-only code or real network bindings.
- The local URLs come from `ASPNETCORE_URLS` in `.vscode/launch.json` (`http://localhost:5199`) and `applicationUrl` in `Properties/launchSettings.json` (`http://localhost:5000`, `https://localhost:5001`).
- `Program.cs` is not one of the curated copies under `.stpr/template/src`.

Layer mapping: only the Api host is touched. Application, Domain and Infrastructure have no changes, and no cross-module dependency is introduced. The repository pattern and the `AuthorizedController` authorization approach are unchanged.

## Goals / Non-Goals

**Goals:**

- Development-only allow-all CORS that answers preflight requests before authentication.
- LAN reachability through launch configuration, with no URL or port in code.
- A regression test that locks CORS off outside Development.

**Non-Goals:**

- A configurable origin list or an `appsettings` key for CORS. Nothing reads such a key yet, and the deployed origin list belongs to a later change.
- An automated test of the Development behavior. See Decision 4.

## Decisions

### 1. Register always, apply only in Development

`builder.Services.AddCors(...)` declares one named policy (`AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod()`). `app.UseCors(<policy>)` is called inside the existing `if (app.Environment.IsDevelopment())` block. The policy name is a single `const` in `Program.cs`, next to the registration.

- The existing Development block already gates everything that is Development-only, so CORS joins it instead of starting a new pattern.
- That block runs before `UseHttpsRedirection()`, `UseAuthentication()` and `UseAuthorization()`, and after the implicit `UseRouting()`. That is the order ASP.NET Core requires for CORS: preflight `OPTIONS` requests are answered without a token and are never redirected.
- Alternative considered: a default policy (`AddDefaultPolicy` + `UseCors()`). Rejected because a named policy makes it explicit at the call site which policy is applied, and it leaves room for a separate deployed policy later.
- Alternative considered: registering the services only in Development. Rejected because registration without `UseCors` has no runtime effect, and gating both lines would duplicate the environment check.

### 2. No credentials

`AllowAnyOrigin` cannot be combined with `AllowCredentials`, and the API authenticates with bearer tokens in the `Authorization` header, not cookies. The policy omits credentials, so the response carries `Access-Control-Allow-Origin: *`.

### 3. LAN binding in launch configuration, not code

- `.vscode/launch.json`: `ASPNETCORE_URLS` becomes `http://0.0.0.0:5199` in both `Debug API` configurations.
  - Kestrel then logs `Now listening on: http://0.0.0.0:5199`, and `serverReadyAction` would open `0.0.0.0` in the browser. Its `uriFormat` becomes the fixed `http://localhost:5199/scalar`.
  - The `pattern` stays as it is, so it still waits for Kestrel to start.
- `Properties/launchSettings.json`: the HTTP URL of the `http` and `https` profiles becomes `http://0.0.0.0:5000`. `https://localhost:5001` is unchanged, because the ASP.NET Core dev certificate is only valid for `localhost`.
- Alternative considered: `http://*:5199` or `http://+:5199`. Rejected in favor of `0.0.0.0`, which is the explicit IPv4 any-address and is what Expo tooling and LAN devices use.

### 4. Testing strategy

- One integration test class at `test/WeightTracker.Integration/Cors/CorsPolicyTests.cs`, modeled on `OpenApi/OpenApiDocumentTests.cs` (an `IntegrationTest` subclass that checks host-level pipeline behavior).
  - The test sends anonymous `GET /exercises` with an `Origin` header.
  - It asserts that the response is `200` and has no `Access-Control-Allow-Origin` header.
  - It uses labelled Arrange/Act/Assert phases, and the request needs no token because the route is public.
- The Development scenarios are verified manually: Scalar on `localhost`, then Expo web and a browser or Expo Go on a LAN device.
  - Booting the factory as `Development` would run the startup migration and seed against the development connection string, which the integration suite must never touch (`AGENTS.md`).
  - Adding a test-only environment switch would change the test host pattern from the template, so it is not added.
- No unit tests, because there is no Domain or Application logic.

## Risks / Trade-offs

- [The development API and its seeded credentials are reachable by anyone on the same network.] → Accepted for local development on a trusted network. It applies only to the repository launch configurations, never to deployed environments. The `AGENTS.md` note says so.
- [The `https` profile redirects HTTP to `https://localhost:5001`. A LAN device following that redirect fails, and a redirected preflight fails in the browser.] → `AGENTS.md` tells LAN clients to use the VS Code debug configurations or the `http` profile.
- [The macOS firewall may block incoming connections the first time the process binds to `0.0.0.0`, and LAN devices time out.] → Mentioned in `AGENTS.md`: allow the prompt.
- [A future change moves `UseCors` after `UseAuthentication`, and preflight requests to protected routes start returning `401`.] → Covered by the manual preflight check, and the placement is explained in a comment next to the call.

## Migration Plan

No data or schema migration. Rolling back means reverting `Program.cs` and the two launch configuration files.
