// ail-derived-from: CopyNotification@a9c7d5d9b4934557
// End-to-end check for CopyNotification: the small "Copied" notification, seen
// as a window titled "Shapeshifter" smaller than the picker. Its text is not
// read here (there is no OCR on the test desktop); its place, size and timing are.
import { afterAll, afterEach, beforeAll, expect } from "bun:test";
import { releaseAll, sleep, TargetApp, test, unique, waitForPicker } from "../../desktop.ts";
import { closeSettings, isTip, notification, pointerAway, setSwitch, waitForNoNotification, watchNotifications, workArea } from "../capture-driver.ts";

let app: TargetApp;
beforeAll(async () => { app = await TargetApp.start(); pointerAway(); }, 90_000);
afterAll(async () => { await releaseAll(); await closeSettings(); await app?.stop(); });
afterEach(async () => { await releaseAll(); await waitForPicker(false); await waitForNoNotification(); pointerAway(); });

/** Copies without the target app's usual wait, so the notification is watched from the start. */
async function copyAndWatch(text: string, ms = 3000) {
  expect(await app.send(`copy-text ${Buffer.from(text).toString("base64")}`)).toBe("OK");
  return watchNotifications(ms);
}

test("a copy shows the Copied notification at the bottom centre for a moment", async () => {
  const seen = await copyAndWatch(`notify ${unique()}`);
  expect(seen.length).toBeGreaterThan(0);
  const first = seen[0], last = seen[seen.length - 1];
  expect(first.at).toBeLessThan(800);
  expect(isTip(first.box)).toBe(false);
  // A single line, centred, just above the bottom of the usable screen.
  const area = workArea();
  const centre = first.box.x + first.box.width / 2;
  expect(Math.abs(centre - (area.x + area.width / 2))).toBeLessThanOrEqual(2);
  expect(area.y + area.height - (first.box.y + first.box.height)).toBe(8);
  // Up for about a second and a half.
  const shown = last.at - first.at;
  expect(shown).toBeGreaterThan(1100);
  expect(shown).toBeLessThan(2100);
  expect(notification()).toBeNull();
}, 30_000);

test("copying the same thing twice in a row shows it only once", async () => {
  const text = `twice ${unique()}`;
  expect((await copyAndWatch(text)).length).toBeGreaterThan(0);
  await waitForNoNotification();
  expect(await copyAndWatch(text)).toEqual([]);
}, 30_000);

test("pointing at the notification makes it go away", async () => {
  expect(await app.send(`copy-text ${Buffer.from(`point ${unique()}`).toString("base64")}`)).toBe("OK");
  let box = notification();
  for (const until = Date.now() + 1500; !box && Date.now() < until; await sleep(30)) box = notification();
  expect(box).not.toBeNull();
  Bun.spawnSync(["xdotool", "mousemove", String(box!.x + 20), String(box!.y + box!.height / 2)], { env: process.env });
  await sleep(300);
  expect(notification()).toBeNull();
}, 30_000);

test("with Copy notifications off, a copy shows nothing", async () => {
  await setSwitch("notifications", false);
  try {
    pointerAway();
    expect(await copyAndWatch(`quiet ${unique()}`)).toEqual([]);
  } finally {
    await setSwitch("notifications", true);
  }
  pointerAway();
  expect((await copyAndWatch(`loud again ${unique()}`)).length).toBeGreaterThan(0);
}, 60_000);
