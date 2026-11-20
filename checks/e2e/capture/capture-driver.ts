// Extra helpers for the end-to-end checks of capturing copies, the "Copied"
// notification and the tips. Like desktop.ts, it knows nothing of
// Shapeshifter's code: it copies with xclip as any X11 app would, reads the
// clipboard back with xclip, looks for small windows titled "Shapeshifter"
// (the notification) and clicks the settings window like a person would.
//
// Needs: DISPLAY, xdotool, xclip, xprop, xwininfo, ImageMagick (import, compare).
// The settings window opens from the tray icon, so changing a setting needs a
// StatusNotifierItem tray on the display: checks/e2e/app/tray-host.sh (an
// xfce4-panel), or status-notifier-watcher with gtk-sni-tray-standalone, on a
// D-Bus session that Shapeshifter was started with. Checks that change a
// setting fail with a clear message without one.
import { mkdtempSync, readFileSync, writeFileSync } from "node:fs";
import { tmpdir } from "node:os";
import { join } from "node:path";
import { TargetApp, keepScreenSoon, keyDown, keyUp, sleep, tap, waitForPicker } from "../desktop.ts";

export const scratch = mkdtempSync(join(tmpdir(), "ail-capture-"));

function run(cmd: string[], input?: Uint8Array): { out: Buffer; ok: boolean } {
  const r = Bun.spawnSync(cmd, { env: process.env, stdin: input ?? "ignore", stdout: "pipe", stderr: "pipe" });
  return { out: Buffer.from(r.stdout), ok: r.exitCode === 0 };
}

const xdotool = (...args: string[]) => run(["xdotool", ...args]).out.toString();

// ---------------------------------------------------------------- the clipboard, as any other app

/**
 * Puts `data` on the clipboard under one type, as an app would (xclip keeps
 * owning the clipboard in the background until something else copies).
 */
export async function xcopy(data: string | Uint8Array, type = "UTF8_STRING", settle = 800) {
  const bytes = typeof data === "string" ? new TextEncoder().encode(data) : data;
  const file = join(scratch, `in-${Math.random().toString(36).slice(2)}`);
  writeFileSync(file, bytes);
  const p = Bun.spawn(["xclip", "-selection", "clipboard", "-t", type, "-i", file], { env: process.env, stdout: "ignore", stderr: "ignore" });
  await p.exited; // xclip forks a child that owns the selection; the parent returns at once.
  await sleep(settle);
}

/** The types the clipboard offers right now. */
export function clipboardTypes(): string[] {
  return run(["xclip", "-selection", "clipboard", "-o", "-t", "TARGETS"]).out.toString().split("\n").filter(Boolean);
}

/** The clipboard's bytes for one type. */
export function clipboardBytes(type: string): Buffer {
  return run(["xclip", "-selection", "clipboard", "-o", "-t", type]).out;
}

/** Whether two PNG files show exactly the same pixels. */
export function samePixels(a: string, b: string): boolean {
  const r = Bun.spawnSync(["compare", "-metric", "AE", a, b, "null:"], { env: process.env, stderr: "pipe" });
  return r.stderr.toString().trim() === "0";
}

// ---------------------------------------------------------------- the history, through the gesture

/**
 * With the target app focused: holds Ctrl+V until the picker appears, taps
 * each key in `keys` (e.g. ["v", "v"], ["Up"], ["3"]) and lets go of Ctrl.
 * Returns false, having let go of everything, if the picker did not appear.
 */
async function tryPick(keys: string[]): Promise<boolean> {
  await keyDown("ctrl");
  try {
    await keyDown("v", 0);
    const shown = await waitForPicker(true, 4000);
    await keyUp("v");
    if (!shown) return false;
    for (const k of keys) await tap(k, 120);
    return true;
  } finally {
    await keyUp("ctrl");
    await waitForPicker(false);
  }
}

/**
 * Picks from the history like a person who, when the history does not come
 * up (the first time right after Shapeshifter starts), simply tries again.
 * `before` runs before each try (e.g. emptying the text box).
 */
export async function pick(keys: string[], before: () => Promise<void> = async () => {}) {
  for (let attempt = 1; attempt <= 3; attempt++) {
    await before();
    if (await tryPick(keys)) return;
    await sleep(1000);
  }
  throw new Error("the picker did not appear while Ctrl+V was held, three times");
}

