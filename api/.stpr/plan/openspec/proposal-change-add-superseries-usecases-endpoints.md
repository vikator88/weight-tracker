## Change

Wire superseries methods in workout with usecases and endpoints

## Objective

In a previous PR we created the concept of superseries in the domain layer and the infra layer. Now i want to create two new usecases: create a superseries and break superseries. Only Admin and Trainer who owns the workout can execute this usecases.
I need to wire the Api with this two new usecases.


## Module Classification

- Application: Workouts -> UseCase -> WeightTracker.Application/UseCases/Workouts
- Api -> Wndpoints -> WeightTracker.Api/Endpoints/Exercises

Prefer canonical references from `.stpr/template/README.md`.

## State And Mutability

- The acions created as endpoints must mutate the state using the methods for supereeries defined in the workout aggregate

## Business Rules

- N/A
  
## Roles And Visibility

- This change can be made only by Admin or Trainers

## Business Identifiers And Mutability

- N/A

If a future change wants any listed identifier to become mutable, exact persistence semantics must be defined before coding starts.

## Tests
- Integration tests must have seed data for workouts with superseries to break it and workouts without superseries to create it.
- Create unit tests

## Out Of Scope

- I know that this is not the logical order to implement, becaouse we don't still have endpoints for managing workout exercises, etc... it will come later.
- Don't create E2E tests. 

## Notes
- Endpoints should be like POST /workouts/{workoutid}/superseries and DELETE /workouts/{workoutid}/superseries/{superseriesid}
- Create one DTO CreateSuperseriesRequest shaped like this: where exerciseIds is an array of guids, each guid is the Id for entity WorkoutExercise
{
    exerciseIds: [
        'guid1', 'guid2'...
    ]
}