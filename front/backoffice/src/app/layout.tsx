import type { Metadata } from "next";
import type { ReactNode } from "react";
import AppProviders from "@/presentation/common/components/app-providers";

export const metadata: Metadata = {
  title: "Weight Tracker",
};

export default function RootLayout({ children }: Readonly<{ children: ReactNode }>) {
  return (
    <html lang="es">
      <body>
        <AppProviders>{children}</AppProviders>
      </body>
    </html>
  );
}
