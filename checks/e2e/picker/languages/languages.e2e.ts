// ail-derived-from: Languages@988032379fe28687
// End-to-end check for Languages: the picker in the language SHAPESHIFTER_LANGUAGE
// names, laid out from right to left in Arabic. Each scenario starts Shapeshifter
// on a desktop of its own with that variable set, as a user would, and reads the
// picker back from the screen (OCR, English letters only).
import { expect } from "bun:test";
import { test } from "../../desktop.ts";
import {
  OwnDesktop, cancelThePicker, copyWithXclip, holdThePickerOpen, readText, readWords, releaseAll, shootPicker, squash,
} from "../picker-driver.ts";

async function withLanguage(language: string, body: (display: string) => Promise<void>) {
  const desktop = await OwnDesktop.start({ width: 1280, height: 800, name: `language-${language}`, env: { SHAPESHIFTER_LANGUAGE: language } });
  try {
    await body(desktop.display);
  } finally {
    await releaseAll(desktop.display);
    await desktop.stop();
  }
}

test("the picker speaks the language SHAPESHIFTER_LANGUAGE names", async () => {
  await withLanguage("de", async (d) => {
    const box = await holdThePickerOpen(d);
    const text = squash(readText(shootPicker(box, d)));
    await cancelThePicker(d);
    expect(text).toContain("Noch nichts kopiert");
    expect(text).toContain("Strg");
    expect(text).toContain("Abbrechen");
    expect(text).not.toContain("Nothing copied yet");
  });
}, 180_000);

test("the Linux look's list headings stay in English", async () => {
  await withLanguage("de", async (d) => {
    await copyWithXclip("ein kurzer Text", undefined, d);
    const box = await holdThePickerOpen(d);
    const text = squash(readText(shootPicker(box, d)));
    await cancelThePicker(d);
    expect(text).toContain("Gerade eben");
    expect(text).toContain("History");
    expect(text).toContain("Actions");
    expect(text).toContain("to choose");
  });
}, 180_000);

test("in Arabic the picker is laid out from right to left", async () => {
  for (const [language, side] of [["de", "left"], ["ar", "right"]] as const) {
    await withLanguage(language, async (d) => {
      const box = await holdThePickerOpen(d);
      const png = shootPicker(box, d);
      await cancelThePicker(d);
      // The first key in the footer, Ctrl (Strg in German), starts the reading order: on the left, or on the right.
      // The footer is the lowest place the key's name is written (the empty picker's hint names it too).
      const first = readWords(png).filter((w) => w.text.includes(language === "de" ? "Strg" : "Ctrl")).sort((a, b) => b.top - a.top)[0];
      expect(first).toBeDefined();
      const middle = box.width; // the screenshot is twice the picker's size
      expect(first!.left < middle ? "left" : "right").toBe(side);
    });
  }
}, 300_000);
