## Change

Add a CORS policy that allows all origins, headers and methods when the API runs in the Development environment, and make the Development HTTP binding listen on all network interfaces so devices on the local network can reach it.

## Objective

The front apps (backoffice and app, see `front/`) are being tested from `localhost` and from devices on the local network (Expo Go, Expo web, a phone browser). Today the API registers no CORS policy, so any browser-based client served from another origin is blocked by the browser before it reaches the API. During local development and debugging the origin is not known in advance (dev server ports and LAN IPs change), so the API must accept any origin while it runs as Development.

## Capabilities

- Browser clients served from any origin can call the API when it runs in Development, including preflighted requests carrying `Authorization: Bearer ...` and `Content-Type: application/json`.
- When started locally for development or debugging, the API listens on `0.0.0.0` over HTTP, so a phone or another device on the LAN can call it at `http://<machine-lan-ip>:<port>` (native Expo Go included).
- No CORS policy is applied outside Development, so Production and Testing behave exactly as they do today.

## Module Classification

- Api -> host configuration (`Program.cs` middleware pipeline) -> `src/WeightTracker.Api/Program.cs`
- Api -> local launch configuration -> `src/WeightTracker.Api/Properties/launchSettings.json` and `.vscode/launch.json`

No domain, application, persistence or auth module is affected. There is no catalog or aggregate involved.

## Canonical Template Mapping

- CORS registration -> `src/WeightTracker.Api/Program.cs` -> follows the existing environment-gated blocks (`MapOpenApi`, `MapScalarApiReference`, migration and seed), which are already enabled with `app.Environment.IsDevelopment()`.
- LAN binding -> `.vscode/launch.json` and `Properties/launchSettings.json` -> the URLs already live there as `ASPNETCORE_URLS` / `applicationUrl`, so the binding changes in place and stays out of code.

## State And Mutability

- No business state changes.
- The change only alters HTTP response headers (`Access-Control-Allow-*`) and the handling of `OPTIONS` preflight requests in Development, plus the network interfaces the local process listens on.

## Business Rules

- N/A

## Roles And Visibility

- CORS does not replace or weaken authorization. Every `[Authorize]` route still requires a valid bearer token; CORS only lets the browser send the request and read the response.
- Binding to `0.0.0.0` exposes the development API, with its seeded credentials, to anyone on the same network. This is accepted for local development on a trusted network.
- The allow-all policy is active only when `ASPNETCORE_ENVIRONMENT` is `Development`, which covers `dotnet run`, the `http`/`https` launch profiles and both VS Code `Debug API` configurations.

## Business Identifiers And Mutability

- N/A

## Tests

- Integration tests run under the `Testing` environment, where CORS stays disabled. Add one integration test asserting that a cross-origin request (with an `Origin` header) gets no `Access-Control-Allow-Origin` header, to lock in that the policy does not leak outside Development.
- The allow-all behavior and the LAN binding are verified manually (Scalar on `localhost`, then Expo web and Expo Go on a device calling `http://<machine-lan-ip>:5199`), since the integration host does not run as Development and does not bind to a real port.
- No unit tests: there is no domain or application logic.

## Out Of Scope

- A configured allow-list of origins for Staging or Production. That is deferred until there is a deployed front-end origin.
- Credentialed CORS (cookies). Auth uses bearer tokens, so `AllowCredentials` is not needed, and it cannot be combined with `AllowAnyOrigin`.
- Changes to the front apps (for example, how they discover the machine's LAN IP).
- Exposing HTTPS on the LAN. The ASP.NET Core dev certificate is only valid for `localhost`, so the HTTPS binding stays on `localhost`.
- The `infra/docker-compose.yml` Postgres binding, which the API reaches locally and devices never call directly.

## Notes

- Register with `builder.Services.AddCors(...)` using a named policy (`AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod()`), and call `app.UseCors(<policy>)` only inside the Development branch, before `UseAuthentication()`/`UseAuthorization()`, so preflight `OPTIONS` requests are answered without a token.
- Keep the policy name as a constant next to its registration, not a repeated string literal.
- `UseHttpsRedirection()` runs before CORS. Clients on the LAN should call the HTTP URL directly; a redirected preflight fails in the browser.
- Native Expo Go requests do not go through a browser and are not subject to CORS. This change is what unblocks Expo web and other browser clients; native clients on the LAN depend on the network binding below.
- **Network binding:**
  - `.vscode/launch.json`: set `ASPNETCORE_URLS` to `http://0.0.0.0:5199` in both `Debug API` and `Debug API (no prelaunch)`. Kestrel then logs `Now listening on: http://0.0.0.0:5199`, so the `serverReadyAction` should use a fixed `uriFormat` of `http://localhost:5199/scalar` instead of `%s/scalar`, to keep opening Scalar on localhost.
  - `Properties/launchSettings.json`: change the HTTP URL of the `http` and `https` profiles to `http://0.0.0.0:5000`. The HTTPS URL of the `https` profile stays `https://localhost:5001`.
  - The port numbers stay as they are, and no URL or port goes into `Program.cs`.
  - On macOS, the firewall may ask to allow incoming connections the first time the API starts on `0.0.0.0`. Allow it, or LAN devices will time out.
- Update `AGENTS.md` "Running And Verifying Locally" stating that in Development CORS is open and the API listens on all interfaces, and that LAN devices call `http://<machine-lan-ip>:5199` (debug) or `:5000` (`dotnet run`). The existing Scalar URL on `localhost:5199` stays valid.
- No deviations from the existing `Program.cs` structure are allowed beyond the CORS registration and middleware call and the launch configuration URLs listed above.
