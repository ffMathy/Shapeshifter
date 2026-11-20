// ail-derived-from: OfferedActions@dfc8b4b4c9f75be5
// End-to-end check for OfferedActions: which actions each kind of item offers,
// in which order, as read from the screen (OCR of the picker's actions column).
// Content is put on the clipboard with xclip, as another app would.
import { afterAll, afterEach, beforeAll, expect } from "bun:test";
import { keyUp, releaseAll, sleep, tap, TargetApp, test, unique, waitForPicker } from "../../desktop.ts";
import { actionTitlesOnScreen, copyAs, makePng, openPicker } from "../actions-driver.ts";
import { mkdtempSync, writeFileSync } from "node:fs";
import { tmpdir } from "node:os";
import { join } from "node:path";

let app: TargetApp;
beforeAll(async () => { app = await TargetApp.start(); }, 90_000);
afterAll(async () => { await releaseAll(); await app?.stop(); });
afterEach(async () => { await releaseAll(); await waitForPicker(false); });

/** Opens the actions of the newest item, reads them, and cancels. */
async function actionsOfNewest(): Promise<string[]> {
  await openPicker();
  await tap("Right");
  await sleep(300);
  const titles = actionTitlesOnScreen();
  await tap("Escape");
  await keyUp("ctrl");
  return titles;
}

const b64 = (s: string) => Buffer.from(s).toString("base64");
const files = (...names: string[]) => {
  const dir = mkdtempSync(join(tmpdir(), "ail-offered-"));
  return names.map((n) => { const p = join(dir, n); writeFileSync(p, n); return `file://${p}`; }).join("\n") + "\n";
};

test("plain text offers Paste, Paste as file and Pin", async () => {
  await app.copy(`plain ${unique()}`);
  expect(await actionsOfNewest()).toEqual(["Paste", "Paste as file", "Pin to clipboard"]);
}, 30_000);

test("formatted text also offers Paste as plain text", async () => {
  const id = unique();
  expect(await app.send(`copy-html ${b64(`bold ${id}`)} ${b64(`<b>bold</b> ${id}`)}`)).toBe("OK");
  await sleep(1200);
  expect(await actionsOfNewest()).toEqual(["Paste", "Paste as file", "Paste as plain text", "Pin to clipboard"]);
}, 30_000);

test("an image offers Paste, Paste as file, Upload and paste link, and Pin", async () => {
  await copyAs("image/png", makePng(31 + Math.floor(Math.random() * 40), 23));
  expect(await actionsOfNewest()).toEqual(["Paste", "Paste as file", "Upload and paste link", "Pin to clipboard"]);
}, 30_000);

test("a file offers Paste, Copy as compressed folder and Pin", async () => {
  await copyAs("text/uri-list", files(`one-${unique()}.txt`));
  expect(await actionsOfNewest()).toEqual(["Paste", "Copy as compressed folder", "Pin to clipboard"]);
}, 30_000);

test("several files offer the same as one file", async () => {
  await copyAs("text/uri-list", files(`a-${unique()}.txt`, `b-${unique()}.txt`));
  expect(await actionsOfNewest()).toEqual(["Paste", "Copy as compressed folder", "Pin to clipboard"]);
}, 30_000);

test("other data offers only Paste and Pin", async () => {
  await copyAs(`application/x-ail-${unique()}`, `data ${unique()}`);
  expect(await actionsOfNewest()).toEqual(["Paste", "Pin to clipboard"]);
}, 30_000);

test("text with links also offers the link actions", async () => {
  const id = unique();
  await copyAs("UTF8_STRING", `see https://example.com/${id}.png and https://example.org/${id}`);
  expect(await actionsOfNewest()).toEqual(["Paste", "Paste as file", "Copy image from link", "Open links", "Pin to clipboard"]);
}, 30_000);
