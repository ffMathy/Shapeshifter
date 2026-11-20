// ail-derived-from: Actions@adb0f4c4431849b6
// End-to-end check for Actions: opening the actions of an item with Right,
// going back with Left, and running the chosen action by letting go of Ctrl.
// It sees only the text that lands in another app and the clipboard (xclip).
import { afterAll, afterEach, beforeAll, expect } from "bun:test";
import { keyUp, releaseAll, sleep, tap, TargetApp, test, unique, waitForPicker } from "../desktop.ts";
import { clipboardFiles, clipboardFormats, openPicker } from "./actions-driver.ts";

let app: TargetApp;
beforeAll(async () => { app = await TargetApp.start(); }, 90_000);
afterAll(async () => { await releaseAll(); await app?.stop(); });
afterEach(async () => { await releaseAll(); await waitForPicker(false); });

test("Right opens the actions, and letting go of Ctrl runs the chosen one", async () => {
  const id = unique();
  await app.copy(`memo ${id}`);
  await app.prepare();
  await openPicker();
  await tap("Right");
  await tap("Down");
  await keyUp("ctrl");
  expect(await waitForPicker(false)).toBe(false);
  await sleep(1500);
  expect(await app.text()).toBe("");
  const files = clipboardFiles();
  expect(files).toHaveLength(1);
  expect(files[0]).toMatch(/\/Text \d{4}-\d\d-\d\d \d\d\.\d\d\.\d\d\.txt$/);
  expect(await Bun.file(files[0]).text()).toBe(`memo ${id}`);
}, 30_000);

test("Left goes back to the history", async () => {
  const id = unique();
  for (const t of ["ant", "bee", "cat"]) await app.copy(`${t} ${id}`);
  await app.prepare();
  await openPicker();
  await tap("Right");
  await tap("Down");
  await tap("Left");
  await tap("Down");
  await keyUp("ctrl");
  expect(await app.waitForText(`bee ${id}`)).toBe(`bee ${id}`);
}, 30_000);

test("choosing another item starts again at Paste", async () => {
  const id = unique();
  for (const t of ["owl", "fox"]) await app.copy(`${t} ${id}`);
  await app.prepare();
  await openPicker();
  await tap("Right");
  await tap("Down");
  await tap("v");
  await keyUp("ctrl");
  expect(await app.waitForText(`owl ${id}`)).toBe(`owl ${id}`);
}, 30_000);

test("Escape in the actions cancels without running anything", async () => {
  const id = unique();
  await app.copy(`calm ${id}`);
  const before = clipboardFormats();
  await app.prepare();
  await openPicker();
  await tap("Right");
  await tap("Down");
  await tap("Escape");
  await keyUp("ctrl");
  await sleep(1000);
  expect(await app.text()).toBe("");
  expect(clipboardFormats()).toEqual(before);
}, 30_000);
