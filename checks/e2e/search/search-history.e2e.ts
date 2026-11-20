// ail-derived-from: SearchHistory@aac74c1589e9545c
// End-to-end check for SearchHistory: S during the gesture, typing in the
// picker, then Enter, Escape or another window, on a real X11 desktop. It sees
// only what a user sees: which window has the keyboard focus, the text that
// lands in another app, files on disk, and the words on the picker.
import { afterAll, afterEach, beforeAll, expect } from "bun:test";
import { readFileSync } from "node:fs";
import { keyDown, keyUp, releaseAll, sleep, tap, TargetApp, test, unique, waitForPicker } from "../desktop.ts";
import { clickAt, clipboardAs, clipboardTargets, focusedWindowName, startSearch, type, waitForFocus, waitForPickerWords } from "./search-driver.ts";

const target = "Shapeshifter E2E Target";
let app: TargetApp;
beforeAll(async () => { app = await TargetApp.start(); }, 90_000);
afterAll(async () => { await releaseAll(); await app?.stop(); });
afterEach(async () => {
  await releaseAll();
  if (focusedWindowName() === "Shapeshifter") await tap("Escape");
  await waitForPicker(false);
});

const copyAll = async (...texts: string[]) => { for (const t of texts) await app.copy(t); };

test("S starts a search, even before the history appears", async () => {
  const id = unique();
  await copyAll(`pear ${id}`);
  await app.prepare();
  await keyDown("ctrl");
  await keyDown("v", 30);
  await tap("s", 30);
  await keyUp("v");
  await keyUp("ctrl");
  expect(await waitForPicker(true)).toBe(true);
  expect(await waitForFocus("Shapeshifter")).toBe(true);
  const words = await waitForPickerWords(["search everything you copied"]);
  expect(words).toContain("search everything you copied");
  expect(await app.text()).toBe("");
}, 30_000);

test("typing narrows the history, and Enter pastes into the app the user came from", async () => {
  const id = unique();
  await copyAll(`plum ${id}`, `fig ${id}`, `lime ${id}`);
  await app.prepare();
  await startSearch();
  await type(`fig ${id}`);
  expect(await app.text()).toBe("");
  await tap("Return");
  expect(await waitForPicker(false)).toBe(false);
  expect(await waitForFocus(target)).toBe(true);
  expect(await app.waitForText(`fig ${id}`)).toBe(`fig ${id}`);
}, 30_000);

test("the arrows move through the results, wrapping around", async () => {
  const id = unique();
  await copyAll(`kiwi red ${id}`, `kiwi green ${id}`, `kiwi blue ${id}`);
  await app.prepare();
  await startSearch();
  await type(`kiwi ${id}`);
  await tap("Up");
  await tap("Return");
  expect(await app.waitForText(`kiwi red ${id}`)).toBe(`kiwi red ${id}`);
}, 30_000);

test("Right at the end of the query opens the actions", async () => {
  const id = unique();
  await copyAll(`memo ${id}`, `other ${id}`);
  await app.prepare();
  await startSearch();
  await type(`memo ${id}`);
  await tap("Right");
  await tap("Down");
  await tap("Return");
  expect(await waitForPicker(false)).toBe(false);
  // The second action, "Paste as file": the clipboard now holds a .txt file with the text.
  let uris = "";
  for (const until = Date.now() + 4000; Date.now() < until && !uris.includes(".txt"); await sleep(200)) {
    if (clipboardTargets().includes("text/uri-list")) uris = clipboardAs("text/uri-list");
  }
  const path = decodeURIComponent(uris.trim().split(/\r?\n/)[0].replace(/^file:\/\//, ""));
  expect(path.endsWith(".txt")).toBe(true);
  expect(readFileSync(path, "utf8").replace(/^\uFEFF/, "")).toBe(`memo ${id}`);
  // A copy made within a moment of "Paste as file" is not kept in the history (not this check's subject): wait.
  await sleep(2000);
}, 30_000);

test("Right inside the query moves the caret instead", async () => {
  const id = unique();
  await copyAll(`note ${id}`, `other ${id}`);
  await app.prepare();
  await startSearch();
  await type(`note ${id}`);
  await tap("Left");
  await tap("Right");
  await tap("Return");
  expect(await app.waitForText(`note ${id}`)).toBe(`note ${id}`);
}, 30_000);

test("Escape gives the focus back without pasting", async () => {
  const id = unique();
  await copyAll(`first ${id}`, `second ${id}`);
  await app.prepare();
  await startSearch();
  await type("first");
  await tap("Escape");
  expect(await waitForPicker(false)).toBe(false);
  expect(await waitForFocus(target)).toBe(true);
  await type("ok");
  expect(await app.waitForText("ok")).toBe("ok");
}, 30_000);

test("clicking another window cancels the search", async () => {
  const id = unique();
  await copyAll(`apricot ${id}`);
  await app.prepare();
  await startSearch();
  await type("apricot");
  // The text box of the other app, left of the picker.
  await clickAt(100, 200);
  expect(await waitForPicker(false)).toBe(false);
  expect(focusedWindowName()).toBe(target);
  await sleep(500);
  expect(await app.text()).toBe("");
}, 30_000);

test("Ctrl+V in the search box pastes nothing on Linux", async () => {
  const id = unique();
  await copyAll(`clip ${id}`);
  await app.prepare();
  await startSearch();
  await keyDown("ctrl");
  await tap("v");
  await keyUp("ctrl");
  await sleep(500);
  expect(focusedWindowName()).toBe("Shapeshifter");
  expect(await waitForPickerWords(["search everything you copied"])).toContain("search everything you copied");
  await tap("Escape");
  await sleep(500);
  expect(await app.text()).toBe("");
}, 30_000);
