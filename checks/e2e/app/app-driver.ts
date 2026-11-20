// Drives the parts of Shapeshifter around the paste gesture the way a person would: the tray icon and its menu, the
// settings window, the "Copied" notification, and what Shapeshifter opens in the browser or the file manager.
// It knows nothing of Shapeshifter's code. It needs, on the same DISPLAY as Shapeshifter:
// - a system tray: checks/e2e/app/tray-host.sh starts one (a session D-Bus and a panel at the bottom of the screen);
//   Shapeshifter must be started after it, with its DBUS_SESSION_BUS_ADDRESS;
// - checks/e2e/app/bin first on Shapeshifter's PATH: its xdg-open stands in for the browser and the file manager and
//   writes what would have opened to .ail/browser/<display>.txt, so no real browser ever opens.
// Positions inside the settings window are those of the Linux look ("Boxed Sheet"), which has no "Shortcut" row.
import { existsSync, readFileSync } from "node:fs";
import { resolve } from "node:path";
import { keepScreenSoon, sleep } from "../desktop.ts";

const root = resolve(import.meta.dir, "../../..");

function run(...args: string[]): string {
  const r = Bun.spawnSync(args, { env: process.env });
  return r.stdout.toString();
}
const xdotool = (...args: string[]) => run("xdotool", ...args);

const visibleWindows = (name: string) => xdotool("search", "--onlyvisible", "--name", name).split("\n").filter(Boolean);

/** The absolute position and size of a window's content, without the title bar. */
function geometry(id: string): { x: number; y: number; width: number; height: number } {
  const info = run("xwininfo", "-id", id);
  const num = (label: string) => Number(new RegExp(`${label}:\\s*(-?\\d+)`).exec(info)?.[1]);
  return { x: num("Absolute upper-left X"), y: num("Absolute upper-left Y"), width: num("Width"), height: num("Height") };
}

// ------------------------------------------------------------------ the tray

/** Where Shapeshifter's icon is: the right end of the panel tray-host.sh puts at the bottom of the screen. */
function trayIconPosition(): { x: number; y: number } {
  const panel = visibleWindows("^xfce4-panel$")
    .map(geometry)
    .find((g) => g.width > 200);
  if (!panel) throw new Error("no system tray on this display: start checks/e2e/app/tray-host.sh, then Shapeshifter with its D-Bus address");
  return { x: panel.x + panel.width - 17, y: panel.y + Math.floor(panel.height / 2) };
}

/** Left-clicks Shapeshifter's tray icon. */
export async function clickTrayIcon() {
  const { x, y } = trayIconPosition();
  xdotool("mousemove", String(x), String(y), "click", "1");
  await sleep(300);
}

/** The tray menu's items, in order; the separators are skipped by the keyboard. */
export const trayItems = ["Settings…", "Open log folder", "Clear history", "Check for updates…", "Report a problem…", "About Shapeshifter", "Quit Shapeshifter"] as const;
export type TrayItem = (typeof trayItems)[number];

/** Opens the tray menu with a right click and chooses an item with the arrow keys and Enter. */
export async function chooseFromTrayMenu(item: TrayItem) {
  const { x, y } = trayIconPosition();
  xdotool("mousemove", String(x), String(y), "click", "3");
  await sleep(900);
  for (let i = 0; i <= trayItems.indexOf(item); i++) { xdotool("key", "Down"); await sleep(80); }
  xdotool("key", "Return");
  await sleep(400);
}

// ------------------------------------------------------------------ the settings window

export const settingsTitle = "^Shapeshifter Settings$";
export const settingsWindows = () => visibleWindows(settingsTitle);

export async function waitForSettings(open: boolean, timeout = 4000): Promise<boolean> {
  const until = Date.now() + timeout;
  while ((settingsWindows().length > 0) !== open) {
    if (Date.now() > until) return !open;
    await sleep(100);
  }
  await sleep(open ? 400 : 0);
  return open;
}

/** Opens the settings with the tray icon, or brings the open window forward. */
export async function openSettings() {
  if (settingsWindows().length === 0) await clickTrayIcon();
  if (!(await waitForSettings(true))) throw new Error("the settings window did not open");
  xdotool("windowactivate", "--sync", settingsWindows()[0]!);
  await sleep(200);
}

/** Closes every settings window the way a user does, with Alt+F4. */
export async function closeSettings() {
  for (const id of settingsWindows()) {
    xdotool("windowactivate", "--sync", id);
    xdotool("key", "alt+F4");
  }
  await waitForSettings(false);
}

/** Where things are in the settings window, from the top left of its content (Linux look). */
const spots = {
  copyVersion: [242, 60], reportProblem: [343, 60], projectPage: [441, 60],
  delay: [403, 282], itemsToRemember: [407, 341], rememberHistory: [473, 401],
  copyNotifications: [473, 510], tips: [473, 569], startAtLogin: [473, 628], automaticUpdates: [473, 687],
} as const;
export type SettingsSpot = keyof typeof spots;

export async function clickInSettings(spot: SettingsSpot) {
  const g = geometry(settingsWindows()[0]!);
  const [dx, dy] = spots[spot];
  xdotool("mousemove", String(g.x + dx), String(g.y + dy), "click", "1");
  await sleep(400);
}

/** Types a number into "Delay before the history appears" or "Items to remember" and moves on with Tab. */
export async function typeNumber(field: "delay" | "itemsToRemember", value: number) {
  await clickInSettings(field);
  xdotool("key", "ctrl+a");
  await sleep(100);
  xdotool("type", "--delay", "40", String(value));
  xdotool("key", "Tab");
  await sleep(500);
}

/** The buttons below the settings, reached from the end of the window with Shift+Tab (they can be off a low screen). */
const buttonsFromTheEnd = { "Open log folder": 1, "Check for updates": 2, "Clear history": 3 } as const;

/** Presses a button below the settings, in a settings window that was just opened. */
export async function pressSettingsButton(button: keyof typeof buttonsFromTheEnd) {
  for (let i = 0; i < buttonsFromTheEnd[button]; i++) { xdotool("key", "shift+Tab"); await sleep(120); }
  xdotool("key", "space");
  await sleep(500);
}

// ------------------------------------------------------------------ what the user sees elsewhere

/** Whether the small "Copied" notification is on screen: a window titled "Shapeshifter", lower than the picker. */
export const notificationVisible = () =>
  visibleWindows("^Shapeshifter$").some((id) => geometry(id).height < 200);

/** Watches for the "Copied" notification for a while; true as soon as it shows. */
export async function notificationAppears(within = 2000): Promise<boolean> {
  for (const until = Date.now() + within; Date.now() < until; await sleep(50)) {
    if (notificationVisible()) { keepScreenSoon(); return true; }
  }
  return false;
}

/** The text on the clipboard. */
export const clipboardText = () => run("xclip", "-selection", "clipboard", "-o");

const openedFile = () => resolve(root, ".ail/browser", (process.env.DISPLAY ?? "none").replace(/[^A-Za-z0-9]/g, "") + ".txt");

/** Everything opened in the browser or the file manager so far, oldest first. */
export function opened(): string[] {
  const file = openedFile();
  return existsSync(file) ? readFileSync(file, "utf8").split("\n").filter(Boolean) : [];
}

/** Waits for the next link or folder Shapeshifter opens after `before` were already open. */
export async function nextOpened(before: number, timeout = 4000): Promise<string | undefined> {
  const until = Date.now() + timeout;
  for (;;) {
    const all = opened();
    if (all.length > before) return all[before];
    if (Date.now() > until) return undefined;
    await sleep(100);
  }
}
