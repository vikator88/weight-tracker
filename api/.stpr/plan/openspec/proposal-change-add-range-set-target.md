## Change

Add range set target

## Objective

We have defined set target by reps, time or max reps. We want to add a new target type: range of reps. Athlete can make between min and max reps and it's ok for their workout

## Capabilities

- Allow to define a set reps range

## Module Classification

- Workout Domain -> mutable aggregate -> WeightTracker.Domain/Workouts
- Workout Mapper-> mutable aggregate -> WeightTracker.Infra/Mappers
- Workout API -> mutable aggregate -> WeightTracker.Api/Endpoints/Workouts


State explicitly whether each affected module is truly catalog-like or only publicly read-only.
Prefer canonical references from `.stpr/template/README.md`.

## Canonical Template Mapping

- SetTargetTypes -> WeightTracker.Domain/Workouts/SetTargetTypes.cs -> in that file are defined the different set target types
- Add Column to Workout Exercises Table -> WeightTracker.Migrations/Migrations/AddEffortToWorkoutExercises_202610131437.cs -> in that file we are adding a column to a table
- WorkoutExercise mapping -> WeightTracker.Infra/Mappers/WorkoutMapper.cs -> here is the mapper, where the new column should be mapped to the SetTarget by WorkoutExercise
- DTO -> WeightTracker.Api/Endpoints/Workout/Dtos/AddOrUpdateExerciseRequest.cs -> here is the Dto where frontend set the new field for range set target type

Every affected capability must name its canonical template reference explicitly.

## State And Mutability

- Include a new column into set target: MaxTarget
- This column should be always null except for Range
- Dto for add or update exercises can receive MaxReps, nullable for all set target types except for range

## Business Rules

- Max number of reps cannot be equal or lower than Min number of reps

## Roles And Visibility

- N/A

## Business Identifiers And Mutability

- N/A

If a future change wants any listed identifier to become mutable, exact persistence semantics must be defined before coding starts.

## Out Of Scope

- <What this version will not include>
- <What should be deferred to a later change>

## Notes

- <Optional implementation hints or domain terms>
- <Summarize unique identifiers, relation semantics, or technical primary keys only when they materially affect this change>
- <Use the shared `Id` value object for all new or changed business identifiers and externally visible codes by default; if any identifier does not use `Id`, justify the exception explicitly>
- <For each system-generated business identifier affected by this change, state that generation uses `Id` unless an explicit exception is approved here>
- <Resolve any conflict such as "replicate categories" vs "state changes internally" before asking for implementation>
- <Document any allowed deviation from the mapped template reference; if none, state that no deviations are allowed>
