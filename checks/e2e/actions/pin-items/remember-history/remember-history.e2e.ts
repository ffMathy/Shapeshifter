// ail-derived-from: RememberHistory@24ab9c40f887e1a6
// End-to-end check for RememberHistory: the "Remember history after restart"
// setting, switched in the settings window (opened from the tray icon), and
// what comes back after quitting Shapeshifter from its tray menu and starting
// it again. Each scenario runs on a desktop of its own (PrivateDesktop); the
// saved history is a folder the user can see in Shapeshifter's data folder.
import { afterEach, expect } from "bun:test";
import { existsSync } from "node:fs";
import { join } from "node:path";
import { keyUp, releaseAll, sleep, tap, TargetApp, test } from "../../../desktop.ts";
import { PrivateDesktop, readableId, openPicker, readPicker } from "../../actions-driver.ts";

let desk: PrivateDesktop | undefined;
let app: TargetApp | undefined;
let restore: (() => void) | undefined;

async function setUp() {
  desk = await PrivateDesktop.start();
  restore = desk.use();
  await desk.startApp();
  app = await TargetApp.start();
  return app;
}

afterEach(async () => {
  await releaseAll();
  await app?.stop();
  await desk?.stop();
  restore?.();
  app = desk = restore = undefined;
});

test("with the setting on, the whole history comes back in its order", async () => {
  const id = readableId();
  const first = await setUp();
  await desk!.toggleRememberHistory();
  for (const t of ["apple", "banana", "cherry"]) await first.copy(`${t} ${id}`);
  await sleep(1000);
  await desk!.quitApp();
  // The app that copied goes too, so nothing is on the clipboard when Shapeshifter starts again.
  await first.stop();
  await desk!.startApp();
  const fresh = await TargetApp.start();
  app = fresh;
  await fresh.prepare();
  await openPicker();
  const history = readPicker("history", `cherry ${id}`, `banana ${id}`, `apple ${id}`);
  const at = (t: string) => history.indexOf(`${t} ${id}`);
  expect(at("cherry")).toBeGreaterThanOrEqual(0);
  expect(at("cherry")).toBeLessThan(at("banana"));
  expect(at("banana")).toBeLessThan(at("apple"));
  await tap("3");
  await keyUp("ctrl");
  expect(await fresh.waitForText(`apple ${id}`)).toBe(`apple ${id}`);
}, 300_000);

test("with the setting off, only pinned items come back", async () => {
  const id = readableId();
  const app = await setUp();
  await app.copy(`lost ${id}`);
  await app.copy(`current ${id}`);
  await desk!.quitApp();
  await desk!.startApp();
  await app.prepare();
  await openPicker();
  expect(readPicker("history", `current ${id}`)).toContain(`current ${id}`);
  expect(readPicker("history")).not.toContain(`lost ${id}`);
  await tap("Escape");
  await keyUp("ctrl");
  expect(existsSync(join(desk!.home, "data", "History"))).toBe(false);
}, 300_000);

test("turning the setting off deletes the saved history", async () => {
  const id = readableId();
  const app = await setUp();
  await desk!.toggleRememberHistory();
  await app.copy(`saved ${id}`);
  await sleep(1000);
  const saved = join(desk!.home, "data", "History");
  expect(existsSync(join(saved, "order.json"))).toBe(true);
  await desk!.toggleRememberHistory();
  await sleep(1000);
  expect(existsSync(saved)).toBe(false);
  await app.copy(`after ${id}`);
  await desk!.quitApp();
  await desk!.startApp();
  await app.prepare();
  await openPicker();
  expect(readPicker("history")).not.toContain(`saved ${id}`);
  await tap("Escape");
  await keyUp("ctrl");
}, 300_000);
