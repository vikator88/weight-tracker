import { describe, expect, it } from "vitest";
import { HttpError, mapHttpError } from "../../../src/infrastructure";

describe("mapHttpError", () => {
  it.each([
    [400, "VALIDATION"],
    [401, "UNAUTHORIZED"],
    [403, "FORBIDDEN"],
    [404, "NOT_FOUND"],
    [409, "CONFLICT"],
    [500, "UNEXPECTED"],
    [418, "UNEXPECTED"],
  ])("maps status %i to %s", (status, code) => {
    // Arrange
    const error = new HttpError(status, { status, detail: "Something failed" });

    // Act
    const result = mapHttpError(error);

    // Assert
    expect(result.code).toBe(code);
  });

  it("keeps the problem detail as the message", () => {
    // Arrange
    const error = new HttpError(409, { status: 409, detail: "Workout is locked" });

    // Act
    const result = mapHttpError(error);

    // Assert
    expect(result.message).toBe("Workout is locked");
  });

  it("falls back to the HTTP message when the body has no detail", () => {
    // Arrange
    const error = new HttpError(404, "<html>Not found</html>");

    // Act
    const result = mapHttpError(error);

    // Assert
    expect(result.message).toBe("HTTP 404");
  });

  it("maps an unreadable 2xx body to UNEXPECTED", () => {
    // Arrange
    const error = new HttpError(200, "not json");

    // Act
    const result = mapHttpError(error);

    // Assert
    expect(result.code).toBe("UNEXPECTED");
  });

  it("maps a fetch rejection to NETWORK", () => {
    // Arrange
    const error = new TypeError("Network request failed");

    // Act
    const result = mapHttpError(error);

    // Assert
    expect(result).toMatchObject({ code: "NETWORK", message: "Network request failed" });
  });
});
