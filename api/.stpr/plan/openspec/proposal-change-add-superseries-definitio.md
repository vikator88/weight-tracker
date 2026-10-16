## Change

Include support for superseries in a workout

## Objective

A superseries is when you have two or more exercises with the same number of sets but, instead doing all sets for an exercise and then all sets for the next exercise, you do the first set of every exercise in the superseries without resting, then you rest and go for the next set of all exercises in the superseries.


## Module Classification

- Domain: Workouts -> Aggregate -> WeightTracker.Domain/Workouts
- Infrastructure -> Database Entity -> WeightTracker.Infra/Entities
- Migrations -> Migration to add a column -> WeightTracker.Migrations/Migrations

Prefer canonical references from `.stpr/template/README.md`.

## State And Mutability

- I want to add a method in the Workout aggregate to create a superserie given a list of WorkoutExercises
- I want to add a method to break the superseries given the superseries Id
- Superseries Id must be an integer in database, must create its own ValueObject like SuperSeriesId.

## Business Rules

- A superseries can be created only if the WorkoutExercises has the same number of sets
- A broken superserie do not delete the exercises, just ungroup then

## Roles And Visibility

- This change can be made only by Admin or Trainers

## Business Identifiers And Mutability

- There's no new entity in database, just a new column in WorkoutExercises called SuperSeriesId that will be nullable.
- The superSeries Id must be only unique in the same workout. We can have SuperSeries ID with the same Id in different workouts. the same Id only group exercies for the same workout

If a future change wants any listed identifier to become mutable, exact persistence semantics must be defined before coding starts.

## Out Of Scope

- Exclude for now changes in Application layer and API layer. I just want to define the business in the aggregate and the related infra stuff: migrations, db entity definition and mapping.

