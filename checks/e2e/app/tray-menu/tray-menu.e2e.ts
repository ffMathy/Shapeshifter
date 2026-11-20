// ail-derived-from: TrayMenu@8b8636bf5f214da9
// End-to-end check for TrayMenu: its scenarios, played on an X11 desktop with a system tray against the running app.
// It sees only what a user sees: windows on screen, the text that lands in another app, and what would have opened
// in the browser or the file manager (see app-driver.ts for what the desktop needs).
import { afterAll, afterEach, beforeAll, expect } from "bun:test";
import { holdVUntilThePickerAppears, keyDown, keyUp, releaseAll, tap, TargetApp, test, unique, waitForPicker } from "../../desktop.ts";
import { chooseFromTrayMenu, clickTrayIcon, closeSettings, nextOpened, opened, settingsWindows, waitForSettings } from "../app-driver.ts";

let app: TargetApp;
beforeAll(async () => { app = await TargetApp.start(); }, 90_000);
afterAll(async () => { await closeSettings(); await releaseAll(); await app?.stop(); });
afterEach(async () => { await closeSettings(); await releaseAll(); await waitForPicker(false); });

const environment = /^Shapeshifter \d+\.\d+\.\d+\S* · .+ · (X64|Arm64|X86|Arm)( · (X11|Wayland))?$/m;

test("clicking the tray icon opens the settings", async () => {
  await clickTrayIcon();
  expect(await waitForSettings(true)).toBe(true);
}, 20_000);

test("Settings… opens the settings, and only once", async () => {
  await chooseFromTrayMenu("Settings…");
  expect(await waitForSettings(true)).toBe(true);
  await chooseFromTrayMenu("Settings…");
  await waitForSettings(true);
  expect(settingsWindows().length).toBe(1);
}, 20_000);

test("About Shapeshifter opens the settings", async () => {
  await chooseFromTrayMenu("About Shapeshifter");
  expect(await waitForSettings(true)).toBe(true);
}, 20_000);

test("Report a problem… opens a new GitHub issue with the version filled in", async () => {
  const before = opened().length;
  await chooseFromTrayMenu("Report a problem…");
  const url = await nextOpened(before);
  expect(url).toStartWith("https://github.com/ffMathy/Shapeshifter/issues/new?labels=bug&body=");
  const body = decodeURIComponent(url!.slice(url!.indexOf("body=") + 5));
  expect(body).toStartWith("**What happened?**");
  expect(body).toContain("**What did you expect to happen?**");
  expect(body).toContain("**Steps to reproduce** (which application you copied from and pasted into)");
  expect(body).toMatch(environment);
  expect(settingsWindows().length).toBe(0);
}, 20_000);

test("Open log folder opens the folder of the logs", async () => {
  const before = opened().length;
  await chooseFromTrayMenu("Open log folder");
  expect(await nextOpened(before)).toMatch(/^\/.*\/logs$/);
}, 20_000);

test("Check for updates… opens the releases page when this copy cannot update itself", async () => {
  const before = opened().length;
  await chooseFromTrayMenu("Check for updates…");
  expect(await nextOpened(before)).toBe("https://github.com/ffMathy/Shapeshifter/releases");
  expect(settingsWindows().length).toBe(0);
}, 20_000);

test("Clear history forgets what was copied before", async () => {
  const id = unique();
  await app.copy(`older ${id}`);
  await app.copy(`latest ${id}`);
  await chooseFromTrayMenu("Clear history");
  await app.prepare();
  await keyDown("ctrl");
  await holdVUntilThePickerAppears();
  await tap("v");
  await keyUp("ctrl");
  await waitForPicker(false);
  const pasted = await app.waitForText(`latest ${id}`);
  expect(pasted).not.toBe(`older ${id}`);
  expect(pasted).toBe(`latest ${id}`);
}, 30_000);
