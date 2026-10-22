import { fireEvent, render, screen, waitFor } from "@testing-library/react";
import { afterEach, describe, expect, it } from "vitest";
import { LanguageSelector } from "@/components/common/LanguageSelector";
import i18n from "@/i18n";

/**
 * Smoke test for existing, shipped code (the language switcher).
 *
 * It exercises the real i18n module (`@/i18n`), including its documented
 * contract that `<html dir/lang>` follows the selected language — the reason
 * the app is reported to support Arabic with proper RTL.
 */
describe("LanguageSelector", () => {
  afterEach(async () => {
    // Restore the shared i18n/document state so this file leaves no side effects.
    await i18n.changeLanguage("en");
    window.localStorage.removeItem("clinic.lang");
  });

  it("renders both supported languages and starts in English with LTR", async () => {
    render(<LanguageSelector />);

    expect(screen.getByRole("button", { name: "English" })).toBeInTheDocument();
    expect(screen.getByRole("button", { name: "العربية" })).toBeInTheDocument();

    // The i18n module applies direction/lang to <html> on load.
    await waitFor(() => {
      expect(document.documentElement.lang).toBe("en");
      expect(document.documentElement.dir).toBe("ltr");
    });
  });

  it("switches the document to Arabic/RTL and back to English/LTR", async () => {
    render(<LanguageSelector />);

    fireEvent.click(screen.getByRole("button", { name: "العربية" }));
    await waitFor(() => {
      expect(document.documentElement.lang).toBe("ar");
      expect(document.documentElement.dir).toBe("rtl");
    });

    fireEvent.click(screen.getByRole("button", { name: "English" }));
    await waitFor(() => {
      expect(document.documentElement.lang).toBe("en");
      expect(document.documentElement.dir).toBe("ltr");
    });
  });
});
