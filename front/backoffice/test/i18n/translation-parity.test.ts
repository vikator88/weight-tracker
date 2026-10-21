import { describe, expect, it } from "vitest";
import resources from "@/i18n/resources";

function keysOf(value: unknown, prefix = ""): string[] {
  if (typeof value !== "object" || value === null) {
    return [prefix];
  }

  return Object.entries(value).flatMap(([key, child]) =>
    keysOf(child, prefix ? `${prefix}.${key}` : key),
  );
}

describe("translations", () => {
  it.each(Object.keys(resources.es))("has the same %s keys in es and en", (namespace) => {
    // Arrange
    const es = resources.es[namespace as keyof typeof resources.es];
    const en = resources.en[namespace as keyof typeof resources.en];

    // Act
    const [esKeys, enKeys] = [keysOf(es).sort(), keysOf(en).sort()];

    // Assert
    expect(enKeys).toEqual(esKeys);
  });

  it("has the same namespaces in es and en", () => {
    // Arrange & Act
    const [es, en] = [Object.keys(resources.es).sort(), Object.keys(resources.en).sort()];

    // Assert
    expect(en).toEqual(es);
  });
});
