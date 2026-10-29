import { afterEach, describe, expect, it, vi } from "vitest";
import {
  currentWeekStart,
  shiftWeek,
  weekEndOf,
  weekMiddleOf,
  weekStartOf,
} from "@/presentation/workouts/week";

describe("weekStartOf", () => {
  it("answers the Monday for a Sunday", () => {
    // Arrange / Act
    const result = weekStartOf("2026-11-01");

    // Assert
    expect(result).toBe("2026-10-26");
  });

  it("answers the Monday for a Wednesday", () => {
    // Arrange / Act
    const result = weekStartOf("2026-10-28");

    // Assert
    expect(result).toBe("2026-10-26");
  });

  it("keeps a Monday", () => {
    // Arrange / Act
    const result = weekStartOf("2026-10-26");

    // Assert
    expect(result).toBe("2026-10-26");
  });

  it("crosses back into the previous year", () => {
    // Arrange / Act
    const result = weekStartOf("2027-01-03");

    // Assert
    expect(result).toBe("2026-12-28");
  });
});

describe("weekMiddleOf", () => {
  it("answers the Wednesday, across a year", () => {
    // Arrange / Act
    const result = weekMiddleOf("2026-12-28");

    // Assert
    expect(result).toBe("2026-12-30");
  });
});

describe("weekEndOf", () => {
  it("answers the Sunday, across a month", () => {
    // Arrange / Act
    const result = weekEndOf("2026-10-26");

    // Assert
    expect(result).toBe("2026-11-01");
  });
});

describe("shiftWeek", () => {
  it("moves one week forward, across a month", () => {
    // Arrange / Act
    const result = shiftWeek("2026-10-26", 1);

    // Assert
    expect(result).toBe("2026-11-02");
  });

  it("moves one week back", () => {
    // Arrange / Act
    const result = shiftWeek("2026-10-26", -1);

    // Assert
    expect(result).toBe("2026-10-19");
  });
});

describe("currentWeekStart", () => {
  afterEach(() => {
    vi.useRealTimers();
  });

  it("answers the Monday of today's week", () => {
    // Arrange
    vi.useFakeTimers({ toFake: ["Date"] });
    vi.setSystemTime(new Date(2026, 9, 29, 12));

    // Act
    const result = currentWeekStart();

    // Assert
    expect(result).toBe("2026-10-26");
  });
});
