import { describe, expect, it } from "vitest";
import { validateProfileName, validateUpdateProfileRequest } from "../../../src/application";

describe("validateProfileName", () => {
  it.each(["", "   "])("answers required for %j", (value) => {
    // Arrange / Act
    const result = validateProfileName(value);

    // Assert
    expect(result).toBe("required");
  });

  it.each([
    ["ab", "minLength"],
    ["  ab  ", "minLength"],
    ["abc", null],
    ["a".repeat(100), null],
    ["a".repeat(101), "maxLength"],
    [`  ${"a".repeat(100)}  `, null],
  ] as const)("answers %j with %j", (value, expected) => {
    // Arrange / Act
    const result = validateProfileName(value);

    // Assert
    expect(result).toBe(expected);
  });
});

describe("validateUpdateProfileRequest", () => {
  it("reports the violation of each field separately", () => {
    // Arrange
    const request = { name: "Ab", surname: "a".repeat(101) };

    // Act
    const result = validateUpdateProfileRequest(request);

    // Assert
    expect(result).toEqual({ name: "minLength", surname: "maxLength" });
  });

  it("reports no violation for valid values", () => {
    // Arrange
    const request = { name: "Ana", surname: "García" };

    // Act
    const result = validateUpdateProfileRequest(request);

    // Assert
    expect(result).toEqual({ name: null, surname: null });
  });
});
