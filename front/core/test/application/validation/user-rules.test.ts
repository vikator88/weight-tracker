import { describe, expect, it } from "vitest";
import { type CreateUserFormValues, validateCreateUserForm } from "../../../src/application";

const valid: CreateUserFormValues = {
  email: "ana@example.com",
  password: "Str0ng!Passw0rd",
  role: "2",
  name: "Ana",
  surname: "García",
  dateBirth: "1990-05-17",
};

describe("validateCreateUserForm", () => {
  it("answers no violation for valid values", () => {
    // Arrange / Act
    const result = validateCreateUserForm(valid);

    // Assert
    expect(result).toEqual({});
  });

  it("reports every empty required field", () => {
    // Arrange
    const values = { ...valid, email: "", password: "", role: "" };

    // Act
    const result = validateCreateUserForm(values);

    // Assert
    expect(result).toEqual({ email: "required", password: "required", role: "required" });
  });

  it("reports a malformed email", () => {
    // Arrange
    const values = { ...valid, email: "ana.example.com" };

    // Act
    const result = validateCreateUserForm(values);

    // Assert
    expect(result).toEqual({ email: "email" });
  });

  it("accepts empty optional fields", () => {
    // Arrange
    const values = { ...valid, name: "", surname: "", dateBirth: "" };

    // Act
    const result = validateCreateUserForm(values);

    // Assert
    expect(result).toEqual({});
  });

  it("reports a name or surname longer than 100 characters after trimming", () => {
    // Arrange
    const values = { ...valid, name: "a".repeat(101), surname: "b".repeat(101) };

    // Act
    const result = validateCreateUserForm(values);

    // Assert
    expect(result).toEqual({ name: "maxLength", surname: "maxLength" });
  });

  it("accepts a name of 100 characters surrounded by spaces", () => {
    // Arrange
    const values = { ...valid, name: `  ${"a".repeat(100)}  ` };

    // Act
    const result = validateCreateUserForm(values);

    // Assert
    expect(result).toEqual({});
  });

  it("reports a date of birth that is not a real calendar day", () => {
    // Arrange
    const values = { ...valid, dateBirth: "2001-02-30" };

    // Act
    const result = validateCreateUserForm(values);

    // Assert
    expect(result).toEqual({ dateBirth: "date" });
  });

  it("leaves password strength to the API", () => {
    // Arrange
    const values = { ...valid, password: "a" };

    // Act
    const result = validateCreateUserForm(values);

    // Assert
    expect(result).toEqual({});
  });
});
