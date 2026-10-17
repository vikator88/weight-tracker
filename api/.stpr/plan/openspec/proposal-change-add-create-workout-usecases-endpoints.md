## Change

Wire workout creation methot with usecase and endpoint

## Objective

The goal is to add a new endpoint to create a new workout. Mandatory fields are:
- workout date (time is not needed)
- userId comes from authenticated user if a user is creating a workout for themselves
- userId comes from Request DTO if a trainer or admin is creating the workout
- trainerId comes from authenticated user if a trainer is creating the workout. In this case trainerId is not mandatory


## Module Classification

- Application: Workouts -> UseCase -> WeightTracker.Application/UseCases/Workouts
- Api -> Endpoints -> WeightTracker.Api/Endpoints/Workouts

Prefer canonical references from `.stpr/template/README.md`.

## State And Mutability

- The acions created as endpoints must mutate the state using the methods Create in the workout aggregate

## Business Rules

- N/A
  
## Roles And Visibility

- Any authenticated user can create a workout. UserId and TrainerId depends on who is creating and what comes in the DTO request, defined at the begining

## Business Identifiers And Mutability

- N/A

If a future change wants any listed identifier to become mutable, exact persistence semantics must be defined before coding starts.

## Tests
- Integration tests do not need seed data
- Create unit tests
- E2E tests can be created as well

## Notes
- Endpoints should be shaped like POST /workouts
- Create one DTO CreateSuperseriesRequest shaped like this
{
    userId: nullable, but mandatory if requesting user is not a regular user,
    trainerId: nullable. never mandatory. only can be set by an admin (trainers cannot create workouts for other trainers)
    workoutDate: mandatory
}