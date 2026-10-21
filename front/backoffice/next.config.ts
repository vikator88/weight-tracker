import type { NextConfig } from "next";

const nextConfig: NextConfig = {
  // Static SPA: no server runtime (STPR_INVARIANTS.md, section 4.2).
  output: "export",
  transpilePackages: ["@weight-tracker/core"],
};

export default nextConfig;
