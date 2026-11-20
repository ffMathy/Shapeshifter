// Helpers for the end-to-end checks of the actions, deleting, pinning and the
// remembered history. Like desktop.ts they see only what a user sees: the
// clipboard (xclip), the picker on screen (read with OCR, tesseract), files on
// disk and windows. Nothing here reads Shapeshifter's code, logs or state file.
//
// The restart checks need an app they may quit and start again, which the
// shared one is not, so PrivateDesktop starts a desktop of its own: an X server,
// openbox, a panel with a tray (to quit Shapeshifter and open its settings as a
// user does), and Shapeshifter with its own SHAPESHIFTER_HOME.
//
// Needs: xclip, xdotool, xwininfo, ImageMagick (import, convert), tesseract,
// Xvfb, openbox, dbus-daemon, xfce4-panel, the .NET runtime.
// AIL_SHAPESHIFTER_DLL overrides the app it starts (default: the Release build).
import { existsSync, mkdirSync, mkdtempSync, writeFileSync } from "node:fs";
import { homedir, tmpdir } from "node:os";
import { join, resolve } from "node:path";
import { holdVUntilThePickerAppears, keyDown, keyUp, sleep, tap } from "../desktop.ts";

const root = resolve(import.meta.dir, "../../..");
const run = (cmd: string[], env: Record<string, string | undefined> = process.env) => Bun.spawnSync(cmd, { env });
const text = (cmd: string[]) => run(cmd).stdout.toString();

// ---------- the clipboard ----------

/** The formats the clipboard offers right now. */
export const clipboardFormats = () => text(["xclip", "-selection", "clipboard", "-t", "TARGETS", "-o"]).split("\n").map((s) => s.trim()).filter(Boolean);

/** The clipboard's content in one format. */
export const clipboardAs = (format: string) => text(["xclip", "-selection", "clipboard", "-t", format, "-o"]);

/** Copies data of one format, as an app that owns the clipboard does (xclip keeps serving it), then gives Shapeshifter time to notice. */
export async function copyAs(format: string, data: string | Uint8Array) {
  const p = Bun.spawn(["xclip", "-selection", "clipboard", "-t", format, "-i"], { stdin: "pipe", stdout: "ignore", stderr: "ignore", env: process.env });
  p.stdin.write(data);
  await p.stdin.end();
  await sleep(1200);
}

/** A small solid PNG image of the given size, made with ImageMagick. */
export function makePng(width: number, height: number, colour = "red"): Uint8Array {
  const r = run(["convert", "-size", `${width}x${height}`, `xc:${colour}`, "png:-"]);
  return new Uint8Array(r.stdout);
}

/** The local paths of the files on the clipboard (text/uri-list). */
export const clipboardFiles = () => clipboardAs("text/uri-list").split(/\r?\n/).map((s) => s.trim()).filter((s) => s.startsWith("file://")).map((s) => decodeURIComponent(new URL(s).pathname));

// ---------- reading the picker ----------

function pickerWindow(): string | undefined {
  const ids = text(["xdotool", "search", "--onlyvisible", "--name", "^Shapeshifter$"]).split("\n").filter(Boolean);
  return ids.find((id) => Number(/Geometry:\s*\d+x(\d+)/.exec(text(["xdotool", "getwindowgeometry", id]))?.[1]) >= 200);
}

/** OCR of a part of the picker: "actions" is the right-hand column, "history" the left one, "all" the whole picker. */
export function readPicker(part: "actions" | "history" | "all" = "all"): string {
  const id = pickerWindow();
  if (!id) throw new Error("the picker is not on screen");
  const dir = mkdtempSync(join(tmpdir(), "ail-actions-ocr-"));
  const shot = join(dir, "picker.png");
  run(["import", "-window", id, shot]);
  const [w, h] = text(["identify", "-format", "%w %h", shot]).split(" ").map(Number);
  // The actions column is 284 px wide plus the margins at the right; the history takes the rest.
  const crop = part === "actions" ? `310x${h}+${w - 310}+0` : part === "history" ? `${w - 310}x${h}+0+0` : `${w}x${h}+0+0`;
  const big = join(dir, "big.png");
  run(["convert", shot, "-crop", crop, "+repage", "-resize", "200%", "-colorspace", "Gray", big]);
  return text(["tesseract", big, "-", "--psm", "6"]);
}

/** Every action title, in the order they are shown, as read from the screen. */
export const ACTION_TITLES = ["Paste", "Paste as file", "Paste as plain text", "Upload and paste link", "Copy as compressed folder",
  "Copy image from link", "Open link", "Open links", "Pin to clipboard", "Unpin from clipboard"];

