## Change

Get Workout endpoint

## Objective

Create an endpoint to get a workout by Id

## Capabilities

- Get one workout by Id, includiong all its related entities, such as Workout exercises, Sets, etc...
- Do not include the exercises info, that info will be available in the frontend to show it.
- Change the create workout to return a 201 with the retrieve workout url

## Module Classification

- API -> read-only catalog -> WeightTracker.API/Endpoints/Workouts
- Application -> read-only catalog -> WeightTracker.Application/UseCases/Workouts

State explicitly whether each affected module is truly catalog-like or only publicly read-only.
Prefer canonical references from `.stpr/template/README.md`.

## Canonical Template Mapping

- API readonly-catalog -> WeightTracker.API/Endpoints/Exercises/ExercisesController.cs -> check the get by id method, same behavior (just change the authentication permissions)
- Application readonly-catalog -> WeightTracker.Application/UseCases/Workouts/GetAllWorkoutsUseCase.cs -> similar to this one, just return by id instead all

Every affected capability must name its canonical template reference explicitly.

## State And Mutability

- Just get info from workouts

## Business Rules

- N/A

## Roles And Visibility

- Admin can get all workouts
- Trainer can get the workouts they own plus the workouts they appear as trainer
- User can get the workouts they own only

## Business Identifiers And Mutability

- N/A

If a future change wants any listed identifier to become mutable, exact persistence semantics must be defined before coding starts.

