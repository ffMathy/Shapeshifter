// ail-derived-from: KeepHistoryOpen@0a094410a23655fe
// End-to-end check for KeepHistoryOpen: Space keeps the picker up without
// holding Ctrl, on a real X11 desktop, seen only through what the user sees.
import { afterAll, afterEach, beforeAll, expect } from "bun:test";
import { keyDown, keyUp, pickerVisible, releaseAll, sleep, tap, TargetApp, test, unique, waitForPicker } from "../../desktop.ts";

let app: TargetApp;
beforeAll(async () => { app = await TargetApp.start(); }, 90_000);
afterAll(async () => { await releaseAll(); await app?.stop(); });
afterEach(async () => { await tap("Escape"); await releaseAll(); await waitForPicker(false); });

const keepOpen = async () => {
  await keyDown("ctrl");
  await keyDown("v");
  await tap("space");
  await keyUp("v");
  expect(await waitForPicker(true)).toBe(true);
  await keyUp("ctrl");
};

test("Space keeps the history open, and Enter pastes", async () => {
  const id = unique();
  for (const t of [`oak ${id}`, `elm ${id}`, `ash ${id}`]) await app.copy(t);
  await app.prepare();
  await keepOpen();
  await sleep(400);
  expect(pickerVisible()).toBe(true);
  await tap("Down");
  await tap("Down");
  await tap("2");
  await tap("Return");
  expect(await app.waitForText(`elm ${id}`)).toBe(`elm ${id}`);
  expect(await waitForPicker(false)).toBe(false);
}, 30_000);

test("Escape closes a history kept open, without pasting", async () => {
  const id = unique();
  for (const t of [`stay ${id}`, `newest ${id}`]) await app.copy(t);
  await app.prepare();
  await keepOpen();
  await tap("Escape");
  expect(await waitForPicker(false)).toBe(false);
  await sleep(700);
  expect(await app.text()).toBe("");
}, 30_000);
