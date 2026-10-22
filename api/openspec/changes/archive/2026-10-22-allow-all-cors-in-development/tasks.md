# Tasks

## 1. Api: Development-only CORS

- [x] 1.1 In `src/WeightTracker.Api/Program.cs`, add a `const` policy name and register `builder.Services.AddCors(...)` with one named policy using `AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod()` (no credentials), next to the other `builder.Services` registrations; verify `dotnet build WeightTracker.slnx` passes with warnings as errors
- [x] 1.2 Inside the existing `if (app.Environment.IsDevelopment())` block, call `app.UseCors(<policy>)` with a short comment explaining that it must stay before `UseHttpsRedirection`/`UseAuthentication` so preflight requests are answered without a token or redirect; verify the build passes
- [x] 1.3 Add `test/WeightTracker.Integration/Cors/CorsPolicyTests.cs` modeled on `OpenApi/OpenApiDocumentTests.cs` (inherits `IntegrationTest`, class doc comment saying what it guards): anonymous `GET /exercises` with an `Origin: http://localhost:8081` header returns `200` and carries no `Access-Control-Allow-Origin` header, with labelled `// Arrange` / `// Act` / `// Assert` phases and one Act; verify the test passes with Docker running
- [x] 1.4 Review the `Program.cs` change against the existing environment-gated blocks: CORS is applied only in the Development block, no URL, port or origin is hardcoded, and no other pipeline order changed

## 2. Launch configuration: LAN binding

- [x] 2.1 In `.vscode/launch.json`, set `ASPNETCORE_URLS` to `http://0.0.0.0:5199` in `Debug API` and `Debug API (no prelaunch)`, and change `serverReadyAction.uriFormat` in `Debug API` to `http://localhost:5199/scalar`; verify the file is valid JSON
- [x] 2.2 In `src/WeightTracker.Api/Properties/launchSettings.json`, change the HTTP URL of the `http` and `https` profiles to `http://0.0.0.0:5000`, keeping `https://localhost:5001`; verify `dotnet run --project src/WeightTracker.Api --launch-profile http` logs `Now listening on: http://0.0.0.0:5000`
- [x] 2.3 Review both files against the design: ports unchanged, HTTPS still on `localhost`, and no environment other than `Development` set

## 3. Documentation

- [x] 3.1 In `AGENTS.md` "Running And Verifying Locally", add a bullet: in Development, CORS allows any origin and the API listens on all interfaces; LAN devices call `http://<machine-lan-ip>:5199` (debug) or `:5000` (`http` profile), not the `https` profile; allow the macOS firewall prompt; the development API is then reachable by anyone on the network. Verify the existing Scalar `localhost:5199` instructions still read correctly

## 4. Verification

- [x] 4.1 Run `dotnet build WeightTracker.slnx` and `dotnet test WeightTracker.slnx` with Docker running; verify both pass with no skipped integration tests
- [x] 4.2 Run the template parity check from `AGENTS.md`; verify it prints no `DRIFT` lines
- [x] 4.3 Manual Development check with the `Debug API` configuration:
  - From another origin, `curl -i -X OPTIONS http://localhost:5199/workouts -H "Origin: http://localhost:8081" -H "Access-Control-Request-Method: POST" -H "Access-Control-Request-Headers: authorization, content-type"` returns a success status with `Access-Control-Allow-Origin: *`.
  - `curl -i http://localhost:5199/workouts -H "Origin: http://localhost:8081"` returns `401`.
  - From a device on the LAN, `http://<machine-lan-ip>:5199/exercises` responds.
- [x] 4.4 Walk the `AGENTS.md` quality checklist for this change and record which items do not apply (no entities, use cases, migrations, mappers or new protected routes)
