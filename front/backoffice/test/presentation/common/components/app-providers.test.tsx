import { render, waitFor } from "@testing-library/react";
import { afterEach, describe, expect, it, vi } from "vitest";
import { DomainError, fail, ok } from "@weight-tracker/core/domain";
import type { Result } from "@weight-tracker/core/domain";
import i18n from "@/infrastructure/i18n";
import AppProviders from "@/presentation/common/components/app-providers";

const preferred = vi.hoisted(() => ({
  result: undefined as unknown as Promise<Result<string | null, DomainError>>,
}));

// The root providers import the built container directly, so the test swaps that module.
vi.mock("@/infrastructure/container", () => ({
  default: {
    resolve: (key: string) => {
      if (key === "getSessionUseCase") {
        return { execute: async () => ok(null) };
      }
      if (key === "getPreferredThemeModeUseCase") {
        return { execute: async () => ok(null) };
      }
      if (key !== "getPreferredLanguageUseCase") {
        throw new Error(`Unexpected dependency ${key}`);
      }
      return { execute: () => preferred.result };
    },
  },
}));

function setUp(stored: Result<string | null, DomainError>, browserLanguages: string[]) {
  preferred.result = Promise.resolve(stored);
  vi.spyOn(navigator, "languages", "get").mockReturnValue(browserLanguages);
}

describe("AppProviders", () => {
  afterEach(async () => {
    vi.restoreAllMocks();
    await i18n.changeLanguage("es");
  });

  it("starts in the stored language over the browser's", async () => {
    // Arrange
    setUp(ok("en"), ["es-ES"]);

    // Act
    render(<AppProviders>content</AppProviders>);

    // Assert
    await waitFor(() => expect(i18n.language).toBe("en"));
    expect(document.documentElement.lang).toBe("en");
  });

  it("follows the browser when no language is stored", async () => {
    // Arrange
    setUp(ok(null), ["en-US", "es-ES"]);

    // Act
    render(<AppProviders>content</AppProviders>);

    // Assert
    await waitFor(() => expect(i18n.language).toBe("en"));
    expect(document.documentElement.lang).toBe("en");
  });

  it("ignores a stored language the backoffice does not support", async () => {
    // Arrange
    setUp(ok("fr"), ["en-US"]);

    // Act
    render(<AppProviders>content</AppProviders>);

    // Assert
    await waitFor(() => expect(i18n.language).toBe("en"));
  });

  it("follows the browser when the stored language cannot be read", async () => {
    // Arrange
    setUp(fail(new DomainError("UNEXPECTED", "")), ["en-US"]);

    // Act
    render(<AppProviders>content</AppProviders>);

    // Assert
    await waitFor(() => expect(i18n.language).toBe("en"));
  });
});
