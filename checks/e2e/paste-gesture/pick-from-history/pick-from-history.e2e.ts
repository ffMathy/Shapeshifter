// ail-derived-from: PickFromHistory@88f45b41e4640889
// End-to-end check for PickFromHistory: choosing an older item with the keys
// already held, on a real X11 desktop, seen only through what the user sees.
import { afterAll, afterEach, beforeAll, expect } from "bun:test";
import { holdVUntilThePickerAppears, keyDown, keyUp, pickerVisible, releaseAll, sleep, tap, TargetApp, test, unique, waitForPicker } from "../../desktop.ts";

let app: TargetApp;
beforeAll(async () => { app = await TargetApp.start(); }, 90_000);
afterAll(async () => { await releaseAll(); await app?.stop(); });
afterEach(async () => { await releaseAll(); await waitForPicker(false); });

const copyAll = async (...texts: string[]) => { for (const t of texts) await app.copy(t); };

test("pressing V again picks the next older item", async () => {
  const id = unique();
  await copyAll(`one ${id}`, `two ${id}`, `three ${id}`);
  await app.prepare();
  await keyDown("ctrl");
  await holdVUntilThePickerAppears();
  await tap("v");
  expect(pickerVisible()).toBe(true);
  await keyUp("ctrl");
  expect(await waitForPicker(false)).toBe(false);
  expect(await app.waitForText(`two ${id}`)).toBe(`two ${id}`);
}, 30_000);

test("the arrow keys move the choice", async () => {
  const id = unique();
  await copyAll(`red ${id}`, `green ${id}`, `blue ${id}`);
  await app.prepare();
  await keyDown("ctrl");
  await holdVUntilThePickerAppears();
  await tap("Down");
  await tap("Down");
  await tap("Up");
  await keyUp("ctrl");
  expect(await app.waitForText(`green ${id}`)).toBe(`green ${id}`);
}, 30_000);

test("a number jumps to that item", async () => {
  const id = unique();
  await copyAll(`first ${id}`, `second ${id}`, `third ${id}`);
  await app.prepare();
  await keyDown("ctrl");
  await keyDown("v");
  await tap("3");
  await keyUp("v");
  expect(await waitForPicker(true)).toBe(true);
  await keyUp("ctrl");
  expect(await app.waitForText(`first ${id}`)).toBe(`first ${id}`);
}, 30_000);

test("Enter pastes right away", async () => {
  const id = unique();
  await copyAll(`early ${id}`, `late ${id}`);
  await app.prepare();
  await keyDown("ctrl");
  await holdVUntilThePickerAppears();
  await tap("Down");
  await tap("Return");
  expect(await waitForPicker(false)).toBe(false);
  expect(await app.waitForText(`early ${id}`)).toBe(`early ${id}`);
  await keyUp("ctrl");
  await sleep(500);
  expect(await app.text()).toBe(`early ${id}`);
}, 30_000);

test("the pasted item moves to the top", async () => {
  const id = unique();
  await copyAll(`older ${id}`, `newer ${id}`);
  await app.prepare();
  await keyDown("ctrl");
  await holdVUntilThePickerAppears();
  await tap("Down");
  await keyUp("ctrl");
  expect(await app.waitForText(`older ${id}`)).toBe(`older ${id}`);
  await sleep(300);
  await app.prepare();
  await keyDown("ctrl");
  await tap("v");
  await keyUp("ctrl");
  expect(await app.waitForText(`older ${id}`)).toBe(`older ${id}`);
}, 30_000);

test("Escape cancels without pasting", async () => {
  const id = unique();
  await copyAll(`keep ${id}`, `latest ${id}`);
  await app.prepare();
  await keyDown("ctrl");
  await holdVUntilThePickerAppears();
  await tap("v");
  await tap("Escape");
  expect(await waitForPicker(false)).toBe(false);
  await keyUp("ctrl");
  await sleep(700);
  expect(await app.text()).toBe("");
}, 30_000);
