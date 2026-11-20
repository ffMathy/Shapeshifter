// ail-derived-from: PinItems@dc042e3d435cd0a3
// End-to-end check for PinItems: pinning and unpinning from the actions, and
// pinned items surviving a restart. Pinned items stay at the top for good, so
// every scenario runs on a desktop of its own (PrivateDesktop), where it may
// also quit Shapeshifter from its tray menu and start it again.
import { afterEach, expect } from "bun:test";
import { keyUp, releaseAll, sleep, tap, TargetApp, test, waitForPicker } from "../../desktop.ts";
import { PrivateDesktop, readableId, actionTitlesOnScreen, openPicker, readPicker } from "../actions-driver.ts";

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

/** With the picker up on the item to (un)pin: the pin action is the last one, so Right then Up chooses it. */
async function pinOrUnpinChosen() {
  await tap("Right");
  await tap("Up");
  await sleep(200);
  await keyUp("ctrl");
  await sleep(1200);
}

test("pinning keeps an item at the top", async () => {
  const id = readableId();
  const app = await setUp();
  await app.copy(`keep ${id}`);
  await app.copy(`later ${id}`);
  await app.prepare();
  await openPicker(1);
  await pinOrUnpinChosen();
  expect(await waitForPicker(false)).toBe(false);
  expect(await app.text()).toBe("");
  await app.copy(`even later ${id}`);
  await app.prepare();
  await openPicker();
  const history = readPicker("history", `keep ${id}`, `even later ${id}`);
  expect(history.indexOf(`keep ${id}`)).toBeGreaterThanOrEqual(0);
  expect(history.indexOf(`keep ${id}`)).toBeLessThan(history.indexOf(`even later ${id}`));
  await keyUp("ctrl");
  expect(await app.waitForText(`even later ${id}`)).toBe(`even later ${id}`);
  await app.prepare();
  await openPicker();
  await tap("1");
  await keyUp("ctrl");
  expect(await app.waitForText(`keep ${id}`)).toBe(`keep ${id}`);
}, 180_000);

test("a pinned item offers Unpin, and unpinning lets it move down again", async () => {
  const id = readableId();
  const app = await setUp();
  await app.copy(`sticky ${id}`);
  await app.prepare();
  await openPicker();
  await pinOrUnpinChosen();
  await app.copy(`next ${id}`);
  await openPicker();
  await tap("1");
  await tap("Right");
  await sleep(300);
  expect(actionTitlesOnScreen()).toEqual(["Paste", "Paste as file", "Unpin from clipboard"]);
  await tap("Up");
  await keyUp("ctrl");
  await sleep(1200);
  await app.copy(`newest ${id}`);
  await app.prepare();
  await openPicker();
  const history = readPicker("history", `newest ${id}`, `sticky ${id}`);
  expect(history.indexOf(`newest ${id}`)).toBeGreaterThanOrEqual(0);
  expect(history.indexOf(`newest ${id}`)).toBeLessThan(history.indexOf(`sticky ${id}`));
  await tap("1");
  await keyUp("ctrl");
  expect(await app.waitForText(`newest ${id}`)).toBe(`newest ${id}`);
}, 180_000);

test("pinned items come back after a restart, the rest does not", async () => {
  const id = readableId();
  const app = await setUp();
  await app.copy(`pinned ${id}`);
  await app.prepare();
  await openPicker();
  await pinOrUnpinChosen();
  await app.copy(`gone ${id}`);
  await app.copy(`current ${id}`);
  await desk!.quitApp();
  await desk!.startApp();
  await app.prepare();
  await openPicker();
  expect(readPicker("history", `pinned ${id}`)).toContain(`pinned ${id}`);
  expect(readPicker("history")).not.toContain(`gone ${id}`);
  await tap("1");
  await keyUp("ctrl");
  expect(await app.waitForText(`pinned ${id}`)).toBe(`pinned ${id}`);
}, 240_000);

test("a deleted pinned item does not come back", async () => {
  const id = readableId();
  const app = await setUp();
  await app.copy(`doomed ${id}`);
  await app.prepare();
  await openPicker();
  await pinOrUnpinChosen();
  await app.copy(`other ${id}`);
  await openPicker();
  await tap("1");
  await tap("Delete");
  await tap("Escape");
  await keyUp("ctrl");
  await desk!.quitApp();
  await desk!.startApp();
  await openPicker();
  expect(readPicker("history")).not.toContain(`doomed ${id}`);
  await tap("Escape");
  await keyUp("ctrl");
}, 240_000);
