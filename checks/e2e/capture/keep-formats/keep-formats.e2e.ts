// ail-derived-from: KeepFormats@feed999fc00cb138
// End-to-end check for KeepFormats: an item picked from the history puts back
// on the clipboard what was copied, format by format. Another app's view of
// the clipboard is read with xclip after the paste.
import { afterAll, afterEach, beforeAll, expect } from "bun:test";
import { mkdirSync, writeFileSync } from "node:fs";
import { join } from "node:path";
import { releaseAll, sleep, TargetApp, test, unique, waitForPicker } from "../../desktop.ts";
import { clipboardBytes, clipboardTypes, pasteFromHistory, pointerAway, samePixels, scratch, waitForNoNotification, xcopy } from "../capture-driver.ts";

let app: TargetApp;
beforeAll(async () => { app = await TargetApp.start(); pointerAway(); }, 90_000);
afterAll(async () => { await releaseAll(); await app?.stop(); });
afterEach(async () => { await releaseAll(); await waitForPicker(false); await waitForNoNotification(); });

const b64 = (s: string) => Buffer.from(s, "utf8").toString("base64");

test("rich text keeps its HTML", async () => {
  const id = unique();
  const html = `<p>Rich <b>bold ${id}</b> and <i>italic</i></p>`;
  expect(await app.send(`copy-html ${b64(`Rich bold ${id} and italic`)} ${b64(html)}`)).toBe("OK");
  await sleep(800);
  await app.copy(`plain after rich ${id}`);
  expect(await pasteFromHistory(app, ["v"])).toBe(`Rich bold ${id} and italic`);
  expect(clipboardTypes()).toContain("text/html");
  expect(clipboardBytes("text/html").toString("utf8")).toBe(html);
}, 30_000);

test("copied files are kept as files", async () => {
  const dir = join(scratch, `files-${unique()}`);
  mkdirSync(dir);
  const file = join(dir, "report.txt");
  writeFileSync(file, "a file to copy");
  await xcopy(`file://${file}\n`, "text/uri-list");
  await app.copy(`text after file ${unique()}`);
  await pasteFromHistory(app, ["v"]);
  expect(clipboardTypes()).toContain("text/uri-list");
  expect(clipboardBytes("text/uri-list").toString("utf8").trim()).toBe(`file://${file}`);
}, 30_000);

test("another app's own format is kept byte for byte", async () => {
  const bytes = new Uint8Array([0, 1, 2, 250, 255, 13, 10, ...Buffer.from(unique())]);
  await xcopy(bytes, "application/x-ail-capture-check");
  await app.copy(`text after custom ${unique()}`);
  await pasteFromHistory(app, ["v"]);
  expect(clipboardTypes()).toContain("application/x-ail-capture-check");
  expect([...clipboardBytes("application/x-ail-capture-check")]).toEqual([...bytes]);
}, 30_000);

test("an image is kept, and other apps can read it back", async () => {
  const png = join(scratch, `image-${unique()}.png`);
  Bun.spawnSync(["convert", "-size", "4x3", "xc:red", "-fill", "blue", "-draw", "point 1,1", "-fill", "lime", "-draw", "point 2,2", png]);
  await xcopy(new Uint8Array(await Bun.file(png).arrayBuffer()), "image/png");
  await app.copy(`text after image ${unique()}`);
  await pasteFromHistory(app, ["v"]);
  expect(clipboardTypes()).toContain("image/png");
  // Read twice, as an app asking for the image again would: same pixels both times.
  for (const n of [1, 2]) {
    const back = join(scratch, `back-${n}-${unique()}.png`);
    writeFileSync(back, clipboardBytes("image/png"));
    expect(samePixels(png, back)).toBe(true);
  }
  // Shapeshifter is still there: the next copy is in the history.
  const id = unique();
  await app.copy(`still running ${id}`);
  expect(await pasteFromHistory(app, [])).toBe(`still running ${id}`);
  expect(await pasteFromHistory(app, ["v"])).not.toBe(`still running ${id}`);
}, 40_000);
