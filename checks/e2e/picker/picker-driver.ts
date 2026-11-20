// Helpers for the picker's end-to-end checks: where its window is, what it
// shows (read back with OCR, as a person reads the screen) and which window
// has the keyboard. It knows nothing of Shapeshifter's code. Some checks need a
// desktop of their own (a short screen, another language): they start the app
// the way ail.config.json says, on a spare display, with an environment
// variable a user could set, and stop it again.
//
// Needs, besides what desktop.ts needs: ImageMagick (import, convert), xdpyinfo,
// Xvfb, and tesseract (apt-get install tesseract-ocr) for reading the screen.
// Spare displays come from AIL_PICKER_SPARE_DISPLAYS (default ":152,:153,:154").
import { existsSync, mkdtempSync, readFileSync } from "node:fs";
import { tmpdir } from "node:os";
import { join, resolve } from "node:path";
import { keepScreen, keepScreenIfNone, sleep } from "../desktop.ts";

const root = resolve(import.meta.dir, "../../..");
const shots = mkdtempSync(join(tmpdir(), "ail-picker-"));

export const mainDisplay = () => process.env.DISPLAY ?? ":0";

function run(cmd: string[], display = mainDisplay()): string {
  const r = Bun.spawnSync(cmd, { env: { ...process.env, DISPLAY: display } });
  return r.stdout.toString();
}

const xdotool = (display: string, ...args: string[]) => run(["xdotool", ...args], display);

export const keyDown = async (key: string, display = mainDisplay(), after = 60) => { xdotool(display, "keydown", key); await sleep(after); };
export const keyUp = async (key: string, display = mainDisplay(), after = 60) => { xdotool(display, "keyup", key); await sleep(after); };
export const tap = async (key: string, display = mainDisplay(), after = 60) => { await keyDown(key, display, 30); await keyUp(key, display, after); };
export const releaseAll = async (display = mainDisplay()) => { for (const k of ["v", "space", "shift", "ctrl"]) xdotool(display, "keyup", k); await sleep(100); };
export const movePointer = (x: number, y: number, display = mainDisplay()) => xdotool(display, "mousemove", String(x), String(y));

export type Box = { x: number; y: number; width: number; height: number };

/** The size of the screen, in pixels. */
export function screenSize(display = mainDisplay()): { width: number; height: number } {
  const m = /dimensions:\s*(\d+)x(\d+)/.exec(run(["xdpyinfo"], display));
  if (!m) throw new Error(`no screen on ${display}`);
  return { width: Number(m[1]), height: Number(m[2]) };
}

/**
 * The screen's working area: the screen less the panels a desktop keeps there (the window manager's _NET_WORKAREA),
 * which is what the picker places itself in. The whole screen when the window manager reports none.
 */
export function workArea(display = mainDisplay()): Box {
  const m = /=\s*(\d+),\s*(\d+),\s*(\d+),\s*(\d+)/.exec(run(["xprop", "-root", "_NET_WORKAREA"], display));
  if (m) return { x: Number(m[1]), y: Number(m[2]), width: Number(m[3]), height: Number(m[4]) };
  return { x: 0, y: 0, ...screenSize(display) };
}

/** The picker on screen: the visible window titled "Shapeshifter" taller than the copy notification; null when none. */
export function picker(display = mainDisplay()): Box | null {
  const ids = xdotool(display, "search", "--onlyvisible", "--name", "^Shapeshifter$").split("\n").filter(Boolean);
  for (const id of ids) {
    const g = xdotool(display, "getwindowgeometry", id);
    const p = /Position:\s*(-?\d+),(-?\d+)/.exec(g);
    const s = /Geometry:\s*(\d+)x(\d+)/.exec(g);
    if (p && s && Number(s[2]) >= 200) return { x: Number(p[1]), y: Number(p[2]), width: Number(s[1]), height: Number(s[2]) };
  }
  return null;
}

export async function waitForPicker(visible: boolean, display = mainDisplay(), timeout = 3000): Promise<boolean> {
  const until = Date.now() + timeout;
  while ((picker(display) !== null) !== visible) {
    if (Date.now() > until) return !visible;
    await sleep(50);
  }
  return visible;
}

/** Waits until the picker's size and place stop changing (it sizes itself to its content). */
export async function settledPicker(display = mainDisplay()): Promise<Box> {
  let last = "";
  for (let i = 0; i < 40; i++) {
    const box = picker(display);
    const now = JSON.stringify(box);
    if (box && now === last) return box;
    last = now;
    await sleep(150);
  }
  throw new Error("the picker did not settle on screen");
}

/** Ctrl held, V held until the picker shows, then V let go: the picker stays up while Ctrl is held. */
export async function holdThePickerOpen(display = mainDisplay()): Promise<Box> {
  await keyDown("ctrl", display);
  await keyDown("v", display, 0);
  const shown = await waitForPicker(true, display);
  await keyUp("v", display);
  if (!shown) throw new Error("the picker did not appear while Ctrl+V was held");
  const box = await settledPicker(display);
  keepScreen(display);
  return box;
}

/** Escape, then Ctrl let go: the picker closes without pasting. */
export async function cancelThePicker(display = mainDisplay()) {
  await tap("Escape", display);
  await keyUp("ctrl", display);
  await waitForPicker(false, display);
}

