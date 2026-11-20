// Drives an X11 desktop the way a person would, for the end-to-end checks of
// the paste gesture. It knows nothing of Shapeshifter's code: it copies text in
// another app, presses real keys (xdotool, through XTEST) and reads what
// arrived in that app's text box, and whether a window titled "Shapeshifter"
// as big as the picker is on screen. So it works against any build of the app.
//
// Needs: DISPLAY, xdotool, the .NET runtime, and AIL_TARGET_APP, the path
// (relative to the project root) of a small app with one text box that takes
// commands on stdin (tests/Shapeshifter.EndToEnd.TargetApp).
import { test as bunTest } from "bun:test";
import { existsSync, writeFileSync } from "node:fs";
import { homedir } from "node:os";
import { join, resolve } from "node:path";

const root = resolve(import.meta.dir, "../..");
export const sleep = (ms: number) => new Promise((r) => setTimeout(r, ms));

/** A short id, so texts from different tests never collide in the history. */
export const unique = () => Math.random().toString(36).slice(2, 8);

/** The other app: one text box. Commands are answered with one line each. */
export class TargetApp {
  private lines: string[] = [];
  private waiters: ((line: string) => void)[] = [];
  private constructor(private proc: Bun.Subprocess<"pipe", "pipe", "pipe">) {}

  static async start(): Promise<TargetApp> {
    const dll = resolve(root, process.env.AIL_TARGET_APP ?? "tests/Shapeshifter.EndToEnd.TargetApp/bin/Release/net11.0/Shapeshifter.EndToEnd.TargetApp.dll");
    if (!existsSync(dll)) throw new Error(`no target app at ${dll}; build tests/Shapeshifter.EndToEnd.TargetApp`);
    const local = join(homedir(), ".dotnet", "dotnet");
    const dotnet = existsSync(local) ? local : "dotnet";
    const proc = Bun.spawn([dotnet, dll], { stdin: "pipe", stdout: "pipe", stderr: "pipe", env: process.env });
    const app = new TargetApp(proc);
    app.read();
    const ready = await app.next(60_000);
    if (ready !== "TARGET_READY") throw new Error(`the target app said "${ready}" instead of TARGET_READY`);
    return app;
  }

  private async read() {
    const decoder = new TextDecoder();
    let buffer = "";
    for await (const chunk of this.proc.stdout) {
      buffer += decoder.decode(chunk);
      let i: number;
      while ((i = buffer.indexOf("\n")) >= 0) {
        const line = buffer.slice(0, i).replace(/\r$/, "");
        buffer = buffer.slice(i + 1);
        const w = this.waiters.shift();
        if (w) w(line); else this.lines.push(line);
      }
    }
  }

  private next(timeout = 10_000): Promise<string> {
    const line = this.lines.shift();
    if (line !== undefined) return Promise.resolve(line);
    return new Promise((ok, fail) => {
      const t = setTimeout(() => fail(new Error("the target app did not answer")), timeout);
      this.waiters.push((l) => { clearTimeout(t); ok(l); });
    });
  }

  async send(command: string): Promise<string> {
    this.proc.stdin.write(command + "\n");
    await this.proc.stdin.flush();
    return this.next();
  }

  private async ok(command: string) {
    const answer = await this.send(command);
    if (answer !== "OK") throw new Error(`"${command}" failed: ${answer}`);
  }

  /** Copies text in this app, then gives the clipboard manager time to notice. */
  async copy(text: string) {
    await this.ok(`copy-text ${b64(text)}`);
    await sleep(800);
  }

  /** Empties the text box and gives it the keyboard focus. */
  async prepare() {
    await this.ok("clear");
    await this.ok("focus");
    await sleep(250);
  }

  async text(): Promise<string> {
    const answer = await this.send("get");
    if (!answer.startsWith("TEXT ")) throw new Error(`the target app answered "${answer}"`);
    return Buffer.from(answer.slice(5), "base64").toString("utf8");
  }

  /** Waits for the text box to show `expected`, then a moment more, so a stray second paste would show. */
  async waitForText(expected: string, timeout = 5000): Promise<string> {
    const until = Date.now() + timeout;
    let text = await this.text();
    while (text !== expected && Date.now() < until) { await sleep(100); text = await this.text(); }
    await sleep(500);
    return this.text();
  }

  async stop() {
    try { await this.send("quit"); } catch { /* already gone */ }
    this.proc.kill();
  }
}

const b64 = (s: string) => Buffer.from(s, "utf8").toString("base64");

function xdotool(...args: string[]): string {
  const r = Bun.spawnSync(["xdotool", ...args], { env: process.env });
  return r.stdout.toString();
}

