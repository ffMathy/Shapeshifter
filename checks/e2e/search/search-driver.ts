// What the search checks need beyond checks/e2e/desktop.ts, still only what a
// user sees and does: which window has the keyboard focus, typing into it, the
// words on the picker (read from a screenshot with OCR), and copying from other
// apps (xclip inside a small xterm with a title of our choosing, so the copy
// comes from a window the user could name).
//
// Needs, besides desktop.ts: xterm, xclip, ImageMagick (import, convert) and tesseract.
import { mkdirSync, writeFileSync } from "node:fs";
import { tmpdir } from "node:os";
import { join } from "node:path";
import { keepScreen, keyDown, keyUp, sleep, tap } from "../desktop.ts";

function run(...args: string[]): string {
  // One thread for tesseract: on a busy machine its thread pool makes it many times slower.
  const r = Bun.spawnSync(args, { env: { ...process.env, OMP_THREAD_LIMIT: "1" } });
  return r.stdout.toString();
}

/** The title of the window that has the keyboard focus. */
export const focusedWindowName = () => run("xdotool", "getactivewindow", "getwindowname").trim();

/** Waits until the window titled `name` has the keyboard focus. */
export async function waitForFocus(name: string, timeout = 3000): Promise<boolean> {
  const until = Date.now() + timeout;
  while (focusedWindowName() !== name) {
    if (Date.now() > until) return false;
    await sleep(50);
  }
  return true;
}

/** Holds Ctrl and V, taps S and lets go: a search, started before the picker would show on its own. */
export async function startSearch() {
  await keyDown("ctrl");
  await keyDown("v");
  await tap("s");
  await keyUp("v");
  await keyUp("ctrl");
  if (!(await waitForFocus("Shapeshifter"))) throw new Error("the picker did not take the keyboard focus for the search");
  await sleep(200);
}

/** Types text with real key presses into whatever has the focus. */
export async function type(text: string) {
  run("xdotool", "type", "--delay", "40", text);
  await sleep(300);
}

/** Clicks at a point on the screen. */
export async function clickAt(x: number, y: number) {
  run("xdotool", "mousemove", String(x), String(y), "click", "1");
  await sleep(300);
}

const scratch = join(tmpdir(), `ail-search-${process.pid}`);
mkdirSync(scratch, { recursive: true });

/** The words on the picker, read from a screenshot of it. Lower-cased, with runs of white space made single. */
export function pickerWords(): string {
  const ids = run("xdotool", "search", "--onlyvisible", "--name", "^Shapeshifter$").split("\n").filter(Boolean);
  const id = ids.find((w) => Number(/Geometry:\s*\d+x(\d+)/.exec(run("xdotool", "getwindowgeometry", w))?.[1]) >= 200);
  if (!id) return "";
  keepScreen();
  const shot = join(scratch, "picker.png");
  const big = join(scratch, "picker-big.png");
  run("import", "-window", id, shot);
  // Twice: once as it is, once in black and white, which also catches the pale placeholder.
  let words = "";
  for (const extra of [[], ["-threshold", "90%"]]) {
    run("convert", shot, "-resize", "200%", "-colorspace", "Gray", ...extra, big);
    words += " " + run("tesseract", big, "stdout");
  }
  return words.replace(/[“”]/g, '"').replace(/\s+/g, " ").toLowerCase();
}

/** Waits until the picker shows all of `words` (case ignored). */
export async function waitForPickerWords(words: string[], timeout = 8000): Promise<string> {
  const until = Date.now() + timeout;
  let seen = pickerWords();
  while (!words.every((w) => seen.includes(w.toLowerCase())) && Date.now() < until) {
    await sleep(200);
    seen = pickerWords();
  }
  return seen;
}

/**
 * Copies in another app: an xterm titled `title` puts `data` on the clipboard as `type` (with xclip, which keeps
 * serving it after the xterm closes), then the clipboard manager is given time to notice. Plain text is copied
 * with type "text", which offers it the way terminals do (UTF8_STRING, STRING, TEXT).
 */
export async function copyFromWindow(title: string, type: string, data: string | Uint8Array) {
  const file = join(scratch, `copy-${Math.random().toString(36).slice(2)}`);
  writeFileSync(file, data);
  const target = type === "text" ? "" : `-t '${type}'`;
  run("xterm", "-T", title, "-e", "sh", "-c", `sleep 0.5; xclip -selection clipboard ${target} -i '${file}'; sleep 1`);
  await sleep(800);
}

/** A file on disk, as the user would copy it in a file manager; returns its path. */
export function makeFile(name: string, content = "hello"): string {
  // Not under a folder named "files", so the path never contains the kind word by chance.
  const dir = join(scratch, "docs");
  mkdirSync(dir, { recursive: true });
  const path = join(dir, name);
  writeFileSync(path, content);
  return path;
}

/** The kinds of data on the clipboard right now (xclip TARGETS). */
export const clipboardTargets = () => run("xclip", "-o", "-selection", "clipboard", "-t", "TARGETS").split("\n").filter(Boolean);

/** The clipboard's content of one kind. */
export const clipboardAs = (type: string) => run("xclip", "-o", "-selection", "clipboard", "-t", type);

/** A small square of a random colour, as PNG: a picture never copied before. */
export function squarePng(): Uint8Array {
  const colour = "#" + Math.floor(Math.random() * 0xffffff).toString(16).padStart(6, "0");
  const r = Bun.spawnSync(["convert", "-size", "24x24", `xc:${colour}`, "png:-"]);
  return r.stdout;
}
