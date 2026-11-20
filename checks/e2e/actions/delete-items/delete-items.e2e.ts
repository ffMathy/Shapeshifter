// End-to-end check for DeleteItems: Delete removes the chosen item from the
// history while the picker is up. It reads the item count on screen (OCR) and
// what arrives in another app.
import { afterAll, afterEach, beforeAll, expect, test } from "bun:test";
import { TargetApp, keyUp, releaseAll, sleep, tap, unique, waitForPicker } from "../../desktop.ts";
import { itemCountOnScreen, openPicker, readPicker } from "../actions-driver.ts";

let app: TargetApp;
beforeAll(async () => { app = await TargetApp.start(); }, 90_000);
afterAll(async () => { await releaseAll(); await app?.stop(); });
afterEach(async () => { await releaseAll(); await waitForPicker(false); });

test("Delete removes the chosen item", async () => {
  const id = unique();
  for (const t of ["keep", "drop", "top"]) await app.copy(`${t} ${id}`);
  await app.prepare();
  await openPicker(1);
  const before = itemCountOnScreen();
  await tap("Delete");
  await sleep(400);
  expect(itemCountOnScreen()).toBe(before - 1);
  expect(readPicker("history")).not.toContain(`drop ${id}`);
  await tap("Escape");
  await keyUp("ctrl");
  await sleep(300);
  await app.prepare();
  await openPicker(1);
  await keyUp("ctrl");
  expect(await app.waitForText(`keep ${id}`)).toBe(`keep ${id}`);
}, 40_000);

test("after Delete the next item is chosen, and letting go of Ctrl pastes it", async () => {
  const id = unique();
  for (const t of ["first", "second", "third"]) await app.copy(`${t} ${id}`);
  await app.prepare();
  await openPicker(1);
  await tap("Delete");
  await keyUp("ctrl");
  expect(await waitForPicker(false)).toBe(false);
  expect(await app.waitForText(`first ${id}`)).toBe(`first ${id}`);
}, 40_000);