/** xdotool key names: ctrl, shift, v, Up, Down, Escape, space, Return, 1–9. */
export const keyDown = async (key: string, after = 60) => { xdotool("keydown", key); await sleep(after); };
export const keyUp = async (key: string, after = 60) => { xdotool("keyup", key); await sleep(after); };
export const tap = async (key: string, after = 60) => { await keyDown(key, 30); await keyUp(key, after); };

/** Lets go of every key a check may have left down, so one failure does not spoil the next check. */
export const releaseAll = async () => { for (const k of ["v", "space", "shift", "ctrl"]) xdotool("keyup", k); await sleep(100); };

/** Whether the picker is on screen: a visible window titled "Shapeshifter", taller than the small "Copied" notification. */
export function pickerVisible(): boolean {
  const ids = xdotool("search", "--onlyvisible", "--name", "^Shapeshifter$").split("\n").filter(Boolean);
  return ids.some((id) => {
    const h = /Geometry:\s*\d+x(\d+)/.exec(xdotool("getwindowgeometry", id))?.[1];
    return Number(h) >= 200;
  });
}

export async function waitForPicker(visible: boolean, timeout = 3000): Promise<boolean> {
  const until = Date.now() + timeout;
  while (pickerVisible() !== visible) {
    if (Date.now() > until) return !visible;
    await sleep(50);
  }
  return visible;
}

/** With Ctrl held: holds V until the picker appears, then lets go of V. */
export async function holdVUntilThePickerAppears() {
  await keyDown("v", 0);
  const shown = await waitForPicker(true);
  await keyUp("v");
  if (!shown) throw new Error("the picker did not appear while V was held");
}

// ---------------------------------------------------------------- screenshots as proof

/** The file name `ail adopt` expects for a test's screenshot (the same rule as AIL's own slugOf). */
export const shotName = (test: string) =>
  `${test.toLowerCase().replace(/[^a-z0-9]+/g, "-").replace(/^-|-$/g, "").slice(0, 60) || "test"}.png`;

/** The whole screen of an X display, as PNG bytes; empty when it cannot be taken. */
export function screenOf(display = process.env.DISPLAY ?? ""): Uint8Array {
  const r = Bun.spawnSync(["import", "-window", "root", "png:-"], { env: { ...process.env, DISPLAY: display }, stdout: "pipe", stderr: "ignore" });
  return r.exitCode === 0 ? r.stdout : new Uint8Array();
}

let keptScreen: Uint8Array | null = null;

/**
 * Keeps the screen as it is now as the scenario's proof, in place of the screen it ends on. The checks' helpers call
 * this at the moment they look at something that is gone by the end, such as the open picker or a notification; the
 * last one kept wins.
 */
export function keepScreen(display = process.env.DISPLAY ?? "") {
  const png = screenOf(display);
  if (png.length) keptScreen = png;
}

let pendingScreen: Promise<void> | null = null;

/**
 * Like keepScreen, but without waiting for the screenshot: for helpers that time what is on screen, such as the
 * notification watchers. The test waits for it before saving.
 */
export function keepScreenSoon(display = process.env.DISPLAY ?? "") {
  const p = Bun.spawn(["import", "-window", "root", "png:-"], { env: { ...process.env, DISPLAY: display }, stdout: "pipe", stderr: "ignore" });
  pendingScreen = (async () => {
    const png = new Uint8Array(await new Response(p.stdout).arrayBuffer());
    if ((await p.exited) === 0 && png.length) keptScreen = png;
  })();
}

/**
 * For a scenario played on a desktop of its own: keeps that desktop's screen before it is closed, unless something
 * was kept already, so the scenario's screenshot shows its own desktop rather than the shared one.
 */
export function keepScreenIfNone(display: string) {
  if (!keptScreen) keepScreen(display);
}

/**
 * A bun:test test that, once its scenario has run (passed or not), saves the screen it ended on as
 * `AIL_SHOTS_DIR/<test name as a slug>.png`, the proof `ail adopt` collects. Use it in place of bun:test's `test`.
 */
export function test(name: string, body: () => Promise<unknown> | unknown, timeout?: number) {
  bunTest(name, async () => {
    keptScreen = null;
    pendingScreen = null;
    try {
      await body();
    } finally {
      await pendingScreen;
      const dir = process.env.AIL_SHOTS_DIR;
      if (dir) {
        const png = keptScreen ?? screenOf();
        if (png.length) writeFileSync(join(dir, shotName(name)), png);
      }
      keptScreen = null;
    }
  }, timeout);
}
