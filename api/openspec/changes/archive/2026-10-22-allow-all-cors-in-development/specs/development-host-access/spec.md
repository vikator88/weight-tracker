# Spec Delta

## Purpose

Defines how local clients (browsers, Expo web and Expo Go on the developer's machine or local network) reach the API while it runs for development, and guarantees that this open access never applies outside the Development environment.

## ADDED Requirements

### Requirement: Development allows cross-origin requests from any origin
When the API runs in the `Development` environment, it SHALL allow cross-origin requests from any origin, with any request header and any HTTP method. It SHALL NOT allow credentialed (cookie-based) cross-origin requests.

#### Scenario: Simple cross-origin request in Development
- **WHEN** a client sends `GET /exercises` with header `Origin: http://192.168.1.50:8081` to the API running in `Development`
- **THEN** the response includes `Access-Control-Allow-Origin: *`

#### Scenario: Preflight for an authenticated JSON request in Development
- **WHEN** a client sends `OPTIONS /workouts` with `Origin: http://localhost:8081`, `Access-Control-Request-Method: POST` and `Access-Control-Request-Headers: authorization, content-type` to the API running in `Development`
- **THEN** the response is successful without a bearer token
- **AND** it allows any origin, the requested method and the requested headers

### Requirement: Cross-origin access does not bypass authorization
Allowing cross-origin requests SHALL NOT change authentication or authorization. Protected routes SHALL keep requiring a valid bearer token whatever the request origin.

#### Scenario: Cross-origin call without a token to a protected route
- **WHEN** a client sends `GET /workouts` with an `Origin` header and no bearer token to the API running in `Development`
- **THEN** the response status is `401`

### Requirement: No cross-origin policy outside Development
When the API runs in any environment other than `Development`, it SHALL NOT emit CORS response headers.

#### Scenario: Cross-origin request under Testing
- **WHEN** a client sends `GET /exercises` with header `Origin: http://localhost:8081` to the API running in `Testing`
- **THEN** the response does not include an `Access-Control-Allow-Origin` header

### Requirement: Development launch is reachable from the local network
When the API is started locally for development or debugging through the repository's launch configurations, it SHALL listen over HTTP on all network interfaces, on the same port each configuration uses today. HTTPS SHALL remain bound to `localhost` only.

#### Scenario: Debugging from VS Code
- **WHEN** the API is started with the `Debug API` or `Debug API (no prelaunch)` configuration
- **THEN** a device on the same local network can call `http://<machine-lan-ip>:5199`
- **AND** the API reference stays reachable at `http://localhost:5199/scalar`

#### Scenario: Running with dotnet run
- **WHEN** the API is started with the `http` or `https` launch profile
- **THEN** a device on the same local network can call `http://<machine-lan-ip>:5000`
