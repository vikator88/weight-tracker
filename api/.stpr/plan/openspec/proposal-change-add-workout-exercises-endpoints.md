## Change

Add workout exercises endpoints

## Objective

We want to implement:
- the endpoint for add exercies to the workout (WorkoutExercise entity). This endpoint will receive the Exercise, the Effort level and a list with the total set prescriptions (maybe just one prescription like 3x15 or several prescriptions like 2x12, 2x10).
- the endpoint for update the exercise. This endpoint will receive the same DTO than the add one, and will pass the exercise id by URL
- tne endpoint for delete the exercise. This endpoint will receive the id by URL

I will need also a new method in Workout Aggregate to update the Exercise (effort/prescription)

## Module Classification

- Domain Workout -> WorkoutAggregate -> WeightTracker.Domain/Workouts/Workout.cs
- Application Workouts -> UseCase -> WeightTracker.Application/UseCases/Workouts
- Api Workouts -> Endpoints -> WeightTracker.Api/Endpoints/Workouts

Prefer canonical references from `.stpr/template/README.md`.

## State And Mutability

- The acions created as endpoints must mutate the state using the methods for AddExercise in the workout aggregate.
- Infrastrucuture is alkready wired, so nothing to change in Domain or Infra i think

## Business Rules

- Update the exercise will override everything. So, it will remove the existing prescription and inser the new one.
  
## Roles And Visibility

- I want to redefine the Workout Owner role.
   - Who is an owner of a workout?
      - Admins
      - If workout has TrainerId, the trainer
      - If workout doesn't have trainerId, the UserId
    - The Athlete is the same that UserId stored.
- This applies for all the business here.
- An athlete can be an owner only if there's no trainerId

In this case, only an owner can add exercises (not an athlete, unless the athlete is an owner.)

## Business Identifiers And Mutability

- N/A

If a future change wants any listed identifier to become mutable, exact persistence semantics must be defined before coding starts.

## Tests
- Integration tests must have seed data for workouts with or without exerecises, and must have seed data for exercises. If there's already enough data, don't need to add more seed data
- Create unit tests
- Create E2E tests with flows like: create a workout, add exercises, read the workout by id.
- Check specially that when we remove an exercise the prescription is removed as well
- Check the update set prescription path so we not only add new set prescriptions, but we remove the previous, even if we are just changeing the effort (sets will be different in database, but in value will remain the same)

## Out Of Scope
- N/A

## Notes
- Three new usecases under Application/USeCases/Workouts
- Endpoints should be like:
   - POST /workouts/{id}/exercises
   - PUT  /workouts/{id}/exercises/{exerciseId}
   - EDIT /workouts/{id}/exercises/{exerciseId}
- Create one DTO AddOrUpdateExerciseRequest shaped like this:
{
    exerciseId: guid, id for an exercise in catalog,
    effort: from the enum, can receive the number or the test, so define it as Effort,
    sets: [ array of SetPrescription
        {
            count: number,
            targetType: Reps|Duration|MaxReps,
            target: number (reps or minutes, 0 if targetType is MaxReps) 
        }
    ]
}
- Extract Reps|Duration|MaxReps in a Constants Class in Domain.Workout as public, use it in the WorkoutMapper instead the private values, use this constants to compare when needed (and to check those values when validating the Request DTO)