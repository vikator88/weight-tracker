import { describe, expect, it } from "vitest";
import type { Dependencies } from "@weight-tracker/core/application";
import container from "@/infrastructure/container";

const keys: (keyof Dependencies)[] = [
  "httpClient",
  "keyValueStore",
  "notificationService",
  "getSessionUseCase",
  "getMyProfileUseCase",
  "updateMyProfileUseCase",
];

describe("container", () => {
  it.each(keys)("resolves %s", (key) => {
    // Arrange & Act
    const dependency = container.resolve(key);

    // Assert
    expect(dependency).toBeDefined();
  });
});
