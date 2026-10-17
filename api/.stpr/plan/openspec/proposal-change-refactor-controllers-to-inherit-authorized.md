## Change

Create an Auth Controller

## Objective

I want to create an Auth Controller to allow auth-needed controllers inherit this AuthController, where will live the token check and the user existence.
We don't want to inject repoitories into API, so I want to create a new UseCase for Check the User existence


## Module Classification

- Application: Auth -> UseCase -> WeightTracker.Application/UseCases/Auth
- Api -> Endpoints -> WeightTracker.Api/Endpoints

Prefer canonical references from `.stpr/template/README.md`.

## State And Mutability

- This will check if the JWT is valid and will have a protected variable User with the logged in user data (or null if JWT is not valid). This Controller can throw 401 for unauthenticated or unexisting users

## Business Rules

- N/A
  
## Roles And Visibility

- N/A

## Business Identifiers And Mutability

- N/A

If a future change wants any listed identifier to become mutable, exact persistence semantics must be defined before coding starts.

## Tests
- Integration tests
- Create unit tests

## Notes
Exercises and Workouts related endpoints must inherit this new controller. Auth must not