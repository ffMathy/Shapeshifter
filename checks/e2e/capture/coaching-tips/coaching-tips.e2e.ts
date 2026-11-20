// ail-derived-from: CoachingTips@ed3a02e30186a25a
// End-to-end check for CoachingTips. A tip is the notification grown to a
// title over a wrapped sentence (taller than the one-line "Copied" one) and
// it stays up for about seven seconds. Tips are at least five minutes apart,
// so a check that expects one first waits until five minutes have passed
// since the last tip it saw; these checks therefore take several minutes.
import { afterAll, afterEach, beforeAll, expect } from "bun:test";
import { keyDown, keyUp, releaseAll, tap, TargetApp, test, unique, waitForPicker } from "../../desktop.ts";
import {
  closeSettings, isTip, pointerAway, sawTip, setSwitch, startOtherApp, tipShownLately,
  waitForNoNotification, waitUntilATipCanShow, watchNotifications,
} from "../capture-driver.ts";

const MINUTES = 60_000;
let app: TargetApp;
beforeAll(async () => { app = await TargetApp.start(); pointerAway(); }, 90_000);
afterAll(async () => { await releaseAll(); await closeSettings(); await app?.stop(); });
afterEach(async () => { await releaseAll(); await waitForPicker(false); await waitForNoNotification(10_000); pointerAway(); });

const b64 = (s: string) => Buffer.from(s, "utf8").toString("base64");

/** Copies in the target app (which has the focus) and watches the notification from that moment. */
async function copyAndWatch(text: string, ms = 2500) {
  await app.prepare();
  expect(await app.send(`copy-text ${b64(text)}`)).toBe("OK");
  return watchNotifications(ms);
}

const tips = (seen: Awaited<ReturnType<typeof watchNotifications>>) => seen.filter((s) => isTip(s.box));

/** Copies "a", then "b", then "a" again: a habit the copied-again tip is about. */
async function copyAgain(id: string, ms = 2500) {
  await app.copy(`again a ${id}`);
  await app.copy(`again b ${id}`);
  await waitForNoNotification();
  return copyAndWatch(`again a ${id}`, ms);
}

test("copying the same thing twice in a row shows no tip", async () => {
  await waitUntilATipCanShow();
  const text = `double ${unique()}`;
  await app.copy(text);
  await waitForNoNotification();
  expect(await copyAndWatch(text)).toEqual([]);
}, 7 * MINUTES);

test("with Tips off, copying something again shows nothing", async () => {
  await waitUntilATipCanShow();
  await setSwitch("tips", false);
  try {
    pointerAway();
    expect(await copyAgain(unique())).toEqual([]);
  } finally {
    await setSwitch("tips", true);
  }
}, 7 * MINUTES);

test("copying something from the history again shows a tip", async () => {
  await waitUntilATipCanShow();
  const seen = await copyAgain(unique(), 9000);
  const shown = tips(seen);
  expect(shown.length).toBeGreaterThan(0);
  sawTip();
  // It stays up for about seven seconds, in the notification's place at the bottom centre.
  expect(shown[shown.length - 1].at - shown[0].at).toBeGreaterThan(6000);
  expect(seen.length).toBe(shown.length);
}, 7 * MINUTES);

test("a second tip within five minutes is not shown", async () => {
  if (!tipShownLately()) {
    await waitUntilATipCanShow();
    expect(tips(await copyAgain(unique(), 3000)).length).toBeGreaterThan(0);
    sawTip();
    await waitForNoNotification(10_000);
  }
  // Another copy of something already in the history: neither a tip nor the "Copied" notification.
  expect(await copyAgain(unique())).toEqual([]);
}, 7 * MINUTES);

test("copying back and forth between two apps shows a tip", async () => {
  await waitUntilATipCanShow();
  const other = await startOtherApp();
  try {
    const id = unique();
    await app.prepare();
    await app.copy(`from target ${id}`);
    await waitForNoNotification();
    // Paste it into the other app with a quick Ctrl+V.
    await other.focus();
    await keyDown("ctrl");
    await tap("v");
    await keyUp("ctrl", 400);
    // Back in the first app, copy the next thing, well within 30 seconds.
    const seen = await copyAndWatch(`next from target ${id}`, 3000);
    expect(tips(seen).length).toBeGreaterThan(0);
    sawTip();
  } finally {
    other.stop();
  }
}, 7 * MINUTES);
