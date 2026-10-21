import type { Dependencies } from "@weight-tracker/core/application";
import container from "@/infrastructure/container";

const keys: (keyof Dependencies)[] = ["httpClient", "keyValueStore", "notificationService"];

describe("container", () => {
  it.each(keys)("resolves %s", (key) => {
    // Arrange & Act
    const dependency = container.resolve(key);

    // Assert
    expect(dependency).toBeDefined();
  });
});
