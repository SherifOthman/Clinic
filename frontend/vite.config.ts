// defineConfig comes from vitest/config so this single config can carry the
// `test` block (environment, setup files) alongside the real Vite build config.
// Vitest then reuses the same `@` alias and plugins as the app.
import { defineConfig } from "vitest/config";
import react from "@vitejs/plugin-react";
import tailwindcss from "@tailwindcss/vite";
import path from "node:path";

export default defineConfig({
  plugins: [react(), tailwindcss()],
  resolve: {
    alias: { "@": path.resolve(import.meta.dirname, "./src") },
  },
  server: {
    port: 5173,
    // Proxy API calls to the backend during development (avoids CORS + matches single-deploy).
    proxy: {
      "/api": "http://localhost:5000",
      "/health": "http://localhost:5000",
    },
  },
  build: {
    outDir: "../backend/src/Clinic.Api/wwwroot",
    emptyOutDir: true,
  },
  test: {
    // Real browser-like DOM for component tests. `jsdom` is an explicit
    // devDependency (it is an *optional* peer of vitest, and this project's
    // `.npmrc` uses legacy-peer-deps, which does not auto-install peers).
    environment: "jsdom",
    setupFiles: "./src/tests/setup.ts",
    // Coverage provider must match the exact vitest version (@vitest/coverage-v8 5.0.5).
    // No thresholds yet: this task only establishes a measured baseline.
    coverage: {
      provider: "v8",
    },
  },
});
