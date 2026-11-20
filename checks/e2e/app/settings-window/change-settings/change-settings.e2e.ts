// ail-derived-from: ChangeSettings@0f6b1c8d37e3d5a5
// End-to-end check for ChangeSettings: its scenarios, played on an X11 desktop with a system tray against the running
// app. It changes settings only through the settings window, as a user would, and puts each back afterwards, so the
// other checks find the defaults. It sees only windows on screen, the clipboard, the text that lands in another app
// and what would have opened in the browser (see app-driver.ts).
import { afterAll, afterEach, beforeAll, expect } from "bun:test";
import { holdVUntilThePickerAppears, keyDown, keyUp, pickerVisible, releaseAll, sleep, tap, TargetApp, test, unique, waitForPicker } from "../../../desktop.ts";
import {
  clickInSettings, clipboardText, closeSettings, nextOpened, notificationAppears, openSettings, opened, pressSettingsButton, typeNumber,
} from "../../app-driver.ts";

let app: TargetApp;
beforeAll(async () => { app = await TargetApp.start(); }, 90_000);
afterAll(async () => { await closeSettings(); await releaseAll(); await app?.stop(); });
afterEach(async () => { await closeSettings(); await releaseAll(); await waitForPicker(false); });

/** Holds Ctrl, then V until the picker appears; lets go of V, then of Ctrl, and returns what was pasted. */
async function pasteSecondItem(expected: string): Promise<string> {
  await app.prepare();
  await keyDown("ctrl");
  await holdVUntilThePickerAppears();
  await tap("v");
  await keyUp("ctrl");
  await waitForPicker(false);
  return app.waitForText(expected);
}

test("a longer delay keeps the history hidden for longer", async () => {
  await openSettings();
  await typeNumber("delay", 1500);
  await closeSettings();
  try {
    await app.copy(`slow ${unique()}`);
    await app.prepare();
    await keyDown("ctrl");
    await keyDown("v", 0);
    let early = false;
    for (const until = Date.now() + 1000; Date.now() < until; await sleep(50)) early ||= pickerVisible();
    const late = await waitForPicker(true, 1500);
    await keyUp("v");
    await tap("Escape");
    await keyUp("ctrl");
    expect(early).toBe(false);
    expect(late).toBe(true);
  } finally {
    await releaseAll();
    await waitForPicker(false);
    await openSettings();
    await typeNumber("delay", 300);
  }
}, 30_000);

test("turning off copy notifications hides the Copied notification", async () => {
  await app.copy(`noisy ${unique()}`);
  await sleep(2500);
  await openSettings();
  await clickInSettings("copyNotifications");
  await closeSettings();
  try {
    await app.send(`copy-text ${Buffer.from(`quiet ${unique()}`).toString("base64")}`);
    expect(await notificationAppears(2000)).toBe(false);
  } finally {
    await openSettings();
    await clickInSettings("copyNotifications");
    await closeSettings();
  }
  await app.send(`copy-text ${Buffer.from(`loud ${unique()}`).toString("base64")}`);
  expect(await notificationAppears(2000)).toBe(true);
}, 30_000);

test("Items to remember limits how much is kept", async () => {
  await openSettings();
  await typeNumber("itemsToRemember", 1);
  await closeSettings();
  const id = unique();
  try {
    await app.copy(`dropped ${id}`);
    await app.copy(`kept ${id}`);
    const pasted = await pasteSecondItem(`kept ${id}`);
    expect(pasted).not.toBe(`dropped ${id}`);
    expect(pasted).toBe(`kept ${id}`);
  } finally {
    await releaseAll();
    await openSettings();
    await typeNumber("itemsToRemember", 50);
  }
}, 30_000);

test("Clear history forgets older copies but keeps the clipboard", async () => {
  const id = unique();
  await app.copy(`before ${id}`);
  await app.copy(`current ${id}`);
  await openSettings();
  await pressSettingsButton("Clear history");
  await closeSettings();
  const pasted = await pasteSecondItem(`current ${id}`);
  expect(pasted).not.toBe(`before ${id}`);
  expect(pasted).toBe(`current ${id}`);
}, 30_000);

test("Copy version puts the version and the system on the clipboard", async () => {
  await openSettings();
  await clickInSettings("copyVersion");
  expect(clipboardText()).toMatch(/^Shapeshifter \d+\.\d+\.\d+\S* · .+ · (X64|Arm64|X86|Arm)( · (X11|Wayland))?$/);
}, 20_000);

test("Report a problem opens a new GitHub issue", async () => {
  await openSettings();
  const before = opened().length;
  await clickInSettings("reportProblem");
  expect(await nextOpened(before)).toStartWith("https://github.com/ffMathy/Shapeshifter/issues/new?labels=bug&body=");
}, 20_000);

test("Project page opens the project on GitHub", async () => {
  await openSettings();
  const before = opened().length;
  await clickInSettings("projectPage");
  expect(await nextOpened(before)).toBe("https://github.com/ffMathy/Shapeshifter");
}, 20_000);

test("Check for updates opens the releases page when this copy cannot update itself", async () => {
  await openSettings();
  const before = opened().length;
  await pressSettingsButton("Check for updates");
  expect(await nextOpened(before)).toBe("https://github.com/ffMathy/Shapeshifter/releases");
}, 20_000);

test("Open log folder opens the folder of the logs", async () => {
  await openSettings();
  const before = opened().length;
  await pressSettingsButton("Open log folder");
  expect(await nextOpened(before)).toMatch(/^\/.*\/logs$/);
}, 20_000);
