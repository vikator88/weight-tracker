## ADDED Requirements

### Requirement: Listing the catalog returns every exercise

Listing the exercise catalog SHALL send `GET /exercises` with no body and SHALL return a successful result holding the exercises the API answers with, unchanged. It SHALL take no input and SHALL NOT cache the result.

#### Scenario: Catalog listed

- **WHEN** the API answers with a list of exercises
- **THEN** exactly one `GET /exercises` request is sent, with no body
- **AND** the result is successful and its value is that list, unchanged

#### Scenario: Empty catalog

- **WHEN** the API answers with an empty list
- **THEN** the result is successful and its value is an empty list

### Requirement: Listing the catalog needs no session

The listing SHALL work for a caller without a stored session, because the endpoint is public, and SHALL NOT fail only because no access token exists.

#### Scenario: No session

- **WHEN** the catalog is listed with no stored session and the API answers with exercises
- **THEN** the result is successful

### Requirement: Catalog failures are returned as results

Listing the catalog SHALL return a `DomainError` result instead of throwing: statuses other than success as `UNEXPECTED`, and a request that gets no response as `NETWORK`.

#### Scenario: Server error

- **WHEN** the API answers 500
- **THEN** the result is a failure with code `UNEXPECTED`

#### Scenario: No response

- **WHEN** the request is rejected without a response
- **THEN** the result is a failure with code `NETWORK`
