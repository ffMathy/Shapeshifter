// ail-derived-from: WhatSearchFinds@b79b620a1398ff71
// End-to-end check for WhatSearchFinds: which copies a search finds, on a real
// X11 desktop. Copies come from the target app and from small xterm windows
// with titles of our choosing; a find is proven by what Enter puts in the
// other app or on the clipboard, and "No matches" by the words on the picker.
import { afterAll, afterEach, beforeAll, expect } from "bun:test";
import { releaseAll, sleep, tap, TargetApp, test, unique, waitForPicker } from "../../desktop.ts";
import {
  clipboardAs, clipboardTargets, copyFromWindow, focusedWindowName, makeFile, squarePng, startSearch, type, waitForPickerWords,
} from "../search-driver.ts";

let app: TargetApp;
beforeAll(async () => { app = await TargetApp.start(); }, 90_000);
afterAll(async () => { await releaseAll(); await app?.stop(); });
afterEach(async () => {
  await releaseAll();
  if (focusedWindowName() === "Shapeshifter") await tap("Escape");
  await waitForPicker(false);
});

/** Waits until the clipboard offers `type` with content that passes `ok`. */
async function clipboardBecomes(type: string, ok: (content: string) => boolean, timeout = 4000): Promise<boolean> {
  for (const until = Date.now() + timeout; Date.now() < until; await sleep(200)) {
    if (clipboardTargets().includes(type) && ok(clipboardAs(type))) return true;
  }
  return false;
}

test("every word must occur, in any order, ignoring case and accents", async () => {
  const id = unique();
  await app.copy(`Crème brûlée ${id}`);
  await app.copy(`crème fraîche ${id}`);
  await app.prepare();
  await startSearch();
  await type(`BRULEE ${id} creme`);
  await tap("Return");
  expect(await app.waitForText(`Crème brûlée ${id}`)).toBe(`Crème brûlée ${id}`);
}, 30_000);

test("a search that finds nothing says so", async () => {
  const id = unique();
  await app.copy(`walnut ${id}`);
  await app.prepare();
  await startSearch();
  await type(`walnut ${id} pecan`);
  const words = await waitForPickerWords(["no matches", "nothing you copied contains"]);
  expect(words).toContain("no matches");
  expect(words).toContain("nothing you copied contains");
  await tap("Return");
  await sleep(500);
  expect(focusedWindowName()).toBe("Shapeshifter");
  expect(await waitForPicker(true)).toBe(true);
  await tap("Escape");
  await sleep(500);
  expect(await app.text()).toBe("");
}, 30_000);

test("the whole text is searched, not only its first line", async () => {
  const id = unique();
  const text = `shopping list ${id}\nbread\nmilk\nlast line saffron${id}`;
  await app.copy(text);
  await app.copy(`other ${id}`);
  await app.prepare();
  await startSearch();
  await type(`saffron${id}`);
  await tap("Return");
  expect(await app.waitForText(text)).toBe(text);
}, 30_000);

test("copied HTML is searched by its words, not its markup", async () => {
  const id = unique();
  await copyFromWindow(`page ${unique()}`, "text/html", `<p>Hello <span class="marker${id}">tangerine${id}</span></p>`);
  await app.copy(`later ${id}`);
  await app.prepare();
  await startSearch();
  await type(`marker${id}`);
  expect(await waitForPickerWords(["no matches"])).toContain("no matches");
  for (let i = 0; i < `marker${id}`.length; i++) await tap("BackSpace", 20);
  await type(`hello tangerine${id}`);
  await tap("Return");
  expect(await waitForPicker(false)).toBe(false);
  expect(await clipboardBecomes("text/html", (html) => html.includes(`tangerine${id}`))).toBe(true);
}, 30_000);

test("a copied file is found by its path and by the word file", async () => {
  const id = unique();
  const path = makeFile(`report-${id}.txt`);
  await copyFromWindow(`manager ${unique()}`, "text/uri-list", `file://${path}\r\n`);
  await app.copy(`later ${id}`);
  await app.prepare();
  await startSearch();
  await type(`file report-${id}`);
  await tap("Return");
  expect(await waitForPicker(false)).toBe(false);
  expect(await clipboardBecomes("text/uri-list", (uris) => uris.includes(`report-${id}.txt`))).toBe(true);
}, 30_000);

test("a copied picture is found by the word image", async () => {
  const id = unique();
  await copyFromWindow(`album ${id}`, "image/png", squarePng());
  await copyFromWindow(`album ${id}`, "text", `caption ${unique()}`);
  await app.copy(`later ${unique()}`);
  await app.prepare();
  await startSearch();
  await type(`image album ${id}`);
  // Only the picture is left, titled "Image · 24 × 24"; the caption from the same window is not. (Pasting the
  // picture is not part of this check: on Linux, serving a restored picture to another app crashed Shapeshifter.)
  const words = await waitForPickerWords(["24 x 24"]);
  expect(words).toMatch(/24 ?[x×] ?24/);
  expect(words).not.toContain("caption");
  expect(words).not.toContain("no matches");
}, 30_000);

test("an item is found by the window it was copied from", async () => {
  const id = unique();
  const words = `sunny weather ${unique()}`;
  await copyFromWindow(`notes ${id}`, "text", words);
  await app.copy(`later ${unique()}`);
  await app.prepare();
  await startSearch();
  await type(`notes ${id}`);
  await tap("Return");
  expect(await app.waitForText(words)).toBe(words);
}, 30_000);