export function actionTitlesOnScreen(): string[] {
  return readPicker("actions").split("\n").map((l) => l.replace(/[^\w ().]/g, "").replace(/\s+/g, " ").trim()).filter((l) => ACTION_TITLES.includes(l));
}

/** The "N items" count in the picker's header. */
export function itemCountOnScreen(): number {
  const m = /(\d+)\s+items?\b/.exec(readPicker("history"));
  if (!m) throw new Error("no item count on screen");
  return Number(m[1]);
}

// ---------- gestures ----------

/** Holds Ctrl+V until the picker is up, then moves `down` items down; Ctrl stays held. */
export async function openPicker(down = 0) {
  await keyDown("ctrl");
  await holdVUntilThePickerAppears();
  for (let i = 0; i < down; i++) await tap("Down");
  await sleep(250);
}

/** With the picker up: Right to the actions, `down` actions down, then lets go of Ctrl. */
export async function chooseAction(down: number) {
  await tap("Right");
  for (let i = 0; i < down; i++) await tap("Down");
  await sleep(200);
  await keyUp("ctrl");
  await sleep(1200);
}

// ---------- a desktop of its own, for restarts ----------

function freeDisplay(): number {
  for (let n = 171; n < 199; n++) {
    if (!existsSync(`/tmp/.X11-unix/X${n}`) && !existsSync(`/tmp/.X${n}-lock`)) return n;
  }
  throw new Error("no free X display between :171 and :198");
}

export class PrivateDesktop {
  readonly display: string;
  /** Where this Shapeshifter keeps everything (its SHAPESHIFTER_HOME); data/ holds what it saves. */
  readonly home: string;
  private dir: string;
  private procs: Bun.Subprocess[] = [];
  private app: Bun.Subprocess | undefined;
  private env: Record<string, string>;

  private constructor(display: number, dir: string) {
    this.display = `:${display}`;
    this.dir = dir;
    this.home = join(dir, "home");
    mkdirSync(this.home, { recursive: true });
    this.env = { ...(process.env as Record<string, string>), DISPLAY: this.display };
  }

  static async start(): Promise<PrivateDesktop> {
    const desk = new PrivateDesktop(freeDisplay(), mkdtempSync(join(tmpdir(), "ail-actions-")));
    const spawn = (cmd: string[], env = desk.env) => { const p = Bun.spawn(cmd, { env, stdout: "ignore", stderr: "ignore" }); desk.procs.push(p); return p; };
    spawn(["Xvfb", desk.display, "-screen", "0", "1280x800x24", "-nolisten", "tcp"]);
    for (let i = 0; i < 50 && run(["xdpyinfo"], desk.env).exitCode !== 0; i++) await sleep(100);
    spawn(["openbox"]);
    await sleep(800);
    // A session bus and a panel with a tray: Avalonia shows its tray icon over D-Bus (StatusNotifierItem).
    const tray = join(desk.dir, "tray");
    const conf = join(tray, "config/xfce4/xfconf/xfce-perchannel-xml");
    mkdirSync(conf, { recursive: true });
    mkdirSync(join(tray, "runtime"), { recursive: true, mode: 0o700 });
    writeFileSync(join(conf, "xfce4-panel.xml"), PANEL);
    const bus = `unix:path=${join(tray, "runtime/bus")}`;
    const trayEnv = { ...desk.env, HOME: tray, XDG_CONFIG_HOME: join(tray, "config"), XDG_DATA_HOME: join(tray, "data"), XDG_CACHE_HOME: join(tray, "cache"), XDG_RUNTIME_DIR: join(tray, "runtime") };
    spawn(["dbus-daemon", "--session", "--nofork", `--address=${bus}`], trayEnv);
    for (let i = 0; i < 50 && !existsSync(join(tray, "runtime/bus")); i++) await sleep(100);
    desk.env.DBUS_SESSION_BUS_ADDRESS = bus;
    trayEnv.DBUS_SESSION_BUS_ADDRESS = bus;
    spawn(["xfce4-panel", "--disable-wm-check", `--sm-client-id=ail-actions-${desk.display.slice(1)}`], trayEnv);
    await sleep(2500);
    return desk;
  }

