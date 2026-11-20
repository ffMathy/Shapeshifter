// ail-derived-from: Capture@60164061625cee49
// End-to-end check for Capture: its scenarios, played on an X11 desktop
// against the running app. Copies are made by other apps (the target app and
// xclip); what the history holds is seen by picking from it with the paste
// gesture and reading what lands in the text box.
import { afterAll, afterEach, beforeAll, expect } from "bun:test";
import { releaseAll, sleep, TargetApp, test, unique, waitForPicker } from "../desktop.ts";
import { notification, pasteFromHistory, pointerAway, waitForNoNotification, watchNotifications, xcopy } from "./capture-driver.ts";

let app: TargetApp;
beforeAll(async () => { app = await TargetApp.start(); pointerAway(); }, 90_000);
afterAll(async () => { await releaseAll(); await app?.stop(); });
afterEach(async () => { await releaseAll(); await waitForPicker(false); await waitForNoNotification(); });

test("each copy joins the history, newest first", async () => {
  const id = unique();
  await app.copy(`older ${id}`);
  await app.copy(`newer ${id}`);
  expect(await pasteFromHistory(app, ["v"])).toBe(`older ${id}`);
}, 30_000);

test("copying something again moves it to the top instead of adding it twice", async () => {
  const id = unique();
  await app.copy(`before ${id}`);
  await app.copy(`again ${id}`);
  await app.copy(`between ${id}`);
  await app.copy(`again ${id}`);
  await watchNotifications(1500); // a tip may show here; the tips checks need to know
  await waitForNoNotification();
  expect(await pasteFromHistory(app, [])).toBe(`again ${id}`);
  expect(await pasteFromHistory(app, ["v"])).toBe(`between ${id}`);
  // Added twice, the second item down would be the first copy of "again".
  expect(await pasteFromHistory(app, ["v", "v"])).toBe(`before ${id}`);
}, 40_000);

test("a password manager's secret is not recorded", async () => {
  const id = unique();
  await app.copy(`plain ${id}`);
  await waitForNoNotification();
  await app.send(`copy-secret ${Buffer.from(`secret ${id}`).toString("base64")}`);
  const seen = await watchNotifications(1500);
  expect(seen).toEqual([]);
  // The newest item in the history is still the plain copy.
  expect(await pasteFromHistory(app, ["v", "Up"])).toBe(`plain ${id}`);
}, 30_000);

test("an app that writes the clipboard several times at once adds one item", async () => {
  const id = unique();
  await app.copy(`steady ${id}`);
  await xcopy(`draft ${id}`, "UTF8_STRING", 0);
  await xcopy(`final ${id}`, "UTF8_STRING", 900);
  expect(await pasteFromHistory(app, [])).toBe(`final ${id}`);
  expect(await pasteFromHistory(app, ["v"])).toBe(`steady ${id}`);
}, 30_000);

test("pasting from the history adds nothing to it", async () => {
  const id = unique();
  await app.copy(`picked ${id}`);
  await app.copy(`last ${id}`);
  await waitForNoNotification();
  await app.prepare();
  const paste = pasteFromHistory(app, ["v"]);
  const seen = watchNotifications(4000);
  expect(await paste).toBe(`picked ${id}`);
  expect(await seen).toEqual([]);
  expect(notification()).toBeNull();
  // The pasted item moved to the top; nothing new came in above it.
  expect(await pasteFromHistory(app, ["v"])).toBe(`last ${id}`);
  await sleep(100);
}, 30_000);
