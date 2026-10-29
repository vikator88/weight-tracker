import { describe, expect, it } from "vitest";
import type { UserRepositoryInterface } from "../../../../src/application";
import { GetUsersUseCase } from "../../../../src/application";
import {
  type CreatedUserResponse,
  DomainError,
  fail,
  ok,
  type Result,
  type UserProfile,
  type UserSummary,
} from "../../../../src/domain";

const user: UserSummary = {
  id: "5f1c2f0e-9d5b-4a53-8c7e-0d6f7c1a2b3c",
  email: "trainer@example.com",
  name: "Ana",
  surname: "García",
  role: "TRAINER",
  isActive: true,
  deactivatedAtUtc: null,
};

class FakeUserRepository implements UserRepositoryInterface {
  listCalls = 0;
  otherCalls = 0;

  constructor(private readonly result: Result<UserSummary[], DomainError>) {}

  async getAll(): Promise<Result<UserSummary[], DomainError>> {
    this.listCalls += 1;
    return this.result;
  }

  async getMe(): Promise<Result<UserProfile, DomainError>> {
    this.otherCalls += 1;
    return fail(new DomainError("UNEXPECTED", "Not used"));
  }

  async updateMe(): Promise<Result<UserProfile, DomainError>> {
    this.otherCalls += 1;
    return fail(new DomainError("UNEXPECTED", "Not used"));
  }

  async create(): Promise<Result<CreatedUserResponse, DomainError>> {
    this.otherCalls += 1;
    return fail(new DomainError("UNEXPECTED", "Not used"));
  }

  async activate(): Promise<Result<UserSummary, DomainError>> {
    this.otherCalls += 1;
    return fail(new DomainError("UNEXPECTED", "Not used"));
  }

  async deactivate(): Promise<Result<UserSummary, DomainError>> {
    this.otherCalls += 1;
    return fail(new DomainError("UNEXPECTED", "Not used"));
  }
}

describe("GetUsersUseCase", () => {
  it("returns the users the repository answers with", async () => {
    // Arrange
    const useCase = new GetUsersUseCase(new FakeUserRepository(ok([user])));

    // Act
    const result = await useCase.execute();

    // Assert
    expect(result).toEqual(ok([user]));
  });

  it("lists the users once and calls nothing else", async () => {
    // Arrange
    const repository = new FakeUserRepository(ok([]));
    const useCase = new GetUsersUseCase(repository);

    // Act
    await useCase.execute();

    // Assert
    expect(repository.listCalls).toBe(1);
    expect(repository.otherCalls).toBe(0);
  });

  it.each(["UNAUTHORIZED", "FORBIDDEN", "UNEXPECTED"] as const)(
    "returns the repository failure %s unchanged",
    async (code) => {
      // Arrange
      const failure = fail(new DomainError(code, "Rejected"));
      const useCase = new GetUsersUseCase(new FakeUserRepository(failure));

      // Act
      const result = await useCase.execute();

      // Assert
      expect(result).toBe(failure);
    },
  );
});
