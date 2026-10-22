/**
 * Global setup for every Vitest test file (referenced from vite.config.ts).
 *
 * - Installs the jest-dom matchers onto Vitest's `expect`
 *   (`@testing-library/jest-dom/vitest` is the Vitest-specific entry point,
 *   verified to exist in @testing-library/jest-dom 7.0.1).
 * - Unmounts React trees between tests: RTL only auto-cleans up when Vitest
 *   `globals: true` is enabled, which this project deliberately does not use.
 * - Restores side effects from previous tests: translation state and
 *   document-level state (language/direction) are shared for the whole file.
 */
import { cleanup } from "@testing-library/react";
import { afterEach } from "vitest";
import "@testing-library/jest-dom/vitest";

afterEach(() => {
  cleanup();
});
