// ail-derived-from: PasteActions@6ff4aa2f0ced6d8d
// End-to-end check for PasteActions: Paste, Paste as plain text and Paste as
// file, seen through what arrives in another app, the clipboard (xclip) and
// the files the user is handed.
import { afterAll, afterEach, beforeAll, expect } from "bun:test";
import { keyUp, releaseAll, sleep, tap, TargetApp, test, unique, waitForPicker } from "../../desktop.ts";
import { actionTitlesOnScreen, chooseAction, clipboardAs, clipboardFiles, clipboardFormats, copyAs, makePng, openPicker } from "../actions-driver.ts";

let app: TargetApp;
beforeAll(async () => { app = await TargetApp.start(); }, 90_000);
afterAll(async () => { await releaseAll(); await app?.stop(); });
afterEach(async () => { await releaseAll(); await waitForPicker(false); });

const b64 = (s: string) => Buffer.from(s).toString("base64");
const copyHtml = async (text: string, html: string) => { expect(await app.send(`copy-html ${b64(text)} ${b64(html)}`)).toBe("OK"); await sleep(1200); };

test("Paste puts back every format", async () => {
  const id = unique();
  await copyHtml(`rich ${id}`, `<b>rich</b> ${id}`);
  await app.copy(`plain ${id}`);
  await app.prepare();
  await openPicker(1);
  await chooseAction(0);
  expect(await app.waitForText(`rich ${id}`)).toBe(`rich ${id}`);
  expect(clipboardFormats()).toContain("text/html");
  expect(clipboardAs("text/html")).toContain(`<b>rich</b> ${id}`);
}, 30_000);

test("Paste as plain text leaves the formatting out", async () => {
  const id = unique();
  await copyHtml(`styled ${id}`, `<i>styled</i> ${id}`);
  await app.prepare();
  await openPicker();
  await chooseAction(2);
  expect(await app.waitForText(`styled ${id}`)).toBe(`styled ${id}`);
  expect(clipboardFormats()).not.toContain("text/html");
  expect(clipboardAs("UTF8_STRING")).toBe(`styled ${id}`);
}, 30_000);

test("Paste as file turns text into a .txt file", async () => {
  const id = unique();
  await app.copy(`notes ${id}\nsecond line`);
  await app.prepare();
  await openPicker();
  await chooseAction(1);
  expect(clipboardFormats()).toContain("text/uri-list");
  const files = clipboardFiles();
  expect(files).toHaveLength(1);
  expect(files[0]).toMatch(/\/Text \d{4}-\d\d-\d\d \d\d\.\d\d\.\d\d\.txt$/);
  expect(await Bun.file(files[0]).text()).toBe(`notes ${id}\nsecond line`);
  expect(await app.text()).toBe("");
}, 30_000);

test("Paste as file turns an image into an image file", async () => {
  const width = 41 + Math.floor(Math.random() * 50);
  await copyAs("image/png", makePng(width, 17, "blue"));
  await app.prepare();
  await openPicker();
  // An image also offers "Upload and paste link", which would send it to a public host: never land on it.
  // Find "Paste as file" on screen first, and cancel unless it is exactly where it should be.
  await tap("Right");
  await sleep(300);
  const titles = actionTitlesOnScreen();
  if (titles.indexOf("Paste as file") !== 1 || titles.indexOf("Upload and paste link") === 1) {
    await tap("Escape");
    await keyUp("ctrl");
    throw new Error(`unexpected actions for an image: ${titles.join(", ")}`);
  }
  await tap("Down");
  await sleep(200);
  await keyUp("ctrl");
  await sleep(1200);
  const files = clipboardFiles();
  expect(files).toHaveLength(1);
  expect(files[0]).toMatch(/\/Image \d{4}-\d\d-\d\d \d\d\.\d\d\.\d\d\.png$/);
  const size = Bun.spawnSync(["identify", "-format", "%w %h", files[0]]).stdout.toString();
  expect(size).toBe(`${width} 17`);
}, 30_000);
