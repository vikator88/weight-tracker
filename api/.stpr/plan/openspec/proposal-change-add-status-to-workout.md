## Change

Add workout status

## Objective

We want to manage the workout status with some rules. New statuses are:
- Draft: workout is  still editable. visible for owner, not visible for athlete (if athlete and owner is the same (e.g. user planning their own workouts) is visible)
- Published: workout is still editable but visible for both owner and athlete
- In progress: workout is not editable
- Finished: workout is not editable

## Capabilities

- Transition between statuses
- Store stauts in DB
- Filter workouts when getting all or by Id (an athlete not owner cannot get Draft workouts)

## Module Classification

- Workouts -> mutable aggregate -> WeightTracker.Domain/Workouts

State explicitly whether each affected module is truly catalog-like or only publicly read-only.
Prefer canonical references from `.stpr/template/README.md`.

## Canonical Template Mapping

- Domain: Workout -> WeightTracker.Domain/Workouts/Workout.cs -> the agggregate we are modifying
- Domain: WorkoutStatus -> WeightTracker.Domain/Workouts/Effort.cs -> the nearest enum in the folder
- Application: Workout -> WeightTracker.Application/UseCases/Workouts/UpdateWorkoutExercisesUseCase.cs -> similar usecase that updates a workout
- Infrastructure: Workout Entity -> WeightTracker.Infra/Entities/WorkoutEntity.cs -> include the new field
- Infrastructure: Workout Mapper -> WeightTracker.Infra/Mappers/WorkoutMapper.cs -> include the new field
- Migration: Status Column -> WeightTracker.Migrations/Migrations/AddSuperSeriesIdToWorkoutExercises_202610161647.cs -> migration that adds a column in Worout table
- API: Workout -> WeightTracker.Api/Endpoints/Workouts/WorkoutsController.cs -> controller where new endpoint will live


Every affected capability must name its canonical template reference explicitly.

## State And Mutability

- New Status field in Workout must be persisted. New WorkoutStatus enum must be defined
- Status is mandatory. Set existing workouts as Draft

## Business Rules

- Status can go from Draft to Published
- Status can go from Published to Draft or Started
- Status can go from Started to Finished
- Workouts in status Started or Finished cannot be edited
- Workouts in status published can be edited, and it's not needed to put the workout in Draft
- Published vs Draft is only about athlete non owner visibility

## Roles And Visibility

- Admins can set the workout in any status skipping the business rules
- Owners not Athletes (trainerId or userId if trainerId is null) can set Draft to Published and back to Draft respecting the business rules
- Athletes not Owners (UserId not owners) only can change the status forward from Published (to Started, then to Finished)
- Athlets Owners can change to any status keeping the business rules

## Business Identifiers And Mutability

- N/A

If a future change wants any listed identifier to become mutable, exact persistence semantics must be defined before coding starts.

