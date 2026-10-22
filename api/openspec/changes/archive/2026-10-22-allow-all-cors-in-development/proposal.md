# Proposal

## Why

The front apps are tested from `localhost` and from devices on the local network (Expo web, Expo Go, a phone browser). Today the API registers no CORS policy, so browsers block calls from any other origin. The API also listens only on `localhost`, so other devices cannot reach it at all. Origins and LAN IPs change between sessions, so the development API has to accept any origin and listen on all interfaces.

## What Changes

- Add a CORS policy that allows any origin, header and method. It is applied only when the API runs in the `Development` environment, and it answers preflight `OPTIONS` requests before authentication runs.
- Outside `Development` (`Testing`, `Production`), no CORS policy is applied, and behavior is unchanged.
- Change the local launch configurations so the Development HTTP binding listens on `0.0.0.0`:
  - `.vscode/launch.json`: both `Debug API` configurations use `http://0.0.0.0:5199`, and Scalar still opens on `http://localhost:5199/scalar`.
  - `Properties/launchSettings.json`: the HTTP URL of the `http` and `https` profiles becomes `http://0.0.0.0:5000`. HTTPS stays on `https://localhost:5001`.
- Document in `AGENTS.md` how to reach the development API from a LAN device.

No ports, URLs or origins are hardcoded in `Program.cs`. Authorization is unchanged: protected routes still require a valid bearer token.

## Capabilities

### New Capabilities

- `development-host-access`: how local clients reach the API while it runs for development. Covers cross-origin access from any origin, reachability from the local network, and the guarantee that neither applies outside `Development`.

### Modified Capabilities

None. No existing capability changes its requirements.

## Template And Invariant Alignment

- Affected area: Api host configuration only (`Program.cs` pipeline and the launch configuration files). No Domain, Application or Infrastructure code, no aggregate, no catalog, and no business identifier is involved.
- Canonical reference: the existing environment-gated blocks in `src/WeightTracker.Api/Program.cs` (`MapOpenApi`, `MapScalarApiReference`, startup migration and seed), which are already guarded by `app.Environment.IsDevelopment()`. The launch URLs already live in `ASPNETCORE_URLS` and `applicationUrl`, and they are changed there.
- Test reference: `test/WeightTracker.Integration/OpenApi/OpenApiDocumentTests.cs`, the existing host-level integration test that asserts environment-dependent pipeline behavior.
- `Program.cs` is not one of the curated copies under `.stpr/template/src`, so no template parity update is needed.
- No conflict with `STPR_INVARIANTS.md` or `AGENTS.md`. Following "never hardcode ports" in `AGENTS.md`, the binding change stays in launch configuration.

## Acceptance Criteria

- In `Development`, a browser on another origin, including a device on the LAN, can complete a preflighted, authenticated request against the API.
- In `Development`, protected routes still return `401` without a valid token.
- Under `Testing`, a request with an `Origin` header gets no `Access-Control-Allow-Origin` header (covered by an integration test).
- Starting the API with either VS Code debug configuration or the `http` profile makes it reachable at `http://<machine-lan-ip>:<port>` from another device.
- `dotnet build WeightTracker.slnx` and `dotnet test WeightTracker.slnx` pass with Docker running.

## Impact

- Code: `src/WeightTracker.Api/Program.cs` (CORS registration and Development-only middleware).
- Configuration: `.vscode/launch.json`, `src/WeightTracker.Api/Properties/launchSettings.json`.
- Tests: one new integration test class under `test/WeightTracker.Integration/`.
- Docs: `AGENTS.md`, "Running And Verifying Locally".
- Security: the development API, along with its seeded credentials, becomes reachable from the local network. This is accepted for development on a trusted network. Production and Testing are not affected.
- Out of scope: an origin allow-list for deployed environments, credentialed CORS (cookies), HTTPS on the LAN, front-end changes, and the Postgres compose binding.
