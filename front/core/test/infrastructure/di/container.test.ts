import { describe, expect, it } from "vitest";
import { Container } from "../../../src/infrastructure";

interface Deps {
  clock: { now: () => number };
  greeter: { greet: () => string };
}

describe("Container", () => {
  it("returns the same instance for a singleton", () => {
    // Arrange
    const container = new Container<Deps>({
      clock: () => ({ now: () => 1 }),
      greeter: () => ({ greet: () => "hola" }),
    });
    const first = container.resolve("clock");

    // Act
    const second = container.resolve("clock");

    // Assert
    expect(second).toBe(first);
  });

  it("returns a new instance for a transient", () => {
    // Arrange
    const container = new Container<Deps>({
      clock: Container.transient(() => ({ now: () => 1 })),
      greeter: () => ({ greet: () => "hola" }),
    });
    const first = container.resolve("clock");

    // Act
    const second = container.resolve("clock");

    // Assert
    expect(second).not.toBe(first);
  });

  it("lets a factory resolve its own dependencies", () => {
    // Arrange
    const container = new Container<Deps>({
      clock: () => ({ now: () => 42 }),
      greeter: (c) => ({ greet: () => `hola ${c.resolve("clock").now()}` }),
    });

    // Act
    const greeting = container.resolve("greeter").greet();

    // Assert
    expect(greeting).toBe("hola 42");
  });
});