/** Pastes what picking `keys` from the history gives, into an empty text box, and returns it. */
export async function pasteFromHistory(app: TargetApp, keys: string[]): Promise<string> {
  await pick(keys, () => app.prepare());
  await sleep(700);
  return app.text();
}

// ---------------------------------------------------------------- the notification

export type Box = { x: number; y: number; width: number; height: number };

function geometry(id: string): Box | null {
  const info = run(["xwininfo", "-id", id]).out.toString();
  const n = (label: string) => Number(new RegExp(`${label}:\\s*(-?\\d+)`).exec(info)?.[1]);
  const box = { x: n("Absolute upper-left X"), y: n("Absolute upper-left Y"), width: n("Width"), height: n("Height") };
  return Number.isFinite(box.height) ? box : null;
}

/**
 * The notification, if one is on screen: a visible window titled
 * "Shapeshifter" smaller than the picker. A plain "Copied" one is a single
 * line; a tip, a title over a wrapped sentence, is taller.
 */
export function notification(): Box | null {
  for (const id of xdotool("search", "--onlyvisible", "--name", "^Shapeshifter$").split("\n").filter(Boolean)) {
    const box = geometry(id);
    if (box && box.height > 0 && box.height < 200) return box;
  }
  return null;
}

export const isTip = (box: Box | null) => box !== null && box.height >= 60;

/** Watches the screen for `ms` and returns every moment a notification was up, with its box. */
export async function watchNotifications(ms: number): Promise<{ at: number; box: Box }[]> {
  const seen: { at: number; box: Box }[] = [];
  const start = Date.now();
  while (Date.now() - start < ms) {
    const box = notification();
    if (box) {
      if (!seen.length) keepScreenSoon();
      seen.push({ at: Date.now() - start, box });
    }
    await sleep(40);
  }
  if (seen.some((s) => isTip(s.box))) lastTipSeen = Date.now();
  return seen;
}

/** Waits until no notification is on screen, so the next check starts from a clean screen. */
export async function waitForNoNotification(timeout = 9000) {
  const until = Date.now() + timeout;
  while (notification() && Date.now() < until) await sleep(100);
}

/** The usable part of the screen (without panels), from the window manager. */
export function workArea(): Box {
  const v = /=\s*(\d+),\s*(\d+),\s*(\d+),\s*(\d+)/.exec(run(["xprop", "-root", "_NET_WORKAREA"]).out.toString());
  if (!v) throw new Error("the window manager does not report a work area");
  return { x: Number(v[1]), y: Number(v[2]), width: Number(v[3]), height: Number(v[4]) };
}

// ---------------------------------------------------------------- tips are at least five minutes apart

const TIP_GAP_MS = 5 * 60_000 + 5_000;
// Checks of other areas that ran just before against the same app may have shown a tip without anyone watching, so
// until a tip is seen here, assume one was shown the moment this file started.
let lastTipSeen = Date.now();

/** Remembers that a tip was just seen (a check that saw one calls this). */
export const sawTip = () => { lastTipSeen = Date.now(); };

/**
 * A tip only shows five minutes after the last one, so a check that expects a
 * tip first waits until five minutes have passed since the last tip it saw.
 */
export async function waitUntilATipCanShow() {
  const wait = lastTipSeen + TIP_GAP_MS - Date.now();
  if (wait > 0) await sleep(wait);
}

// ---------------------------------------------------------------- the settings window

/** Opens the settings window from the tray icon and returns its client area. */
export async function openSettings(): Promise<Box> {
  let win = settingsWindow();
  if (!win) {
    const icon = trayIcon();
    if (!icon) throw new Error("no tray on this display: the settings window opens from Shapeshifter's tray icon (start checks/e2e/app/tray-host.sh, then Shapeshifter with its D-Bus address)");
    xdotool("mousemove", String(icon.x), String(icon.y), "click", "1");
    for (let i = 0; i < 50 && !(win = settingsWindow()); i++) await sleep(100);
    if (!win) throw new Error("clicking the tray icon did not open the settings window");
  }
  await sleep(500);
  return win;
}

/**
 * Where Shapeshifter's tray icon is: the right end of the xfce4-panel that
 * checks/e2e/app/tray-host.sh puts at the bottom of the screen, or the only
 * icon of a gtk-sni-tray-standalone bar.
 */
