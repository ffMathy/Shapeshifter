// ail-derived-from: PasteGesture@c39fdb090a7b5b8b
// End-to-end check for PasteGesture: its scenarios, played with real keys on
// an X11 desktop against the running app. It sees only what a user sees: the
// text that lands in another app, and whether the picker is on screen.
import { afterAll, afterEach, beforeAll, expect } from "bun:test";
import { holdVUntilThePickerAppears, keyDown, keyUp, pickerVisible, releaseAll, sleep, tap, TargetApp, test, unique, waitForPicker } from "../desktop.ts";

let app: TargetApp;
beforeAll(async () => { app = await TargetApp.start(); }, 90_000);
afterAll(async () => { await releaseAll(); await app?.stop(); });
afterEach(async () => { await releaseAll(); await waitForPicker(false); });

test("a quick Ctrl+V pastes what was copied last", async () => {
  const text = `alpha ${unique()}`;
  await app.copy(text);
  await app.prepare();
  await keyDown("ctrl");
  await tap("v");
  expect(pickerVisible()).toBe(false);
  await keyUp("ctrl");
  expect(await app.waitForText(text)).toBe(text);
  expect(pickerVisible()).toBe(false);
}, 20_000);

test("keeping Ctrl held, each tap of V pastes again", async () => {
  const text = `again ${unique()}`;
  await app.copy(text);
  await app.prepare();
  await keyDown("ctrl");
  await tap("v");
  expect(await app.waitForText(text)).toBe(text);
  await tap("v");
  expect(await app.waitForText(text + text)).toBe(text + text);
  expect(pickerVisible()).toBe(false);
  await keyUp("ctrl");
  await sleep(500);
  expect(await app.text()).toBe(text + text);
}, 20_000);

test("letting go of Ctrl a moment before V still pastes", async () => {
  const text = `rolling ${unique()}`;
  await app.copy(text);
  await app.prepare();
  await keyDown("ctrl");
  await keyDown("v", 40);
  await keyUp("ctrl");
  await keyUp("v");
  expect(await app.waitForText(text)).toBe(text);
}, 20_000);

test("typing V without Ctrl types a v", async () => {
  await app.prepare();
  await tap("v");
  await tap("v");
  expect(await app.waitForText("vv")).toBe("vv");
}, 20_000);

test("Ctrl+Shift+V is left to the app", async () => {
  await app.copy(`unrelated ${unique()}`);
  await app.prepare();
  await keyDown("ctrl");
  await keyDown("shift");
  await keyDown("v", 0);
  let appeared = false;
  for (const until = Date.now() + 700; Date.now() < until; await sleep(50)) appeared ||= pickerVisible();
  await keyUp("v");
  await keyUp("shift");
  await keyUp("ctrl");
  expect(appeared).toBe(false);
}, 20_000);

test("holding Ctrl+V shows the history", async () => {
  const id = unique();
  await app.copy(`first ${id}`);
  await app.copy(`newest ${id}`);
  await app.prepare();
  await keyDown("ctrl");
  await holdVUntilThePickerAppears();
  expect(pickerVisible()).toBe(true);
  await keyUp("ctrl");
  expect(await waitForPicker(false)).toBe(false);
  expect(await app.waitForText(`newest ${id}`)).toBe(`newest ${id}`);
}, 20_000);