  /** Starts Shapeshifter and waits until it is ready, as `run.legacy` does. */
  async startApp() {
    const dll = resolve(root, process.env.AIL_SHAPESHIFTER_DLL ?? "src/Shapeshifter.Desktop/bin/Release/net11.0/Shapeshifter.dll");
    const local = join(homedir(), ".dotnet", "dotnet");
    const env = { ...this.env, SHAPESHIFTER_HOME: this.home, SHAPESHIFTER_NO_UPDATE: "1", SHAPESHIFTER_NO_INSTALL: "1", SHAPESHIFTER_LOOK: "linux" };
    const app = Bun.spawn([existsSync(local) ? local : "dotnet", dll, "--no-install"], { env, cwd: root, stdout: "pipe", stderr: "ignore" });
    this.app = app;
    const reader = app.stdout.getReader();
    const decoder = new TextDecoder();
    let out = "";
    const until = Date.now() + 90_000;
    while (!out.includes("SHAPESHIFTER_READY")) {
      if (Date.now() > until) throw new Error("Shapeshifter did not get ready");
      const { value, done } = await reader.read();
      if (done) throw new Error("Shapeshifter exited while starting");
      out += decoder.decode(value);
    }
    reader.releaseLock();
    app.stdout.cancel().catch(() => {});
    await sleep(1500);
  }

  private trayIcon(): [number, number] { return [1280 - 17, 800 - 16]; }

  /** Quits through the tray menu's last item, "Quit Shapeshifter", and waits for the app to be gone. */
  async quitApp() {
    const [x, y] = this.trayIcon();
    run(["xdotool", "mousemove", String(x), String(y), "click", "3"], this.env);
    await sleep(1200);
    run(["xdotool", "key", "Up"], this.env);
    await sleep(300);
    run(["xdotool", "key", "Return"], this.env);
    const exited = await Promise.race([this.app!.exited.then(() => true), sleep(15_000).then(() => false)]);
    if (!exited) throw new Error("Shapeshifter did not quit from its tray menu");
    this.app = undefined;
  }

  /** Opens the settings with a click on the tray icon and flips the "Remember history after restart" switch. */
  async toggleRememberHistory() {
    const [x, y] = this.trayIcon();
    run(["xdotool", "mousemove", String(x), String(y), "click", "1"], this.env);
    let id = "";
    for (let i = 0; i < 50 && !id; i++) { await sleep(100); id = run(["xdotool", "search", "--onlyvisible", "--name", "^Shapeshifter Settings$"], this.env).stdout.toString().split("\n")[0] ?? ""; }
    if (!id) throw new Error("the settings window did not open");
    await sleep(800);
    const info = run(["xwininfo", "-id", id], this.env).stdout.toString();
    const left = Number(/Absolute upper-left X:\s*(-?\d+)/.exec(info)![1]);
    const top = Number(/Absolute upper-left Y:\s*(-?\d+)/.exec(info)![1]);
    const width = Number(/Width:\s*(\d+)/.exec(info)![1]);
    // Find the row by its title on screen; its switch sits at the right end of the row.
    const shot = join(this.dir, "settings.png");
    run(["import", "-window", id, shot], this.env);
    const tsv = text(["tesseract", shot, "-", "tsv"]);
    const word = tsv.split("\n").map((l) => l.split("\t")).find((c) => c[11] === "Remember");
    if (!word) throw new Error('no "Remember history after restart" in the settings');
    const rowY = top + Number(word[7]) + Number(word[9]) / 2 + 10;
    run(["xdotool", "mousemove", String(left + width - 66), String(Math.round(rowY)), "click", "1"], this.env);
    await sleep(1500);
    run(["xdotool", "windowactivate", "--sync", id, "key", "alt+F4"], this.env);
    await sleep(800);
  }

  /** Points the shared helpers (desktop.ts, TargetApp, OCR) at this desktop; returns a function that points them back. */
  use(): () => void {
    const before = process.env.DISPLAY;
    process.env.DISPLAY = this.display;
    return () => { process.env.DISPLAY = before; };
  }

  async stop() {
    this.app?.kill();
    for (const p of this.procs.reverse()) p.kill();
    await sleep(300);
  }
}

const PANEL = `<?xml version="1.0" encoding="UTF-8"?>
<channel name="xfce4-panel" version="1.0">
  <property name="configver" type="int" value="2"/>
  <property name="panels" type="array">
    <value type="int" value="1"/>
    <property name="panel-1" type="empty">
      <property name="position" type="string" value="p=8;x=0;y=0"/>
      <property name="length" type="uint" value="100"/>
      <property name="position-locked" type="bool" value="true"/>
      <property name="size" type="uint" value="32"/>
      <property name="icon-size" type="uint" value="24"/>
      <property name="plugin-ids" type="array"><value type="int" value="1"/><value type="int" value="2"/></property>
    </property>
  </property>
  <property name="plugins" type="empty">
    <property name="plugin-1" type="string" value="separator"><property name="expand" type="bool" value="true"/><property name="style" type="uint" value="0"/></property>
    <property name="plugin-2" type="string" value="systray"><property name="square-icons" type="bool" value="true"/></property>
  </property>
</channel>
`;

export { keyDown, keyUp, sleep, tap };