/** The name of the window that has the keyboard focus. */
export const focusedWindowName = (display = mainDisplay()) => xdotool(display, "getactivewindow", "getwindowname").trim();

/** Puts text (or, with a type such as image/png, a file's bytes) on the clipboard, as another application would. */
export async function copyWithXclip(data: string, type?: string, display = mainDisplay()) {
  const args = ["xclip", "-selection", "clipboard", ...(type ? ["-t", type] : []), "-i"];
  if (type?.startsWith("image/")) args.push(data);
  const proc = Bun.spawn(args, { stdin: type?.startsWith("image/") ? "ignore" : "pipe", env: { ...process.env, DISPLAY: display }, stdout: "ignore", stderr: "ignore" });
  if (!type?.startsWith("image/")) { (proc.stdin as any).write(data); (proc.stdin as any).end(); }
  await sleep(1000);
}

/** A PNG of the given size in a colour of its own, to copy as an image. */
export function makeImage(width: number, height: number): string {
  const file = join(shots, `copied-${Math.random().toString(36).slice(2, 8)}.png`);
  const colour = "#" + Math.floor(Math.random() * 0xffffff).toString(16).padStart(6, "0");
  Bun.spawnSync(["convert", "-size", `${width}x${height}`, `xc:${colour}`, file]);
  return file;
}

/** A screenshot of the picker, enlarged twice so the small type reads well. */
export function shootPicker(box: Box, display = mainDisplay()): string {
  const file = join(shots, `picker-${Date.now()}-${Math.random().toString(36).slice(2, 6)}.png`);
  run(["import", "-window", "root", "-crop", `${box.width}x${box.height}+${box.x}+${box.y}`, "+repage", "-resize", "200%", file], display);
  return file;
}

/** What a screenshot says, read with OCR (English letters). */
export function readText(png: string): string {
  return Bun.spawnSync(["tesseract", png, "-", "--psm", "3"]).stdout.toString();
}

export type Word = { text: string; left: number; top: number };

/** Every word OCR finds, with where it is (in the screenshot's pixels). */
export function readWords(png: string): Word[] {
  const tsv = Bun.spawnSync(["tesseract", png, "-", "--psm", "11", "tsv"]).stdout.toString();
  return tsv.split("\n").slice(1).map((line) => line.split("\t")).filter((c) => c.length >= 12 && c[11].trim())
    .map((c) => ({ text: c[11].trim(), left: Number(c[6]), top: Number(c[7]) }));
}

/** Collapses OCR's spacing, so a check can look for a phrase. */
export const squash = (text: string) => text.replace(/\s+/g, " ").trim();

/**
 * Shapeshifter on a desktop of its own: a fresh Xvfb screen of the given size on a spare display, the app started as
 * ail.config.json says (run.app, else run.legacy) with a fresh state folder and the extra environment variables.
 */
export class OwnDesktop {
  private constructor(readonly display: string, private xvfb: Bun.Subprocess, private app: Bun.Subprocess) {}

  static async start(options: { width: number; height: number; env?: Record<string, string>; name: string }): Promise<OwnDesktop> {
    const display = spareDisplay();
    const xvfb = Bun.spawn(["Xvfb", display, "-screen", "0", `${options.width}x${options.height}x24`, "-nolisten", "tcp"], { stdout: "ignore", stderr: "ignore" });
    for (let i = 0; i < 50 && !existsSync(`/tmp/.X11-unix/X${display.slice(1)}`); i++) await sleep(100);
    const config = JSON.parse(readFileSync(join(root, "ail.config.json"), "utf8"));
    const how = config.run?.app ?? config.run?.legacy;
    const app = Bun.spawn(["bash", "-c", how.start], {
      cwd: resolve(root, how.cwd ?? "."),
      env: { ...process.env, ...(how.env ?? {}), DISPLAY: display, AIL_DISPLAY: display, AIL_STATE: `.ail/picker-${options.name}`, AIL_SKIP_BUILD: "1", ...(options.env ?? {}) },
      stdout: "ignore", stderr: "ignore",
    });
    const desktop = new OwnDesktop(display, xvfb, app);
    await desktop.waitUntilReady();
    return desktop;
  }

  /** Ready once holding Ctrl+V shows the picker, as a person would find out. */
  private async waitUntilReady(timeout = 120_000) {
    const until = Date.now() + timeout;
    while (Date.now() < until) {
      await sleep(1500);
      await keyDown("ctrl", this.display);
      await keyDown("v", this.display, 0);
      const shown = await waitForPicker(true, this.display, 2000);
      await keyUp("v", this.display);
      if (shown) { await cancelThePicker(this.display); return; }
      await keyUp("ctrl", this.display);
    }
    throw new Error(`Shapeshifter did not start on ${this.display}`);
  }

  async stop() {
    keepScreenIfNone(this.display);
    this.app.kill("SIGTERM");
    await Promise.race([this.app.exited, sleep(5000)]);
    this.xvfb.kill();
    await this.xvfb.exited;
  }
}

function spareDisplay(): string {
  const spares = (process.env.AIL_PICKER_SPARE_DISPLAYS ?? ":152,:153,:154").split(",").map((d) => d.trim());
  const free = spares.find((d) => !existsSync(`/tmp/.X11-unix/X${d.slice(1)}`) && !existsSync(`/tmp/.X${d.slice(1)}-lock`));
  if (!free) throw new Error(`no free spare display among ${spares.join(", ")}`);
  return free;
}
