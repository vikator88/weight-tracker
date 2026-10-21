import type { DomainError, Result } from "../../domain";

export default interface UseCaseInterface<TInput, TOutput> {
  execute(input: TInput): Promise<Result<TOutput, DomainError>>;
}