function trayIcon(): { x: number; y: number } | null {
  const find = (name: string) => xdotool("search", "--onlyvisible", "--name", name).split("\n").filter(Boolean).map(geometry);
  const panel = find("^xfce4-panel$").find((g) => g && g.width > 200);
  if (panel) return { x: panel.x + panel.width - 17, y: panel.y + Math.floor(panel.height / 2) };
  const bar = find("^gtk-sni-tray-standalone$").find(Boolean);
  return bar ? { x: bar.x + 15, y: bar.y + Math.floor(bar.height / 2) } : null;
}

function settingsWindow(): Box | null {
  const id = xdotool("search", "--onlyvisible", "--name", "^Shapeshifter Settings$").split("\n").filter(Boolean)[0];
  return id ? geometry(id) : null;
}

export async function closeSettings() {
  for (const id of xdotool("search", "--name", "^Shapeshifter Settings$").split("\n").filter(Boolean)) xdotool("windowclose", id);
  await sleep(300);
}

/**
 * Rows of the settings window, as offsets from its top-left corner (the
 * Linux look at 1×; the window is 540 wide). The switches sit at the right
 * end of their row; "Items to remember" has its number left of − and +.
 */
const ROW = { items: 341, notifications: 510, tips: 569 };
const SWITCH_X = 473;
const NUMBER_X = 407;

async function click(win: Box, dx: number, dy: number) {
  xdotool("mousemove", String(win.x + dx), String(win.y + dy), "click", "1");
  await sleep(300);
}

/** Whether a switch is on: an on switch is filled with the accent colour, an off one is light grey. */
function switchIsOn(win: Box, dy: number): boolean {
  const file = join(scratch, `switch-${Date.now()}.txt`);
  run(["import", "-window", "root", "-crop", `1x1+${win.x + SWITCH_X - 12}+${win.y + dy}`, `txt:${file}`]);
  const m = /#([0-9A-F]{2})([0-9A-F]{2})([0-9A-F]{2})/i.exec(readFileSync(file, "utf8"));
  if (!m) throw new Error("could not read the switch's colour");
  const [r, g, b] = [m[1], m[2], m[3]].map((h) => parseInt(h, 16));
  return b - r > 60; // the accent is blue; off is a neutral grey
}

/** Turns a switch of the settings window on or off, like a person: by clicking it if it is not already so. */
export async function setSwitch(name: "notifications" | "tips", on: boolean) {
  const win = await openSettings();
  if (switchIsOn(win, ROW[name]) !== on) await click(win, SWITCH_X, ROW[name]);
  if (switchIsOn(win, ROW[name]) !== on) throw new Error(`the ${name} switch did not turn ${on ? "on" : "off"}`);
  await closeSettings();
}

/** Types a number into "Items to remember". */
export async function setItemsToRemember(count: number) {
  const win = await openSettings();
  await click(win, NUMBER_X, ROW.items);
  xdotool("key", "ctrl+a");
  xdotool("type", String(count));
  xdotool("key", "Tab");
  await sleep(400);
  await closeSettings();
}

// ---------------------------------------------------------------- small helpers shared by the checks

/** Moves the pointer to a corner, so it never rests on the notification (pointing at it dismisses it). */
export function pointerAway() {
  xdotool("mousemove", "1270", "10");
}

/** Whether a tip was seen in the last five minutes, so a new one cannot show yet. */
export const tipShownLately = () => lastTipSeen > 0 && Date.now() - lastTipSeen < 5 * 60_000;

/** Starts a second, different app (xlogo) and returns a way to focus it and to close it. */
export async function startOtherApp(): Promise<{ focus: () => Promise<void>; stop: () => void }> {
  // xlogo does not say which process it is, so look for the XLogo window that was not there before.
  const windows = () => xdotool("search", "--onlyvisible", "--class", "XLogo").split("\n").filter(Boolean);
  const before = new Set(windows());
  const proc = Bun.spawn(["xlogo", "-geometry", "200x200+700+100"], { env: process.env, stdout: "ignore", stderr: "ignore" });
  let id = "";
  for (let i = 0; i < 50 && !id; i++) {
    await sleep(100);
    id = windows().find((w) => !before.has(w)) ?? "";
  }
  if (!id) { proc.kill(); throw new Error("xlogo did not open a window"); }
  return {
    focus: async () => { xdotool("windowactivate", "--sync", id); await sleep(300); },
    stop: () => proc.kill(),
  };
}
