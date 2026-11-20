// ail-derived-from: OpenPicker@bce9a81188a5f8ed
// End-to-end check for OpenPicker: where the picker appears, that it leaves the
// keyboard with the app the user is in, and what happens on a screen too short
// for it. Seen only through the windows on screen and what they show.
import { afterAll, afterEach, beforeAll, expect } from "bun:test";
import { TargetApp, test, unique } from "../../desktop.ts";
import {
  OwnDesktop, cancelThePicker, copyWithXclip, focusedWindowName, holdThePickerOpen, makeImage, movePointer,
  readText, releaseAll, shootPicker, squash, waitForPicker, workArea, type Box,
} from "../picker-driver.ts";

let app: TargetApp;
beforeAll(async () => { app = await TargetApp.start(); }, 90_000);
afterAll(async () => { await releaseAll(); await app?.stop(); });
afterEach(async () => { await releaseAll(); await waitForPicker(false); });

/**
 * Where a picker of `size` belongs in the screen's working area (the screen less its panels): centred, its top 16 %
 * down, or higher so that it ends at the bottom.
 */
function placeFor(size: { width: number; height: number }) {
  const area = workArea();
  return {
    x: area.x + Math.floor(Math.max(0, (area.width - size.width) / 2)),
    y: area.y + Math.floor(Math.max(0, Math.min(area.height * 0.16, area.height - size.height))),
  };
}

/** Shows the picker twice for the same history and returns the second one, so it is the size it was the time before. */
async function openTwice(display?: string): Promise<Box> {
  await holdThePickerOpen(display);
  await cancelThePicker(display);
  return holdThePickerOpen(display);
}

test("the picker opens centred near the top of the screen", async () => {
  await app.copy(`centred ${unique()}`);
  await app.prepare();
  movePointer(200, 650);
  const box = await openTwice();
  await cancelThePicker();
  const place = placeFor(box);
  expect(Math.abs(box.x - place.x)).toBeLessThanOrEqual(1);
  const area = workArea();
  expect(Math.abs(box.y - (area.y + Math.floor(area.height * 0.16)))).toBeLessThanOrEqual(1);
}, 30_000);

test("the picker never takes the keyboard focus", async () => {
  await app.copy(`focus ${unique()}`);
  await app.prepare();
  const before = focusedWindowName();
  await holdThePickerOpen();
  expect(focusedWindowName()).toBe(before);
  await cancelThePicker();
  expect(focusedWindowName()).toBe(before);
}, 30_000);

test("a picker too tall for its usual place moves up to end at the bottom of the screen", async () => {
  await copyWithXclip(makeImage(640, 400), "image/png");
  await app.prepare();
  const box = await openTwice();
  await cancelThePicker();
  const area = workArea();
  expect(box.height).toBeGreaterThan(area.height * 0.84);
  expect(Math.abs(box.y + box.height - (area.y + area.height))).toBeLessThanOrEqual(1);
  expect(Math.abs(box.x - placeFor(box).x)).toBeLessThanOrEqual(1);
}, 30_000);

test("the picker is placed for the size it had the time before", async () => {
  await copyWithXclip(makeImage(640, 400), "image/png");
  await app.prepare();
  const tall = await openTwice();
  await cancelThePicker();
  await app.copy(`short ${unique()}`);
  await app.prepare();
  const short = await holdThePickerOpen();
  await cancelThePicker();
  expect(short.height).toBeLessThan(tall.height);
  expect(Math.abs(short.y - placeFor(tall).y)).toBeLessThanOrEqual(1);
}, 30_000);

test("on a screen too short for the picker, it is shown in turn whole without its preview and cut off with it", async () => {
  const desktop = await OwnDesktop.start({ width: 1280, height: 480, name: "short-screen" });
  try {
    const d = desktop.display;
    await copyWithXclip("first short line\nsecond short line\nthird short line", undefined, d);
    const seen: string[] = [];
    for (let i = 0; i < 4; i++) {
      const box = await holdThePickerOpen(d);
      const text = squash(readText(shootPicker(box, d)));
      await cancelThePicker(d);
      const preview = text.includes("51 characters");
      const footer = text.includes("Esc");
      if (!preview && footer && box.height < 480) seen.push("whole, without its preview");
      else if (preview && !footer && box.y + box.height >= 480) seen.push("cut off, with its preview");
      else seen.push(`neither (${box.height} high at ${box.y}, preview ${preview}, footer ${footer})`);
    }
    expect(seen).toContain("whole, without its preview");
    expect(seen).toContain("cut off, with its preview");
    expect(seen.filter((s) => s.startsWith("neither"))).toEqual([]);
  } finally {
    await desktop.stop();
  }
}, 180_000);
