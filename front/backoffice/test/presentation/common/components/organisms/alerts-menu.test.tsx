import { screen, waitFor, within } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { describe, expect, it } from "vitest";
import AlertsMenu from "@/presentation/common/components/organisms/alerts-menu";
import renderWithProviders from "../../../../support/render-with-providers";

const alertTexts = [
  "Juan Pérez ha completado su entrenamiento.",
  "María López ha aceptado tu invitación.",
  "Revisión semanal pendiente de Carlos Ruiz.",
];

describe("AlertsMenu", () => {
  it("shows the number of alerts on the bell", async () => {
    // Arrange & Act
    await renderWithProviders(<AlertsMenu />);

    // Assert
    expect(
      within(screen.getByRole("button", { name: "Alertas" })).getByText("3"),
    ).toBeInTheDocument();
  });

  it("lists the alerts under their title when opened", async () => {
    // Arrange
    await renderWithProviders(<AlertsMenu />);

    // Act
    await userEvent.setup().click(screen.getByRole("button", { name: "Alertas" }));

    // Assert
    const menu = await screen.findByRole("menu");
    expect(within(menu).getByText("Alertas")).toBeInTheDocument();
    expect(
      within(menu)
        .getAllByRole("menuitem")
        .map((item) => item.textContent),
    ).toEqual(alertTexts);
  });

  it("closes when an alert is chosen", async () => {
    // Arrange
    await renderWithProviders(<AlertsMenu />);
    const user = userEvent.setup();
    await user.click(screen.getByRole("button", { name: "Alertas" }));
    const menu = await screen.findByRole("menu");

    // Act
    await user.click(within(menu).getByRole("menuitem", { name: alertTexts[0] }));

    // Assert
    await waitFor(() => expect(screen.queryByRole("menu")).not.toBeInTheDocument());
  });
});
