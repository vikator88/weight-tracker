import { act, renderHook } from "@testing-library/react";
import Favorite from "@mui/icons-material/Favorite";
import { afterEach, describe, expect, it } from "vitest";
import i18n from "@/infrastructure/i18n";
import useExerciseLabels from "@/presentation/workouts/hooks/use-exercise-labels";

const levels = ["LOW", "MIDLOW", "MID", "MIDHIGH", "HIGH"];

describe("useExerciseLabels", () => {
  afterEach(async () => {
    await act(() => i18n.changeLanguage("es"));
  });

  it("names the known types and gives each its colour, icon and pastel tone", () => {
    // Arrange & Act
    const { result } = renderHook(() => useExerciseLabels());

    // Assert
    const { typeLabel, typeColor, typeIcon, typeIconColor } = result.current;
    expect(["STRENGTH", "CARDIO", "STRETCHING"].map(typeLabel)).toEqual([
      "Fuerza",
      "Cardio",
      "Estiramiento",
    ]);
    expect(["STRENGTH", "CARDIO", "STRETCHING"].map(typeColor)).toEqual([
      "strength",
      "cardio",
      "stretching",
    ]);
    expect(typeIcon("CARDIO")).toBe(Favorite);
    expect(typeIconColor("CARDIO")).toBe("cardio.pastel");
  });

  it("names an unknown type as it comes, neutral and with no icon", () => {
    // Arrange & Act
    const { result } = renderHook(() => useExerciseLabels());

    // Assert
    expect(result.current.typeLabel("YOGA")).toBe("YOGA");
    expect(result.current.typeColor("YOGA")).toBe("default");
    expect(result.current.typeIcon("YOGA")).toBeUndefined();
    expect(result.current.typeWatermark("YOGA")).toBeUndefined();
    expect(result.current.typeColor("toString")).toBe("default");
  });

  it("reads RIR and RPE the same way in English", async () => {
    // Arrange
    const { result } = renderHook(() => useExerciseLabels());

    // Act
    await act(() => i18n.changeLanguage("en"));

    // Assert
    expect(levels.map((level) => result.current.effortLabel(level, "STRENGTH"))).toEqual([
      "RIR 5+",
      "RIR 4",
      "RIR 3",
      "RIR 1-2",
      "RIR 0",
    ]);
    expect(levels.map((level) => result.current.effortLabel(level, "CARDIO"))).toEqual([
      "RPE 1-2",
      "RPE 3-4",
      "RPE 5-6",
      "RPE 7-8",
      "RPE 9-10",
    ]);
    expect(result.current.typeLabel("STRENGTH")).toBe("Strength");
  });
});
