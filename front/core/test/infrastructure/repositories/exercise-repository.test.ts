import { describe, expect, it } from "vitest";
import type { HttpClientInterface, HttpRequest } from "../../../src/application";
import { ok, type Exercise } from "../../../src/domain";
import { ExerciseRepository, HttpError } from "../../../src/infrastructure";

const exercise: Exercise = {
  id: "1f2e3d4c-5b6a-4978-8695-a4b3c2d1e0f9",
  freeExerciseDbId: "Barbell_Squat",
  videoUrl: null,
  muscle: "quadriceps",
  bodyPart: "LEGS",
  type: "STRENGTH",
  translations: [],
};

class FakeHttpClient implements HttpClientInterface {
  readonly requests: HttpRequest[] = [];

  constructor(private readonly respond: () => Promise<unknown>) {}

  async request<T>(httpRequest: HttpRequest): Promise<T> {
    this.requests.push(httpRequest);
    return (await this.respond()) as T;
  }
}

describe("ExerciseRepository", () => {
  describe("getAll", () => {
    it("gets /exercises without a body", async () => {
      // Arrange
      const httpClient = new FakeHttpClient(async () => [exercise]);
      const repository = new ExerciseRepository(httpClient);

      // Act
      await repository.getAll();

      // Assert
      expect(httpClient.requests).toEqual([{ method: "GET", path: "/exercises" }]);
    });

    it("returns the exercises on success", async () => {
      // Arrange
      const repository = new ExerciseRepository(new FakeHttpClient(async () => [exercise]));

      // Act
      const result = await repository.getAll();

      // Assert
      expect(result).toEqual(ok([exercise]));
    });

    it("returns each exercise's type unchanged, known or not", async () => {
      // Arrange
      const cardio = { ...exercise, id: "2a3b4c5d-6e7f-4809-9a1b-2c3d4e5f6a7b", type: "CARDIO" };
      const unknown = { ...exercise, id: "3b4c5d6e-7f80-4a1b-8c2d-3e4f5a6b7c8d", type: "YOGA" };
      const repository = new ExerciseRepository(new FakeHttpClient(async () => [cardio, unknown]));

      // Act
      const result = await repository.getAll();

      // Assert
      expect(result).toEqual(ok([cardio, unknown]));
    });

    it("returns an empty list when the catalog is empty", async () => {
      // Arrange
      const repository = new ExerciseRepository(new FakeHttpClient(async () => []));

      // Act
      const result = await repository.getAll();

      // Assert
      expect(result).toEqual(ok([]));
    });

    it("maps status 500 to UNEXPECTED", async () => {
      // Arrange
      const httpClient = new FakeHttpClient(async () => {
        throw new HttpError(500, { status: 500, detail: "Something failed" });
      });
      const repository = new ExerciseRepository(httpClient);

      // Act
      const result = await repository.getAll();

      // Assert
      expect(result).toEqual({ ok: false, error: expect.objectContaining({ code: "UNEXPECTED" }) });
    });

    it("returns the network code when there is no response", async () => {
      // Arrange
      const httpClient = new FakeHttpClient(async () => {
        throw new TypeError("Failed to fetch");
      });
      const repository = new ExerciseRepository(httpClient);

      // Act
      const result = await repository.getAll();

      // Assert
      expect(result).toEqual({ ok: false, error: expect.objectContaining({ code: "NETWORK" }) });
    });
  });
});
