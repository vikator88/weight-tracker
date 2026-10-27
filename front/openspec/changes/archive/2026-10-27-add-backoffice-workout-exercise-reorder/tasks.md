# Tasks

## 1. Contract snapshot

- [x] 1.1 Run the API on the `add-reorder-workout-exercises` change (Debug API, port 5199), then `pnpm --filter @weight-tracker/core openapi:refresh` and `pnpm --filter @weight-tracker/core openapi:generate`; verify the diff of `core/openapi/v1.json` and `core/src/contract/generated` adds only `PUT /workouts/{workoutId}/exercises/order` and `ReorderWorkoutExercisesRequest` (stop and report a missing endpoint or any other diff)
- [x] 1.2 Confirm nothing under `core/src/contract/generated` was edited by hand and commit the snapshot as a step of its own; verify `pnpm --filter @weight-tracker/core typecheck` passes

## 2. Core domain, port and repository

- [x] 2.1 Add `ReorderWorkoutExercisesRequest` (alias of the generated schema) and `ReorderWorkoutExercisesInput` to `core/src/domain/workouts/workout.ts` and re-export them; verify with the core typecheck
- [x] 2.2 Add `reorderExercises(workoutId, request): Promise<Result<WorkoutDetail, DomainError>>` to `core/src/application/ports/workout-repository-interface.ts`; verify with the core typecheck
- [x] 2.3 Implement `WorkoutRepository.reorderExercises` in `core/src/infrastructure/repositories/workout-repository.ts` following `update` (`PUT /workouts/{workoutId}/exercises/order`, JSON body, `mapHttpError`); verify with the core typecheck
- [x] 2.4 Extend `core/test/infrastructure/repositories/workout-repository.test.ts` for `reorderExercises`: method, path and body; success returns the detail; `400` → `VALIDATION`, `401` → `UNAUTHORIZED`, `403` → `FORBIDDEN`, `404` → `NOT_FOUND`, `409` → `CONFLICT`, `500` → `UNEXPECTED`, rejected request → `NETWORK`; AAA with one Act each; verify with `pnpm --filter @weight-tracker/core test`

## 3. Core use case

- [x] 3.1 Create `core/src/application/use-cases/workouts/reorder-workout-exercises-use-case.ts` (splits `workoutId` from the body, returns the repository `Result`, no validation) and export it from `index.ts`; verify with the core typecheck
- [x] 3.2 Add `reorderWorkoutExercisesUseCase` to `core/src/application/dependencies.ts`; verify with the core typecheck
- [x] 3.3 Add `core/test/application/use-cases/workouts/reorder-workout-exercises-use-case.test.ts` following `remove-exercise-from-workout-use-case.test.ts`: id and `exerciseIds` forwarded unchanged and in order, success and failure returned unchanged, repository called once; AAA; verify with the core tests
- [x] 3.4 Review core against `update-workout-use-case.ts`, `remove-exercise-from-workout-use-case.ts`, `WorkoutRepository.update` and their tests; verify the shape matches with no documented differences

## 4. Wiring in both fronts

- [x] 4.1 Register `reorderWorkoutExercisesUseCase` in `backoffice/src/infrastructure/container.ts` and `app/src/infrastructure/container.ts`, add its placeholder to both `test/support/render-with-providers.tsx`, and add the key to both `test/infrastructure/container.test.ts`; verify `pnpm typecheck` and both container tests pass

## 5. Backoffice dependency and state hook

- [x] 5.1 From `backoffice/`, run `pnpm add @dnd-kit/core @dnd-kit/sortable @dnd-kit/utilities`; verify they appear only in `backoffice/package.json` and `pnpm --filter backoffice build` still produces the static export
- [x] 5.2 Create `backoffice/src/presentation/workouts/hooks/use-reorder-workout-exercises.ts` following `use-remove-workout-exercise.ts` (`reordering`, `errorCode` cleared on start, `reorder` through `runWithSession` resolving to `true` on success); verify with `pnpm --filter backoffice typecheck`

## 6. Backoffice components, copy and view

- [x] 6.1 Add the `workout.exercises.reorder` and `workout.exercises.dnd.*` keys from the design to `backoffice/src/i18n/es/workouts.json` and `en/workouts.json`; verify the translation parity test passes
- [x] 6.2 Add the optional `leading` slot to `ExerciseListItem` (molecule), rendered before the position number; verify the existing catalog and view tests still pass unchanged
- [x] 6.3 Create `components/molecules/sortable-exercise-list-item.tsx`: `useSortable({ id, disabled })`, transform and transition on a wrapper, a `DragIndicator` handle on the `IconButton` atom labelled "Reordenar ejercicio" with activator ref, listeners and attributes, disabled when not sortable; verify with the backoffice typecheck and lint (no MUI widget import outside atoms)
- [x] 6.4 Extend `WorkoutExercisesList` (organism) with `DndContext` (stable `id`, `closestCenter`, pointer and keyboard sensors, translated announcements and instructions), `SortableContext` over the saved entry ids, pending items rendered disabled, the `onReorder`, `reordering` and `reorderErrorCode` props, and the reorder error `Alert`; verify with the backoffice typecheck
- [x] 6.5 Wire `LoadedWorkout` in `workout-view.tsx`: `handleReorder` with an early return on a same-position drop, an optimistic `arrayMove`, one `reorder` call with the saved `exerciseId`s, and an order restored by id on failure; verify with the backoffice typecheck
- [x] 6.6 Extend `backoffice/test/presentation/workouts/views/workout-view.test.tsx` with one test per spec scenario: a handle per exercise; a keyboard move renumbers before the answer and sends one reorder with the new ids; success keeps the order; `409` restores the order and shows the `CONFLICT` message; a drop in place or Escape sends nothing; handles are disabled while a reorder is in progress; a pending handle is disabled and pending items are never sent; the selection is kept. Stub `getBoundingClientRect` locally in that file with restore. AAA with one Act each; verify with `pnpm --filter backoffice test`
- [x] 6.7 Review the backoffice against `use-remove-workout-exercise.ts`, `exercise-list-item.tsx` and the existing view test; verify the only differences are the dnd-kit pattern and the local rect stub documented in the design

## 7. Backoffice E2E

- [x] 7.1 Extend `backoffice/e2e/workout.spec.ts`. Intercept the workout, catalog and `PUT .../exercises/order` calls with `page.route`. Reorder two exercises with the keyboard and assert the request body and the new numbering. Answer `409` in a second case and assert the order is restored and the message is shown. Verify with `pnpm test:e2e`

## 8. Verification

- [x] 8.1 Run the `.stpr/template/` drift check from `AGENTS.md`; verify it prints no `DRIFT`
- [x] 8.2 Run `pnpm typecheck && pnpm lint && pnpm test && pnpm test:e2e` from `front/`; verify everything passes

## Workflow follow-up

- Archive the change once the API change `add-reorder-workout-exercises` is merged and this change is reviewed.
