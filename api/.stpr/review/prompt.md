# Review Prompt

Use this prompt after project finalization, or after any implementation step you want to audit.

## Objective

Audit the latest execution result and the STPR control artifacts that shaped it.
This step is not for finishing code. It is for evaluating whether the current STPR system prevented ambiguity, drift, and repeated mistakes.

## Instructions

1. Audit the last execution result against:
   - `.stpr/template/`
   - `.stpr/template/STPR_INVARIANTS.md`
   - the relevant STPR work artifacts when they exist, such as `.stpr/work/project/plan.md`, `.stpr/work/project/tasks.md`, or `.stpr/work/changes/<change-slug>/`

2. Verify the result using:
   - `.stpr/review/CHECKLIST_REVISION.md`

3. Do not fix the project directly in this step unless explicitly asked.

4. Evaluate only the current repository state.
   - Do not describe historical mistakes, intermediate states, or issues that have already been fixed.
   - Do not use phrases such as `initially`, `before`, `earlier`, `during implementation`, `previously`, or equivalent wording.
   - If something is correct in the current state, do not report it as a problem.

5. If you find deviations, drift, repeated mistakes, or avoidable ambiguity:
   - identify whether the problem belongs to the template, invariants, plan artifacts, prompts, or review artifacts
   - explain which source artifact should have prevented the issue
   - propose concrete improvements in those artifacts for future executions
   - focus on improving the system that produced the result, not only the result itself

6. When reviewing generated files:
   - report only real, verifiable issues that still exist now
   - distinguish between a real current problem and a minor parity/style deviation
   - if a concern no longer exists, omit it or place it in a clearly separate `No Longer A Problem` section

## Expected Output

## Current Problems

- Findings ordered by severity
- Only include real current issues
- Prefer file-by-file references when possible

## Minor Deviations

- Include parity, style, traceability, or coverage issues that do not currently break the approved result

## Template Or Invariant Deviations

- List only deviations that still exist in the current repository state

## Checklist Items Not Satisfied

- Include only checklist items that are not satisfied now

## Source Artifact To Tighten

- For each important issue, name the artifact that should be improved:
  - template
  - invariants
  - plan artifacts
  - prompts
  - review artifacts

## Improvement Proposals

- Concrete changes to STPR artifacts that would prevent the issue in future executions

## Residual Risks

- Current residual risks only

## Optional: No Longer A Problem

- Use only if it is genuinely useful
- Keep it separate from current issues

## Notes

- Prefer pointing to the source artifact that should prevent the issue in the future.
- If the generated result is correct but the process was unnecessarily ambiguous, propose tightening the artifact that should have made the decision explicit.
- Keep this prompt distinct from project-finalization prompts: this one is for audit and method improvement, not for completing implementation work.
- Do not mix already-fixed issues with current issues.
- If there is no current problem in a file, do not invent one.
